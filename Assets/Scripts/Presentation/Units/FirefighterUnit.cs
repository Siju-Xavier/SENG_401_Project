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

            // Set 8 isometric directional sprites (4 standing + 4 running)
            mover.SetDirectionalSprites(
                config.StandTopLeft, config.StandTopRight,
                config.StandBottomLeft, config.StandBottomRight,
                config.RunTopLeft, config.RunTopRight,
                config.RunBottomLeft, config.RunBottomRight);
            mover.SetSpeed(config.MoveSpeed);

            // Set initial sprite to running direction toward target
            if (spriteRenderer != null)
            {
                Vector3 dir = targetWorldPos - home;
                Sprite initial;
                if (dir.x >= 0)
                    initial = dir.y >= 0 ? config.RunTopRight : config.RunBottomRight;
                else
                    initial = dir.y >= 0 ? config.RunTopLeft : config.RunBottomLeft;
                if (initial != null)
                    spriteRenderer.sprite = initial;
            }

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
