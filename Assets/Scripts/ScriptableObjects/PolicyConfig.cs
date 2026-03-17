namespace ScriptableObjects {
    using UnityEngine;

    [CreateAssetMenu(fileName = "PolicyConfig", menuName = "Config/Policy")]
    public class PolicyConfig : ScriptableObject {
        [SerializeField] private float spreadReductionModifier;
        [SerializeField] private int costToImplement;
        [SerializeField] private int requiredLevel;
    }
}
