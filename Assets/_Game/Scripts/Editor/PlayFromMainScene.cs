using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GemPop.EditorTools
{
    /// <summary>
    /// The Play button always starts from MainScene, whichever scene is open in the editor,
    /// so the real flow is exercised: menu -> tap LevelButton -> level.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMainScene
    {
        private const string MainScenePath = "Assets/_Game/Scenes/MainScene.unity";

        // Deferred: on a fresh import the scene assets are not in the database yet when [InitializeOnLoad] runs.
        static PlayFromMainScene() => EditorApplication.delayCall += Apply;

        private static void Apply()
        {
            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
            if (scene == null) { Debug.LogWarning($"[GemPop] MainScene not found at {MainScenePath}"); return; }
            EditorSceneManager.playModeStartScene = scene;
        }
    }
}
