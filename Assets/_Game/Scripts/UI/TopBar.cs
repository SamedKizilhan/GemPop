using System.Collections.Generic;
using GemPop.Systems;
using TMPro;
using UnityEngine;

namespace GemPop.UI
{
    /// <summary>
    /// Goal panel + move counter. Pure view: it subscribes to GoalTracker/MoveTracker events and redraws;
    /// it never decides anything about the game.
    /// </summary>
    public class TopBar : MonoBehaviour
    {
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private TMP_Text[] tabLabels;
        [SerializeField] private GoalSlot[] slots;
        [SerializeField] private Sprite crateIcon;
        [SerializeField] private Sprite rockIcon;
        [SerializeField] private Sprite coinIcon;

        private GoalTracker _goals;
        private MoveTracker _moves;
        private readonly Dictionary<GoalType, GoalSlot> _slotByGoal = new();

        private void Awake()
        {
            TextOutline.Apply(movesText);
            if (tabLabels != null)
                foreach (TMP_Text label in tabLabels) TextOutline.ApplyBold(label);
        }

        public void Bind(GoalTracker goals, MoveTracker moves)
        {
            Unbind();
            _goals = goals;
            _moves = moves;

            foreach (GoalSlot slot in slots) slot.gameObject.SetActive(false);
            _slotByGoal.Clear();
            int count = Mathf.Min(goals.Goals.Count, slots.Length);
            for (int i = 0; i < count; i++)
            {
                GoalType type = goals.Goals[i];
                slots[i].Setup(IconFor(type));
                ((RectTransform)slots[i].transform).anchoredPosition = SlotPosition(count, i);
                _slotByGoal[type] = slots[i];
            }

            _goals.Changed += RefreshGoals;
            _moves.Changed += RefreshMoves;
            RefreshGoals();
            RefreshMoves(_moves.Remaining);
        }

        private void OnDestroy() => Unbind();

        private void Unbind()
        {
            if (_goals != null) _goals.Changed -= RefreshGoals;
            if (_moves != null) _moves.Changed -= RefreshMoves;
        }

        private void RefreshGoals()
        {
            foreach (var pair in _slotByGoal) pair.Value.SetCount(_goals.Remaining(pair.Key));
        }

        private void RefreshMoves(int remaining) => movesText.text = remaining.ToString();

        /// <summary>Slot offsets inside the goal panel: 1 centered, 2 side by side, 3 as a triangle (like the reference).</summary>
        private static Vector2 SlotPosition(int count, int index)
        {
            const float dx = 46f, dy = 42f;
            Vector2 nudge = new Vector2(0f, -8f);   // the count labels hang below the icons; shift the group down a bit
            switch (count)
            {
                case 1:  return nudge;
                case 2:  return new Vector2(index == 0 ? -dx : dx, 0f) + nudge;
                default: return index switch
                {
                    0 => new Vector2(-dx, dy) + nudge,
                    1 => new Vector2(dx, dy) + nudge,
                    _ => new Vector2(0f, -dy) + nudge,
                };
            }
        }

        private Sprite IconFor(GoalType type) => type switch
        {
            GoalType.Crate => crateIcon,
            GoalType.Rock => rockIcon,
            _ => coinIcon,
        };

#if UNITY_EDITOR
        public void EditorWire(TMP_Text moves, GoalSlot[] goalSlots, Sprite crate, Sprite rock, Sprite coin, TMP_Text[] tabs)
        {
            movesText = moves; slots = goalSlots; crateIcon = crate; rockIcon = rock; coinIcon = coin; tabLabels = tabs;
        }
#endif
    }
}
