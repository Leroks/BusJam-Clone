using System.Collections.Generic;
using UnityEngine;

public class PoolService
{
    private readonly Dictionary<string, object> _pools = new();
        private readonly Transform _poolsParentTransform;

        public PoolService()
        {
            GameObject poolsGO = GameObject.Find("Pools");
            if (poolsGO == null)
            {
                poolsGO = new GameObject("Pools");
                Object.DontDestroyOnLoad(poolsGO);
            }
            _poolsParentTransform = poolsGO.transform;
        }

        public ObjectPool<T> RegisterPool<T>(string id, T prefab, int prewarm = 10, string parentName = null) where T : Component
        {
            Transform specificPoolParent = _poolsParentTransform;
            if (!string.IsNullOrEmpty(parentName))
            {
                Transform foundParent = _poolsParentTransform.Find(parentName);
                if (foundParent == null)
                {
                    GameObject newParentGO = new GameObject(parentName);
                    newParentGO.transform.SetParent(_poolsParentTransform);
                    specificPoolParent = newParentGO.transform;
                }
                else
                {
                    specificPoolParent = foundParent;
                }
            }
            
            var pool = new ObjectPool<T>(prefab, prewarm, specificPoolParent);
            _pools[id] = pool;
            return pool;
        }

    public ObjectPool<T> Get<T>(string id) where T : Component => _pools[id] as ObjectPool<T>;
}