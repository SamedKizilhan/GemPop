using System;
using System.Collections.Generic;
using GemPop.Gameplay;
using GemPop.Items;

namespace GemPop.Systems
{
    public enum GoalType { Vase, Stone, Chalice }

    /// <summary>
    /// "Obstacles are goals of levels." Keeps the remaining count per goal type by recounting the board,
    /// which is cheap (<= 100 cells) and can never drift out of sync with the model.
    /// Chalice goal = sum of chalices still inside every box (10 per box).
    /// </summary>
    public class GoalTracker
    {
        private readonly Board _board;
        private readonly Dictionary<GoalType, int> _remaining = new();
        private readonly List<GoalType> _goals = new();

        /// <summary>Goal types present when the level started, in display order.</summary>
        public IReadOnlyList<GoalType> Goals => _goals;
        public bool IsComplete { get; private set; }

        public event Action Changed;

        public GoalTracker(Board board)
        {
            _board = board;
            Count();
            foreach (GoalType type in new[] { GoalType.Chalice, GoalType.Stone, GoalType.Vase })
                if (_remaining[type] > 0) _goals.Add(type);
        }

        public int Remaining(GoalType type) => _remaining.TryGetValue(type, out int n) ? n : 0;

        public void Recount()
        {
            Count();
            Changed?.Invoke();
        }

        private void Count()
        {
            _remaining[GoalType.Vase] = 0;
            _remaining[GoalType.Stone] = 0;
            _remaining[GoalType.Chalice] = 0;

            foreach (GridItem item in _board.AllItems())
            {
                switch (item)
                {
                    case Vase:           _remaining[GoalType.Vase]++; break;
                    case Stone:          _remaining[GoalType.Stone]++; break;
                    case ChaliceBox box: _remaining[GoalType.Chalice] += box.ChalicesRemaining; break;
                }
            }

            IsComplete = true;
            foreach (GoalType type in _goals.Count > 0 ? (IEnumerable<GoalType>)_goals : _remaining.Keys)
                if (_remaining[type] > 0) { IsComplete = false; break; }
        }
    }
}
