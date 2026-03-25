namespace BusinessLogic
{
    using System.Collections.Generic;
    using GameState;
    using UnityEngine;

    public class TileAssignmentManager : MonoBehaviour
    {
        public static TileAssignmentManager Instance { get; private set; }

        private Dictionary<Tile, GameObject> assignments = new Dictionary<Tile, GameObject>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool TryAssign(Tile tile, GameObject unit)
        {
            if (tile == null) return false;
            if (assignments.ContainsKey(tile))
            {
                // Clean stale
                if (assignments[tile] == null)
                {
                    assignments.Remove(tile);
                    tile.ClearAssignment();
                }
                else
                {
                    return false;
                }
            }
            assignments[tile] = unit;
            tile.AssignedUnit = unit;
            return true;
        }

        public void Unassign(Tile tile)
        {
            if (tile == null) return;
            assignments.Remove(tile);
            tile.ClearAssignment();
        }

        public void UnassignAll(GameObject unit)
        {
            var toRemove = new List<Tile>();
            foreach (var kvp in assignments)
            {
                if (kvp.Value == unit) toRemove.Add(kvp.Key);
            }
            foreach (var tile in toRemove) Unassign(tile);
        }

        public bool IsAssigned(Tile tile)
        {
            if (tile == null) return false;
            if (assignments.TryGetValue(tile, out var unit))
            {
                if (unit == null)
                {
                    assignments.Remove(tile);
                    tile.ClearAssignment();
                    return false;
                }
                return true;
            }
            return false;
        }

        public Tile FindNearestUnassigned(List<Tile> burningTiles, int fromX, int fromY)
        {
            Tile nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var tile in burningTiles)
            {
                if (!tile.IsOnFire || IsAssigned(tile)) continue;
                float dist = (fromX - tile.X) * (fromX - tile.X)
                           + (fromY - tile.Y) * (fromY - tile.Y);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = tile;
                }
            }
            return nearest;
        }

        public void CleanupStale()
        {
            var stale = new List<Tile>();
            foreach (var kvp in assignments)
            {
                if (kvp.Value == null) stale.Add(kvp.Key);
            }
            foreach (var tile in stale) Unassign(tile);
        }
    }
}
