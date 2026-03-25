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
        [SerializeField] private TextMeshProUGUI budgetText;
        [SerializeField] private TextMeshProUGUI reputationText;
        [SerializeField] private TextMeshProUGUI roundText;

        [Header("Alert Banner")]
        [Tooltip("The root GameObject of the alert banner — set inactive by default.")]
        [SerializeField] private GameObject alertBanner;
        [SerializeField] private TextMeshProUGUI alertText;

        [Header("Level Display")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Alert Settings")]
        [SerializeField] private float alertDuration = 3f;

        [Header("Managers")]
        [SerializeField] private BusinessLogic.ProgressionManager progressionManager;

        // ── Private State ────────────────────────────────────────────────────
        private Coroutine _alertCoroutine;

        // ── Unity Lifecycle ──────────────────────────────────────────────────
        private void OnEnable()
        {
            Core.EventBroker.Instance.Subscribe(Core.EventType.LevelUp, OnLevelUp);
            Core.EventBroker.Instance.Subscribe(Core.EventType.BudgetChanged, OnBudgetChanged);
        }

        private void OnDisable()
        {
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.LevelUp, OnLevelUp);
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.BudgetChanged, OnBudgetChanged);
        }

        private void Start()
        {
            // Auto-find references to make setup easier
            if (progressionManager == null) progressionManager = FindFirstObjectByType<BusinessLogic.ProgressionManager>();

            // Hide alert banner on start
            if (alertBanner != null) alertBanner.SetActive(false);

            // Show default values
            UpdateBudgetDisplay(1000);
            UpdateReputationDisplay(50);
            UpdateRoundDisplay(1);
            
            // Set initial level
            int initialLevel = progressionManager != null ? progressionManager.CurrentLevel : 1;
            UpdateLevelDisplay(initialLevel);
        }

        private void OnBudgetChanged(object data)
        {
            if (data is int budget)
                UpdateBudgetDisplay(budget);
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

        /// <summary>Updates the Budget text in the HUD.</summary>
        public void UpdateBudgetDisplay(int currentBudget)
        {
            if (budgetText != null)
                budgetText.text = $"💰 ${currentBudget}";
        }

        /// <summary>Updates the Reputation text in the HUD.</summary>
        public void UpdateReputationDisplay(int currentReputation)
        {
            if (reputationText != null)
                reputationText.text = $"⭐ {currentReputation}";
        }

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

        /// <summary>Convenience wrapper — updates all three HUD values at once.</summary>
        public void UpdateProgressionDisplay(int budget, int reputation, int round)
        {
            UpdateBudgetDisplay(budget);
            UpdateReputationDisplay(reputation);
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
