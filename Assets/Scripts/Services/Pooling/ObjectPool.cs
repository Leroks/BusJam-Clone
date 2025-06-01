using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    private readonly T _prefab;
        private readonly Transform _root;
        private readonly Stack<T> _cache = new();

        public ObjectPool(T prefab, int prewarm, Transform root)
        {
            _prefab = prefab;
            _root = root;
            Prewarm(prewarm);
        }

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
                Despawn(Create());
        }

        private T Create()
        {
            T instance = Object.Instantiate(_prefab, _root);
            instance.gameObject.SetActive(false);
            return instance;
        }

        public T Spawn()
        {
            T obj = _cache.Count > 0 ? _cache.Pop() : Create();
            obj.gameObject.SetActive(true);
            obj.SendMessage("OnSpawn", SendMessageOptions.DontRequireReceiver);
            return obj;
        }

        public void Despawn(T obj)
        {
            obj.SendMessage("OnDespawn", SendMessageOptions.DontRequireReceiver);
            obj.gameObject.SetActive(false);
            if (obj.transform.parent != _root)
            {
                obj.transform.SetParent(_root);
            }
            _cache.Push(obj);
    }
}
