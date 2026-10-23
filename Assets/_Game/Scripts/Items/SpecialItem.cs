namespace GemPop.Items
{
    /// <summary>Rockets and TNTs. They fall like cubes and explode when tapped or damaged (Day 4).</summary>
    public abstract class SpecialItem : GridItem
    {
        public override bool CanFall => true;

        /// <summary>
        /// A damaged special item is destroyed and explodes individually at its own cell;
        /// the board queues that explosion (chain reaction) when it sees this outcome on a SpecialItem.
        /// </summary>
        public override DamageOutcome TakeDamage(DamageInfo info) => DamageOutcome.Destroyed;
    }
}
