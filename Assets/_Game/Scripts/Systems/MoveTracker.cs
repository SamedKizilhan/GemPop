using System;

namespace GemPop.Systems
{
    /// <summary>Counts the remaining moves of a level. Pure C#; UI subscribes to Changed.</summary>
    public class MoveTracker
    {
        public int Remaining { get; private set; }
        public bool HasMoves => Remaining > 0;

        public event Action<int> Changed;

        public MoveTracker(int totalMoves)
        {
            Remaining = totalMoves;
        }

        /// <summary>Spec: "A move is spent after a valid tap operation."</summary>
        public void Spend()
        {
            if (Remaining <= 0) return;
            Remaining--;
            Changed?.Invoke(Remaining);
        }
    }
}
