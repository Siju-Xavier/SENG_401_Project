// ============================================================================
// PauseMenuController.cs — In-game pause menu (ESC key or Pause button)
// ============================================================================
// Attach to any persistent GameObject in the Game scene.
// Auto-finds PauseButton and PausePanel by name if not set in the Inspector.
// ============================================================================

namespace Presentation
{
    using Core;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.InputSystem;
    using UnityEngine.SceneManagement;

    public class PauseMenuController : MonoBehaviour
    {
        // ── Inspector References ─────────────────────────────────────────────
        [SerializeField] private GameObject pauseButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button pauseBtn;
        [SerializeField] private Button resumeBtn;
        [SerializeField] private Button settingsBtn;
        [SerializeField] private Button quitBtn;

        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeSettingsBtn;

        private bool isPaused;

        // ── Unity Lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            // Auto-find objects if not assigned in Inspector
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                if (pauseButton == null) pauseButton = canvas.transform.Find("PauseButton")?.gameObject;
                if (pausePanel  == null) pausePanel  = canvas.transform.Find("PausePanel")?.gameObject;
            }

            if (pauseButton != null && pauseBtn == null)
                pauseBtn = pauseButton.GetComponent<Button>();

            if (pausePanel != null)
            {
                if (resumeBtn == null) resumeBtn = pausePanel.transform.Find("PausePanelContainer/ResumeButton")?.GetComponent<Button>()
                                                ?? pausePanel.transform.Find("ResumeButton")?.GetComponent<Button>();
                if (settingsBtn == null) settingsBtn = pausePanel.transform.Find("PausePanelContainer/SettingsButton")?.GetComponent<Button>()
                                                ?? pausePanel.transform.Find("SettingsButton")?.GetComponent<Button>();
                if (quitBtn == null) quitBtn = pausePanel.transform.Find("PausePanelContainer/QuitButton")?.GetComponent<Button>()
                                                ?? pausePanel.transform.Find("QuitButton")?.GetComponent<Button>();
            }

            // Wire button clicks
            if (pauseBtn  != null) pauseBtn.onClick.AddListener(PauseGame);
            if (resumeBtn != null) resumeBtn.onClick.AddListener(ResumeGame);
            if (settingsBtn != null) settingsBtn.onClick.AddListener(OpenSettings);
            if (quitBtn   != null) quitBtn.onClick.AddListener(QuitGame);
            if (closeSettingsBtn != null) closeSettingsBtn.onClick.AddListener(CloseSettings);

            SetPaused(false);
        }

        private void Update()
        {
            // NEW Input System check
            bool escNew = false;
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                escNew = UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
                if (escNew) Debug.Log("[PauseMenu] NEW Input System detected ESCAPE");
            }

            // OLD Input System check (fallback)
            bool escOld = Input.GetKeyDown(KeyCode.Escape);
            if (escOld) Debug.Log("[PauseMenu] OLD Input System detected ESCAPE");

            if (escNew || escOld)
            {
                TogglePause();
            }
        }

        private void TogglePause()
        {
            if (isPaused) ResumeGame();
            else          PauseGame();
        }

        // ── Public Methods (wired to buttons) ────────────────────────────────

        public void PauseGame()
        {
            SetPaused(true);
            Debug.Log("HERE HERE");
            var gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.PauseGame();
            else throw new System.Exception("GameManager not found in scene!");
        }

        public void ResumeGame()
        {
            SetPaused(false);
            var gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.ResumeGame();
            else throw new System.Exception("GameManager not found in scene!");
        }

        public void OpenSettings()
        {
            Debug.Log("[PauseMenu] Opening Settings...");
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(!settingsPanel.activeSelf);
            }
            else
            {
                Debug.LogWarning("[PauseMenu] No settings panel assigned!");
            }
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;
            SceneLoader.LoadScene("MainMenu");
        }

        // ── Private Helpers ──────────────────────────────────────────────────

        private void SetPaused(bool paused)
        {
            isPaused = paused;
            if (pausePanel  != null) pausePanel.SetActive(paused);
            if (pauseButton != null) pauseButton.SetActive(!paused);
        }
    }
}
