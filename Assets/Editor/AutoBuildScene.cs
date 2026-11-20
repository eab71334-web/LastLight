using System.IO;
using UnityEditor;

/// <summary>
/// Cloud builders (Unity Build Automation) open the project without running our scripts.
/// If the generated scene is missing, build it as soon as the Editor loads.
/// </summary>
[InitializeOnLoad]
public static class AutoBuildScene
{
    static AutoBuildScene()
    {
        EditorApplication.delayCall += Ensure;
    }

    static void Ensure()
    {
        if (File.Exists("Assets/Scenes/Main.unity")) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SceneBuilder.BuildScene();
    }
}
