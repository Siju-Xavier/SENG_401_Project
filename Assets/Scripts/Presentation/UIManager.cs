namespace Presentation {
    using GameState;
    using Core;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class UIManager : MonoBehaviour {
        [SerializeField] private PlayerProgression progression;

        [Header("UI Text References")]
        [SerializeField] private TMPro.TextMeshProUGUI budgetText;
        [SerializeField] private TMPro.TextMeshProUGUI reputationText;

        private void Start() {
            // EventBroker.Instance.Subscribe(EventType.BudgetChanged, UpdateBudgetDisplay);

            UpdateBudgetDisplay(1000);
            UpdateReputationDisplay(50);
        }

        public void UpdateBudgetDisplay(int currentBudget) {
            if (budgetText != null) {
                budgetText.text = $"Budget: ${currentBudget}";
            }
        }

        public void UpdateReputationDisplay(int currentReputation) {
            if (reputationText != null) {
                reputationText.text = $"Reputation: {currentReputation}";
            }
        }

        public void UpdateProgressionDisplay() { }
        public void ShowAlert(string message) { }
        public void ShowPolicyPanel() { }
        public void ShowDeploymentPanel() { }
        public void ShowFinalResult(object e) { }
    }
}
