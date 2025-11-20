using UnityEngine;
using static Game.PoolManagerMono;

namespace Game
{
    [SelectionBase]
    public class PoolPrefab : MonoBehaviour
    {
        [SerializeField] private PoolType _poolType;
        [SerializeField] private int _poolSize = 10;

        public PoolType poolType => _poolType;
        public int poolSize => _poolSize;
    }
}
