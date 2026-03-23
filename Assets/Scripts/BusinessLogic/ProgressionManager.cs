namespace BusinessLogic {
    using System.Collections;
    using GameState;
    using Core;
    using Persistence;
    using UnityEngine;

    public class ProgressionManager : MonoBehaviour {
        [SerializeField] private PlayerProgression progressionData;
        [SerializeField] private int playerId = 1;

        /// <summary>Add points to the player's score and sync to cloud.</summary>
        public void AddToScore(int points) {
            if (progressionData == null) return;
            progressionData.CurrentScore += points;
            Debug.Log($"[Progression] Score: {progressionData.CurrentScore}");

            // Sync to cloud
            StartCoroutine(SyncProgressionToCloud());
        }

        /// <summary>Check if the player qualifies for a level-up.</summary>
        public void CheckScore() {
            if (progressionData == null) return;
            int threshold = progressionData.CurrentLevel * 100;

            if (progressionData.CurrentScore >= threshold) {
                progressionData.CurrentLevel++;
                Debug.Log($"[Progression] Level up! Now level {progressionData.CurrentLevel}");
                EventBroker.Instance.Publish(Core.EventType.LevelUp, progressionData.CurrentLevel);

                // Unlock a feature at every level
                string unlock = $"Level{progressionData.CurrentLevel}Reward";
                if (!progressionData.UnlockedFeatures.Contains(unlock)) {
                    progressionData.UnlockedFeatures.Add(unlock);
                    StartCoroutine(PersistUnlock(unlock));
                }

                StartCoroutine(SyncProgressionToCloud());
            }
        }

        /// <summary>Force a level up regardless of score (e.g. at the end of a round with no cities burned).</summary>
        public void ForceLevelUp() {
            if (progressionData == null) return;
            
            progressionData.CurrentLevel++;
            Debug.Log($"[Progression] Forced Level up! Now level {progressionData.CurrentLevel}");
            EventBroker.Instance.Publish(Core.EventType.LevelUp, progressionData.CurrentLevel);

            string unlock = $"Level{progressionData.CurrentLevel}Reward";
            if (!progressionData.UnlockedFeatures.Contains(unlock)) {
                progressionData.UnlockedFeatures.Add(unlock);
                StartCoroutine(PersistUnlock(unlock));
            }

            StartCoroutine(SyncProgressionToCloud());
        }

        public int CurrentLevel => progressionData != null ? progressionData.CurrentLevel : 1;

        public int GetCurrentScore() {
            return progressionData != null ? progressionData.CurrentScore : 0;
        }

        public bool CalculateProgressionLevel(string topic) {
            CheckScore();
            return progressionData != null && progressionData.CurrentLevel > 1;
        }

        // ── Global Difficulty Multipliers ──────────────────────────────
        
        /// <summary>
        /// Global multiplier for fire spread chance.
        /// Level 1 = 1.0x, Level 2 = 1.1x, Level 3 = 1.2x...
        /// </summary>
        public float GetGlobalSpreadMultiplier() {
            int level = CurrentLevel;
            return 1.0f + (level - 1) * 0.1f;
        }

        /// <summary>
        /// Global multiplier for random fire spawn chance.
        /// Level 1 = 1.0x, Level 2 = 1.2x, Level 3 = 1.4x...
        /// </summary>
        public float GetGlobalSpawnMultiplier() {
            int level = CurrentLevel;
            return 1.0f + (level - 1) * 0.2f;
        }

        // ── Cloud sync helpers ─────────────────────────────────────────

        private IEnumerator SyncProgressionToCloud() {
            var db = DatabaseProvider.Instance;
            if (db == null || !db.IsConfigured) yield break;

            yield return db.UpdatePlayerProgression(playerId,
                progressionData.CurrentLevel,
                progressionData.CurrentScore,
                ok => {
                    if (ok) Debug.Log("[Progression] Synced to cloud.");
                });
        }

        private IEnumerator PersistUnlock(string itemName) {
            var db = DatabaseProvider.Instance;
            if (db == null || !db.IsConfigured) yield break;

            yield return db.UnlockItem(playerId, itemName, ok => {
                if (ok) Debug.Log($"[Progression] Unlocked '{itemName}' saved to cloud.");
            });
        }
    }
}

