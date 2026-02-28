using UnityEngine;

namespace EndlessRunner
{
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

    public class Poolable : MonoBehaviour, IPoolable
    {
        public virtual void OnSpawned()
        {
        }

        public virtual void OnDespawned()
        {
        }
    }
}
