using GemPop.Items;

namespace GemPop.Audio
{
    /// <summary>Which sound an item event gets. Mirrors BoardFx for particles; gameplay code never names a sound.</summary>
    public static class BoardSfx
    {
        public static void ItemDestroyed(GridItem item)
        {
            switch (item)
            {
                case Cube:       AudioManager.Play(Sfx.CubePop); break;
                case Crate:       AudioManager.Play(Sfx.CrateBreak); break;
                case Rock:      AudioManager.Play(Sfx.RockCrack); break;
                case Chest: AudioManager.Play(Sfx.ChestBreak); AudioManager.Play(Sfx.DoorBreak); break;
            }
        }

        public static void ItemDamaged(GridItem item)
        {
            switch (item)
            {
                case Crate:
                    AudioManager.Play(Sfx.CrateCrack);
                    break;
                case Chest box:
                    // the first hit opens the door (no coin taken yet); later hits collect coins
                    if (box.CoinsRemaining == Chest.CoinsPerBox) AudioManager.Play(Sfx.DoorBreak);
                    else AudioManager.Play(Sfx.CoinClink);
                    break;
            }
        }
    }
}
