using System.Collections;
using UnityEngine;

namespace EndlessRunner
{
    public class HitStopper : MonoBehaviour
    {
        private Coroutine routine;
        private float restoreScale = 1f;
        private bool inHitStop;
        private GameManager gameManager;

        private void Awake()
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
        }

        public void Trigger(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            if (gameManager != null && gameManager.State != GameState.Running && !inHitStop)
            {
                return;
            }

            if (gameManager == null && Time.timeScale <= 0f && !inHitStop)
            {
                return;
            }

            if (gameManager == null && Time.timeScale > 0f)
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
            if (gameManager != null)
            {
                gameManager.RequestPause(this);
            }
            else
            {
                Time.timeScale = 0f;
            }
            yield return new WaitForSecondsRealtime(duration);
            if (gameManager != null)
            {
                gameManager.ReleasePause(this);
            }
            else
            {
                Time.timeScale = restoreScale;
            }
            inHitStop = false;
            routine = null;
        }
    }
}
