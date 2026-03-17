namespace BusinessLogic {
    using GameState;
    using Core;
    using UnityEngine;

    public class FireEngine : MonoBehaviour {
        [SerializeField] private WeatherSystem weatherSystem;
        private bool isRunning;
        private float fireTickTimer;

        public void CalculateSpread() { 
            // Gets wind from WeatherSystem, updates Tiles
            // EventBroker.Instance.Publish(EventType.TileUpdated, updatedTile);
        }

        public void Tick() { }

        public void Pause() { 
            isRunning = false; 
        }

        public void Resume() { 
            isRunning = true; 
        }
    }
}
