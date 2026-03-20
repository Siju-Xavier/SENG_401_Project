namespace BusinessLogic {
    using System.Collections;
    using GameState;
    using Persistence;
    using UnityEngine;

    public class ReputationManager : MonoBehaviour {
        [SerializeField] private int playerId = 1;

        /// <summary>
        /// Update reputation for a city and persist the change to the cloud.
        /// </summary>
        public void UpdateReputation(City city, string scoreType, int level) {
            if (city == null) return;

            // Apply reputation change based on score type
            int delta = scoreType switch {
                "positive" =>  level * 5,
                "negative" => -level * 3,
                _          =>  0
            };

            city.Reputation = Mathf.Clamp(city.Reputation + delta, 0, 100);
            Debug.Log($"[Reputation] {city.CityName} reputation: {city.Reputation} (delta: {delta})");

            // Sync to cloud
            StartCoroutine(SyncReputationToCloud(city.CityName, city.Reputation));
        }

        public int GetLocalReputation() {
            return 0;
        }

        public int CalculateNextReputation(int level, City city1, City city2) {
            if (city1 == null || city2 == null) return 0;
            return (city1.Reputation + city2.Reputation) / 2;
        }

        private IEnumerator SyncReputationToCloud(string cityName, int reputation) {
            var db = DatabaseProvider.Instance;
            if (db == null || !db.IsConfigured) yield break;

            yield return db.UpsertCityReputation(playerId, cityName, reputation, ok => {
                if (ok) Debug.Log($"[Reputation] {cityName} synced to cloud ({reputation}).");
            });
        }
    }
}

