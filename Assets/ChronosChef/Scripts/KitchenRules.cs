using System.Collections.Generic;
using UnityEngine;

namespace ChronosChef
{
    public enum Food { Empty, RawDodo, ChoppedDodo, CookedDodo, Burger, Burnt, Root, Roots, Soup, Bread, ChoppedRoot, ChoppedRoots, Toast }
    public enum Station { Supply, Cut, Stove, Assemble, Trash, Customer0, Customer1, Customer2, Roots }

    // Kept independent from the view so recipes and routing can be extended separately.
    public static class KitchenRules
    {
        public const int Columns = 12, Rows = 4;
        // Floor footprints in KitchenStage coordinates, with room for the chef's feet.
        // The art extends above these bases; collision uses the bases, not the sprite's head.
        public const float ChefRadius = 22f;
        static readonly Rect[] SolidFixtures =
        {
            new Rect(580, 445, 180, 115), // laser cutting station
            new Rect(855, 442, 180, 120), // plasma stove
            new Rect(100, 420, 205, 135), // roots cooler
            new Rect(90, 290, 205, 135),  // dodo cooler
            new Rect(65, 145, 190, 140),  // quantum bin
            new Rect(1340, 355, 210, 245),// assembly counter
            new Rect(300, 575, 1070, 80) // serving counter
        };
        public const float CookSeconds = 5f, BurnSeconds = 11f, ShiftSeconds = 120f;
        public static readonly string[] FoodNames = { "Manos libres", "Dodo crudo", "Dodo cortado", "Dodo cocinado", "Cyber-Dodo Burger", "Comida quemada", "Raíz neón (1/2)", "Raíces neón (2/2)", "Sopa cuántica", "Pan espacial", "Raíz cortada (1/2)", "Raíces cortadas (2/2)", "Tostada mutante" };
        public static Vector2Int Approach(Station station)
        {
            switch (station)
            {
                case Station.Supply: return new Vector2Int(0, 2);
                case Station.Cut: return new Vector2Int(4, 2);
                case Station.Stove: return new Vector2Int(7, 2);
                case Station.Assemble: return new Vector2Int(11, 2);
                case Station.Trash: return new Vector2Int(0, 0);
                case Station.Roots: return new Vector2Int(0, 3);
                // Service from free floor beside/in front of the tables, never inside them.
                case Station.Customer0: return new Vector2Int(2, 3);
                case Station.Customer1: return new Vector2Int(6, 2);
                case Station.Customer2: return new Vector2Int(9, 3);
                default: throw new System.ArgumentOutOfRangeException(nameof(station));
            }
        }
        public static Vector2 Position(Vector2Int cell) => new Vector2(360 + cell.x * 80, 245 + cell.y * 80);
        public static bool IsFloorPositionClear(Vector2 position)
        {
            foreach (var fixture in SolidFixtures)
            {
                var clearance = Rect.MinMaxRect(fixture.xMin - ChefRadius, fixture.yMin - ChefRadius,
                    fixture.xMax + ChefRadius, fixture.yMax + ChefRadius);
                if (clearance.Contains(position)) return false;
            }
            return true;
        }
        public static bool IsWalkable(Vector2Int cell, Vector2Int blocked)
            => cell.x >= 0 && cell.x < Columns && cell.y >= 0 && cell.y < Rows
            && cell != blocked && IsFloorPositionClear(Position(cell));
        // Clip the whole movement segment against each expanded footprint, even on slow frames.
        public static bool ClearSegment(Vector2 from, Vector2 to, Vector2Int blocked)
        {
            foreach (var r in SolidFixtures)
                if (Intersects(from, to, r.min - Vector2.one * ChefRadius, r.max + Vector2.one * ChefRadius)) return false;
            if (blocked.x < 0) return true;
            Vector2 center = Position(blocked), radius = Vector2.one * 35;
            return !Intersects(from, to, center - radius, center + radius);
        }
        static bool Intersects(Vector2 from, Vector2 to, Vector2 min, Vector2 max)
        {
            float enter = 0, leave = 1;
            for (int axis = 0; axis < 2; axis++)
            {
                float delta = to[axis] - from[axis];
                if (Mathf.Abs(delta) < .0001f)
                {
                    if (from[axis] <= min[axis] || from[axis] >= max[axis]) return false;
                    continue;
                }
                float near = (min[axis] - from[axis]) / delta, far = (max[axis] - from[axis]) / delta;
                enter = Mathf.Max(enter, Mathf.Min(near, far));
                leave = Mathf.Min(leave, Mathf.Max(near, far));
                if (enter >= leave) return false;
            }
            return true;
        }
        public static bool IsStationApproach(Vector2Int cell)
        {
            for (int i = 0; i < 9; i++) if (Approach((Station)i) == cell) return true;
            return false;
        }
        public static List<Vector2Int> Route(Vector2Int from, Vector2Int to, Vector2Int blocked)
        {
            if (!IsWalkable(from, blocked) || !IsWalkable(to, blocked)) return new List<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            var previous = new Dictionary<Vector2Int, Vector2Int>();
            queue.Enqueue(from); previous[from] = from;
            var directions = new[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                if (cell == to) break;
                foreach (var d in directions)
                {
                    var next = cell + d;
                    if (!IsWalkable(next, blocked) || previous.ContainsKey(next)) continue;
                    previous[next] = cell; queue.Enqueue(next);
                }
            }
            var result = new List<Vector2Int>();
            if (!previous.ContainsKey(to)) return result;
            for (var p = to; p != from; p = previous[p]) result.Add(p);
            result.Reverse(); return result;
        }
    }
}
