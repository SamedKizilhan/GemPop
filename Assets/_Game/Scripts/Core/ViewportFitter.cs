using UnityEngine;
using UnityEngine.SceneManagement;

namespace GemPop.Core
{
    /// <summary>
    /// Keeps the whole board visible on every aspect ratio.
    /// The game is laid out for a 9:16 view that is 20 world units tall (orthographic size 10) and therefore
    /// 11.25 units wide. Taller screens (9:19.5, 9:20...) would show less width and cut the board's sides,
    /// so the camera zooms out until at least that width fits; the extra space goes above and below.
    /// A tiny persistent watcher re-fits whenever the camera aspect changes (scene load, window resize,
    /// rotation) and also re-covers the scene background sprite. Uses Camera.aspect rather than
    /// Screen.width/height, which the editor reports inconsistently during scene loads.
    /// </summary>
    public static class ViewportFitter
    {
        public const float DesignHeight = 20f;                 // world units visible at 9:16
        public const float DesignWidth = DesignHeight * 9f / 16f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("ViewportFitter");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Watcher>();
            SceneManager.sceneLoaded += (_, _) => Fit();
        }

        public static void Fit()
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) return;

            float aspect = cam.aspect;
            float heightForWidth = DesignWidth / aspect;             // height needed so DesignWidth fits
            cam.orthographicSize = Mathf.Max(DesignHeight, heightForWidth) * 0.5f;

            var background = GameObject.Find("SceneBackground");
            if (background != null && background.TryGetComponent(out SpriteRenderer sr) && sr.sprite != null)
            {
                float viewHeight = cam.orthographicSize * 2f;
                float viewWidth = viewHeight * aspect;
                Vector2 size = sr.sprite.bounds.size;
                background.transform.localScale = Vector3.one * Mathf.Max(viewHeight / size.y, viewWidth / size.x);
                background.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, background.transform.position.z);
            }
        }

        /// <summary>Re-fits when the main camera or its aspect changes.</summary>
        private class Watcher : MonoBehaviour
        {
            private Camera _lastCamera;
            private float _lastAspect;

            private void LateUpdate()
            {
                Camera cam = Camera.main;
                if (cam == null) return;
                if (cam != _lastCamera || !Mathf.Approximately(cam.aspect, _lastAspect))
                {
                    _lastCamera = cam;
                    _lastAspect = cam.aspect;
                    Fit();
                }
            }
        }
    }
}
