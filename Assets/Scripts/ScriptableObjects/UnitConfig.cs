namespace ScriptableObjects {
    using UnityEngine;

    [CreateAssetMenu(fileName = "UnitConfig", menuName = "Config/Unit")]
    public class UnitConfig : ScriptableObject {
        [SerializeField] private int deploymentCost = 100;
        [SerializeField] private int maxWaterCapacity = 50;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float extinguishRate = 0.5f;
        [SerializeField] private GameObject unitPrefab;

        [Header("Directional Sprites")]
        [SerializeField] private Sprite spriteBottomLeft;
        [SerializeField] private Sprite spriteBottomRight;
        
        [Header("Animation Sprites")]
        [SerializeField] private Sprite[] walkSprites;

        public int DeploymentCost => deploymentCost;
        public int MaxWaterCapacity => maxWaterCapacity;
        public float MoveSpeed => moveSpeed;
        public float ExtinguishRate => extinguishRate;
        public GameObject UnitPrefab => unitPrefab;
        public Sprite SpriteBottomLeft => spriteBottomLeft;
        public Sprite SpriteBottomRight => spriteBottomRight;
        public Sprite[] WalkSprites => walkSprites;
    }
}
