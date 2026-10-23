using GemPop.Audio;
using GemPop.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GemPop.UI
{
    /// <summary>
    /// MainScene button. Shows "Level N" or "Finished" and opens the LevelScene.
    /// It does not decide what the current level is; it asks LevelProgress and
    /// subscribes to its Changed event so it refreshes itself (Observer pattern).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LevelButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string levelFormat = "Level {0}";
        [SerializeField] private string finishedText = "Finished";

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        private void OnEnable()
        {
            LevelProgress.Changed += OnLevelChanged;
            Refresh();
        }

        private void OnDisable()
        {
            LevelProgress.Changed -= OnLevelChanged;   // always unsubscribe: static events outlive scenes
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        private void OnLevelChanged(int _) => Refresh();

        private void Refresh()
        {
            label.text = LevelProgress.AllLevelsFinished
                ? finishedText
                : string.Format(levelFormat, LevelProgress.CurrentLevel);
        }

        private void OnClicked()
        {
            // Spec: when all levels are finished the button only displays text, there is nothing to play.
            if (LevelProgress.AllLevelsFinished) return;
            AudioManager.Play(Sfx.Click);
            SceneLoader.LoadLevelScene();
        }
    }
}
