namespace ScriptableObjects {
    using UnityEngine;

    [CreateAssetMenu(fileName = "UnitConfig", menuName = "Config/Unit")]
    public class UnitConfig : ScriptableObject {
        [SerializeField] private int deploymentCost = 100;
        [SerializeField] private int maxWaterCapacity = 50;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float extinguishRate = 0.5f;
        [SerializeField] private GameObject unitPrefab;

        [Header("Standing Sprites (Extinguishing)")]
        [SerializeField] private Sprite standTopLeft;
        [SerializeField] private Sprite standTopRight;
        [SerializeField] private Sprite standBottomLeft;
        [SerializeField] private Sprite standBottomRight;

        [Header("Multi-Target")]
        [Tooltip("Max tiles a firefighter can extinguish before returning")]
        [SerializeField] private int maxTargets = 1;
        [Tooltip("Max tile distance to search for next fire")]
        [SerializeField] private int searchRadius = 10;

        [Header("Running Sprites (Movement)")]
        [SerializeField] private Sprite runTopLeft;
        [SerializeField] private Sprite runTopRight;
        [SerializeField] private Sprite runBottomLeft;
        [SerializeField] private Sprite runBottomRight;

        public int MaxTargets => maxTargets;
        public int SearchRadius => searchRadius;
        public int DeploymentCost => deploymentCost;
        public int MaxWaterCapacity => maxWaterCapacity;
        public float MoveSpeed => moveSpeed;
        public float ExtinguishRate => extinguishRate;
        public GameObject UnitPrefab => unitPrefab;
        public Sprite StandTopLeft => standTopLeft;
        public Sprite StandTopRight => standTopRight;
        public Sprite StandBottomLeft => standBottomLeft;
        public Sprite StandBottomRight => standBottomRight;
        public Sprite RunTopLeft => runTopLeft;
        public Sprite RunTopRight => runTopRight;
        public Sprite RunBottomLeft => runBottomLeft;
        public Sprite RunBottomRight => runBottomRight;
    }
}
