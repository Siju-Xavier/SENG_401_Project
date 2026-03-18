namespace Core {
    using UnityEngine;
    using Business_Logic;

    public class GameManager : MonoBehaviour {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private WeatherSystem weatherSystem;
        [SerializeField] private FireEngine fireEngine;

        private void Start() {
            EventBroker.Instance.Subscribe(EventType.GameEnded, EndGame);
        }

        public void StartGame() { }
        public void PauseGame() { }
        public void ResumeGame() { }
        public void EndGame(object data = null) { }
    }
}
