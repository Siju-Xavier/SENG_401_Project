namespace BusinessLogic {
    using UnityEngine;
    using Core;
    using Persistence;

    public class AutoSaveController : MonoBehaviour {
        [SerializeField] private float autoSaveInterval = 60f;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private GameManager gameManager;

        private bool isRunning;

        /// <summary>Start the auto-save loop at the configured interval.</summary>
        public void Run() {
            if (isRunning) return;
            isRunning = true;
            InvokeRepeating(nameof(TriggerAutoSave), autoSaveInterval, autoSaveInterval);
            Debug.Log($"[AutoSave] Started — interval {autoSaveInterval}s.");
        }

        /// <summary>
        /// Gathers game state and saves to both local file and cloud.
        /// Called automatically by the repeating timer, or manually.
        /// </summary>
        public void TriggerAutoSave() {
            if (gameManager == null || saveManager == null) {
                Debug.LogWarning("[AutoSave] Missing GameManager or SaveManager reference.");
                return;
            }

            Debug.Log("[AutoSave] Triggering auto-save...");

            // Save to local file
            saveManager.SaveFile();

            // Also save to cloud
            var data = gameManager.BuildSaveData();
            int playerId = gameManager.PlayerId;
            StartCoroutine(saveManager.SaveToCloud(playerId, data,
                slotName: "autosave", displayName: "Auto Save"));
        }

        /// <summary>Stop the auto-save loop.</summary>
        public void Stop() {
            if (!isRunning) return;
            isRunning = false;
            CancelInvoke(nameof(TriggerAutoSave));
            Debug.Log("[AutoSave] Stopped.");
        }
    }
}

