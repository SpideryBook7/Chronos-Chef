using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class ChronosAndroidBuild
{
    [MenuItem("Chronos Chef/Android/Build validation bundle")]
    public static void BuildValidation() => Build(false);

    [MenuItem("Chronos Chef/Android/Build signed upload bundle")]
    public static void BuildForUpload() => Build(true);

    static void Build(bool forUpload)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new BuildFailedException("Switch the active build target to Android first.");
        if ((int)PlayerSettings.Android.targetSdkVersion < 36)
            throw new BuildFailedException("Set the Android target API to 36 or higher.");
        if (forUpload && (!PlayerSettings.Android.useCustomKeystore ||
            !File.Exists(PlayerSettings.Android.keystoreName) || string.IsNullOrWhiteSpace(PlayerSettings.Android.keyaliasName)))
            throw new BuildFailedException("Configure your upload keystore and alias in Android Publishing Settings.");
        var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0 || scenes[0] != "Assets/ChronosChef/Scenes/ChronosKitchen.unity")
            throw new BuildFailedException("ChronosKitchen must be the first enabled scene.");
        bool previousBundle = EditorUserBuildSettings.buildAppBundle;
        bool previousExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        string channel = forUpload ? "Upload" : "Validation";
        string directory = Path.Combine("Builds", "Android", channel);
        Directory.CreateDirectory(directory);
        try
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.buildAppBundle = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.Android,
                locationPathName = Path.Combine(directory, "ChronosChef-" + PlayerSettings.Android.bundleVersionCode + ".aab"),
                options = BuildOptions.StrictMode
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Android bundle failed: " + report.summary.result);
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = previousExport;
        }
    }
}
