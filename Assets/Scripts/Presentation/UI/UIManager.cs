// ============================================================================
// UIManager.cs — In-game HUD: budget, reputation, round, and alert banner
// ============================================================================
// Attach to the Canvas or a UIManager GameObject in the Game scene.
// Wire all text and panel references in the Inspector.
// ============================================================================

namespace Presentation
{
    using System.Collections;
    using UnityEngine;
    using TMPro;

    public class UIManager : MonoBehaviour
    {
        // ── Inspector References ─────────────────────────────────────────────
        [Header("HUD Text")]
        [SerializeField] private TextMeshProUGUI roundText;

        [Header("Alert Banner")]
        [Tooltip("The root GameObject of the alert banner — set inactive by default.")]
        [SerializeField] private GameObject alertBanner;
        [SerializeField] private TextMeshProUGUI alertText;

        [Header("Level Display")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI firesCountText;

        [Header("Alert Settings")]
        [SerializeField] private float alertDuration = 3f;

        [Header("Managers")]
        [SerializeField] private BusinessLogic.ProgressionManager progressionManager;
        
        // ── Private State ────────────────────────────────────────────────────
        private Coroutine _alertCoroutine;
        private BusinessLogic.FireEngine fireEngine;

        // ── Unity Lifecycle ──────────────────────────────────────────────────
        private void OnEnable()
        {
            Core.EventBroker.Instance.Subscribe(Core.EventType.LevelUp, OnLevelUp);
        }

        private void OnDisable()
        {
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.LevelUp, OnLevelUp);
        }

        private void Start()
        {
            // Auto-find references to make setup easier
            if (progressionManager == null) progressionManager = FindFirstObjectByType<BusinessLogic.ProgressionManager>();
            if (fireEngine == null) fireEngine = FindFirstObjectByType<BusinessLogic.FireEngine>();

            // Hide alert banner on start
            if (alertBanner != null) alertBanner.SetActive(false);

            // Show default values
            UpdateRoundDisplay(1);
            
            // Set initial level
            int initialLevel = progressionManager != null ? progressionManager.CurrentLevel : 1;
            UpdateLevelDisplay(initialLevel);
        }

        private void Update()
        {
            if (firesCountText != null && fireEngine != null) {
                firesCountText.text = $"# of Fires: {fireEngine.BurningTileCount}";
            }
        }



        private void OnLevelUp(object data)
        {
            if (data is int level)
            {
                UpdateLevelDisplay(level);
                ShowAlert($"Level {level} Reached!");
            }
        }

        // ── HUD Updates ──────────────────────────────────────────────────────

        /// <summary>Updates the Round/Tick counter in the HUD.</summary>
        public void UpdateRoundDisplay(int round)
        {
            if (roundText != null)
                roundText.text = $"Round {round}";
        }

        /// <summary>Updates the Level counter in the HUD.</summary>
        public void UpdateLevelDisplay(int level)
        {
            if (levelText != null)
                levelText.text = $"Lvl: {level}";
        }

        /// <summary>Updates the Round Timer counter in the HUD.</summary>
        public void UpdateTimerDisplay(float timeRemaining)
        {
            if (timerText != null)
            {
                int seconds = Mathf.Max(0, Mathf.CeilToInt(timeRemaining));
                timerText.text = $"Time: {seconds}s";
            }
        }

        /// <summary>Convenience wrapper — updates HUD values.</summary>
        public void UpdateProgressionDisplay(int round)
        {
            UpdateRoundDisplay(round);
        }

        // ── Alert Banner ─────────────────────────────────────────────────────

        /// <summary>
        /// Shows a temporary alert banner with the given message.
        /// Auto-hides after <see cref="alertDuration"/> seconds.
        /// </summary>
        public void ShowAlert(string message)
        {
            if (alertBanner == null || alertText == null)
            {
                Debug.LogWarning("[UIManager] Alert banner or text not assigned in Inspector.");
                return;
            }

            // Cancel any currently running alert timer
            if (_alertCoroutine != null)
                StopCoroutine(_alertCoroutine);

            alertText.text = message;
            alertBanner.SetActive(true);
            _alertCoroutine = StartCoroutine(HideAlertAfterDelay());
        }

        private IEnumerator HideAlertAfterDelay()
        {
            yield return new WaitForSecondsRealtime(alertDuration);
            if (alertBanner != null) alertBanner.SetActive(false);
        }

        // ── Panel Stubs ──────────────────────────────────────────────────────
        // These will be wired up when those panels are built.

        public void ShowPolicyPanel()    => Debug.Log("[UIManager] Policy panel — coming soon.");
        public void ShowDeploymentPanel() => Debug.Log("[UIManager] Deployment panel — coming soon.");
        public void ShowFinalResult(object e) => Debug.Log($"[UIManager] Game ended: {e}");
    }
}
