namespace BusinessLogic {
    using UnityEngine;

    public class WeatherSystem : MonoBehaviour {
        private Vector2 currentWindDirection;
        private float windSpeed;

        public Vector2 GetNextWindDirection() { 
            return currentWindDirection; 
        }

        public float GetWindSpeed() { 
            return windSpeed; 
        }

        public bool IsValid() { 
            return true; 
        }

        public void UpdateWeatherState() { 
            // Changes wind periodically
        }
    }
}
