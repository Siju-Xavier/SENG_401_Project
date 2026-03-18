namespace Core {
    using UnityEngine;
    using BusinessLogic;
    using BusinessLogic.MapGeneration;

    public class GameManager : MonoBehaviour {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private WeatherSystem weatherSystem;
        [SerializeField] private FireEngine fireEngine;

        private void Start() {
            EventBroker.Instance.Subscribe(EventType.GameEnded, EndGame);
        }

        public void StartGame() { }
        
        public void PauseGame() {
            Time.timeScale = 0f; // Stops all time-based logic and animations
        }
        
        public void ResumeGame() {
            Time.timeScale = 1f; // Resumes game at normal speed
        }
        
        public void EndGame(object data = null) { }
    }
}
