using UnityEngine;

namespace GemPop.Items
{
    /// <summary>2 health, falls, shows a cracked sprite after the first hit.</summary>
    public class Crate : Obstacle
    {
        [SerializeField] private Sprite intactSprite;
        [SerializeField] private Sprite damagedSprite;

        private int _lastEventId = -1;

        public override bool CanFall => true;

        /// <summary>Spec: cleared with two damages, "no more than one damage from a single blast" (we apply that per event).</summary>
        public override DamageOutcome TakeDamage(DamageInfo info)
        {
            if (info.EventId == _lastEventId) return DamageOutcome.Ignored;
            _lastEventId = info.EventId;

            Health = Mathf.Max(0, Health - 1);
            RefreshSprite();
            return Health == 0 ? DamageOutcome.Destroyed : DamageOutcome.Damaged;
        }

        protected override void Awake()
        {
            base.Awake();
            RefreshSprite();
        }

        protected void RefreshSprite()
        {
            mainRenderer.sprite = Health >= maxHealth ? intactSprite : damagedSprite;
        }

#if UNITY_EDITOR
        public void EditorSetSprites(Sprite intact, Sprite damaged)
        {
            intactSprite = intact;
            damagedSprite = damaged;
            maxHealth = 2;
        }
#endif
    }
}
