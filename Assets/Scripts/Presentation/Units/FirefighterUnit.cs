namespace Presentation
{
    using System.Collections.Generic;
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

        private int tilesExtinguished;
        private GridSystem gridSystem;
        private UnityEngine.Tilemaps.Tilemap groundTilemap;
        private GameOverManager gameOverManager;

        public UnitState State => state;

        public void Initialize(FireEngine engine, UnitConfig unitConfig, Tile target,
                               Vector3 home, Vector3 targetWorld,
                               GridSystem grid = null,
                               UnityEngine.Tilemaps.Tilemap tilemap = null)
        {
            fireEngine = engine;
            config = unitConfig;
            targetTile = target;
            homePosition = home;
            targetWorldPos = targetWorld;
            tilesExtinguished = 0;
            gridSystem = grid;
            groundTilemap = tilemap ?? Object.FindFirstObjectByType<UnityEngine.Tilemaps.Tilemap>();
            gameOverManager = Object.FindFirstObjectByType<GameOverManager>();

            mover = GetComponent<SpriteMover>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            // Set 8 isometric directional sprites
            mover.SetDirectionalSprites(
                config.StandTopLeft, config.StandTopRight,
                config.StandBottomLeft, config.StandBottomRight,
                config.RunTopLeft, config.RunTopRight,
                config.RunBottomLeft, config.RunBottomRight);
            mover.SetSpeed(config.MoveSpeed);

            // Set initial sprite facing target
            if (spriteRenderer != null)
            {
                Vector3 dir = targetWorldPos - home;
                Sprite initial;
                if (dir.x >= 0)
                    initial = dir.y >= 0 ? config.RunTopRight : config.RunBottomRight;
                else
                    initial = dir.y >= 0 ? config.RunTopLeft : config.RunBottomLeft;
                if (initial != null) spriteRenderer.sprite = initial;
            }

            // Assign tile
            TileAssignmentManager.Instance?.TryAssign(target, gameObject);

            mover.OnArrived = OnArrivedAtTarget;
            NavigateToTile(target, home);
            state = UnitState.EnRoute;
        }

        private void Update()
        {
            // Stop all activity when game is over — head home
            if (gameOverManager != null && gameOverManager.IsGameOver) {
                if (state != UnitState.Returning && state != UnitState.Idle) {
                    TileAssignmentManager.Instance?.Unassign(targetTile);
                    ReturnHome();
                }
                return;
            }

            // During EnRoute: check if target fire went out
            if (state == UnitState.EnRoute && targetTile != null && !targetTile.IsOnFire)
            {
                TileAssignmentManager.Instance?.Unassign(targetTile);
                tilesExtinguished++;
                if (tilesExtinguished < config.MaxTargets)
                    TryFindNextTarget();
                else
                    ReturnHome();
                return;
            }

            if (state == UnitState.Extinguishing && targetTile != null)
            {
                // Stop extinguishing if tile is no longer on fire OR has become burnt
                if (!targetTile.IsOnFire || targetTile.IsBurnt)
                {
                    OnTileExtinguished();
                    return;
                }

                targetTile.FireIntensity -= config.ExtinguishRate * Time.deltaTime;

                if (targetTile.FireIntensity <= 0f)
                {
                    fireEngine.ExtinguishTile(targetTile);
                    OnTileExtinguished();
                }
            }
        }

        private void OnTileExtinguished()
        {
            tilesExtinguished++;
            TileAssignmentManager.Instance?.Unassign(targetTile);

            if (tilesExtinguished < config.MaxTargets)
                TryFindNextTarget();
            else
                ReturnHome();
        }

        private void TryFindNextTarget()
        {
            if (fireEngine == null)
            {
                ReturnHome();
                return;
            }

            var burningTiles = fireEngine.GetBurningTiles();
            Tile nextTile = null;

            // Use assignment manager if available, otherwise find nearest burning tile directly
            if (TileAssignmentManager.Instance != null)
            {
                nextTile = TileAssignmentManager.Instance.FindNearestUnassigned(
                    burningTiles, targetTile.X, targetTile.Y);
            }
            else
            {
                float nearestDist = float.MaxValue;
                foreach (var tile in burningTiles)
                {
                    if (!tile.IsOnFire || tile == targetTile) continue;
                    float d = (targetTile.X - tile.X) * (targetTile.X - tile.X)
                            + (targetTile.Y - tile.Y) * (targetTile.Y - tile.Y);
                    if (d < nearestDist) { nearestDist = d; nextTile = tile; }
                }
            }

            if (nextTile == null)
            {
                ReturnHome();
                return;
            }

            // Check search radius
            float dist = Mathf.Sqrt(
                (nextTile.X - targetTile.X) * (nextTile.X - targetTile.X) +
                (nextTile.Y - targetTile.Y) * (nextTile.Y - targetTile.Y));
            if (dist > config.SearchRadius)
            {
                ReturnHome();
                return;
            }

            TileAssignmentManager.Instance?.TryAssign(nextTile, gameObject);

            Debug.Log($"[Firefighter] Chaining to next fire at ({nextTile.X}, {nextTile.Y}), tiles done: {tilesExtinguished}/{config.MaxTargets}");

            var fromTile = targetTile;
            targetTile = nextTile;

            mover.OnArrived = OnArrivedAtTarget;
            NavigateToTile(nextTile, TileToWorld(fromTile));
            state = UnitState.EnRoute;
        }

        private void NavigateToTile(Tile target, Vector3 fromWorldPos)
        {
            targetWorldPos = TileToWorld(target);

            // Try grid pathfinding if available
            if (gridSystem != null && groundTilemap != null)
            {
                // Find the tile closest to current position
                var cellPos = groundTilemap.WorldToCell(fromWorldPos);
                var startTile = gridSystem.GetTileAt(cellPos.x, cellPos.y);

                if (startTile != null)
                {
                    var path = GridPathfinder.FindPath(gridSystem, startTile, target);
                    if (path != null && path.Count > 1)
                    {
                        var waypoints = new List<Vector3>();
                        for (int i = 1; i < path.Count; i++)
                            waypoints.Add(TileToWorld(path[i]));
                        mover.SetWaypoints(waypoints);
                        return;
                    }
                }
            }

            // Fallback: direct movement
            mover.SetTarget(targetWorldPos);
        }

        private Vector3 TileToWorld(Tile tile)
        {
            if (groundTilemap != null)
                return groundTilemap.GetCellCenterWorld(new Vector3Int(tile.X, tile.Y, 0));
            return new Vector3(tile.X, tile.Y, 0);
        }

        private void ReturnHome()
        {
            state = UnitState.Returning;
            mover.OnArrived = OnArrivedHome;

            // Use grid pathfinding for return trip too
            if (gridSystem != null && groundTilemap != null)
            {
                var cellPos = groundTilemap.WorldToCell(transform.position);
                var startTile = gridSystem.GetTileAt(cellPos.x, cellPos.y);
                var homeCell = groundTilemap.WorldToCell(homePosition);
                var homeTile = gridSystem.GetTileAt(homeCell.x, homeCell.y);

                if (startTile != null && homeTile != null)
                {
                    var path = GridPathfinder.FindPath(gridSystem, startTile, homeTile);
                    if (path != null && path.Count > 1)
                    {
                        var waypoints = new List<Vector3>();
                        for (int i = 1; i < path.Count; i++)
                            waypoints.Add(TileToWorld(path[i]));
                        mover.SetWaypoints(waypoints);
                        return;
                    }
                }
            }

            mover.SetTarget(homePosition);
        }

        private void OnArrivedAtTarget()
        {
            // Only start extinguishing if the tile is actively burning and NOT already burnt
            if (targetTile != null && targetTile.IsOnFire && !targetTile.IsBurnt)
            {
                state = UnitState.Extinguishing;
            }
            else
            {
                TileAssignmentManager.Instance?.Unassign(targetTile);
                tilesExtinguished++;
                if (tilesExtinguished < config.MaxTargets)
                    TryFindNextTarget();
                else
                    ReturnHome();
            }
        }

        private void OnArrivedHome()
        {
            state = UnitState.Idle;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            TileAssignmentManager.Instance?.UnassignAll(gameObject);
        }
    }
}
