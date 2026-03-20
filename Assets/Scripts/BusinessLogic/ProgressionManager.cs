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

                // Unlock a feature at every level
                string unlock = $"Level{progressionData.CurrentLevel}Reward";
                if (!progressionData.UnlockedFeatures.Contains(unlock)) {
                    progressionData.UnlockedFeatures.Add(unlock);
                    StartCoroutine(PersistUnlock(unlock));
                }

                StartCoroutine(SyncProgressionToCloud());
            }
        }

        public int GetCurrentScore() {
            return progressionData != null ? progressionData.CurrentScore : 0;
        }

        public bool CalculateProgressionLevel(string topic) {
            CheckScore();
            return progressionData != null && progressionData.CurrentLevel > 1;
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

