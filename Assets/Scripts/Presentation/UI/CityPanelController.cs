namespace Presentation
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.UI;
    using GameState;
    using BusinessLogic;

    public class CityPanelController : MonoBehaviour
    {
        public static CityPanelController Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI cityNameText;
        
        [Header("Legacy/Combined Text")]
        [SerializeField] private TextMeshProUGUI statsText; // Keep for fallback
        
        [Header("Detailed Stats Text")]
        [SerializeField] private TextMeshProUGUI budgetIncomeText;
        [SerializeField] private TextMeshProUGUI ignitionRateText;
        [SerializeField] private TextMeshProUGUI recoveryRateText;

        [Header("Buttons")]
        [SerializeField] private Button sendFirefighterButton;
        [SerializeField] private Button policyButton;
        [SerializeField] private Button closeButton;

        private City currentCity;
        private ResourceManager resourceManager;
        private ProgressionManager progressionManager;
        private ScriptableObjects.EconomyConfig economyConfig;

        private void Awake()
        {
            Debug.Log($"[CityPanelController] Awake running on {gameObject.name}");
            if (Instance != null && Instance != this) {
                Debug.Log($"[CityPanelController] Duplicate found on {gameObject.name}, destroying. Instance is on {Instance.gameObject.name}");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (panelRoot != null) panelRoot.SetActive(false);

            if (closeButton != null) closeButton.onClick.AddListener(HidePanel);
            if (sendFirefighterButton != null) sendFirefighterButton.onClick.AddListener(OnSendFirefighterClicked);
            if (policyButton != null) policyButton.onClick.AddListener(OnPolicyClicked);
        }

        private void Start()
        {
            resourceManager = FindFirstObjectByType<ResourceManager>();
            progressionManager = FindFirstObjectByType<ProgressionManager>();
#if UNITY_EDITOR
            if (economyConfig == null) {
                economyConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObjects.EconomyConfig>("Assets/Sprites/ScriptableObjects/EconomyConfig.asset");
            }
#endif
        }

        private void Update()
        {
            if (currentCity != null && panelRoot != null && panelRoot.activeSelf) {
                UpdateStatsText();
            }
        }

        public void ShowPanel(City city)
        {
            Debug.Log($"[CityPanelController] ShowPanel triggered for city: {(city != null ? city.CityName : "null")}");
            if (city == null) return;
            currentCity = city;

            if (cityNameText != null) cityNameText.text = city.CityName;
            else Debug.LogWarning("[CityPanelController] cityNameText is null!");

            UpdateStatsText();

            if (panelRoot != null) {
                panelRoot.SetActive(true);
                Debug.Log($"[CityPanelController] panelRoot set to active. Current parent: {panelRoot.transform.parent.name}");
            } else {
                Debug.LogWarning("[CityPanelController] panelRoot is null! Cannot activate the panel.");
            }
        }

        private void UpdateStatsText()
        {
            if (currentCity == null) return;

            int currentLevel = progressionManager != null ? progressionManager.CurrentLevel : 1;
            
            // Income calculation
            float baseIncomeRate = economyConfig != null ? economyConfig.BaseIncomePerSecond : 1f;
            float increaseRate = economyConfig != null ? economyConfig.IncomeIncreasePerSecondPerLevel : 0.2f;
            float incomePerSec = baseIncomeRate + (currentLevel - 1) * increaseRate;

            // Stats
            float ignitionRate = progressionManager != null ? progressionManager.GetIgnitionRate() : 0.1f;
            float recoveryRate = progressionManager != null ? progressionManager.GetRecoveryRate() : 0.25f;

            // Policy UI staging 
            string ignitionPolicy = "";
            string recoveryPolicy = "";

            if (BusinessLogic.PolicyManager.Instance != null && BusinessLogic.PolicyManager.Instance.ForceShowUIModifiers) {
                ignitionPolicy = " <color=green>-20%</color>"; // Staging for active policies
                recoveryPolicy = " <color=green>+50%</color>"; // Staging for active policies
            }

            string budgetStr = $"Budget: ${currentCity.Budget}  <color=green>+${incomePerSec:F1} per sec</color>";
            string ignitionStr = $"Fire Ignition Rate: {ignitionRate:F2} {ignitionPolicy}";
            string recoveryStr = $"Land Recovery Rate: {recoveryRate:F2} {recoveryPolicy}";

            // Populate individual fields if they exist
            if (budgetIncomeText != null) budgetIncomeText.text = budgetStr;
            if (ignitionRateText != null) ignitionRateText.text = ignitionStr;
            if (recoveryRateText != null) recoveryRateText.text = recoveryStr;

            // Fallback to giant text blob if detailed texts aren't assigned
            if (budgetIncomeText == null && ignitionRateText == null && statsText != null) {
                statsText.text = $"{budgetStr}\n{ignitionStr}\n{recoveryStr}";
            }

            // Update button text
            if (sendFirefighterButton != null) {
                int cost = resourceManager != null ? resourceManager.GetDeploymentCost(currentLevel) : 5;
                var btnText = sendFirefighterButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) {
                    btnText.text = $"Firefighter\nCost: ${cost}";
                }
            }
        }

        public void HidePanel()
        {
            currentCity = null;
            if (panelRoot != null) panelRoot.SetActive(false);
            
            // Clear borders when panel is closed
            if (TerritoryRenderer.Instance != null)
            {
                TerritoryRenderer.Instance.ClearBorders();
            }
        }

        private void OnSendFirefighterClicked()
        {
            if (currentCity == null) return;
            Debug.Log($"[CityPanel] Sending firefighter from {currentCity.CityName}");
            if (resourceManager == null) {
                resourceManager = FindFirstObjectByType<ResourceManager>();
            }
            if (resourceManager != null) {
                resourceManager.DeployFirefighterFromCity(currentCity);
            } else {
                Debug.LogWarning("[CityPanel] ResourceManager not found in scene!");
            }
        }

        private void OnPolicyClicked()
        {
            if (currentCity == null) return;
            Debug.Log($"[CityPanel] Opening policy menu for {currentCity.CityName}");
            // Future logic: Trigger policy UI or enact 'Fire Ban'
        }
    }
}
