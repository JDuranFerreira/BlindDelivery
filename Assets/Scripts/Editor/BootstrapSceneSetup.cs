using System.IO;
using System.Linq;
using BlindDelivery.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlindDelivery.Editor
{
    /// <summary>
    /// Creates the two scenes the boot contract needs â€” Bootstrap at build index 0 and a MainMenu
    /// placeholder the boot loads â€” and puts them in Build Settings. Idempotent: existing scenes
    /// are left alone, only the build list is repaired.
    ///
    /// Also callable headless:
    /// Unity.exe -batchmode -quit -projectPath &lt;project&gt; -executeMethod BlindDelivery.Editor.BootstrapSceneSetup.CreateBootScenes
    /// </summary>
    public static class BootstrapSceneSetup
    {
        const string ScenesFolder = "Assets/Scenes";
        const string BootstrapScenePath = ScenesFolder + "/Bootstrap.unity";
        const string MainMenuScenePath = ScenesFolder + "/MainMenu.unity";

        [MenuItem("BlindDelivery/Setup/Create Boot Scenes")]
        public static void CreateBootScenes()
        {
            Directory.CreateDirectory(ScenesFolder);
            EnsureBootstrapScene();
            EnsureMainMenuScene();
            EnsureBuildSettings();
            AssetDatabase.SaveAssets();
        }

        static void EnsureBootstrapScene()
        {
            if (File.Exists(BootstrapScenePath))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject host = new GameObject(nameof(AppBootstrap));
            host.AddComponent<AppBootstrap>();
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        static void EnsureMainMenuScene()
        {
            if (File.Exists(MainMenuScenePath))
            {
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        }

        static void EnsureBuildSettings()
        {
            string[] ordered = { BootstrapScenePath, MainMenuScenePath };

            EditorBuildSettings.scenes = ordered
                .Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(EditorBuildSettings.scenes.Where(s => !ordered.Contains(s.path)))
                .ToArray();
        }
    }
}


