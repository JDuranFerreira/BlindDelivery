using System;
using System.Threading;
using System.Threading.Tasks;
using BlindDelivery.Core.Boot;
using BlindDelivery.Core.Events;
using BlindDelivery.Core.Input;
using BlindDelivery.Core.Localization;
using BlindDelivery.Core.Pooling;
using BlindDelivery.Core.Save;
using BlindDelivery.Core.Scenes;
using BlindDelivery.Core.Analytics;
using BlindDelivery.Core.Consent;
using BlindDelivery.Core.RemoteConfig;
using BlindDelivery.Core.Services;
using BlindDelivery.Core.Settings;
using BlindDelivery.Services.Audio;
using BlindDelivery.Services.Events;
using BlindDelivery.Services.Input;
using BlindDelivery.Services.Localization;
using BlindDelivery.Services.Pooling;
using BlindDelivery.Services.Save;
using BlindDelivery.Services.Scenes;
using BlindDelivery.Services.Settings;
using BlindDelivery.Services.UI;
using UnityEngine;

namespace BlindDelivery.Gameplay
{
    /// <summary>
    /// The composition root (architecture note, Decisions table: "Bootstrap constructs, everything
    /// else receives"). It owns the registry and the bus, constructs every service, and runs the
    /// boot chain. Nothing else in the BlindDelivery news up a service.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class AppBootstrap : MonoBehaviour
    {
        /// <summary>First real scene loaded once boot succeeds. Change it per game.</summary>
        public const string DefaultFirstScene = "MainMenu";

        [SerializeField] string firstSceneName = DefaultFirstScene;

        /// <summary>Keys-to-clips for the audio host. Null is legal: audio then logs misses and plays nothing.</summary>
        [SerializeField] AudioLibrary audioLibrary;

        public static AppBootstrap Instance { get; private set; }

        public ServiceRegistry Services { get; private set; }

        public IEventBus Bus { get; private set; }

        public BootResult Result { get; private set; }

        public bool IsBooted { get; private set; }

        /// <summary>
        /// Ensures services exist when Play is pressed from any scene, not only Bootstrap. Runs
        /// before the first scene is loaded, so the scene's own objects wake up with the bus and
        /// registry already in place.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureBootstrapExists()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject host = new GameObject(nameof(AppBootstrap));
            DontDestroyOnLoad(host);
            host.AddComponent<AppBootstrap>();
        }

        void Awake()
        {
            // A scene that ships its own AppBootstrap loses to the one created above; the survivor
            // must be the single composition root or two registries would fight over the services.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Bus = new EventBus();
            Services = new ServiceRegistry();
            RegisterServices(Services);
        }

        void Start()
        {
            // Fire and forget by design: Awake/Start cannot await, and a failed boot is reported on
            // the bus (AppBootFailed) and through Result rather than thrown into the player loop.
            _ = BootAsync();
        }

        public async Task<BootResult> BootAsync(CancellationToken token = default)
        {
            if (IsBooted)
            {
                return Result;
            }

            AppBoot boot = new AppBoot(Services, Bus);
            Result = await boot.RunAsync(token);
            IsBooted = Result.Succeeded;

            if (!Result.Succeeded)
            {
                if (Result.WasCancelled)
                {
                    Debug.LogWarning("[AppBootstrap] Boot cancelled.", this);
                }
                else
                {
                    Debug.LogError(
                        $"[AppBootstrap] Boot failed in '{Result.FailedService}': {Result.Error?.Message}",
                        this);
                }

                return Result;
            }

            ISceneLoader scenes = Services.Get<ISceneLoader>();
            await scenes.LoadAsync(firstSceneName, token);
            return Result;
        }

