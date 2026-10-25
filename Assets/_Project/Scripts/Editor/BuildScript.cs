using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CBJ.Editor
{
    /// <summary>
    /// Builds the Android APK with the shipping player settings (IL2CPP, ARM64, portrait), which it writes into the
    /// project settings first. Run it from the menu or
    /// headless with <c>-executeMethod CBJ.Editor.BuildScript.BuildAndroid</c>.
    /// </summary>
    public static class BuildScript
    {
        private const string MenuPath = "Tools/Build/Android";
        private const string OutputPath = "Builds/Android/ColorBlockJam.apk";

        /// <summary>Applies the Android player settings and builds the scenes in the build settings to <see cref="OutputPath"/>.</summary>
        [MenuItem(MenuPath)]
        public static void BuildAndroid()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Android build succeeded: {OutputPath}, {new FileInfo(OutputPath).Length / (1024f * 1024f):F1} MB, " +
                          $"{summary.totalWarnings} warning(s), {summary.totalTime.TotalSeconds:F0} s.");
            }
            else
            {
                Debug.LogError($"Android build {summary.result}: {summary.totalErrors} error(s). See the build report above.");
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }
    }
}
