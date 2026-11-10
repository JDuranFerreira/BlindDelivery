using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Template.Core.Ads;
using Template.Core.Audio;
using Template.Core.Analytics;
using Template.Core.Consent;
using Template.Core.Events;
using Template.Core.GameServices;
using Template.Core.Input;
using Template.Core.Localization;
using Template.Core.Notifications;
using Template.Core.OptIn;
using Template.Core.Pooling;
using Template.Core.Purchases;
using Template.Core.RemoteConfig;
using Template.Core.Save;
using Template.Core.Scenes;
using Template.Core.Settings;
using Template.Core.UI;
using Template.Gameplay;
using Template.Services.Input;
using Template.Services.Pooling;
using Template.Services.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Template.Examples
{
    /// <summary>
    /// The SystemsDemo scene's driver: one button per template system, every call against the
    /// service seam the game would use. Reads AppBootstrap.Instance for the registry and the bus
    /// â€” the demo news up nothing of the template's, it only receives, exactly like gameplay
    /// should (architecture note: "Bootstrap constructs, everything else receives").
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class SystemsDemoRunner : MonoBehaviour
    {
        const string DemoSaveKey = "demo_progress";
        const string PoolKey = "DemoCube";

        [SerializeField] Text statusLabel;
        [SerializeField] Text fpsLabel;
        [SerializeField] GameObject pooledPrefab;
        [SerializeField] DemoScreen overlayScreen;
        [SerializeField] DemoPopupHost popupHost;

        readonly Queue<string> statusLines = new Queue<string>();
        readonly List<PooledCube> liveCubes = new List<PooledCube>();

        CurrencyWallet wallet;
        FpsMonitor fps;
        IObjectPool<PooledCube> cubePool;
        float fpsTimer;
        int overlayVisits;
        bool migrationRegistered;

        // --- reserved subscriptions: unsubscribe is OnDisable's job, not optional -------------------
        Action<SaveWritten> onSaveWritten;
        Action<SceneTransitionStarted> onSceneStarted;
        Action<SceneTransitionCompleted> onSceneCompleted;
        Action<ServiceInitialized> onServiceInitialized;
        Action<AppBootStarted> onBootStarted;
        Action<AppBootFailed> onBootFailed;
        Action<AppBootCompleted> onBootCompleted;
        Action<NotificationScheduled> onNotificationScheduled;
        Action<ConsentChanged> onConsentChanged;
        Action<SettingsChanged> onSettingsChanged;
        Action<string> onLocaleChanged;
        Action<Template.Core.Input.Vector2> onTap, onDoubleTap, onHold, onSwipe, onDrag;
        Action<float> onPinch;

        void Awake()
        {
            if (!Application.isPlaying)
            {
                return; // services exist only under Play: demo keeps its edit-time surface inert
            }

            RegisterPoolFactory();
            wallet = new CurrencyWallet();
            wallet.Changed += change => Log($"[wallet] {change.currency} {(change.spent ? "spent" : "granted")} {change.amount}, balance {change.balanceAfter}");
            fps = new FpsMonitor(90);
            HookButtons();

            if (statusLabel != null)
            {
                statusLabel.text = "Press Play: AppBootstrap boots every service at BeforeSceneLoad.\nButtons call the same seams your game calls.";
            }
        }

        void Start()
        {
            if (!Application.isPlaying || AppBootstrap.Instance == null)
            {
                return;
            }

            SubscribeOnEnable();
            SubscribeInput();
            SubscribeLocalization();

            // Runnable once boot already put the save migration chain in place; registering the
            // same step twice throws, so the demo-careful path is: register before the first load.
            if (AppBootstrap.Instance.IsBooted)
            {
                TryRegisterMigration();
            }
            else
            {
                onBootCompleted = _ => TryRegisterMigration();
                AppBootstrap.Instance.Bus.Subscribe(onBootCompleted);
            }
        }

        void SubscribeOnEnable()
        {
            IEventBus bus = AppBootstrap.Instance != null ? AppBootstrap.Instance.Bus : null;
            if (bus == null)
            {
                return;
            }

            onSaveWritten = e => Log($"[save] '{e.Key}' written at schema v{e.Version}");
            onSceneStarted = e => Log($"[scene] transition started â†’ {e.SceneName}");
            onSceneCompleted = e => Log($"[scene] arrived '{e.SceneName}' in {e.ElapsedSeconds:0.00}s (fade out + load + fade in)");
            onServiceInitialized = e => Log($"[boot] {e.ServiceName} ready (#{e.Order})");
            onBootStarted = e => Log($"[boot] AppBootStarted with {e.ServiceCount} services");
            onBootFailed = e => Log($"[boot] FAILED at {e.ServiceName}: {e.Reason}");
            onNotificationScheduled = e => Log($"[notification] scheduled '{e.id}' for {e.fireAtUtc:HH:mm:ss} UTC");
            onConsentChanged = e => Log("[consent] decision changed â€” analytics/ads re-read it before their next move");
            onSettingsChanged = e => Log($"[settings] {e.Key} changed (AudioHost and friends apply what they own)");

            bus.Subscribe(onSaveWritten);
            bus.Subscribe(onSceneStarted);
            bus.Subscribe(onSceneCompleted);
            bus.Subscribe(onServiceInitialized);
            bus.Subscribe(onBootStarted);
            bus.Subscribe(onBootFailed);
            bus.Subscribe(onNotificationScheduled);
            bus.Subscribe(onConsentChanged);
            bus.Subscribe(onSettingsChanged);
        }

        void SubscribeInput()
        {
            if (!AppBootstrap.Instance.Services.TryGet(out IInputService input))
            {
                return;
            }

            // One clean layer over touch: gameplay subscribes, never polls, and the game code
            // here would be identical on device â€” Input System maps touch and mouse both.
            onTap = pos => Log($"[input] tap at {pos}");
            onDoubleTap = pos => Log($"[input] double tap at {pos}");
            onHold = pos => Log($"[input] hold at {pos}");
            onSwipe = dir => Log($"[input] swipe {dir}");
            onDrag = delta => Log($"[input] drag delta {delta}");
            onPinch = scale => Log($"[input] pinch {scale:0.00})");

            input.OnTap += onTap;
            input.OnDoubleTap += onDoubleTap;
            input.OnHold += onHold;
            input.OnSwipe += onSwipe;
            input.OnDrag += onDrag;
            input.OnPinch += onPinch;

            if (AppBootstrap.Instance.GetComponent<InputService>() is InputService concrete && concrete.inputActions == null)
            {
                // Not a failure of the seam: the template ships no InputActionAsset on purpose â€”
                // your game authors one (Tap, Hold, Swipe, Drag, Pinch, PointerPosition,
                // PointerPress actions) and assigns it in the composition root.
                Log("[input] no InputActionAsset assigned yet â€” the demo shows the subscription shape; gestures stay quiet until one is wired");
            }
        }

        void SubscribeLocalization()
        {
            if (!AppBootstrap.Instance.Services.TryGet(out ILocalizationService localization))
            {
                return;
            }

            onLocaleChanged = code => Log($"[loc] locale â†’ {code}");
            localization.OnLocaleChanged += onLocaleChanged;
            Log($"[loc] current locale {localization.CurrentLocale}, tables live in the game (the template ships the seam)");
        }

        void OnDisable()
        {
            // Unsubscribe obligations, not decoration: these handlers outlive this scene on the
            // persistent bus, and an unremoved one is a leak that fires into a destroyed view.
            if (AppBootstrap.Instance == null)
            {
                return;
            }

            IEventBus bus = AppBootstrap.Instance.Bus;
            if (bus == null)
            {
                return;
            }

            Unsubscribe(onSaveWritten, onSceneStarted, onSceneCompleted, onServiceInitialized, onBootCompleted,
                onBootStarted, onBootFailed, onNotificationScheduled, onConsentChanged, onSettingsChanged);

            if (AppBootstrap.Instance.Services.TryGet(out IInputService input))
            {
                input.OnTap -= onTap;
                input.OnDoubleTap -= onDoubleTap;
                input.OnHold -= onHold;
                input.OnSwipe -= onSwipe;
                input.OnDrag -= onDrag;
                input.OnPinch -= onPinch;
            }

            if (AppBootstrap.Instance.Services.TryGet(out ILocalizationService localization))
            {
                localization.OnLocaleChanged -= onLocaleChanged;
            }
        }

        void Unsubscribe(
            Action<SaveWritten> saveWritten,
            Action<SceneTransitionStarted> sceneStarted,
            Action<SceneTransitionCompleted> sceneCompleted,
            Action<ServiceInitialized> serviceInitialized,
            Action<AppBootCompleted> bootCompleted,
            Action<AppBootStarted> bootStarted,
            Action<AppBootFailed> bootFailed,
            Action<NotificationScheduled> notificationScheduled,
            Action<ConsentChanged> consentChanged,
            Action<SettingsChanged> settingsChanged)
        {
            IEventBus bus = AppBootstrap.Instance.Bus;
            if (saveWritten != null) bus.Unsubscribe(saveWritten);
            if (sceneStarted != null) bus.Unsubscribe(sceneStarted);
            if (sceneCompleted != null) bus.Unsubscribe(sceneCompleted);
            if (serviceInitialized != null) bus.Unsubscribe(serviceInitialized);
            if (bootCompleted != null) bus.Unsubscribe(bootCompleted);
            if (bootStarted != null) bus.Unsubscribe(bootStarted);
            if (bootFailed != null) bus.Unsubscribe(bootFailed);
            if (notificationScheduled != null) bus.Unsubscribe(notificationScheduled);
            if (consentChanged != null) bus.Unsubscribe(consentChanged);
            if (settingsChanged != null) bus.Unsubscribe(settingsChanged);
        }

        void Update()
        {
            if (fps != null)
            {
                fps.AddFrame(Time.unscaledDeltaTime);
            }

            fpsTimer += Time.unscaledDeltaTime;
            if (fpsLabel != null && fpsTimer >= 0.5f)
            {
                fpsLabel.text = "fps " + fps.AverageFps.ToString("0") + " (worst " + fps.WorstFps.ToString("0") + ")";
                fpsTimer = 0f;
            }
        }

        // =====================================================================================
        // One button per system - each handler is a working example of its seam
        // =====================================================================================

        void HookButtons()
        {
            Hook("btn_LoadMenu", OnLoadMenu);
            Hook("btn_Save", OnSave);
            Hook("btn_Load", OnLoad);
            Hook("btn_Settings", OnSettings);
            Hook("btn_Audio", OnAudio);
            Hook("btn_Localize", OnLocalize);
            Hook("btn_PoolSpawn", OnPoolSpawn);
            Hook("btn_PoolRelease", OnPoolRelease);
            Hook("btn_UiPush", OnUiPush);
            Hook("btn_UiBack", OnUiBack);
            Hook("btn_UiPopup", OnUiPopup);
            Hook("btn_Consent", OnConsent);
            Hook("btn_Analytics", OnAnalytics);
            Hook("btn_RemoteConfig", OnRemoteConfig);
            Hook("btn_AdsRewarded", OnAdsRewarded);
            Hook("btn_AdsInterstitial", OnAdsInterstitial);
            Hook("btn_AdsBanner", OnAdsBanner);
            Hook("btn_AdsRemove", OnAdsRemove);
            Hook("btn_Purchases", OnPurchases);
            Hook("btn_PurchasesRestore", OnPurchasesRestore);
            Hook("btn_GameServices", OnGameServices);
            Hook("btn_Notifications", OnNotifications);
            Hook("btn_Currency", OnCurrency);
        }

        void Hook(string name, Action onClick)
        {
            GameObject go = GameObject.Find(name);
            if (go == null || go.GetComponent<Button>() == null)
            {
                Debug.LogWarning("[demo] button '" + name + "' is missing from the scene.");
                return;
            }

            go.GetComponent<Button>().onClick.AddListener(() => onClick());
        }

        bool ServicesReady()
        {
            if (AppBootstrap.Instance != null && AppBootstrap.Instance.IsBooted)
            {
                return true;
            }

            Log("[demo] boot has not finished yet - press the button again in a beat.");
            return false;
        }

        void Log(string line)
        {
            Debug.Log(line);
            statusLines.Enqueue("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + line);
            while (statusLines.Count > 14)
            {
                statusLines.Dequeue();
            }

            if (statusLabel != null)
            {
                statusLabel.text = string.Join("\n", statusLines);
            }
        }

        /// <summary>
        /// Attaches a PoolFactory entry for the demo cube to the PoolManager GameObject, so
        /// Prewarm<T> finds a create function the same way a game would configure its own.
        /// </summary>
        void RegisterPoolFactory()
        {
            PoolManager pooling = AppBootstrap.Instance != null
                ? AppBootstrap.Instance.GetComponent<PoolManager>()
                : null;
            if (pooling == null || pooledPrefab == null)
            {
                return;
            }

            PoolFactory factory = pooling.GetComponent<PoolFactory>();
            if (factory == null)
            {
                factory = pooling.gameObject.AddComponent<PoolFactory>();
            }

            if (factory.pools == null)
            {
                factory.pools = new PoolFactory.PoolEntry[0];
            }

            foreach (PoolFactory.PoolEntry entry in factory.pools)
            {
                if (entry.key == PoolKey)
                {
                    return; // already configured
                }
            }

            PoolFactory.PoolEntry[] entries = new PoolFactory.PoolEntry[factory.pools.Length + 1];
            for (int i = 0; i < factory.pools.Length; i++)
            {
                entries[i] = factory.pools[i];
            }

            entries[factory.pools.Length] = new PoolFactory.PoolEntry
            {
                key = PoolKey,
                prefab = pooledPrefab,
                prewarmCount = 12,
            };
            factory.pools = entries;
        }


        void TryRegisterMigration()
        {
            if (migrationRegistered || !AppBootstrap.Instance.Services.TryGet(out ISaveService save))
            {
                return;
            }

            try
            {
                save.RegisterMigration(new DemoSaveMigration());
                Log("[save] migration 1 -> 2 registered; the chain continues at v" + save.CurrentVersion.ToString());
            }
            catch (InvalidOperationException)
            {
                Log("[save] migration already in the chain from an earlier run - nothing to do");
            }

            migrationRegistered = true;
        }

        // --- the handlers ----------------------------------------------------------------------

        async void OnLoadMenu()
        {
            if (!ServicesReady()) return;
            try
            {
                Log("[scene] covering load back to MainMenu (fade overlay + progress events)");
                await AppBootstrap.Instance.Services.Get<ISceneLoader>().LoadAsync("MainMenu");
            }
            catch (Exception ex)
            {
                Log("[scene] LoadAsync failed: " + ex.Message);
            }
        }

        async void OnSave()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out ISaveService save)) return;
            try
            {
                DemoSaveDocument doc = await save.LoadAsync<DemoSaveDocument>(DemoSaveKey);
                doc.coins += 12;
                if (doc.bestScore <= 0) doc.bestScore = 40;
                doc.runsPlayed++;
                await save.SaveAsync(DemoSaveKey, doc);
                Log("[save] wrote '" + DemoSaveKey + "' at schema v" + save.CurrentVersion.ToString() + " (+12 coins)");
            }
            catch (Exception ex)
            {
                Log("[save] save failed: " + ex.Message);
            }
        }

        async void OnLoad()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out ISaveService save)) return;
            try
            {
                DemoSaveDocument doc = await save.LoadAsync<DemoSaveDocument>(DemoSaveKey);
                Log("[save] loaded '" + DemoSaveKey + "': " + doc.coins.ToString() + " coins, best " + doc.bestScore.ToString() + ", " + doc.runsPlayed.ToString() + " runs");
            }
            catch (Exception ex)
            {
                Log("[save] load failed: " + ex.Message);
            }
        }

        void OnSettings()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out ISettingsService settings)) return;

            settings.MusicVolume = Mathf.Clamp01(settings.MusicVolume + 0.1f);
            Log("[settings] MusicVolume -> " + settings.MusicVolume.ToString("0.00") + " (the SettingsChanged event applies it at every owner)");
        }

        void OnAudio()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out IAudioService audio)) return;

            audio.PlaySfx("demo_click");
            audio.PlayMusic("demo_music");
            audio.Duck(0.4f, 1.5f);
            Log("[audio] asked the host for demo_click + demo_music, ducked 0.4 for 1.5s - no AudioLibrary asset yet, so the keys resolve to silence (the game supplies clips, the host stays)");
        }

        async void OnLocalize()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out ILocalizationService loc)) return;

            System.Collections.Generic.IReadOnlyList<string> locales = loc.AvailableLocales;
            if (locales == null || locales.Count == 0)
            {
                Log("[loc] no locale tables registered yet - the game adds its tables; the seam already flows");
                return;
            }

            int next = 0;
            for (int i = 0; i < locales.Count; i++)
            {
                if (locales[i] == loc.CurrentLocale)
                {
                    next = (i + 1) % locales.Count;
                    break;
                }
            }

            try
            {
                bool changed = await loc.SetLocaleAsync(locales[next]);
                Log("[loc] SetLocaleAsync -> " + locales[next] + ": " + changed);
            }
            catch (Exception ex)
            {
                Log("[loc] SetLocaleAsync failed: " + ex.Message);
            }
        }

        void OnPoolSpawn()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out IPoolManager pools)) return;
            if (pooledPrefab == null)
            {
                Log("[pool] no pooledPrefab wired on the runner.");
                return;
            }

            if (cubePool == null)
            {
                pools.Prewarm<PooledCube>(PoolKey, 12);
                cubePool = pools.GetPool<PooledCube>(PoolKey);
                Log("[pool] pool '" + PoolKey + "' created from the PoolFactory entry (prewarmed)");
            }

            for (int i = 0; i < 12; i++)
            {
                PooledCube cube = cubePool.Get();
                if (cube != null)
                {
                    if (!cube.gameObject.activeSelf)
                    {
                        cube.gameObject.SetActive(true); // the factory creates inactive; Get does not activate
                    }

                    liveCubes.Add(cube);
                }
            }

            Log("[pool] spawned 12 cubes (Get + OnSpawn only, zero Instantiate)");
        }

        void OnPoolRelease()
        {
            if (cubePool == null || liveCubes.Count == 0)
            {
                Log("[pool] no live cubes to release.");
                return;
            }

            foreach (PooledCube cube in liveCubes)
            {
                cubePool.Release(cube);
            }

            liveCubes.Clear();
            Log("[pool] all cubes returned (OnDespawn ran, zero Destroy)");
        }

        void OnUiPush()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out UIManager ui)) return;

            overlayVisits++;
            overlayScreen.SetId("demo_overlay_" + overlayVisits.ToString());
            ui.RegisterScreen(overlayScreen);
            _ = ui.PushAsync(overlayScreen.ScreenId);
            Log("[ui] pushed '" + overlayScreen.ScreenId + "' (visit " + overlayVisits.ToString() + "); the stack owns show/hide");
        }

        async void OnUiBack()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out UIManager ui)) return;

            try
            {
                bool popped = await ui.BackAsync();
                Log("[ui] BackAsync -> " + (popped ? "popped" : "stack already empty"));
            }
            catch (Exception ex)
            {
                Log("[ui] BackAsync failed: " + ex.Message);
            }
        }

        void OnUiPopup()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out UIManager ui)) return;

            ui.BindPopups(popupHost);
            ui.EnqueuePopup(new PopupRequest(
                "demo_popup",
                "Queued popup",
                "The UIManager pumps one popup at a time; the demo card's OK button completes its task.",
                "OK",
                null));
            _ = ui.PumpPopupsAsync();
            Log("[ui] popup enqueued and the pump started");
        }

        void OnConsent()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out IConsentService consent)) return;

            Log("[consent] state was " + consent.State.ToString() + "; toggling");
            if (consent.State == ConsentState.Granted)
            {
                consent.Deny();
            }
            else
            {
                consent.Grant();
            }
        }

        void OnAnalytics()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out IAnalyticsService analytics)) return;

            analytics.LogEvent(
                AnalyticsEventNames.SessionStarted,
                new AnalyticsParam("scene", "SystemsDemo"),
                new AnalyticsParam("button", "btn_Analytics"));
            Log("[analytics] session_started queued - the template ships a dropping sink, and consent gates it");
        }

        async void OnRemoteConfig()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out IRemoteConfig config)) return;

            try
            {
                RemoteConfigFetchResult fetch = await config.FetchAsync();
                Log("[remote] fetch " + fetch.Status.ToString() + " (v" + config.Version + ", remote values " + config.HasRemoteValues + "); demo reads: bool " + config.GetBool("demo_enabled", true) + ", int " + config.GetInt("demo_lives", 3) + ", string '" + config.GetString("demo_greeting", "hello") + "'");
            }
            catch (Exception ex)
            {
                Log("[remote] fetch failed: " + ex.Message);
            }
        }

        void OnAdsRewarded()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out IAdService ads)) return;

            AdShowResult result = ads.ShowRewarded("demo_reward", reward =>
                Log("[ads] reward '" + reward.Tag + "' earned=" + reward.EarnedReward + " (grant inside this callback only)"));
            Log("[ads] ShowRewarded -> " + result.ToString() + " (stub no-fill is a normal answer)");
        }

        void OnAdsInterstitial()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out IAdService ads)) return;

            AdShowResult result = ads.ShowInterstitial();
            Log("[ads] ShowInterstitial -> " + result.ToString() + " (subject to the AdPolicy caps)");
        }

        void OnAdsBanner()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out IAdService ads)) return;

            ads.ShowBanner();
            Log("[ads] ShowBanner (removed=" + ads.AreInterstitialsRemoved + ")");
        }

        void OnAdsRemove()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out IAdService ads)) return;

            ads.PurchaseRemoveAds();
            Log("[ads] PurchaseRemoveAds; AreInterstitialsRemoved=" + ads.AreInterstitialsRemoved + " (persisted now, restored at boot)");
        }

        async void OnPurchases()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out IPurchaseService purchases)) return;

            try
            {
                PurchaseResult result = await purchases.PurchaseAsync("demo_remove_ads");
                Log(result.Success
                    ? "[purchases] bought " + result.ProductId
                    : "[purchases] " + result.ProductId + " failed: " + result.FailureReason + " (the template's catalog is empty on purpose)");
            }
            catch (Exception ex)
            {
                Log("[purchases] purchase failed: " + ex.Message);
            }
        }

        async void OnPurchasesRestore()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out IPurchaseService purchases)) return;

            try
            {
                System.Collections.Generic.IReadOnlyCollection<string> owned = await purchases.RestorePurchasesAsync();
                Log("[purchases] restore returned " + owned.Count.ToString() + " owned products");
            }
            catch (Exception ex)
            {
                Log("[purchases] restore failed: " + ex.Message);
            }
        }

        async void OnGameServices()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out IGameServices gameServices)) return;

            try
            {
                bool signedIn = await gameServices.SignInAsync();
                Log("[gameservices] SignInAsync -> " + signedIn + " (no provider: reports queue offline and flush later)");

                gameServices.ReportScore("demo_board", (long)(Time.realtimeSinceStartup * 100d));
                Log("[gameservices] ReportScore queued for 'demo_board'");
            }
            catch (Exception ex)
            {
                Log("[gameservices] call failed: " + ex.Message);
            }
        }

        async void OnNotifications()
        {
            if (!ServicesReady()) return;
            if (!AppBootstrap.Instance.Services.TryGet(out INotificationService notifications)) return;

            try
            {
                NotificationPermission permission = await notifications.RequestAuthorizationAsync();
                Log("[notifications] permission " + permission.ToString() + " - the OS prompt fires only from the game's benefit moment, never at start");

                NotificationScheduleOutcome outcome = await notifications.ScheduleAsync(
                    "demo_reminder",
                    "Demo reminder",
                    "Scheduled one minute from the SystemsDemo button.",
                    "demo",
                    DateTime.UtcNow.AddMinutes(1d));
                Log("[notifications] outcome " + outcome.ToString() + "; the pending list is persisted and reapplied every launch");
            }
            catch (Exception ex)
            {
                Log("[notifications] call failed: " + ex.Message);
            }
        }

        void OnCurrency()
        {
            wallet.Grant("coins", 25);
            bool spent = wallet.TrySpend("coins", 10);
            Log("[wallet] granted 25, spent 10 -> " + spent + "; balance " + wallet.GetBalance("coins") + " (each change also raised a Changed event)");
        }
    }
}
