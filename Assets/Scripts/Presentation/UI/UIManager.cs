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

        [Header("Game Over Panel")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverStatsText;
        [SerializeField] private UnityEngine.UI.Button mainMenuButton;
        [SerializeField] private UnityEngine.UI.Button retryButton;

        [Header("Alert Settings")]
        [SerializeField] private float alertDuration = 3f;
        [SerializeField] private float destructionAlertDuration = 5f;

        [Header("Managers")]
        [SerializeField] private BusinessLogic.ProgressionManager progressionManager;
        
        // ── Private State ────────────────────────────────────────────────────
        private Coroutine _alertCoroutine;
        private BusinessLogic.FireEngine fireEngine;

        // ── Unity Lifecycle ──────────────────────────────────────────────────
        private void OnEnable()
        {
            Core.EventBroker.Instance.Subscribe(Core.EventType.LevelUp, OnLevelUp);
            Core.EventBroker.Instance.Subscribe(Core.EventType.CityInDanger, OnCityInDanger);
            Core.EventBroker.Instance.Subscribe(Core.EventType.CityCritical, OnCityCritical);
            Core.EventBroker.Instance.Subscribe(Core.EventType.CityDestroyed, OnCityDestroyed);
        }

        private void OnDisable()
        {
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.LevelUp, OnLevelUp);
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.CityInDanger, OnCityInDanger);
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.CityCritical, OnCityCritical);
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.CityDestroyed, OnCityDestroyed);
        }

        private void Start()
        {
            // Auto-find references to make setup easier
            if (progressionManager == null) progressionManager = FindFirstObjectByType<BusinessLogic.ProgressionManager>();
            if (fireEngine == null) fireEngine = FindFirstObjectByType<BusinessLogic.FireEngine>();

            // Hide alert banner and game over panel on start
            if (alertBanner != null) alertBanner.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            // Wire game over buttons
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);

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
        public void ShowAlert(string message, float duration = -1f)
        {
            // Auto-create alert banner if not wired in Inspector
            if (alertBanner == null || alertText == null)
                CreateAlertBanner();

            if (alertBanner == null || alertText == null) return;

            // Cancel any currently running alert timer
            if (_alertCoroutine != null)
                StopCoroutine(_alertCoroutine);

            alertText.text = message;
            alertBanner.SetActive(true);
            float dur = duration > 0f ? duration : alertDuration;
            _alertCoroutine = StartCoroutine(HideAlertAfterDelay(dur));
        }

        private IEnumerator HideAlertAfterDelay(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            if (alertBanner != null) alertBanner.SetActive(false);
        }

        private void CreateAlertBanner()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Banner background — anchored to top-center
            alertBanner = new GameObject("AlertBanner");
            alertBanner.transform.SetParent(canvas.transform, false);
            var rt = alertBanner.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.85f);
            rt.anchorMax = new Vector2(0.8f, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = alertBanner.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.85f, 0.15f, 0.1f, 0.9f);

            // Alert text
            var textGO = new GameObject("AlertText");
            textGO.transform.SetParent(alertBanner.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10f, 0f);
            textRT.offsetMax = new Vector2(-10f, 0f);
            alertText = textGO.AddComponent<TextMeshProUGUI>();
            alertText.alignment = TMPro.TextAlignmentOptions.Center;
            alertText.fontSize = 22;
            alertText.color = Color.white;
            alertText.fontStyle = TMPro.FontStyles.Bold;

            alertBanner.SetActive(false);
        }

        // ── City Danger / Destruction Alerts ─────────────────────────────────

        private void OnCityInDanger(object data)
        {
            if (data is GameState.City city)
                ShowAlert($"{city.CityName} is under threat! Deploy firefighters now!");
        }

        private void OnCityCritical(object data)
        {
            if (data is GameState.City city)
                ShowAlert($"{city.CityName} is burning! Act now or lose the city!");
        }

        private void OnCityDestroyed(object data)
        {
            if (data is GameState.City city)
                ShowAlert($"{city.CityName} has been lost to the fire.", destructionAlertDuration);
        }

        // ── Game Over ───────────────────────────────────────────────────────

        /// <summary>Show the game over panel with final stats.</summary>
        public void ShowGameOver(int roundReached, int levelReached)
        {
            // Hide the alert banner so it doesn't sit on top of the game over panel
            if (_alertCoroutine != null) StopCoroutine(_alertCoroutine);
            if (alertBanner != null) alertBanner.SetActive(false);

            // Create panel at runtime if not wired in Inspector
            if (gameOverPanel == null)
                CreateGameOverPanel();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                // Ensure game over panel renders on top of everything
                gameOverPanel.transform.SetAsLastSibling();

                if (gameOverStatsText != null)
                {
                    // Build destruction summary
                    var gom = FindFirstObjectByType<BusinessLogic.GameOverManager>();
                    string cityList = "";
                    if (gom != null && gom.DestructionLog.Count > 0) {
                        foreach (var record in gom.DestructionLog) {
                            cityList += $"\n  {record.CityName} — fell at Level {record.Level}";
                        }
                    }

                    gameOverStatsText.text =
                        $"All cities have fallen.\n" +
                        cityList + "\n\n" +
                        $"Level Reached: {levelReached}";
                }
            }
        }

        /// <summary>Creates a simple game over panel at runtime when not assigned in Inspector.</summary>
        private void CreateGameOverPanel()
        {
            // Find or create a canvas
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Dark overlay
            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(canvas.transform, false);
            var rt = gameOverPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = gameOverPanel.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0f, 0f, 0f, 0.85f);

            // Stats text
            var statsGO = new GameObject("StatsText");
            statsGO.transform.SetParent(gameOverPanel.transform, false);
            var statsRT = statsGO.AddComponent<RectTransform>();
            statsRT.anchorMin = new Vector2(0.2f, 0.35f);
            statsRT.anchorMax = new Vector2(0.8f, 0.75f);
            statsRT.offsetMin = Vector2.zero;
            statsRT.offsetMax = Vector2.zero;
            gameOverStatsText = statsGO.AddComponent<TextMeshProUGUI>();
            gameOverStatsText.alignment = TMPro.TextAlignmentOptions.Center;
            gameOverStatsText.fontSize = 28;
            gameOverStatsText.color = Color.white;

            // Main Menu button
            CreateButton(gameOverPanel.transform, "Main Menu", new Vector2(0.3f, 0.15f), new Vector2(0.5f, 0.25f), OnMainMenuClicked);

            // Retry button
            CreateButton(gameOverPanel.transform, "Retry", new Vector2(0.5f, 0.15f), new Vector2(0.7f, 0.25f), OnRetryClicked);
        }

        private void CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
        {
            var btnGO = new GameObject(label + "Button");
            btnGO.transform.SetParent(parent, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = anchorMin;
            btnRT.anchorMax = anchorMax;
            btnRT.offsetMin = Vector2.zero;
            btnRT.offsetMax = Vector2.zero;

            var btnImg = btnGO.AddComponent<UnityEngine.UI.Image>();
            btnImg.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            var btn = btnGO.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(onClick);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontSize = 20;
            tmp.color = Color.white;
        }

        private bool sceneLoadRequested;

        private void OnMainMenuClicked()
        {
            if (sceneLoadRequested) return;
            sceneLoadRequested = true;
            Time.timeScale = 1f;
            SceneLoader.LoadScene("MainMenu");
        }

        private void OnRetryClicked()
        {
            if (sceneLoadRequested) return;
            sceneLoadRequested = true;
            Time.timeScale = 1f;
            SceneLoader.LoadScene("Game 1");
        }

        // ── Panel Stubs ──────────────────────────────────────────────────────

        public void ShowPolicyPanel()    => Debug.Log("[UIManager] Policy panel — coming soon.");
        public void ShowDeploymentPanel() => Debug.Log("[UIManager] Deployment panel — coming soon.");
        public void ShowFinalResult(object e) => Debug.Log($"[UIManager] Game ended: {e}");
    }
}
