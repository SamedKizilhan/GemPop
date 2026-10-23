using System;
using System.Collections;
using GemPop.Audio;
using GemPop.Gameplay;
using GemPop.Systems.Fx;
using UnityEngine;
using UnityEngine.UI;

namespace GemPop.UI
{
    /// <summary>
    /// Spec: "Celebration particles and animation should be shown to the user", then MainScene loads.
    /// Shows the star with a pop + slow spin and waits for a tap (reference screenshot: "Tap To Continue").
    /// Particles are added on the polish day.
    /// </summary>
    public class WinCelebration : MonoBehaviour
    {
        [SerializeField] private Button tapCatcher;
        [SerializeField] private RectTransform star;
        [SerializeField] private RectTransform title;
        [SerializeField] private float spinDegreesPerSecond = 25f;
        [SerializeField] private Sprite starParticle;
        [SerializeField] private Sprite sparkleParticle;   // additive sprite (black background = transparent)
        [SerializeField] private int particleSortingOrder = 3000;   // above the Screen Space - Camera canvas (2000)

        private Action _onContinue;
        private bool _ready;

        private void Awake()
        {
            tapCatcher.onClick.AddListener(() => { if (!_ready) return; AudioManager.Play(Sfx.Click); _onContinue?.Invoke(); });
        }

        public void Show(Action onContinue)
        {
            _onContinue = onContinue;
            _ready = false;
            gameObject.SetActive(true);
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            // Spec: "Celebration particles and animation should be shown to the user"
            ParticleFx.Celebrate(new Vector3(0f, -9f, 0f), starParticle, sparkleParticle, particleSortingOrder);
            yield return Tween.PopIn(star, 0.45f);
            yield return Tween.PopIn(title, 0.25f);
            _ready = true;
            while (true)
            {
                star.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
                yield return null;
            }
        }

#if UNITY_EDITOR
        public void EditorWire(Button catcher, RectTransform starRect, RectTransform titleRect, Sprite starSprite, Sprite sparkleSprite)
        {
            tapCatcher = catcher; star = starRect; title = titleRect; starParticle = starSprite; sparkleParticle = sparkleSprite;
        }
#endif
    }
}
