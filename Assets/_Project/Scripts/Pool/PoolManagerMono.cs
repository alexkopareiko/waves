using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used to manage pools from main scene. Separate from PoolManagerAuthoring
/// </summary>
namespace Game
{
    public class PoolManagerMono : MonoBehaviour, IGameModule
    {
        public static PoolManagerMono Instance => s_Instance;

        public bool IsLoaded => _isInitialized;

        private static PoolManagerMono s_Instance;
        private bool _isInitialized = false;

        private Dictionary<PoolType, ObjectPool> _pools = new();

        [Header("Water Prefabs")]
        [SerializeField] private List<PoolPrefab> _waterPrefabs = new();


        [Header("Other")]
        [SerializeField] private List<PoolPrefab> _otherPrefabs = new();

        //[SerializeField] private GameObject _lootPrefab;


        public enum PoolType
        {
            // WATER
            WaterCube = 0,

            // FX
            ExplosionFX = 1,

            // OBSTACLES AND MONSTERS
            Tentacles = 2,

            // check on biggest number
        }

        private void OnEnable()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;

            //DontDestroyOnLoad(this.gameObject);

            // Initialize();
        }

        public void Initialize()
        {
            List<PoolPrefab> all = new();

            all.AddRange(_waterPrefabs);
            all.AddRange(_otherPrefabs);

            foreach (PoolPrefab prefab in all)
                CreatePool(prefab.poolType, prefab, prefab.poolSize);

        }

        public void CreatePool(PoolType key, PoolPrefab prefab, int size = 5)
        {
            if (!_pools.ContainsKey(key))
            {
                GameObject poolObject = new GameObject(key + "Pool");
                poolObject.transform.parent = transform;

                ObjectPool objectPool = poolObject.AddComponent<ObjectPool>();
                objectPool.Initialize(prefab, size);

                _pools.Add(key, objectPool);
            }
            else
            {
                Debug.LogWarning("Pool with key " + key + " already exists.");
            }
        }

        /// <summary>
        /// Returns deactivated object from the pool
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public GameObject GetObjectFromPool(PoolType key)
        {
            if (_pools.ContainsKey(key))
            {
                return _pools[key].GetObjectFromPool();
            }

            Debug.LogWarning("Pool with key " + key + " not found.");
            return null;
        }

        public void ReturnObjectToPool(PoolType key, GameObject obj)
        {
            if (_pools.ContainsKey(key))
            {
                _pools[key].ReturnObjectToPool(obj);
            }
            else
            {
                Debug.LogWarning("Pool with key " + key + " not found.");
            }
        }

        public void RemoveObjectFromPool(PoolType key, GameObject obj)
        {
            if (_pools.ContainsKey(key))
            {
                _pools[key].RemoveFromPool(obj);
            }
            else
            {
                Debug.LogWarning("Pool with key " + key + " not found.");
            }
        }

        public void Load()
        {
            
        }

        void IGameModule.Initialize()
        {
            Initialize();
        }

        #region Getters

        #endregion
    }

}
