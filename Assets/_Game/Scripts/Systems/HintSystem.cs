using System.Collections.Generic;
using GemPop.Gameplay;
using GemPop.Items;

namespace GemPop.Systems
{
    /// <summary>
    /// Spec: "Cube groups should display a special item logo as a hint on them if they are eligible to create one."
    /// Runs after the board settles; every cube gets the hint of the group it belongs to.
    /// </summary>
    public static class HintSystem
    {
        public static void Refresh(Board board)
        {
            List<List<Cube>> groups = MatchFinder.FindAllGroups(board);
            foreach (List<Cube> group in groups)
            {
                HintType hint = MatchFinder.HintFor(group.Count);
                foreach (Cube cube in group) cube.SetHint(hint);
            }
        }
    }
}
