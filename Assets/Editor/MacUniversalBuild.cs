using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MacUniversalBuild
{
    // Batch entry point: -executeMethod MacUniversalBuild.Run
    public static void Run()
    {
        const string output = "Builds/MacUniversal";
        Directory.CreateDirectory(output);
        var previousBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
        try
        {
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.x64ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = output + "/PiratesOfTheMediterranean.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            });
            var messages = report.steps.SelectMany(step => step.messages)
                .Where(message => message.type == LogType.Error || message.type == LogType.Warning)
                .Select(message => message.content).ToArray();
            File.WriteAllText(output + "/build-report.txt",
                $"Target: macOS Universal (x86_64 + arm64)\nBackend: Mono\nDevelopment: False\nResult: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nSize: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\n" + string.Join("\n", messages));
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0 ||
                messages.Any(message => message.IndexOf("Shader error", StringComparison.OrdinalIgnoreCase) >= 0))
                throw new InvalidOperationException("Build failed validation; inspect " + output + "/build-report.txt");
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, previousBackend);
        }
    }
}
