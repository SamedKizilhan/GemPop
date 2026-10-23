using GemPop.Items;
using UnityEngine;

namespace GemPop.Gameplay
{
    /// <summary>
    /// Converts between grid cells and world positions, and sizes the grid so any 6..10 board fits
    /// the portrait screen. The model (Board) knows nothing about this; it is pure presentation.
    /// Named BoardLayout (not GridLayout) to avoid clashing with UnityEngine.GridLayout.
    /// </summary>
    public class BoardLayout : MonoBehaviour
    {
        [Header("Available area in world units (camera ortho size 10 => screen is 11.25 x 20)")]
        [SerializeField] private float maxBoardWidth = 10.4f;
        [SerializeField] private float maxBoardHeight = 12.5f;
        [SerializeField] private Vector2 boardCenter = new Vector2(0f, -0.8f);

        [Header("Background (9-sliced sprite)")]
        [SerializeField] private SpriteRenderer background;
        [Tooltip("Extra panel size around the cells: x = left/right, y = top/bottom (world units).")]
        [SerializeField] private Vector2 backgroundPadding = new Vector2(0.18f, 0.235f);

        public float CellSize { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private Vector2 _cellZeroCenter;   // world position of the center of cell (0,0)

        public void Configure(int width, int height)
        {
            Width = width;
            Height = height;
            CellSize = Mathf.Min(maxBoardWidth / width, maxBoardHeight / height);

            float boardW = width * CellSize;
            float boardH = height * CellSize;
            _cellZeroCenter = boardCenter - new Vector2(boardW, boardH) * 0.5f + Vector2.one * (CellSize * 0.5f);

            if (background != null)
            {
                background.drawMode = SpriteDrawMode.Sliced;
                background.size = new Vector2(boardW + backgroundPadding.x * 2f, boardH + backgroundPadding.y * 2f);
                background.transform.position = boardCenter;
            }
        }

        public Vector3 CellToWorld(int x, int y) => _cellZeroCenter + new Vector2(x, y) * CellSize;
        public Vector3 CellToWorld(Vector2Int cell) => CellToWorld(cell.x, cell.y);

        /// <summary>Center of the item: for a 2x2 item this is the shared corner of its 4 cells.</summary>
        public Vector3 ItemWorldPosition(GridItem item)
        {
            float spanOffset = (item.CellSpan - 1) * CellSize * 0.5f;
            Vector2 nudge = item.VisualOffset * CellSize;
            return CellToWorld(item.Position) + new Vector3(spanOffset + nudge.x, spanOffset + nudge.y, 0f);
        }

        /// <summary>Where a new cube appears before falling in: above the top row of a column.</summary>
        public Vector3 SpawnPosition(int x, int rowsAboveTop) => CellToWorld(x, Height - 1 + rowsAboveTop);

#if UNITY_EDITOR
        public void EditorSetArea(float maxWidth, float maxHeight, Vector2 center)
        {
            maxBoardWidth = maxWidth; maxBoardHeight = maxHeight; boardCenter = center;
        }
#endif

        public bool TryWorldToCell(Vector3 world, out Vector2Int cell)
        {
            Vector2 local = (Vector2)world - _cellZeroCenter + Vector2.one * (CellSize * 0.5f);
            cell = new Vector2Int(Mathf.FloorToInt(local.x / CellSize), Mathf.FloorToInt(local.y / CellSize));
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }
    }
}
