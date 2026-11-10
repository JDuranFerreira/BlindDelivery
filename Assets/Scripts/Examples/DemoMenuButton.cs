using Template.Core.Scenes;
using Template.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Template.Examples
{
    /// <summary>
    /// MainMenu's launcher button. Scene names live here as constants; the transition goes
    /// through <see cref="ISceneLoader"/>, never SceneManager directly — so the fade overlay,
    /// progress and the SceneTransitionStarted/Completed events happen the same way they will in
    /// a build. Play-pressed-anywhere: AppBootstrap boots at BeforeSceneLoad; this waits for
    /// IsBooted so the call never races the boot chain.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class DemoMenuButton : MonoBehaviour
    {
        public const string DemoSceneName = "SystemsDemo";

        Button button;

        void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClicked);
        }

        void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnClicked);
            }
        }

        void OnClicked()
        {
            AppBootstrap boot = AppBootstrap.Instance;
            if (boot == null || !boot.IsBooted)
            {
                Debug.Log("[Demo] Boot has not finished yet — the services are not ready for scene work.");
                return;
            }

            ISceneLoader loader = boot.Services.Get<ISceneLoader>();
            _ = loader.LoadAsync(DemoSceneName);
        }
    }
}