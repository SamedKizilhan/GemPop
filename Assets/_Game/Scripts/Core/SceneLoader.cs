using UnityEngine.SceneManagement;

namespace GemPop.Core
{
    /// <summary>
    /// The only place that knows scene names. If a scene is renamed, one constant changes.
    /// Both scenes must be added to the Build Profiles > Scene List, otherwise LoadScene fails.
    /// </summary>
    public static class SceneLoader
    {
        public const string MainScene = "MainScene";
        public const string LevelScene = "LevelScene";

        public static void LoadMainScene() => SceneManager.LoadScene(MainScene);
        public static void LoadLevelScene() => SceneManager.LoadScene(LevelScene);
    }
}
