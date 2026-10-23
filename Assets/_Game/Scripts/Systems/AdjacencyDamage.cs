using System.Collections.Generic;
using GemPop.Gameplay;
using GemPop.Items;
using UnityEngine;

namespace GemPop.Systems
{
    /// <summary>
    /// "Adjacent blast" damage: when a cube group blasts, every obstacle touching the group is hit once,
    /// with Amount = how many cubes of the group touch it (the chalice box uses that number; vases and the
    /// door phase only care that it happened). Call it BEFORE the group is removed from the board.
    /// </summary>
    public static class AdjacencyDamage
    {
        public static Dictionary<Obstacle, int> Collect(Board board, List<Cube> group)
        {
            var hits = new Dictionary<Obstacle, int>();
            foreach (Cube cube in group)
            {
                foreach (Vector2Int dir in Board.Directions)
                {
                    if (board.Get(cube.Position + dir) is Obstacle obstacle)
                        hits[obstacle] = hits.TryGetValue(obstacle, out int n) ? n + 1 : 1;
                }
            }
            return hits;
        }
    }
}
