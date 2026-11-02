using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WindowsLinkBuild
{
    public static void Run()
    {
        string folder = Environment.GetEnvironmentVariable("ET_WINDOWS_BUILD_DIR");
        if (string.IsNullOrWhiteSpace(folder)) throw new InvalidOperationException("Set ET_WINDOWS_BUILD_DIR to the output folder.");
        Directory.CreateDirectory(folder);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Project/Scenes/01_Opening.unity" },
            locationPathName = Path.Combine(folder, "ET-Training.exe"),
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = BuildOptions.CompressWithLz4HC
        });
        File.WriteAllText(Path.Combine(folder,"BuildResult.txt"),
            $"Result: {report.summary.result}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\nBytes: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\nScene: Assets/Project/Scenes/01_Opening.unity\nTarget: Windows x64 / Meta Link\n");
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows Link build failed. See build log.");
        Debug.Log("Windows Link build succeeded: " + folder);
    }
}
