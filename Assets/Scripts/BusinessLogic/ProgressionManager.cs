namespace BusinessLogic {
    using GameState;
    using Core;
    using UnityEngine;

    public class ProgressionManager : MonoBehaviour {
        [SerializeField] private PlayerProgression progressionData;

        public void AddToScore() { }

        public void CheckScore() { }

        public int GetCurrentScore() { 
            return 0; 
        }

        public bool CalculateProgressionLevel(string topic) { 
            return false; 
        }
    }
}
