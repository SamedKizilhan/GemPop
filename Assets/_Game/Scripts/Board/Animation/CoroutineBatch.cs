using System.Collections;
using UnityEngine;

namespace GemPop.Gameplay
{
    /// <summary>
    /// Starts several coroutines in parallel and lets the caller wait until all of them have finished.
    /// Example: 20 cubes fall at the same time; the turn continues only when the last one has landed.
    /// </summary>
    public class CoroutineBatch
    {
        private readonly MonoBehaviour _host;
        private int _running;

        public CoroutineBatch(MonoBehaviour host)
        {
            _host = host;
        }

        public void Add(IEnumerator routine)
        {
            _running++;
            _host.StartCoroutine(Track(routine));
        }

        private IEnumerator Track(IEnumerator routine)
        {
            yield return routine;
            _running--;
        }

        public IEnumerator WaitAll()
        {
            while (_running > 0) yield return null;
        }
    }
}
