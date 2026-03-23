namespace ScriptableObjects {
    using UnityEngine;

    [CreateAssetMenu(fileName = "PolicyConfig", menuName = "Config/Policy")]
    public class PolicyConfig : ScriptableObject {
        [Tooltip("Reduces the chance of fire spreading (e.g. 0.8 = 20% reduction)")]
        [SerializeField] private float spreadReductionModifier = 1.0f;

        [Tooltip("Reduces the chance of new random fires starting (e.g. 0.5 = 50% reduction)")]
        [SerializeField] private float spawnReductionModifier = 1.0f;

        [SerializeField] private int costToImplement;
        [SerializeField] private int requiredLevel = 1;

        public float SpreadReductionModifier => spreadReductionModifier;
        public float SpawnReductionModifier => spawnReductionModifier;
        public int CostToImplement => costToImplement;
        public int RequiredLevel => requiredLevel;
    }
}
