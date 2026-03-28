namespace BusinessLogic {
    using UnityEngine;

    public class WeatherSystem : MonoBehaviour {
        [Header("Wind Settings")]
        [SerializeField] private float windChangeInterval = 15f;
        [SerializeField] private float maxWindSpeed = 1.5f;

        private Vector2 currentWindDirection;
        private float windSpeed;
        private float windTimer;
        private GameOverManager gameOverManager;

        private void Start() {
            gameOverManager = FindFirstObjectByType<GameOverManager>();
            UpdateWeatherState();
        }

        private void Update() {
            if (gameOverManager != null && gameOverManager.IsGameOver) return;

            windTimer += Time.deltaTime;
            if (windTimer >= windChangeInterval) {
                windTimer = 0f;
                UpdateWeatherState();
            }
        }

        public Vector2 GetNextWindDirection() {
            return currentWindDirection;
        }

        public float GetWindSpeed() {
            return windSpeed;
        }

        public bool IsValid() {
            return true;
        }

        public void SetWind(Vector2 direction, float speed) {
            currentWindDirection = direction;
            windSpeed = speed;
        }

        public void UpdateWeatherState() {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            windSpeed = Random.Range(0.3f, maxWindSpeed);
            currentWindDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * windSpeed;
        }
    }
}
