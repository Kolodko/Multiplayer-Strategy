using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TurnBasedStrategy.Utils
{
    public class ObjectPool<T> where T : Component
    {
        private readonly Queue<T> _pool = new Queue<T>();
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly int _initialSize;
        
        public ObjectPool(GameObject prefab, Transform parent = null, int initialSize = 10)
        {
            _prefab = prefab;
            _parent = parent;
            _initialSize = initialSize;
            
            InitializePool().Forget();
        }
        
        private async UniTaskVoid InitializePool()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                var obj = CreateNewObject();
                obj.gameObject.SetActive(false);
                _pool.Enqueue(obj);
                
                if (i % 5 == 0)
                {
                    await UniTask.Yield();
                }
            }
        }
        
        public T Get()
        {
            T obj;
            
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
            }
            
            obj.gameObject.SetActive(true);
            return obj;
        }
        
        public void Return(T obj)
        {
            if (obj == null) return;
            
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
        
        private T CreateNewObject()
        {
            var go = Object.Instantiate(_prefab, _parent);
            return go.GetComponent<T>();
        }
        
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                var obj = _pool.Dequeue();
                if (obj != null)
                {
                    Object.Destroy(obj.gameObject);
                }
            }
        }
    }
}