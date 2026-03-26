namespace BusinessLogic {
    using System.Collections;
    using GameState;
    using Core;
    using Persistence;
    using UnityEngine;

    public class ProgressionManager : MonoBehaviour {
        [SerializeField] private PlayerProgression progressionData;
        [SerializeField] private int playerId = 1;

        // Score checking methods removed. Levels are now managed simply by ForceLevelUp() at the end of each survived round.

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

        public int CurrentLevel => progressionData != null ? Mathf.Max(1, progressionData.CurrentLevel) : 1;

        private void Awake()
        {
            if (progressionData != null && progressionData.CurrentLevel < 1)
                progressionData.CurrentLevel = 1;
        }

        public bool CalculateProgressionLevel(string topic) {
            return progressionData != null && progressionData.CurrentLevel > 1;
        }

        // ── Rate-Based Difficulty Scaling ─────────────────────────────
        
        [Header("Fire Difficulty Rates")]
        [Tooltip("Ignition rate at Level 1 (fires per second).")]
        [SerializeField] private float minIgnitionRate = 0.1f;
        [Tooltip("Ignition rate at reference level (fires per second).")]
        [SerializeField] private float maxIgnitionRate = 2.0f;
        [Tooltip("Spread rate at Level 1 (spreads per second per burning tile).")]
        [SerializeField] private float minSpreadRate = 0.1f;
        [Tooltip("Spread rate at reference level (spreads per second per burning tile).")]
        [SerializeField] private float maxSpreadRate = 1.0f;
        [Tooltip("Level at which max rates are reached. Growth continues linearly beyond.")]
        [SerializeField] private int referenceLevel = 100;

        [Header("Land Recovery")]
        [Tooltip("Fraction of burnt edge tiles that recover naturally at the end of a round (e.g., 0.25 = 25%).")]
        [SerializeField] private float baseRecoveryFraction = 0.25f;

        /// <summary>
        /// Ignition rate (fires/sec) for the current level.
        /// Linear scaling: Level 1 = minIgnitionRate, referenceLevel = maxIgnitionRate.
        /// Continues growing linearly beyond referenceLevel.
        /// </summary>
        public float GetIgnitionRate() {
            int level = CurrentLevel;
            return minIgnitionRate + (maxIgnitionRate - minIgnitionRate) * (level - 1) / (float)(referenceLevel - 1);
        }

        /// <summary>
        /// Base spread rate (spreads/sec per burning tile) for the current level.
        /// Linear scaling: Level 1 = minSpreadRate, referenceLevel = maxSpreadRate.
        /// Continues growing linearly beyond referenceLevel.
        /// </summary>
        public float GetSpreadRate() {
            int level = CurrentLevel;
            return minSpreadRate + (maxSpreadRate - minSpreadRate) * (level - 1) / (float)(referenceLevel - 1);
        }

        /// <summary>
        /// Gets the fraction of land that recovers at the end of a round.
        /// </summary>
        public float GetRecoveryRate() {
            return baseRecoveryFraction;
        }

        // ── Cloud sync helpers ─────────────────────────────────────────

        private IEnumerator SyncProgressionToCloud() {
            var db = DatabaseProvider.Instance;
            if (db == null || !db.IsConfigured) yield break;

            yield return db.UpdatePlayerProgression(playerId,
                progressionData.CurrentLevel,
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

