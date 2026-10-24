using System.Collections.Generic;
using UnityEngine;

namespace GemPop.Items
{
    /// <summary>
    /// 2x2 obstacle. Position is its bottom-left cell; it occupies 4 cells, all pointing to this object.
    /// Two phases: Door (doors sprite visible) then Coin (collect coins). Logic arrives on Day 5.
    /// </summary>
    public class Chest : Obstacle
    {
        public enum Phase { Door, Coin }

        [SerializeField] private SpriteRenderer doorsRenderer;
        [SerializeField] private SpriteRenderer[] coinRenderers;   // 10 small coins on the two shelves

        public const int CoinsPerBox = 10;

        private int _lastDoorEventId = -1;

        public Phase CurrentPhase { get; private set; } = Phase.Door;
        public int CoinsRemaining => Health;
        public override int CellSpan => 2;

        /// <summary>
        /// Door phase: one damage per event (a Bomb covering 4 cells still counts once) opens the door.
        /// Coin phase: every hit collects 'Amount' coins (a blast passes its adjacent-cube count,
        /// an explosion passes 1 per cell). The box is cleared when all coins are collected.
        /// </summary>
        public override DamageOutcome TakeDamage(DamageInfo info)
        {
            if (CurrentPhase == Phase.Door)
            {
                if (info.EventId == _lastDoorEventId) return DamageOutcome.Ignored;
                _lastDoorEventId = info.EventId;
                CurrentPhase = Phase.Coin;
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

        /// <summary>Doors while closed; afterwards one small coin per remaining point, like the reference.</summary>
        protected void RefreshVisuals()
        {
            bool open = CurrentPhase == Phase.Coin;
            if (doorsRenderer != null) doorsRenderer.enabled = !open;
            if (coinRenderers == null) return;
            for (int i = 0; i < coinRenderers.Length; i++)
                if (coinRenderers[i] != null) coinRenderers[i].enabled = open && i < Health;
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
            if (coinRenderers != null)
                for (int i = 0; i < coinRenderers.Length; i++)
                    if (coinRenderers[i] != null)
                        coinRenderers[i].sortingOrder = order + 1 + PinDepth(i);   // front pins over back pins
            if (doorsRenderer != null) doorsRenderer.sortingOrder = order + 5;
        }

#if UNITY_EDITOR
        public void EditorSetRenderers(SpriteRenderer background, SpriteRenderer doors, SpriteRenderer[] coins)
        {
            mainRenderer = background;
            doorsRenderer = doors;
            coinRenderers = coins;
        }
#endif
    }
}
