using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Build Automation builds the project itself and never calls BuildScript.Build().
/// Scenes/ is git-ignored, so we generate the scene synchronously as soon as the Editor
/// loads scripts in batch mode (before the cloud builder reads the build settings).
/// </summary>
[InitializeOnLoad]
public static class AutoBuildScene
{
    static AutoBuildScene()
    {
        if (!Application.isBatchMode) { EditorApplication.delayCall += EnsureEditor; return; }
        Ensure();
    }

    static void EnsureEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists("Assets/Scenes/Main.unity")) Ensure();
    }

    /// <summary>Also usable as the "Pre-export method" in Unity Build Automation.</summary>
    public static void Ensure()
    {
        try
        {
            SceneBuilder.BuildScene();
            BuildScript.ApplySettings();
            BuildScript.ApplyAndroidSettings();
            Debug.Log("[AutoBuildScene] Main scene generated and build settings applied.");
        }
        catch (Exception e)
        {
            Debug.LogError("[AutoBuildScene] failed: " + e);
        }
    }
}
