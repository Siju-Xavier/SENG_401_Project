namespace ScriptableObjects {
    using UnityEngine;

    [CreateAssetMenu(fileName = "UnitConfig", menuName = "Config/Unit")]
    public class UnitConfig : ScriptableObject {
        [SerializeField] private int deploymentCost;
        [SerializeField] private int maxWaterCapacity;
        [SerializeField] private float moveSpeed;
        [SerializeField] private float extinguishRate;
    }
}
