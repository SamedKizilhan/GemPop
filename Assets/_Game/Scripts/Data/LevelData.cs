using System;
using System.Collections.Generic;
using UnityEngine;

namespace GemPop.Data
{
    /// <summary>
    /// Plain data object that mirrors one level_XX.json file.
    /// JsonUtility maps JSON keys to fields with EXACTLY the same name, so the field names are
    /// snake_case. They are private + [SerializeField]: JsonUtility can still fill them, but
    /// nobody outside this class can modify level data after it is loaded (encapsulation).
    /// </summary>
    [Serializable]
    public class LevelData
    {
        [SerializeField] private int level_number;
        [SerializeField] private int grid_width;
        [SerializeField] private int grid_height;
        [SerializeField] private int move_count;
        [SerializeField] private List<string> grid;

        public int LevelNumber => level_number;
        public int Width => grid_width;
        public int Height => grid_height;
        public int MoveCount => move_count;

        /// <summary>
        /// The JSON grid starts at the BOTTOM-LEFT cell and goes row by row to the top-right.
        /// index = y * width + x, where y = 0 is the bottom row.
        /// </summary>
        public string GetCode(int x, int y) => grid[y * grid_width + x];

        public bool Validate(out string error)
        {
            if (grid == null) { error = "grid is missing"; return false; }
            if (grid_width < 6 || grid_width > 10 || grid_height < 6 || grid_height > 10)
            { error = $"grid size {grid_width}x{grid_height} must be between 6 and 10"; return false; }
            if (grid.Count != grid_width * grid_height)
            { error = $"grid has {grid.Count} cells, expected {grid_width * grid_height}"; return false; }
            error = null;
            return true;
        }
    }
}
