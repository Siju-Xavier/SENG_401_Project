using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using ScriptableObjects;

[InitializeOnLoad]
public class AutoFixer
{
    static AutoFixer()
    {
        EditorApplication.delayCall += DoFix;
    }

    private static void DoFix()
    {
        string spritePath = "Assets/Sprites/Characters/firefighter_walk-removebg-preview.png";
        string configPath = "Assets/Sprites/ScriptableObjects/UnitConfig.asset";

        var sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        List<Sprite> spriteList = new List<Sprite>();
        foreach (var obj in sprites)
        {
            if (obj is Sprite s) spriteList.Add(s);
        }

        var config = AssetDatabase.LoadAssetAtPath<UnitConfig>(configPath);
        if (config != null)
        {
            SerializedObject so = new SerializedObject(config);
            bool isModified = false;

            if (spriteList.Count > 0)
            {
                SerializedProperty prop = so.FindProperty("walkSprites");
                if (prop != null)
                {
                    prop.arraySize = spriteList.Count;
                    for (int i = 0; i < spriteList.Count; i++)
                    {
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = spriteList[i];
                    }
                    isModified = true;
                    Debug.Log("[AutoFixer] Successfully assigned 16 Walk Sprites to UnitConfig.");
                }
            }

            SerializedProperty prefabProp = so.FindProperty("unitPrefab");
            if (prefabProp != null && prefabProp.objectReferenceValue == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Fire/Firefighter.prefab");
                if (prefab != null)
                {
                    prefabProp.objectReferenceValue = prefab;
                    isModified = true;
                    Debug.Log("[AutoFixer] Successfully assigned Firefighter Prefab to UnitConfig.");
                }
            }

            if (isModified)
            {
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }
    }
}
