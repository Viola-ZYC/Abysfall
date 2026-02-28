using UnityEngine;

namespace EndlessRunner
{
    public class Obstacle : MonoBehaviour
    {
        private bool consumed;

        private void OnEnable()
        {
            consumed = false;
        }

        public bool Consume()
        {
            if (consumed)
            {
                return false;
            }

            consumed = true;
            return true;
        }
    }
}
