using System.Collections.Generic;
using UnityEngine;

namespace GemPop.Items
{
    /// <summary>
    /// Base class of everything that can sit in a board cell.
    /// Subclasses override the virtual members to describe HOW they differ
    /// (polymorphism) instead of the board asking "what type are you?" everywhere.
    /// </summary>
    public abstract class GridItem : MonoBehaviour
    {
        [SerializeField] protected SpriteRenderer mainRenderer;

        [Tooltip("1 = sprite's larger side equals one cell. >1 overlaps neighbours slightly, <1 leaves a gap.")]
        [SerializeField, Range(0.5f, 1.5f)] protected float cellFill = 1f;

        [Tooltip("Visual nudge in cell units, e.g. (0, -0.04) draws the sprite slightly lower than its cell center.")]
        [SerializeField] protected Vector2 visualOffset = Vector2.zero;

        [Tooltip("Extra vertical scale on top of the uniform fit (1 = none). Lets a square sprite match the taller cube rows.")]
        [SerializeField, Range(0.8f, 1.3f)] protected float verticalStretch = 1f;

        /// <summary>Bottom-left cell of the item. Only the Board may change it.</summary>
        public Vector2Int Position { get; private set; }

        /// <summary>Does gravity move this item? Cubes, specials and vases: yes. Stones, chalice boxes: no.</summary>
        public virtual bool CanFall => false;

        /// <summary>Is this item one of the level goals?</summary>
        public virtual bool IsObstacle => false;

        /// <summary>How many cells wide/tall the item is (1 for everything except the 2x2 Chalice Box).</summary>
        public virtual int CellSpan => 1;

        public virtual IEnumerable<Vector2Int> OccupiedCells
        {
            get { yield return Position; }
        }

        internal void SetPosition(Vector2Int position) => Position = position;

        /// <summary>
        /// How this item reacts to a hit. Abstract on purpose: every item type must decide for itself
        /// (polymorphism) instead of the board containing a chain of type checks.
        /// The caller removes the item from the board when the outcome is Destroyed.
        /// </summary>
        public abstract DamageOutcome TakeDamage(DamageInfo info);

        /// <summary>How much of a cell the sprite should cover. Subclasses may vary it per state.</summary>
        protected virtual float FillFactor => cellFill;

        /// <summary>Where the sprite is drawn relative to the cell center, in cell units.</summary>
        public Vector2 VisualOffset => visualOffset;

        /// <summary>Scales the item so the larger side of its sprite spans CellSpan cells (times FillFactor).</summary>
        public void FitToCell(float cellSize)
        {
            if (mainRenderer == null || mainRenderer.sprite == null) return;
            Vector3 size = mainRenderer.sprite.bounds.size;            // world units at scale 1
            float largestSide = Mathf.Max(size.x, size.y);
            float scale = cellSize * CellSpan * FillFactor / largestSide;
            transform.localScale = new Vector3(scale, scale * verticalStretch, 1f);
        }

        /// <summary>Lower rows are drawn in front of higher rows, so stacked cubes look right.</summary>
        public virtual void SetSortingOrder(int order)
        {
            if (mainRenderer != null) mainRenderer.sortingOrder = order;
        }

        protected virtual void Reset()
        {
            mainRenderer = GetComponent<SpriteRenderer>();
        }

#if UNITY_EDITOR
        public void EditorSetRenderer(SpriteRenderer renderer) => mainRenderer = renderer;
        public void EditorSetCellFill(float value) => cellFill = value;
        public void EditorSetVisualOffset(Vector2 value) => visualOffset = value;
        public void EditorSetVerticalStretch(float value) => verticalStretch = value;
#endif
    }
}
