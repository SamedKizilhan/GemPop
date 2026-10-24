namespace GemPop.Items
{
    /// <summary>What caused the damage. Obstacles react differently to each (e.g. rocks ignore blasts).</summary>
    public enum DamageSource { Blast, Rocket, Bomb, Combo }

    public enum DamageOutcome { Ignored, Damaged, Destroyed }

    /// <summary>
    /// One hit on one item. EventId identifies the explosion/blast the hit belongs to, so an item can
    /// apply "at most once per event" rules (crate, coin box door) without knowing who hit it.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly DamageSource Source;
        public readonly int EventId;
        public readonly int Amount;

        public DamageInfo(DamageSource source, int eventId, int amount = 1)
        {
            Source = source;
            EventId = eventId;
            Amount = amount;
        }
    }
}
