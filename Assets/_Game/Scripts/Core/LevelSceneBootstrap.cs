using GemPop.Audio;
using GemPop.Data;
using GemPop.Gameplay;
using GemPop.Items;
using GemPop.Systems;
using GemPop.UI;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace GemPop.Core
{
    /// <summary>
    /// Entry point of the LevelScene: reads the current level number, loads its data,
    /// and (from Day 2 on) hands it to the board. For now it shows the data on screen
    /// and offers editor-only debug keys so the win/lose/menu flow can be tested
    /// before any gameplay exists.
    /// </summary>
    public class LevelSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private BoardController board;
        [SerializeField] private TopBar topBar;
        [SerializeField] private FailPopup failPopup;
        [SerializeField] private WinCelebration winCelebration;
        [SerializeField] private TMP_Text debugLabel;

        private LevelData _level;
        private MoveTracker _moves;
        private GoalTracker _goals;
        private bool _finished;

        private void Start()
        {
            int levelNumber = LevelProgress.CurrentLevel;
            _level = LevelRepository.Load(levelNumber);

            if (_level == null)
            {
                Debug.LogError($"[LevelScene] Could not load level {levelNumber}, returning to menu.");
                SceneLoader.LoadMainScene();
                return;
            }

            _moves = new MoveTracker(_level.MoveCount);
            _moves.Changed += _ => RefreshLabel();
            board.TurnEnded += OnTurnEnded;
            board.ItemDestroyed += OnBoardChanged;
            board.ItemDamaged += OnBoardChanged;
            board.Initialize(_level, _moves);

            _goals = new GoalTracker(board.Board);
            if (topBar != null) topBar.Bind(_goals, _moves);
            RefreshLabel();
        }

        private void OnDestroy()
        {
            if (board == null) return;
            board.TurnEnded -= OnTurnEnded;
            board.ItemDestroyed -= OnBoardChanged;
            board.ItemDamaged -= OnBoardChanged;
        }

        private void OnBoardChanged(GridItem _) => _goals?.Recount();

        private void RefreshLabel()
        {
            if (debugLabel != null)
                debugLabel.text = $"Level {_level.LevelNumber}   moves {_moves.Remaining}   [W] win  [L] lose  [Esc] menu";
        }

#if UNITY_EDITOR
        public void EditorWire(TopBar bar, FailPopup fail, WinCelebration win)
        {
            topBar = bar; failPopup = fail; winCelebration = win; debugLabel = null;
        }
#endif

        /// <summary>
        /// Evaluated only when the board is still: win if every goal is cleared, otherwise lose when no
        /// moves remain. Clearing the last obstacle with the last move is therefore a win.
        /// </summary>
        private void OnTurnEnded()
        {
            if (_finished) return;
            if (_goals.IsComplete) Win();
            else if (!_moves.HasMoves) Lose();
        }

        public void Win()
        {
            if (_finished) return;
            _finished = true;
            board.InputLocked = true;
            Debug.Log($"[LevelScene] WIN level {_level.LevelNumber}");
            AudioManager.StopMusic();
            AudioManager.Play(Sfx.Win);
            LevelProgress.AdvanceToNextLevel();

            if (winCelebration != null) winCelebration.Show(SceneLoader.LoadMainScene);
            else SceneLoader.LoadMainScene();
        }

        public void Lose()
        {
            if (_finished) return;
            _finished = true;
            board.InputLocked = true;
            Debug.Log($"[LevelScene] LOSE level {_level.LevelNumber}");
            AudioManager.StopMusic();
            AudioManager.Play(Sfx.Fail);

            if (failPopup != null) failPopup.Show(SceneLoader.LoadMainScene, SceneLoader.LoadLevelScene);
            else SceneLoader.LoadLevelScene();
        }

#if UNITY_EDITOR
        // Debug shortcuts, compiled only in the editor. New Input System API (the project uses it exclusively).
        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.wKey.wasPressedThisFrame) Win();
            else if (kb.lKey.wasPressedThisFrame) Lose();
            else if (kb.escapeKey.wasPressedThisFrame)
            {
                Debug.Log("[LevelScene] ESC -> loading MainScene");
                SceneLoader.LoadMainScene();
            }
        }
#endif
    }
}
