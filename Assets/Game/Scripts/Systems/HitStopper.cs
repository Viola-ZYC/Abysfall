using System.Collections;
using UnityEngine;

namespace EndlessRunner
{
    public class HitStopper : MonoBehaviour
    {
        private Coroutine routine;
        private float restoreScale = 1f;
        private bool inHitStop;

        public void Trigger(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            if (Time.timeScale <= 0f && !inHitStop)
            {
                return;
            }

            if (Time.timeScale > 0f)
            {
                restoreScale = Time.timeScale;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(StopRoutine(duration));
        }

        private IEnumerator StopRoutine(float duration)
        {
            inHitStop = true;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = restoreScale;
            inHitStop = false;
            routine = null;
        }
    }
}
