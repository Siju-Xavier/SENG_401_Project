namespace GameState {
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class PlayerProgression {
        [SerializeField] private int currentLevel;
        [SerializeField] private List<string> unlockedFeatures = new List<string>();

        public int CurrentLevel  { get => currentLevel;  set => currentLevel = value; }
        public List<string> UnlockedFeatures => unlockedFeatures;

        /// <summary>Restore progression state from save data.</summary>
        public void SetProgression(int level, List<string> unlocks) {
            currentLevel = level;
            unlockedFeatures = unlocks ?? new List<string>();
        }
    }
}
