using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[InitializeOnLoad]
public class ForceBuildSettings
{
    static ForceBuildSettings()
    {
        EditorApplication.delayCall += Sync;
    }

    [MenuItem("Tools/Force Update Build Settings")]
    public static void Sync()
    {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
        var scenes = new List<EditorBuildSettingsScene>();

        foreach (var guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            scenes.Add(new EditorBuildSettingsScene(path, true));
            Debug.Log($"[ForceBuildSettings] Adding scene to build: {path}");
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[ForceBuildSettings] Build Settings Updated!");
    }
}
