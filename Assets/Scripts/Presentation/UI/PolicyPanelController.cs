namespace Presentation
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using GameState;
    using BusinessLogic;
    using ScriptableObjects;

    public class PolicyPanelController : MonoBehaviour
    {
        [Header("Policies (assign 3 PolicyConfig assets)")]
        [SerializeField] private PolicyConfig policy1;
        [SerializeField] private PolicyConfig policy2;
        [SerializeField] private PolicyConfig policy3;

        [Header("Policy Buttons (replace SendFirefighter & PolicyButton)")]
        [SerializeField] private Button button1;
        [SerializeField] private Button button2;
        [SerializeField] private Button button3;

        [Header("Info Texts")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI budgetText;
        [SerializeField] private TextMeshProUGUI ignitionRateText;
        [SerializeField] private TextMeshProUGUI recoveryRateText;

        [Header("Panel")]
        [SerializeField] private Button closeButton;

        private City currentCity;
        private Region currentRegion;
        private GridSystem gridSystem;
        private ProgressionManager progressionManager;
        private EconomyConfig economyConfig;

        private static readonly Color ACTIVE_COLOR = new Color(0.2f, 0.7f, 0.2f, 1f);
        private static readonly Color INACTIVE_COLOR = new Color(0.8f, 0.25f, 0.25f, 1f);
        private static readonly Color DISABLED_COLOR = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        private bool listenersWired;

        private void Awake()
        {
            WireListeners();
        }

        private void OnEnable()
        {
            WireListeners();
            gridSystem = FindFirstObjectByType<GridSystem>();
            progressionManager = FindFirstObjectByType<ProgressionManager>();
#if UNITY_EDITOR
            if (economyConfig == null)
                economyConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<EconomyConfig>("Assets/Sprites/ScriptableObjects/EconomyConfig.asset");
#endif
        }

        private void WireListeners()
        {
            if (listenersWired) return;
            listenersWired = true;

            if (closeButton != null)
                closeButton.onClick.AddListener(OnBackClicked);
            if (button1 != null) button1.onClick.AddListener(() => TogglePolicy(policy1));
            if (button2 != null) button2.onClick.AddListener(() => TogglePolicy(policy2));
            if (button3 != null) button3.onClick.AddListener(() => TogglePolicy(policy3));
        }

        public void Show(City city)
        {
            currentCity = city;
            currentRegion = FindRegionForCity(city);
            gameObject.SetActive(true);
            RefreshUI();
        }

        private void Update()
        {
            if (currentCity != null && gameObject.activeSelf)
                RefreshUI();
        }

        private void RefreshUI()
        {
            if (currentCity == null) return;

            int cityCount = gridSystem != null ? gridSystem.Regions.Count : 1;
            int level = progressionManager != null ? progressionManager.CurrentLevel : 1;

            // Update info texts
            if (titleText != null) titleText.text = currentCity.CityName + " - Policies";

            if (budgetText != null)
            {
                float baseIncome = economyConfig != null ? economyConfig.BaseIncomePerSecond : 1f;
                float incRate = economyConfig != null ? economyConfig.IncomeIncreasePerSecondPerLevel : 0.2f;
                float income = baseIncome + (level - 1) * incRate;
                budgetText.text = $"Budget: ${currentCity.Budget}  <color=green>+${income:F1}/sec</color>";
            }

            if (ignitionRateText != null)
            {
                float ignRate = progressionManager != null ? progressionManager.GetIgnitionRate() : 0.1f;
                string mod = "";
                if (PolicyManager.Instance != null && currentRegion != null)
                {
                    float spawnMod = PolicyManager.Instance.GetSpawnModifierForRegion(currentRegion);
                    if (spawnMod < 1f) mod = $" <color=green>-{(1f - spawnMod) * 100:F0}%</color>";
                }
                ignitionRateText.text = $"Fire Ignition: {ignRate:F2}{mod}";
            }

            if (recoveryRateText != null)
            {
                float recRate = progressionManager != null ? progressionManager.GetRecoveryRate() : 0.25f;
                string mod = "";
                if (PolicyManager.Instance != null && currentRegion != null)
                {
                    float bonus = PolicyManager.Instance.GetRecoveryBonusForRegion(currentRegion);
                    if (bonus > 0f) mod = $" <color=green>+{bonus * 100:F0}%</color>";
                }
                recoveryRateText.text = $"Land Recovery: {recRate:F2}{mod}";
            }

            // Update buttons
            UpdateButton(button1, policy1, cityCount);
            UpdateButton(button2, policy2, cityCount);
            UpdateButton(button3, policy3, cityCount);
        }

        private void UpdateButton(Button btn, PolicyConfig policy, int cityCount)
        {
            if (btn == null || policy == null) return;

            bool isActive = PolicyManager.Instance != null
                         && PolicyManager.Instance.IsPolicyActive(policy, currentRegion);
            bool needsMoreCities = policy.RequiresMultipleCities && cityCount <= 1;
            int level = progressionManager != null ? progressionManager.CurrentLevel : 1;
            bool levelTooLow = level < policy.RequiredLevel;
            bool isDisabled = needsMoreCities || levelTooLow;

            var img = btn.GetComponent<Image>();
            var txt = btn.GetComponentInChildren<TextMeshProUGUI>();

            // Cost + level string
            string costLine;
            if (policy.CostPerSecond > 0)
                costLine = $"Cost: ${policy.CostPerSecond:F1}/sec | Lvl {policy.RequiredLevel}";
            else if (policy.IncomeRedistributionRate > 0)
                costLine = $"Gives {policy.IncomeRedistributionRate * 100:F0}% income | Lvl {policy.RequiredLevel}";
            else
                costLine = $"Free | Lvl {policy.RequiredLevel}";

            if (isDisabled)
            {
                btn.interactable = false;
                if (img != null) img.color = DISABLED_COLOR;
                string reason = levelTooLow
                    ? $"(Requires Lvl {policy.RequiredLevel})"
                    : "(Requires 2+ cities)";
                if (txt != null) txt.text = $"{policy.PolicyName}\n<size=12>{reason}</size>";
            }
            else
            {
                btn.interactable = true;
                if (img != null) img.color = isActive ? ACTIVE_COLOR : INACTIVE_COLOR;
                if (txt != null) txt.text = $"{policy.PolicyName}\n<size=12>{costLine}</size>";
            }
        }

        private void TogglePolicy(PolicyConfig policy)
        {
            Debug.Log($"[PolicyPanel] TogglePolicy called. policy={policy?.PolicyName ?? "NULL"}, region={currentRegion?.RegionName ?? "NULL"}, PM={PolicyManager.Instance != null}");

            if (policy == null) { Debug.LogWarning("[PolicyPanel] policy is null"); return; }
            if (PolicyManager.Instance == null) { Debug.LogWarning("[PolicyPanel] PolicyManager.Instance is null"); return; }
            if (currentRegion == null)
            {
                // Try to re-find region
                currentRegion = FindRegionForCity(currentCity);
                Debug.Log($"[PolicyPanel] Re-found region: {currentRegion?.RegionName ?? "STILL NULL"}, city={currentCity?.CityName ?? "NULL"}");
                if (currentRegion == null) { Debug.LogWarning("[PolicyPanel] currentRegion is null"); return; }
            }

            bool wasActive = PolicyManager.Instance.IsPolicyActive(policy, currentRegion);
            if (wasActive)
                PolicyManager.Instance.RemovePolicy(policy, currentRegion);
            else
                PolicyManager.Instance.AddPolicy(policy, currentRegion);

            bool isNowActive = PolicyManager.Instance.IsPolicyActive(policy, currentRegion);
            Debug.Log($"[PolicyPanel] {policy.PolicyName}: was={wasActive} now={isNowActive}");
        }

        private void OnBackClicked()
        {
            gameObject.SetActive(false);
            if (CityPanelController.Instance != null)
                CityPanelController.Instance.ShowFromPolicyBack();
        }

        private Region FindRegionForCity(City city)
        {
            if (gridSystem == null || city == null) return null;
            foreach (var region in gridSystem.Regions)
                if (region.City == city) return region;
            return null;
        }
    }
}
