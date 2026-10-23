using UnityEngine;

namespace GemPop.Items
{
    public class Rocket : SpecialItem
    {
        [SerializeField] private Sprite horizontalSprite;
        [SerializeField] private Sprite verticalSprite;
        [SerializeField, Range(0.5f, 1.5f)] private float verticalFill = 0.95f;

        public RocketDirection Direction { get; private set; }

        public void Initialize(RocketDirection direction)
        {
            Direction = direction;
            mainRenderer.sprite = direction == RocketDirection.Horizontal ? horizontalSprite : verticalSprite;
        }

        /// <summary>The vertical sprite's artwork fills its texture more than the horizontal one, so it gets its own factor.</summary>
        protected override float FillFactor => Direction == RocketDirection.Vertical ? verticalFill : cellFill;

#if UNITY_EDITOR
        public void EditorSetSprites(Sprite horizontal, Sprite vertical)
        {
            horizontalSprite = horizontal;
            verticalSprite = vertical;
        }
#endif
    }
}
