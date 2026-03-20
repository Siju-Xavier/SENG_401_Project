namespace BusinessLogic {
    using UnityEngine;
    using Persistence;

    public class AutoSaveController : MonoBehaviour {
        [SerializeField] private float autoSaveInterval = 60f;
        [SerializeField] private SaveManager saveManager;

        private bool isRunning;

        /// <summary>Start the auto-save loop at the configured interval.</summary>
        public void Run() {
            if (isRunning) return;
            isRunning = true;
            InvokeRepeating(nameof(TriggerAutoSave), autoSaveInterval, autoSaveInterval);
            Debug.Log($"[AutoSave] Started — interval {autoSaveInterval}s.");
        }

        /// <summary>
        /// Gathers game state and saves via the active IStorageProvider.
        /// Called automatically by the repeating timer, or manually.
        /// </summary>
        public void TriggerAutoSave() {
            if (saveManager == null) {
                Debug.LogWarning("[AutoSave] Missing SaveManager reference.");
                return;
            }

            Debug.Log("[AutoSave] Triggering auto-save...");
            saveManager.SaveFile();
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

