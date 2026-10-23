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
                case Vase:       AudioManager.Play(Sfx.VaseBreak); break;
                case Stone:      AudioManager.Play(Sfx.StoneCrack); break;
                case ChaliceBox: AudioManager.Play(Sfx.ChaliceBoxBreak); AudioManager.Play(Sfx.DoorBreak); break;
            }
        }

        public static void ItemDamaged(GridItem item)
        {
            switch (item)
            {
                case Vase:
                    AudioManager.Play(Sfx.VaseCrack);
                    break;
                case ChaliceBox box:
                    // the first hit opens the door (no chalice taken yet); later hits collect chalices
                    if (box.ChalicesRemaining == ChaliceBox.ChalicesPerBox) AudioManager.Play(Sfx.DoorBreak);
                    else AudioManager.Play(Sfx.ChaliceClink);
                    break;
            }
        }
    }
}
