using System;
using GemPop.Data;
using UnityEngine;

namespace GemPop.Core
{
    /// <summary>
    /// Persists "which level should the player play next" using PlayerPrefs.
    /// Everything else (LevelButton, editor menu, win flow) goes through this class,
    /// so the storage key and the clamping rules exist in exactly one place.
    /// </summary>
    public static class LevelProgress
    {
        private const string Key = "GemPop.LastPlayedLevel";
        private const int FirstLevel = 1;

        /// <summary>Raised whenever the current level changes (UI can refresh itself).</summary>
        public static event Action<int> Changed;

        /// <summary>
        /// The level the player will play next. Starts at 1.
        /// Can be TotalLevels + 1, which means "all levels finished".
        /// </summary>
        public static int CurrentLevel
        {
            get => Mathf.Max(FirstLevel, PlayerPrefs.GetInt(Key, FirstLevel));
            set
            {
                int clamped = Mathf.Clamp(value, FirstLevel, LevelRepository.TotalLevels + 1);
                PlayerPrefs.SetInt(Key, clamped);
                PlayerPrefs.Save();
                Changed?.Invoke(clamped);
            }
        }

        public static bool AllLevelsFinished => CurrentLevel > LevelRepository.TotalLevels;

        /// <summary>Called after a win. Moves to the next level (or to the "finished" state).</summary>
        public static void AdvanceToNextLevel() => CurrentLevel = CurrentLevel + 1;

        public static void Reset() => CurrentLevel = FirstLevel;
    }
}
