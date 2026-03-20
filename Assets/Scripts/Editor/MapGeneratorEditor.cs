using UnityEngine;
using UnityEditor;
using Core;

namespace Presentation.MapGeneration.Editor
{
    [CustomEditor(typeof(MapGenerationOrchestrator))]
    public class MapGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            MapGenerationOrchestrator orchestrator = (MapGenerationOrchestrator)target;

            if (DrawDefaultInspector())
            {
                if (orchestrator.autoUpdate)
                {
                    orchestrator.GenerateWorld();
                }
            }

            if (GUILayout.Button("Generate"))
            {
                orchestrator.GenerateWorld();
            }
        }
    }
}
