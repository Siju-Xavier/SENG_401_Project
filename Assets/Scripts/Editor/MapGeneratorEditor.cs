using UnityEngine;
using UnityEditor;
using Core;

namespace Presentation.MapGeneration.Editor
{
    [CustomEditor(typeof(MapGenerationOrchestrator))]
    [InitializeOnLoad]
    public class MapGeneratorEditor : UnityEditor.Editor
    {
        static MapGeneratorEditor()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            // Clear all generated content before domain reload so Unity
            // doesn't have to serialize thousands of runtime objects
            var orchestrators = FindObjectsByType<MapGenerationOrchestrator>(FindObjectsSortMode.None);
            foreach (var orchestrator in orchestrators)
            {
                orchestrator.ClearGeneratedContent();
            }
        }

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
