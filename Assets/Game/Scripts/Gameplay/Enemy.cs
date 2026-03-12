using UnityEngine;

namespace EndlessRunner
{
    public class Enemy : Poolable
    {
        private bool isDead;

        public bool IsAlive => !isDead;

        public override void OnSpawned()
        {
            isDead = false;
        }

        public virtual void OnHitByAttack()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;

            if (ObjectPool.Instance != null)
            {
                ObjectPool.Instance.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
