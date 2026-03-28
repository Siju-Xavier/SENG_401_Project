namespace Presentation
{
    using Core;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.InputSystem;

    public class PauseMenuController : MonoBehaviour
    {
        [Header("Pause")]
        [SerializeField] private GameObject pauseButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button pauseBtn;
        [SerializeField] private Button resumeBtn;
        [SerializeField] private Button settingsBtn;
        [SerializeField] private Button saveBtn;
        [SerializeField] private Button mainMenuBtn;

        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button closeSettingsBtn;

        [Header("Save Panel")]
        [SerializeField] private GameObject savePanel;
        [SerializeField] private Button closeSaveBtn;

        private bool isPaused;
        private BusinessLogic.GameOverManager gameOverManager;

        private void Start()
        {
            gameOverManager = FindFirstObjectByType<BusinessLogic.GameOverManager>();
            var canvas = GameObject.Find("CityCanvas") ?? GameObject.Find("Canvas");
            if (canvas != null)
            {
                if (pauseButton == null) pauseButton = (canvas.transform.Find("PopArt_PauseButton") ?? canvas.transform.Find("PauseButton"))?.gameObject;
                if (pausePanel  == null) pausePanel  = (canvas.transform.Find("PopArt_PausePanel") ?? canvas.transform.Find("PausePanel"))?.gameObject;
                if (settingsPanel == null) settingsPanel = (canvas.transform.Find("PopArt_SettingsPanel") ?? canvas.transform.Find("SettingsPanel"))?.gameObject;
                if (savePanel == null) savePanel = (canvas.transform.Find("PopArt_SavePanel") ?? canvas.transform.Find("SavePanel"))?.gameObject;
            }

            if (pauseButton != null && pauseBtn == null)
                pauseBtn = pauseButton.GetComponent<Button>();

            if (pausePanel != null)
            {
                if (resumeBtn == null) resumeBtn = FindButton(pausePanel, "ResumeButton");
                if (settingsBtn == null) settingsBtn = FindButton(pausePanel, "SettingsButton");
                if (saveBtn == null) saveBtn = FindButton(pausePanel, "SaveButton");
                if (mainMenuBtn == null) mainMenuBtn = FindButton(pausePanel, "MainMenuButton")
                                                    ?? FindButton(pausePanel, "QuitButton");
            }

            if (settingsPanel != null && closeSettingsBtn == null)
                closeSettingsBtn = FindButton(settingsPanel, "CloseSettingsButton")
                                ?? FindButton(settingsPanel, "CloseButton");

            if (savePanel != null && closeSaveBtn == null)
                closeSaveBtn = FindButton(savePanel, "CloseSaveButton")
                            ?? FindButton(savePanel, "CloseButton");

            // Wire button clicks
            if (pauseBtn != null) pauseBtn.onClick.AddListener(PauseGame);
            if (resumeBtn != null) resumeBtn.onClick.AddListener(ResumeGame);
            if (settingsBtn != null) settingsBtn.onClick.AddListener(OpenSettings);
            if (saveBtn != null) saveBtn.onClick.AddListener(OpenSavePanel);
            if (mainMenuBtn != null) mainMenuBtn.onClick.AddListener(GoToMainMenu);
            if (closeSettingsBtn != null) closeSettingsBtn.onClick.AddListener(CloseSettings);
            if (closeSaveBtn != null) closeSaveBtn.onClick.AddListener(CloseSavePanel);

            SetPaused(false);
        }

        private void Update()
        {
            bool escNew = false;
            if (Keyboard.current != null)
                escNew = Keyboard.current.escapeKey.wasPressedThisFrame;

            bool escOld = Input.GetKeyDown(KeyCode.Escape);

            if (escNew || escOld)
                TogglePause();
        }

        private void TogglePause()
        {
            // Don't allow pause toggle when game is over
            if (gameOverManager != null && gameOverManager.IsGameOver) return;

            // Don't allow pause during tutorial
            var tutorial = FindFirstObjectByType<TutorialManager>();
            if (tutorial != null && tutorial.IsTutorialActive) return;

            // If a sub-panel is open, close it back to pause panel
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
                return;
            }
            if (savePanel != null && savePanel.activeSelf)
            {
                CloseSavePanel();
                return;
            }

            if (isPaused) ResumeGame();
            else          PauseGame();
        }

        public void PauseGame()
        {
            // Don't allow pausing when game is over
            if (gameOverManager != null && gameOverManager.IsGameOver) return;

            SetPaused(true);
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null) gm.PauseGame();
        }

        public void ResumeGame()
        {
            SetPaused(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (savePanel != null) savePanel.SetActive(false);
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null) gm.ResumeGame();
        }

        public void OpenSettings()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        public void OpenSavePanel()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (savePanel != null) savePanel.SetActive(true);
            else Debug.LogWarning("[PauseMenuController] Cannot open Save Panel because it is NULL. Assign it in the Inspector!");
        }

        public void CloseSavePanel()
        {
            if (savePanel != null) savePanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneLoader.LoadScene("Login");
        }

        private void SetPaused(bool paused)
        {
            isPaused = paused;
            if (pausePanel  != null) pausePanel.SetActive(paused);
            if (pauseButton != null) pauseButton.SetActive(!paused);
        }

        private static Button FindButton(GameObject parent, string name)
        {
            // Search in known container layouts, then anywhere in children
            var t = parent.transform.Find("Container/" + name)
                 ?? parent.transform.Find("PausePanelContainer/" + name)
                 ?? parent.transform.Find(name);
            if (t == null)
            {
                foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name)
                        return child.GetComponent<Button>();
                }
            }
            return t != null ? t.GetComponent<Button>() : null;
        }
    }
}
