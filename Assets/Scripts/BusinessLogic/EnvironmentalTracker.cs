namespace BusinessLogic {
    using UnityEngine;
    using Core;
    using Presentation.MapGeneration;

    public class EnvironmentalTracker : MonoBehaviour {
        private int treesLost;
        private int tilesScorched;
        private TreePlacer treePlacer;

        private const float CarbonPerTree = 0.5f;

        public int TreesLost => treesLost;
        public int TilesScorched => tilesScorched;
        public float CarbonEmitted => treesLost * CarbonPerTree;

        public float BiodiversityScore {
            get {
                if (treePlacer == null || treePlacer.OriginalTreeCount == 0) return 100f;
                return 100f * treePlacer.CurrentTreeCount / treePlacer.OriginalTreeCount;
            }
        }

        private void Start() {
            treePlacer = FindFirstObjectByType<TreePlacer>();
        }

        private void OnEnable() {
            Core.EventBroker.Instance.Subscribe(Core.EventType.FireStarted, OnFireStarted);
            Core.EventBroker.Instance.Subscribe(Core.EventType.FireExtinguished, OnFireExtinguished);
        }

        private void OnDisable() {
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.FireStarted, OnFireStarted);
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.FireExtinguished, OnFireExtinguished);
        }

        private void OnFireStarted(object data) {
            tilesScorched++;
            Core.EventBroker.Instance.Publish(Core.EventType.EnvironmentImpact, this);
        }

        private void OnFireExtinguished(object data) {
            // A tree was burned (TreePlacer removes it on extinguish)
            var tile = data as GameState.Tile;
            if (tile != null) {
                treesLost++;
                Core.EventBroker.Instance.Publish(Core.EventType.EnvironmentImpact, this);
            }
        }
    }
}
