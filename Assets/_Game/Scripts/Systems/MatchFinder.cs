using System.Collections.Generic;
using GemPop.Gameplay;
using GemPop.Items;
using UnityEngine;

namespace GemPop.Systems
{
    /// <summary>
    /// Finds groups of same-colored, 4-neighbour-connected cubes (flood fill / BFS).
    /// Static and side-effect free: give it a board, get lists back.
    /// </summary>
    public static class MatchFinder
    {
        public const int MinBlastSize = 2;
        public const int RocketThreshold = 4;   // 4 or 5 cubes -> rocket
        public const int BombThreshold = 6;      // 6+ cubes -> Bomb

        public static HintType HintFor(int groupSize)
        {
            if (groupSize >= BombThreshold) return HintType.Bomb;
            if (groupSize >= RocketThreshold) return HintType.Rocket;
            return HintType.None;
        }

        /// <summary>All cubes connected to the cube at 'start' with the same color (including itself).</summary>
        public static List<Cube> FindGroup(Board board, Vector2Int start)
        {
            var result = new List<Cube>();
            if (!(board.Get(start) is Cube first)) return result;

            var visited = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                result.Add((Cube)board.Get(cell));

                foreach (Vector2Int dir in Board.Directions)
                {
                    Vector2Int next = cell + dir;
                    if (visited.Contains(next)) continue;
                    if (board.Get(next) is Cube cube && cube.Color == first.Color)
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }
            return result;
        }

        /// <summary>All special items connected to the one at 'start' (4-neighbours, any special kind). Used for combos.</summary>
        public static List<SpecialItem> FindSpecialGroup(Board board, Vector2Int start)
        {
            var result = new List<SpecialItem>();
            if (!(board.Get(start) is SpecialItem)) return result;

            var visited = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int cell = queue.Dequeue();
                result.Add((SpecialItem)board.Get(cell));

                foreach (Vector2Int dir in Board.Directions)
                {
                    Vector2Int next = cell + dir;
                    if (visited.Contains(next) || !(board.Get(next) is SpecialItem)) continue;
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
            return result;
        }

        /// <summary>True if the player can do something: a special item, or two same-colored neighbours.</summary>
        public static bool HasAnyMove(Board board)
        {
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    GridItem item = board.Get(x, y);
                    if (item is SpecialItem) return true;
                    if (item is Cube cube &&
                        ((board.Get(x + 1, y) is Cube r && r.Color == cube.Color) ||
                         (board.Get(x, y + 1) is Cube u && u.Color == cube.Color)))
                        return true;
                }
            return false;
        }

        /// <summary>Every cube group on the board, each cube in exactly one group. Used by the hint system.</summary>
        public static List<List<Cube>> FindAllGroups(Board board)
        {
            var groups = new List<List<Cube>>();
            var seen = new HashSet<Cube>();

            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    if (!(board.Get(x, y) is Cube cube) || seen.Contains(cube)) continue;
                    List<Cube> group = FindGroup(board, new Vector2Int(x, y));
                    foreach (Cube c in group) seen.Add(c);
                    groups.Add(group);
                }
            return groups;
        }
    }
}
