namespace GameState {
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class PlayerProgression {
        [SerializeField] private int currentLevel;
        [SerializeField] private int currentScore;
        [SerializeField] private List<string> unlockedFeatures = new List<string>();

        public int CurrentLevel  { get => currentLevel;  set => currentLevel = value; }
        public int CurrentScore  { get => currentScore;  set => currentScore = value; }
        public List<string> UnlockedFeatures => unlockedFeatures;

        /// <summary>Restore progression state from save data.</summary>
        public void SetProgression(int level, int score, List<string> unlocks) {
            currentLevel = level;
            currentScore = score;
            unlockedFeatures = unlocks ?? new List<string>();
        }
    }
}
