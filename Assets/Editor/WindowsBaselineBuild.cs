using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WindowsBaselineBuild
{
    // Batch entry point: -executeMethod WindowsBaselineBuild.Run
    public static void Run()
    {
        const string output = "Builds/Windows";
        const string shaderPath = "Assets/3rd party/KriptoFX/WaterSystem2/WaterResources/Shaders/Resources/Common/CommandPass/KWS_WavesFFT.compute";
        Directory.CreateDirectory(output);
        AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        var shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(shaderPath);
        if (shader == null || !shader.HasKernel("ComputeNormal"))
            throw new InvalidOperationException("Water ComputeNormal kernel is missing.");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = output + "/PiratesOfTheMediterranean.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        var messages = report.steps.SelectMany(step => step.messages)
            .Where(message => message.type == LogType.Error || message.type == LogType.Warning)
            .Select(message => message.content).ToArray();
        File.WriteAllText(output + "/build-report.txt",
            $"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nSize: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\n" + string.Join("\n", messages));
        // Unity can report Succeeded while shipping a broken compute shader.
        if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0 ||
            messages.Any(message => message.IndexOf("Shader error", StringComparison.OrdinalIgnoreCase) >= 0))
            throw new InvalidOperationException("Build failed validation; inspect " + output + "/build-report.txt");
    }
}
