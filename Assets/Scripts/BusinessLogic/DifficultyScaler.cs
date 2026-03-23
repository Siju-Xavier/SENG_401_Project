namespace BusinessLogic {
    using UnityEngine;
    using Core;

    public class DifficultyScaler : MonoBehaviour {
        [SerializeField] private FireEngine fireEngine;

        private void OnEnable() {
            Core.EventBroker.Instance.Subscribe(Core.EventType.LevelUp, OnLevelUp);
        }

        private void OnDisable() {
            Core.EventBroker.Instance.Unsubscribe(Core.EventType.LevelUp, OnLevelUp);
        }

        private void Awake() {
            if (fireEngine == null)
                fireEngine = FindFirstObjectByType<FireEngine>();

            ApplyDifficulty(1);
        }

        private void OnLevelUp(object data) {
            int level = data is int l ? l : 1;
            ApplyDifficulty(level);
        }

        private void ApplyDifficulty(int level) {
            if (fireEngine == null) return;

            float spreadChance = Mathf.Min(0.05f + (level - 1) * 0.02f, 0.45f);
            float tickInterval = Mathf.Max(0.5f, 2.5f - (level - 1) * 0.08f);
            int fireCount = 1 + (level - 1);
            float growthRate = Mathf.Min(0.05f + (level - 1) * 0.01f, 0.4f);

            fireEngine.SetDifficulty(spreadChance, tickInterval, fireCount, growthRate);
            Debug.Log($"[DifficultyScaler] Level {level}: spread={spreadChance:F2}, interval={tickInterval:F2}, fires={fireCount}, growth={growthRate:F2}");
        }
    }
}
