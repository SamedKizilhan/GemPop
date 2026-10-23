using System.Collections.Generic;
using GemPop.Gameplay;
using GemPop.Items;
using UnityEngine;

namespace GemPop.Systems
{
    /// <summary>One item moving down inside its column.</summary>
    public readonly struct FallMove
    {
        public readonly GridItem Item;
        public readonly Vector2Int From;
        public readonly Vector2Int To;

        public FallMove(GridItem item, Vector2Int from, Vector2Int to)
        {
            Item = item; From = from; To = to;
        }
    }

    /// <summary>An empty cell at the top of a column that a new cube should fall into.</summary>
    public readonly struct SpawnRequest
    {
        public readonly Vector2Int Cell;
        /// <summary>How many rows above the board the new cube starts, so several spawns stack up nicely.</summary>
        public readonly int RowsAboveTop;

        public SpawnRequest(Vector2Int cell, int rowsAboveTop)
        {
            Cell = cell; RowsAboveTop = rowsAboveTop;
        }
    }

    /// <summary>
    /// Gravity on the MODEL only: decides what moves where and updates the Board.
    /// It returns the moves so the view can animate them. No Unity physics involved (spec requirement).
    /// Rules: items fall straight down; they never pass through other items; non-falling items
    /// (stones, chalice boxes) block everything above them, so empty cells below them stay empty.
    /// </summary>
    public static class GravitySystem
    {
        public static List<FallMove> Collapse(Board board)
        {
            var moves = new List<FallMove>();

            for (int x = 0; x < board.Width; x++)
            {
                int writeY = 0;   // lowest cell an item may fall into in this column

                for (int y = 0; y < board.Height; y++)
                {
                    GridItem item = board.Get(x, y);
                    if (item == null) continue;

                    if (!item.CanFall)
                    {
                        writeY = y + 1;    // nothing can fall past this item
                        continue;
                    }

                    if (y > writeY)
                    {
                        var from = new Vector2Int(x, y);
                        var to = new Vector2Int(x, writeY);
                        board.Move(item, to);
                        moves.Add(new FallMove(item, from, to));
                    }
                    writeY++;
                }
            }
            return moves;
        }

        /// <summary>
        /// Empty cells that are reachable from the top of their column (walk down from the top row
        /// until the first item). Must be called after Collapse.
        /// </summary>
        public static List<SpawnRequest> FindSpawnCells(Board board)
        {
            var spawns = new List<SpawnRequest>();

            for (int x = 0; x < board.Width; x++)
            {
                int emptyCount = 0;
                for (int y = board.Height - 1; y >= 0 && board.IsEmpty(x, y); y--) emptyCount++;

                // lowest empty cell starts 1 row above the board, the one above it 2 rows, ...
                for (int i = 0; i < emptyCount; i++)
                {
                    int y = board.Height - emptyCount + i;
                    spawns.Add(new SpawnRequest(new Vector2Int(x, y), i + 1));
                }
            }
            return spawns;
        }
    }
}
