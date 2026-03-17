namespace BusinessLogic {
    using GameState;
    using Core;
    using UnityEngine;

    public class PolicyManager : MonoBehaviour {
        [SerializeField] private PlayerProgression progression;

        public void AddPolicy(string policyType, Region region) {
            // Applies PolicyConfig effects to Region
        }

        public void RemovePolicyFromEngine(string policyName) { }

        public float CalculatePolicyEffect(object policy, Region region) { 
            return 0f; 
        }
    }
}
