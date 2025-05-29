using System.Collections.Generic;
using UnityEngine;

namespace Services.Pooling
{
    public class PoolService
    {
        private readonly Dictionary<string, object> _pools = new();
        private readonly Transform _root;

        public PoolService()
        {
            _root = new GameObject("Pools").transform;
            Object.DontDestroyOnLoad(_root);
        }

        public ObjectPool<T> RegisterPool<T>(string id, T prefab, int prewarm = 10) where T : Component
        {
            var pool = new ObjectPool<T>(prefab, prewarm, _root);
            _pools[id] = pool;
            return pool;
        }

        public ObjectPool<T> Get<T>(string id) where T : Component =>
            _pools[id] as ObjectPool<T>;
    }
}