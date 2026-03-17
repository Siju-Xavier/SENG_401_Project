namespace BusinessLogic {
    using GameState;
    using Core;
    using UnityEngine;

    public class ResourceManager : MonoBehaviour {
        [SerializeField] private PlayerProgression progression;
        private int globalAvailableBudget;

        public void MoveEntity(string id, string category, int amount) { }

        public void DeployUnit(string id, string unitType, object refType) {
            // Subtract budget, create ActiveResponseUnit
        }

        public void TransferResources(City fromCity, City toCity, int amount) { }

        public void TrackAvailableResources() { }
    }
}
