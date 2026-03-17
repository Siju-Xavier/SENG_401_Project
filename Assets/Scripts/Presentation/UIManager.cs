namespace Presentation {
    using GameState;
    using Core;
    using UnityEngine;

    public class UIManager : MonoBehaviour {
        [SerializeField] private PlayerProgression progression;

        private void Start() {
            // EventBroker.Instance.Subscribe(EventType.BudgetChanged, UpdateBudgetDisplay);
        }

        public void UpdateBudgetDisplay() { }
        public void UpdateReputationDisplay() { }
        public void UpdateProgressionDisplay() { }
        public void ShowAlert(string message) { }
        public void ShowPolicyPanel() { }
        public void ShowDeploymentPanel() { }
        public void ShowPauseMenu() { }
        public void ShowFinalResult(object e) { }
    }
}
