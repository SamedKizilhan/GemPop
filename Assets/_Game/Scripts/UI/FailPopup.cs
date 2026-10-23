using System;
using GemPop.Audio;
using GemPop.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace GemPop.UI
{
    /// <summary>Spec: fail popup with a close button (back to MainScene) and a try again button (replay).</summary>
    public class FailPopup : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button tryAgainButton;

        private Action _onClose;
        private Action _onTryAgain;

        private void Awake()
        {
            closeButton.onClick.AddListener(() => { AudioManager.StopSfx(); AudioManager.Play(Sfx.Click); _onClose?.Invoke(); });
            tryAgainButton.onClick.AddListener(() => { AudioManager.StopSfx(); AudioManager.Play(Sfx.Click); _onTryAgain?.Invoke(); });
        }

        public void Show(Action onClose, Action onTryAgain)
        {
            _onClose = onClose;
            _onTryAgain = onTryAgain;
            gameObject.SetActive(true);
            StartCoroutine(Tween.PopIn(panel, 0.25f));
        }

#if UNITY_EDITOR
        public void EditorWire(RectTransform popupPanel, Button close, Button tryAgain)
        {
            panel = popupPanel; closeButton = close; tryAgainButton = tryAgain;
        }
#endif
    }
}
