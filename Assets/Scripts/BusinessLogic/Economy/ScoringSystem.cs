namespace BusinessLogic {
    using System.Collections;
    using GameState;
    using Core;
    using Persistence;
    using UnityEngine;

    public class ScoringSystem : MonoBehaviour {
        // Accumulated session stats
        private int sessionScore;
        private int firesExtinguished;
        private int citiesSaved;
        private int citiesLost;
        private int totalXpEarned;
        private int ticksSurvived;
        private int unitsDeployed;
        private string highestThreatHandled;

        // Public accessors for other systems to increment
        public int SessionScore        { get => sessionScore;        set => sessionScore = value; }
        public int FiresExtinguished   { get => firesExtinguished;   set => firesExtinguished = value; }
        public int CitiesSaved         { get => citiesSaved;         set => citiesSaved = value; }
        public int CitiesLost          { get => citiesLost;          set => citiesLost = value; }
        public int TotalXpEarned       { get => totalXpEarned;       set => totalXpEarned = value; }
        public int TicksSurvived       { get => ticksSurvived;       set => ticksSurvived = value; }
        public int UnitsDeployed       { get => unitsDeployed;       set => unitsDeployed = value; }
        public string HighestThreatHandled { get => highestThreatHandled; set => highestThreatHandled = value; }

        public void FindHighestScoredTopic(string topic, object func) { }

        /// <summary>Calculate session score based on response time and accumulated stats.</summary>
        public void CalculateScore(float responseTime) {
            // Simple scoring formula — can be refined
            int timeBonus = Mathf.Max(0, 100 - Mathf.RoundToInt(responseTime));
            sessionScore += timeBonus + (firesExtinguished * 10) + (citiesSaved * 50);
            Debug.Log($"[Scoring] Score updated: {sessionScore}");
        }

        public bool FindProgress() {
            return sessionScore > 0;
        }

        /// <summary>
        /// Save end-of-session stats to the cloud via DatabaseProvider.
        /// Called by GameManager.EndGame().
        /// </summary>
        public IEnumerator SaveStatsToCloud(int playerId, int roundNumber,
            int mapWidth, int mapHeight, int cityCount) {

            var db = DatabaseProvider.Instance;
            if (db == null || !db.IsConfigured) {
                Debug.Log("[Scoring] DatabaseProvider not configured — skipping stats save.");
                yield break;
            }

            var stats = new DatabaseProvider.GameStatsPayload {
                player_id              = playerId,
                session_score          = sessionScore,
                fires_extinguished     = firesExtinguished,
                cities_saved           = citiesSaved,
                cities_lost            = citiesLost,
                total_xp_earned        = totalXpEarned,
                ticks_survived         = ticksSurvived,
                final_level            = 1,
                highest_threat_handled = highestThreatHandled ?? "",
                round_number           = roundNumber,
                units_deployed         = unitsDeployed,
                map_width              = mapWidth,
                map_height             = mapHeight,
                city_count             = cityCount
            };

            yield return db.SaveGameStats(stats, ok => {
                if (ok) Debug.Log("[Scoring] Game stats saved to cloud.");
                else    Debug.LogWarning("[Scoring] Failed to save game stats.");
            });
        }
    }
}

