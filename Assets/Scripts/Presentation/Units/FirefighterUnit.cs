namespace Presentation
{
    using UnityEngine;
    using GameState;
    using BusinessLogic;
    using ScriptableObjects;

    [RequireComponent(typeof(SpriteMover))]
    public class FirefighterUnit : MonoBehaviour
    {
        private SpriteMover mover;
        private SpriteRenderer spriteRenderer;
        private FireEngine fireEngine;
        private UnitConfig config;
        private Tile targetTile;
        private Vector3 homePosition;
        private Vector3 targetWorldPos;
        private UnitState state = UnitState.Idle;
        private bool targetSet;

        public UnitState State => state;

        public void Initialize(FireEngine engine, UnitConfig unitConfig, Tile target, Vector3 home, Vector3 targetWorld)
        {
            fireEngine = engine;
            config = unitConfig;
            targetTile = target;
            homePosition = home;
            targetWorldPos = targetWorld;

            mover = GetComponent<SpriteMover>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            // Set directional sprites and speed from config
            mover.SetDirectionalSprites(config.SpriteBottomLeft, config.SpriteBottomRight, config.WalkSprites);
            mover.SetSpeed(config.MoveSpeed);

            // Set initial sprite
            if (spriteRenderer != null && config.SpriteBottomRight != null)
                spriteRenderer.sprite = config.SpriteBottomRight;

            mover.OnArrived = OnArrivedAtTarget;
            mover.SetTarget(targetWorldPos);

            state = UnitState.EnRoute;
            targetSet = true;
        }

        private void Update()
        {
            if (state == UnitState.Extinguishing && targetTile != null)
            {
                if (!targetTile.IsOnFire)
                {
                    ReturnHome();
                    return;
                }

                targetTile.FireIntensity -= config.ExtinguishRate * Time.deltaTime;

                if (targetTile.FireIntensity <= 0f)
                {
                    fireEngine.ExtinguishTile(targetTile);
                    ReturnHome();
                }
            }
        }

        private void ReturnHome()
        {
            state = UnitState.Returning;
            mover.OnArrived = OnArrivedHome;
            mover.SetTarget(homePosition);
        }

        private void OnArrivedAtTarget()
        {
            if (targetTile != null && targetTile.IsOnFire)
            {
                state = UnitState.Extinguishing;
            }
            else
            {
                ReturnHome();
            }
        }

        private void OnArrivedHome()
        {
            state = UnitState.Idle;
            Destroy(gameObject);
        }
    }
}
