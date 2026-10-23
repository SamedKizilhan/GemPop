using System.Collections.Generic;
using GemPop.Items;
using UnityEngine;

namespace GemPop.Gameplay
{
    /// <summary>
    /// Pure data model of the grid: which item sits in which cell. No rendering, no Unity scene logic,
    /// so it can be reasoned about (and unit-tested) on its own.
    /// Only this class writes to the cell array; everyone else reads through Get/IsEmpty.
    /// </summary>
    public class Board
    {
        public static readonly Vector2Int[] Directions =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public int Width { get; }
        public int Height { get; }

        private readonly GridItem[,] _cells;

        public Board(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new GridItem[width, height];
        }

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
        public bool InBounds(Vector2Int c) => InBounds(c.x, c.y);

        public GridItem Get(int x, int y) => InBounds(x, y) ? _cells[x, y] : null;
        public GridItem Get(Vector2Int c) => Get(c.x, c.y);

        public bool IsEmpty(int x, int y) => InBounds(x, y) && _cells[x, y] == null;
        public bool IsEmpty(Vector2Int c) => IsEmpty(c.x, c.y);

        /// <summary>Puts an item at a bottom-left cell and marks every cell it occupies.</summary>
        public void Place(GridItem item, Vector2Int bottomLeft)
        {
            item.SetPosition(bottomLeft);
            foreach (Vector2Int c in item.OccupiedCells)
            {
                if (!InBounds(c))
                {
                    Debug.LogError($"[Board] {item.name} does not fit at {bottomLeft}");
                    continue;
                }
                if (_cells[c.x, c.y] != null)
                    Debug.LogWarning($"[Board] Overwriting {_cells[c.x, c.y].name} at {c} with {item.name}");
                _cells[c.x, c.y] = item;
            }
        }

        /// <summary>Clears every cell the item occupies. Does not destroy the GameObject.</summary>
        public void Remove(GridItem item)
        {
            foreach (Vector2Int c in item.OccupiedCells)
                if (InBounds(c) && _cells[c.x, c.y] == item)
                    _cells[c.x, c.y] = null;
        }

        /// <summary>Moves a 1-cell item to an empty cell (used by gravity).</summary>
        public void Move(GridItem item, Vector2Int to)
        {
            Remove(item);
            Place(item, to);
        }

        /// <summary>Every distinct item on the board (a 2x2 box is returned once).</summary>
        public IEnumerable<GridItem> AllItems()
        {
            var seen = new HashSet<GridItem>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    GridItem item = _cells[x, y];
                    if (item != null && seen.Add(item)) yield return item;
                }
        }
    }
}
