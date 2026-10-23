using UnityEngine;

namespace GemPop.Data
{
    /// <summary>
    /// Single place that knows WHERE levels live and HOW they are parsed.
    /// If tomorrow levels come from a server instead of Resources, only this class changes.
    /// </summary>
    public static class LevelRepository
    {
        private const string LevelsFolder = "Levels";   // Assets/_Game/Resources/Levels
        private static int _totalLevels = -1;

        /// <summary>Number of level files shipped with the game (10 shipped).</summary>
        public static int TotalLevels
        {
            get
            {
                if (_totalLevels < 0)
                    _totalLevels = Resources.LoadAll<TextAsset>(LevelsFolder).Length;
                return _totalLevels;
            }
        }

        public static bool Exists(int levelNumber) =>
            levelNumber >= 1 && levelNumber <= TotalLevels;

        public static LevelData Load(int levelNumber)
        {
            string path = $"{LevelsFolder}/level_{levelNumber:00}";
            TextAsset json = Resources.Load<TextAsset>(path);
            if (json == null)
            {
                Debug.LogError($"[LevelRepository] Level file not found: Resources/{path}.json");
                return null;
            }

            LevelData data = JsonUtility.FromJson<LevelData>(json.text);
            if (!data.Validate(out string error))
            {
                Debug.LogError($"[LevelRepository] Level {levelNumber} is invalid: {error}");
                return null;
            }
            return data;
        }
    }
}
