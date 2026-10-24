namespace GemPop.Items
{
    /// <summary>1 health, does not fall, only special items can damage it.</summary>
    public class Rock : Obstacle
    {
        public override DamageOutcome TakeDamage(DamageInfo info)
        {
            if (info.Source == DamageSource.Blast) return DamageOutcome.Ignored;   // spec: "only from a special item explosion"
            Health = 0;
            return DamageOutcome.Destroyed;
        }
    }
}
