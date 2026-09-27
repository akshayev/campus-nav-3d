using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Lets us build from the command line (no Editor window), per CLAUDE.md:
//   Unity -batchmode -quit -projectPath ./UnityProject -executeMethod BuildScript.BuildWebGL -logFile build.log
// It lives in an "Editor" folder, so Unity never includes it in the actual game build.
public static class BuildScript
{
    public static void BuildWebGL()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        });

        Debug.Log($"WebGL build result: {report.summary.result}, errors: {report.summary.totalErrors}");

        // A non-zero exit code makes the failure visible to scripts/CI, not just in the log.
        if (report.summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }
}
