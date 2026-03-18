namespace Presentation {
    using GameState;
    using Core;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class UIManager : MonoBehaviour {
        [SerializeField] private PlayerProgression progression;
        
        [Header("UI Panel References")]
        [SerializeField] private GameObject pausePanel;

        [Header("UI Text References")]
        [SerializeField] private TMPro.TextMeshProUGUI budgetText;
        [SerializeField] private TMPro.TextMeshProUGUI reputationText;

        private void Start() {
            // EventBroker.Instance.Subscribe(EventType.BudgetChanged, UpdateBudgetDisplay);
            
            // For now, let's just test that the linking works:
            UpdateBudgetDisplay(1000);
            UpdateReputationDisplay(50);
            
            // Ensure the pause menu is hidden when the game starts
            if (pausePanel != null) {
                pausePanel.SetActive(false);
            }
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

        // --- PAUSE MENU LOGIC --- //

        // Linked to the "PAUSE" button on the HUD
        public void ShowPauseMenu() { 
            if (pausePanel != null) {
                pausePanel.SetActive(true); // Turn on the visual panel
                
                // Find the GameManager and tell it to stop time
                GameManager gm = FindObjectOfType<GameManager>();
                if (gm != null) gm.PauseGame();
            }
        }

        // Linked to the "Resume Game" button inside the Pause Panel
        public void ResumeGameClicked() {
            if (pausePanel != null) {
                pausePanel.SetActive(false); // Turn off the visual panel
                
                // Find the GameManager and tell it to resume time
                GameManager gm = FindObjectOfType<GameManager>();
                if (gm != null) gm.ResumeGame();
            }
        }

        // Linked to the "Save" button inside the Pause Panel
        public void SaveGameClicked() {
            Persistence.SaveManager saveManager = FindObjectOfType<Persistence.SaveManager>();
            if (saveManager != null) {
                saveManager.SaveFile();
                Debug.Log("Game Saved via UI!");
            } else {
                Debug.LogWarning("SaveManager not found in scene!");
            }
        }

        // Linked to the "Quit to Menu" button inside the Pause Panel
        public void QuitToMenuClicked() {
            // First, ensure time is unpaused before leaving, otherwise the main menu will be frozen!
            Time.timeScale = 1f; 
            SceneManager.LoadScene("MainMenu");
        }

        public void UpdateProgressionDisplay() { }
        public void ShowAlert(string message) { }
        public void ShowPolicyPanel() { }
        public void ShowDeploymentPanel() { }
        public void ShowFinalResult(object e) { }
    }
}