        /// <summary>
        /// Mobile durability: pause and quit are where Unity warns that the process may vanish, so
        /// pending save state is flushed here (architecture note: save on OnApplicationPause /
        /// OnApplicationQuitting). The game itself saves on its own gameplay events; this hook only
        /// guarantees that whatever is queued up becomes durable before the process dies.
        /// </summary>
        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                FlushSave();
            }
        }

        void OnApplicationQuit()
        {
            FlushSave();
        }

        void FlushSave()
        {
            if (Services == null || !Services.TryGet<ISaveService>(out ISaveService save))
            {
                return;
            }

            // Writes are synchronous today, so the flush is already durable when SaveAsync
            // returned; the fire-and-forget keeps that contract for a future queueing store.
            _ = save.FlushAsync();
        }

        /// <summary>
        /// M3 chain: save, settings, audio, UI, scene loader, input, pooling.
        /// Save first (everyone loads defaults). UI manager before loader (hands overlay component).
        /// Input/pooling after the core visual/audio stack - they depend on no one, and no one depends on them yet.
        /// </summary>
        void RegisterServices(ServiceRegistry registry)
        {
            registry.Register(new SaveService(new FileSaveStore(), Bus));

            registry.Register(new SettingsService(Bus, new PlayerPrefsStore()));

            AudioHost audio = gameObject.AddComponent<AudioHost>();
            audio.Library = audioLibrary;
            audio.Construct(Bus, Services.Get<ISettingsService>());
            registry.Register(audio);

            UIManager ui = new UIManager();
            registry.Register(ui);

            GameObject overlayHost = new GameObject("SceneTransitionOverlay");
            DontDestroyOnLoad(overlayHost);
            SceneFadeOverlay overlay = overlayHost.AddComponent<SceneFadeOverlay>();

            registry.Register(new TransitionSceneLoader(Bus, overlay, UnloadUnusedAssetsAsync));

            // M4: Input (needs InputActionAsset assigned in the scene or via Resources)
            InputService input = gameObject.AddComponent<InputService>();
            registry.Register(input);

            // M4: Pooling (needs PoolFactory on the same GameObject)
            PoolManager pooling = gameObject.AddComponent<PoolManager>();
            registry.Register(pooling);

            // M5: Localization
            LocalizationService localization = gameObject.AddComponent<LocalizationService>();
            registry.Register(localization);

            // M6: consent before analytics (GDPR/ATT - nothing analytics-shaped initializes until
            // the decision is Granted) and remote config between them, per the fixed init order
            // save - settings - audio - localization - analytics.
            BlindDelivery.Services.Consent.ConsentService consent =
                new BlindDelivery.Services.Consent.ConsentService(new PlayerPrefsStore(), Bus);
            registry.Register(consent);

            // Defaults are the game's choice (kill switches, ad frequency, force-update flag);
            // the BlindDelivery ships an empty document and a null source - no remote package yet.
            registry.Register(new BlindDelivery.Services.RemoteConfig.RemoteConfigService(
                new RemoteConfigDocument { version = "0" },
                null,
                new PlayerPrefsStore()));

            // The sink is the game's concrete provider (Firebase Analytics, ...); null keeps the
            // service a dropper-only no-op in the BlindDelivery, at zero package dependency.
            registry.Register(new BlindDelivery.Services.Analytics.AnalyticsService(consent, null));

            // M7: ads, purchases, game services. The providers here are the BlindDelivery stubs - no
            // SDK package, no fill, no sign-in. A game swaps in LevelPlay/AdMob, Unity IAP and
            // Play Games/Game Center by passing its own implementations at this one seam.
            registry.Register(new BlindDelivery.Services.Ads.AdService(
                new BlindDelivery.Services.Ads.StubAdProvider(),
                new BlindDelivery.Core.Ads.AdPolicy(),
                Bus,
                Services.Get<ISaveService>(),
                Services.Get<BlindDelivery.Core.Audio.IAudioService>()));

            // Catalog ids are the game asset (assign on the AppBootstrap prefab); until one is
            // authored the store answers UnknownProduct and the BlindDelivery stays honest.
            registry.Register(new BlindDelivery.Services.Purchases.PurchaseService(
                new BlindDelivery.Services.Purchases.StubStoreSink(),
                ScriptableObject.CreateInstance<BlindDelivery.Services.Purchases.ScriptablePurchaseCatalog>(),
                Services.Get<ISaveService>()));

            // No provider: the social platform stays optional - the game is fully playable and
            // offline reports queue until a game wires Play Games / Game Center here.
            registry.Register(new BlindDelivery.Services.GameServices.GameServicesService(
                null,
                new BlindDelivery.Core.GameServices.PlatformIdMap()));

            // M8: notifications. The provider here is the BlindDelivery stub - no SDK package, no OS
            // prompt by itself. A game swaps in com.unity.mobile.notifications by passing its own
            // provider at this seam, and asks for permission only after its own benefit screen.
            registry.Register(new BlindDelivery.Services.Notifications.NotificationService(
                new BlindDelivery.Services.Notifications.StubNotificationProvider(),
                Bus,
                Services.Get<ISaveService>()));
        }

        /// <summary>
        /// The loader's after-transition hook (M3 plan: UnloadUnusedAssets after each covered
        /// load, so twenty transitions do not creep memory with assets the old scene dropped).
        /// Driven by Awaitable instead of a blocking read of isDone.
        /// </summary>
        static async Task UnloadUnusedAssetsAsync()
        {
            AsyncOperation unload = Resources.UnloadUnusedAssets();
            while (!unload.isDone)
            {
                await Awaitable.NextFrameAsync();
            }
        }
    }
}


