using GemPop.Core;
using GemPop.Data;
using UnityEditor;
using UnityEngine;

namespace GemPop.EditorTools
{
    /// <summary>
    /// Requirement: "A Unity editor menu item should be implemented to set the last played level number."
    /// Lives in an "Editor" folder, so it is compiled only for the editor and never shipped in a build.
    /// Menu: GemPop > Level Progress
    /// </summary>
    public class LevelProgressMenu : EditorWindow
    {
        private int _levelToSet;

        [MenuItem("GemPop/Level Progress/Set Last Played Level...")]
        private static void Open()
        {
            var window = GetWindow<LevelProgressMenu>("Level Progress");
            window._levelToSet = LevelProgress.CurrentLevel;
            window.minSize = new Vector2(280, 110);
            window.Show();
        }

        [MenuItem("GemPop/Level Progress/Reset To Level 1")]
        private static void ResetToFirst()
        {
            LevelProgress.Reset();
            Debug.Log("[GemPop] Last played level reset to 1.");
        }

        [MenuItem("GemPop/Level Progress/Mark All Levels Finished")]
        private static void MarkFinished()
        {
            LevelProgress.CurrentLevel = LevelRepository.TotalLevels + 1;
            Debug.Log("[GemPop] All levels marked as finished.");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField($"Current level: {LevelProgress.CurrentLevel}   (total levels: {LevelRepository.TotalLevels})");
            EditorGUILayout.Space();
            _levelToSet = EditorGUILayout.IntField("Set last played level", _levelToSet);
            EditorGUILayout.HelpBox($"Valid range: 1 .. {LevelRepository.TotalLevels + 1}  ({LevelRepository.TotalLevels + 1} = all finished)", MessageType.None);

            if (GUILayout.Button("Apply"))
            {
                LevelProgress.CurrentLevel = _levelToSet;
                Debug.Log($"[GemPop] Last played level set to {LevelProgress.CurrentLevel}.");
                Repaint();
            }
        }
    }
}
