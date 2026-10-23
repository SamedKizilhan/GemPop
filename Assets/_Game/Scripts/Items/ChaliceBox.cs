using System.Collections.Generic;
using UnityEngine;

namespace GemPop.Items
{
    /// <summary>
    /// 2x2 obstacle. Position is its bottom-left cell; it occupies 4 cells, all pointing to this object.
    /// Two phases: Door (doors sprite visible) then Chalice (collect chalices). Logic arrives on Day 5.
    /// </summary>
    public class ChaliceBox : Obstacle
    {
        public enum Phase { Door, Chalice }

        [SerializeField] private SpriteRenderer doorsRenderer;
        [SerializeField] private SpriteRenderer[] chaliceRenderers;   // 10 small chalices on the two shelves

        public const int ChalicesPerBox = 10;

        private int _lastDoorEventId = -1;

        public Phase CurrentPhase { get; private set; } = Phase.Door;
        public int ChalicesRemaining => Health;
        public override int CellSpan => 2;

        /// <summary>
        /// Door phase: one damage per event (a TNT covering 4 cells still counts once) opens the door.
        /// Chalice phase: every hit collects 'Amount' chalices (a blast passes its adjacent-cube count,
        /// an explosion passes 1 per cell). The box is cleared when all chalices are collected.
        /// </summary>
        public override DamageOutcome TakeDamage(DamageInfo info)
        {
            if (CurrentPhase == Phase.Door)
            {
                if (info.EventId == _lastDoorEventId) return DamageOutcome.Ignored;
                _lastDoorEventId = info.EventId;
                CurrentPhase = Phase.Chalice;
                RefreshVisuals();
                return DamageOutcome.Damaged;
            }

            Health = Mathf.Max(0, Health - info.Amount);
            RefreshVisuals();
            return Health == 0 ? DamageOutcome.Destroyed : DamageOutcome.Damaged;
        }

        public override IEnumerable<Vector2Int> OccupiedCells
        {
            get
            {
                yield return Position;
                yield return Position + Vector2Int.right;
                yield return Position + Vector2Int.up;
                yield return Position + Vector2Int.one;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            RefreshVisuals();
        }

        /// <summary>Doors while closed; afterwards one small chalice per remaining point, like the reference.</summary>
        protected void RefreshVisuals()
        {
            bool open = CurrentPhase == Phase.Chalice;
            if (doorsRenderer != null) doorsRenderer.enabled = !open;
            if (chaliceRenderers == null) return;
            for (int i = 0; i < chaliceRenderers.Length; i++)
                if (chaliceRenderers[i] != null) chaliceRenderers[i].enabled = open && i < Health;
        }

        /// <summary>Per shelf the prefab orders pins as [back, back, middle, middle, front] -> depth 0, 1, 2.</summary>
        private static int PinDepth(int index)
        {
            int pin = index % 5;
            return pin < 2 ? 0 : pin < 4 ? 1 : 2;
        }

        public override void SetSortingOrder(int order)
        {
            base.SetSortingOrder(order);              // background
            if (chaliceRenderers != null)
                for (int i = 0; i < chaliceRenderers.Length; i++)
                    if (chaliceRenderers[i] != null)
                        chaliceRenderers[i].sortingOrder = order + 1 + PinDepth(i);   // front pins over back pins
            if (doorsRenderer != null) doorsRenderer.sortingOrder = order + 5;
        }

#if UNITY_EDITOR
        public void EditorSetRenderers(SpriteRenderer background, SpriteRenderer doors, SpriteRenderer[] chalices)
        {
            mainRenderer = background;
            doorsRenderer = doors;
            chaliceRenderers = chalices;
        }
#endif
    }
}
