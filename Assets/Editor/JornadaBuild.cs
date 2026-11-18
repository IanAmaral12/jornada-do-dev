using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class JornadaBuild
{
    [MenuItem("Jornada/Build Windows")]
    public static void BuildWindows()
    {
        var opcoes = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Build/JornadaDoDev.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opcoes);
        Debug.Log($"Jornada build: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB");
        if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
