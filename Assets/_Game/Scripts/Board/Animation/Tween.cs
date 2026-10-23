using System.Collections;
using UnityEngine;

namespace GemPop.Gameplay
{
    /// <summary>
    /// The handful of hand-written animations the game needs. Everything here is plain code driven by
    /// Time.deltaTime: no Physics, no Animator (spec requirement for falling), and no third-party package.
    /// Each method is a coroutine so callers can 'yield return' it and wait for it to finish.
    /// </summary>
    public static class Tween
    {
        // ---- easing curves (t in 0..1) ----
        public static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        public static float EaseInQuad(float t) => t * t;
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        /// <summary>
        /// Gravity fall: speed grows every frame (v += g * dt) up to a cap, position moves by v * dt,
        /// and the item snaps to the target when it would overshoot. Ends with a small landing squash.
        /// </summary>
        public static IEnumerator Fall(Transform t, Vector3 target, float gravity = 85f, float maxSpeed = 28f, float startSpeed = 0f)
        {
            float speed = startSpeed;
            Vector3 pos = t.position;
            pos.x = target.x;

            while (pos.y > target.y)
            {
                speed = Mathf.Min(maxSpeed, speed + gravity * Time.deltaTime);
                pos.y = Mathf.Max(target.y, pos.y - speed * Time.deltaTime);
                t.position = pos;
                yield return null;
            }
            t.position = target;
            yield return Squash(t, 0.9f, 0.08f);
        }

        /// <summary>Moves from the current position to target in 'duration' seconds with ease-out.</summary>
        public static IEnumerator MoveTo(Transform t, Vector3 target, float duration)
        {
            Vector3 start = t.position;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                t.position = Vector3.LerpUnclamped(start, target, EaseInQuad(time / duration));
                yield return null;
            }
            t.position = target;
        }

        /// <summary>Scales to a multiple of the current scale (0 = vanish) in 'duration' seconds.</summary>
        public static IEnumerator ScaleTo(Transform t, float multiplier, float duration)
        {
            Vector3 start = t.localScale;
            Vector3 end = start * multiplier;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                t.localScale = Vector3.LerpUnclamped(start, end, EaseInQuad(time / duration));
                yield return null;
            }
            t.localScale = end;
        }

        /// <summary>Appear with a small overshoot (0 -> 1.1 -> 1). Expects the final scale to be set already.</summary>
        public static IEnumerator PopIn(Transform t, float duration = 0.18f)
        {
            Vector3 final = t.localScale;
            t.localScale = Vector3.zero;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                t.localScale = final * EaseOutBack(time / duration);
                yield return null;
            }
            t.localScale = final;
        }

        /// <summary>Quick vertical squash and recover, used when something lands.</summary>
        public static IEnumerator Squash(Transform t, float squashY, float duration)
        {
            Vector3 normal = t.localScale;
            Vector3 squashed = new Vector3(normal.x * (2f - squashY), normal.y * squashY, normal.z);
            float half = duration * 0.5f;
            for (float time = 0f; time < half; time += Time.deltaTime)
            {
                t.localScale = Vector3.Lerp(normal, squashed, time / half);
                yield return null;
            }
            for (float time = 0f; time < half; time += Time.deltaTime)
            {
                t.localScale = Vector3.Lerp(squashed, normal, time / half);
                yield return null;
            }
            t.localScale = normal;
        }
    }
}
