namespace Presentation {
    using Core;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.InputSystem;
    using UnityEngine.SceneManagement;

    public class PauseMenuController : MonoBehaviour {
        [SerializeField] private GameObject pauseButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button pauseBtn;
        [SerializeField] private Button resumeBtn;
        [SerializeField] private Button saveBtn;
        [SerializeField] private Button quitBtn;

        private bool isPaused;

        private void Start() {
            // Auto-find objects if not assigned in Inspector
            var canvas = GameObject.Find("Canvas");
            if (canvas != null) {
                if (pauseButton == null) pauseButton = canvas.transform.Find("PauseButton")?.gameObject;
                if (pausePanel == null) pausePanel = canvas.transform.Find("PausePanel")?.gameObject;
            }

            if (pauseButton != null && pauseBtn == null)
                pauseBtn = pauseButton.GetComponent<Button>();
            if (pausePanel != null) {
                if (resumeBtn == null) resumeBtn = pausePanel.transform.Find("ResumeButton")?.GetComponent<Button>();
                if (saveBtn == null) saveBtn = pausePanel.transform.Find("SaveButton")?.GetComponent<Button>();
                if (quitBtn == null) quitBtn = pausePanel.transform.Find("QuitButton")?.GetComponent<Button>();
            }

            // Wire button clicks
            if (pauseBtn != null) pauseBtn.onClick.AddListener(PauseGame);
            if (resumeBtn != null) resumeBtn.onClick.AddListener(ResumeGame);
            if (saveBtn != null) saveBtn.onClick.AddListener(SaveGame);
            if (quitBtn != null) quitBtn.onClick.AddListener(QuitGame);

            SetPaused(false);
        }

        private void Update() {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) {
                if (isPaused) ResumeGame();
                else PauseGame();
            }
        }

        private void PauseGame() {
            SetPaused(true);
            var gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.PauseGame();
            else Time.timeScale = 0f;
        }

        private void ResumeGame() {
            SetPaused(false);
            var gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.ResumeGame();
            else Time.timeScale = 1f;
        }

        private void SaveGame() {
            var saveManager = FindObjectOfType<Persistence.SaveManager>();
            if (saveManager != null) {
                saveManager.SaveFile();
                Debug.Log("Game Saved!");
            } else {
                Debug.LogWarning("SaveManager not found in scene!");
            }
        }

        private void QuitGame() {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private void SetPaused(bool paused) {
            isPaused = paused;
            if (pausePanel != null) pausePanel.SetActive(paused);
            if (pauseButton != null) pauseButton.SetActive(!paused);
        }
    }
}
