using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Template.Editor
{
    /// <summary>
    /// Bonus system 5 (opt-in file, not base wiring): the build pipeline — versioning from one
    /// constant (application version, Android version code, iOS build number), a prerequisite
    /// check (boot scenes present in Build Settings) and one-method automated builds callable
    /// headless:
    ///
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod Template.Editor.TemplateBuildPipeline.BuildAndroid
    ///
    /// A failed prerequisite or build throws, so the headless process exits non-zero instead of
    /// quietly shipping a broken artefact.
    /// </summary>
    public static class TemplateBuildPipeline
    {
        /// <summary>Single source of truth for every platform's version stamp. A game replaces this.</summary>
        public const string TemplateVersion = "1.0.0";

        public const string AndroidOutputPath = "Builds/Android/template.apk";

        /// <summary>Stamps the version everywhere the stores read it. Also safe to run without building.</summary>
        [MenuItem("Template/Build/Apply Versioning")]
        public static void ApplyVersioning()
        {
            PlayerSettings.bundleVersion = TemplateVersion;
            PlayerSettings.Android.bundleVersionCode = VersionToVersionCode(TemplateVersion);
            PlayerSettings.iOS.buildNumber = VersionToBuildNumber(TemplateVersion);
            Debug.Log("[Build] Version stamps applied: " + TemplateVersion);
        }

        [MenuItem("Template/Build/Android (Release)")]
        public static void BuildAndroidMenu()
        {
            BuildAndroid();
        }

        /// <summary>Headless entry point. Throws on any failure so CI exits non-zero.</summary>
        public static void BuildAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            CheckPrerequisites(BuildTarget.Android);
            ApplyVersioning();

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = SceneList(),
                locationPathName = AndroidOutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });

            RequireSuccess(report, BuildTarget.Android);
        }

        /// <summary>
        /// The boot scenes the M1 contract depends on must be in the build — Bootstrap.unity at
        /// minimum — so an automated build can never ship without its entry scene.
        /// </summary>
        public static void CheckPrerequisites(BuildTarget target)
        {
            string[] enabledScenes = SceneList();
            if (enabledScenes.Length == 0)
            {
                throw new InvalidOperationException(
                    "No enabled scenes in Build Settings; run Template/Setup/Create Boot Scenes first.");
            }

            if (!enabledScenes.Any(scene => scene.EndsWith("Bootstrap.unity", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "Bootstrap.unity (the boot contract's entry scene) is not among the enabled build scenes.");
            }
        }

        static string[] SceneList()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }

        static void RequireSuccess(BuildReport report, BuildTarget target)
        {
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    target + " build failed: " + report.summary.result +
                    " (" + report.summary.totalErrors + " errors). See the editor log for the errors.");
            }
        }

        /// <summary>major*10000 + minor*100 + patch; format-only, the stores just need monotonicity.</summary>
        public static int VersionToVersionCode(string version)
        {
            Version parsed = Parse(version);
            return parsed.Major * 10000 + parsed.Minor * 100 + Math.Max(parsed.Build, 0);
        }

        public static string VersionToBuildNumber(string version)
        {
            return Parse(version).ToString();
        }

        static Version Parse(string version)
        {
            if (!Version.TryParse(version, out Version parsed))
            {
                throw new ArgumentException("'" + version + "' is not a version number (expected m.m.p...).", nameof(version));
            }

            return parsed;
        }
    }
}
