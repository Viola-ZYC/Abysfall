using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner
{
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [SerializeField] private bool useAsSingleton = true;
        [SerializeField] private Transform poolRoot;

        private readonly Dictionary<GameObject, Stack<GameObject>> available = new();
        private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();

        private void Awake()
        {
            if (useAsSingleton)
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                Instance = this;
            }

            if (poolRoot == null)
            {
                poolRoot = transform;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                return null;
            }

            if (!available.TryGetValue(prefab, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                available[prefab] = stack;
            }

            GameObject instance;
            if (stack.Count > 0)
            {
                instance = stack.Pop();
            }
            else
            {
                UnityEngine.Object created = Instantiate((UnityEngine.Object)prefab);
                instance = created as GameObject;
                if (instance == null)
                {
                    Debug.LogError("ObjectPool.Get failed: prefab is not a GameObject.", prefab);
                    return null;
                }
            }
            instanceToPrefab[instance] = prefab;
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.SetParent(null);
            instance.SetActive(true);

            IPoolable poolable = instance.GetComponent<IPoolable>();
            poolable?.OnSpawned();

            return instance;
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!instance.activeSelf && instance.transform.parent == poolRoot)
            {
                return;
            }

            if (!instanceToPrefab.TryGetValue(instance, out GameObject prefab))
            {
                Destroy(instance);
                return;
            }

            if (!available.TryGetValue(prefab, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                available[prefab] = stack;
            }

            instance.SetActive(false);
            instance.transform.SetParent(poolRoot);

            IPoolable poolable = instance.GetComponent<IPoolable>();
            poolable?.OnDespawned();

            stack.Push(instance);
        }

        public bool IsPooled(GameObject instance)
        {
            return instance != null && instanceToPrefab.ContainsKey(instance);
        }
    }
}
