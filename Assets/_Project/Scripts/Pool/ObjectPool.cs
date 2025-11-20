using System.Collections.Generic;
using UnityEngine;
using static Game.PoolManagerMono;

namespace Game
{
    public class ObjectPool : MonoBehaviour
    {
        private PoolPrefab _prefab;
        private int _poolSize = 10;
        private PoolType _poolType;

        private List<PoolPrefab> pool = new List<PoolPrefab>();

        public PoolPrefab prefab => _prefab;
        public int poolSize => _poolSize;

        public void Initialize(PoolPrefab prefab, int poolSize)
        {
            _prefab = prefab;
            _poolSize = poolSize;
            _poolType = prefab.poolType;

            // Create the pool
            for (int i = 0; i < _poolSize; i++)
            {
                prefab.gameObject.SetActive(false);
                PoolPrefab pref = Instantiate(_prefab);
                pref.gameObject.SetActive(false);
                pool.Add(pref);
            }
        }


        public GameObject GetObjectFromPool()
        {
            // Find an inactive object in the pool and return it
            foreach (PoolPrefab obj in pool)
            {
                if (!obj.gameObject.activeInHierarchy)
                {
                    //obj.gameObject.SetActive(true);
                    return obj.gameObject;
                }
            }

            // If no inactive object is found, create a new one and add it to the pool
            PoolPrefab newObj = Instantiate(prefab);
            pool.Add(newObj);
            return newObj.gameObject;
        }

        public void ReturnObjectToPool(GameObject obj)
        {
            // Deactivate the object and return it to the pool
            obj.SetActive(false);
        }

        public void RemoveFromPool(GameObject obj)
        {
            pool.Remove(obj.GetComponent<PoolPrefab>());
        }
    }

}