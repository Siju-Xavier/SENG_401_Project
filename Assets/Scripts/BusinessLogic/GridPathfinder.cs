namespace BusinessLogic
{
    using System.Collections.Generic;
    using GameState;

    public static class GridPathfinder
    {
        public static List<Tile> FindPath(GridSystem grid, Tile start, Tile end)
        {
            if (grid == null || start == null || end == null) return null;
            if (start == end) return new List<Tile> { start };

            var queue = new Queue<Tile>();
            var cameFrom = new Dictionary<Tile, Tile>();

            queue.Enqueue(start);
            cameFrom[start] = null;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == end) break;

                foreach (var neighbour in grid.GetNeighbours(current))
                {
                    if (cameFrom.ContainsKey(neighbour)) continue;
                    cameFrom[neighbour] = current;
                    queue.Enqueue(neighbour);
                }
            }

            if (!cameFrom.ContainsKey(end)) return null;

            var path = new List<Tile>();
            var step = end;
            while (step != null)
            {
                path.Add(step);
                cameFrom.TryGetValue(step, out step);
            }
            path.Reverse();
            return path;
        }
    }
}
