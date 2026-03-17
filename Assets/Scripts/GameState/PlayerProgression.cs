namespace GameState {
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class PlayerProgression {
        [SerializeField] private int currentLevel;
        [SerializeField] private int currentScore;
        [SerializeField] private List<string> unlockedFeatures;
    }
}
