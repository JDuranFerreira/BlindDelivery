using System.IO;
using System.Linq;
using Template.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Template.Editor
{
    /// <summary>
    /// Creates the two scenes the boot contract needs — Bootstrap at build index 0 and a MainMenu
    /// placeholder the boot loads — and puts them in Build Settings. Idempotent: existing scenes
    /// are left alone, only the build list is repaired.
    ///
    /// Also callable headless:
    /// Unity.exe -batchmode -quit -projectPath &lt;project&gt; -executeMethod Template.Editor.BootstrapSceneSetup.CreateBootScenes
    /// </summary>
    public static class BootstrapSceneSetup
    {
        const string ScenesFolder = "Assets/Scenes";
        const string BootstrapScenePath = ScenesFolder + "/Bootstrap.unity";
        const string MainMenuScenePath = ScenesFolder + "/MainMenu.unity";

        [MenuItem("Template/Setup/Create Boot Scenes")]
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
