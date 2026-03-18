using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BusinessLogic.MapGeneration;

namespace Presentation.MapGeneration.Editor
{
    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorEditor : UnityEditor.Editor
    {
    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        // If any value in the inspector changes...
        if (DrawDefaultInspector())
        {
            // ...and "autoUpdate" is true, regenerate the map immediately
            if (mapGen.autoUpdate)
            {
                mapGen.GenerateMap();
            }
        }

        // Add a manual "Generate" button to the inspector
        if (GUILayout.Button("Generate"))
        {
            mapGen.GenerateMap();
        }
    }
}
}
