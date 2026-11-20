using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Entry point for CI (GameCI passes -customBuildPath / -customBuildTarget / -customBuildName).
/// It regenerates the scene from code, applies mobile settings and builds.
/// </summary>
public static class BuildScript
{
    const string ProductName = "The Last Light";

    static string Arg(string[] args, string key, string fallback)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == key) return args[i + 1];
        return fallback;
    }

    public static void Build()
    {
        string[] args = Environment.GetCommandLineArgs();
        string path = Arg(args, "-customBuildPath", "Builds/Out");
        string targetName = Arg(args, "-customBuildTarget", "Android");
        string buildName = Arg(args, "-customBuildName", "LastLight");
        BuildTarget target = (BuildTarget)Enum.Parse(typeof(BuildTarget), targetName);

        SceneBuilder.BuildScene();
        ApplySettings();

        string location;
        if (target == BuildTarget.Android)
        {
            Directory.CreateDirectory(path);
            location = Path.Combine(path, buildName + ".apk");
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.lastlight.game");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        }
        else if (target == BuildTarget.iOS)
        {
            Directory.CreateDirectory(path);
            location = path; // Xcode project folder
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.lastlight.game");
            PlayerSettings.iOS.targetOSVersionString = "13.0";
        }
        else
        {
            throw new Exception("Unsupported build target: " + target);
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            target = target,
            locationPathName = location,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("Build result: " + report.summary.result + " (" + report.summary.totalSize + " bytes)");
        if (report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    static void ApplySettings()
    {
        PlayerSettings.companyName = "LastLight";
        PlayerSettings.productName = ProductName;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
    }
}
