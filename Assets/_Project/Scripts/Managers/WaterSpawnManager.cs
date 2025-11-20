using System.Collections.Generic;
using Bitgem.VFX.StylisedWater;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Keeps a grid of pooled water cubes centered around the boat.
    /// </summary>
    public class WaterSpawnManager : MonoBehaviour, IGameModule
    {
        [Header("References")]
        [SerializeField] private Transform _ship;
        [SerializeField] private PoolManagerMono _poolManager;
        [SerializeField] private WaterVolumeTransforms _waterVolume;

        [Header("Grid Settings")]
        [SerializeField] private Vector3 _cellSize = new Vector3(20f, 10f, 20f);
        [SerializeField] private Vector2Int _gridRadius = new Vector2Int(2, 2);
        [SerializeField] private float _fixedYLevel = 0f;
        [SerializeField] private Vector3 _gridOriginOffset = Vector3.zero;

        private readonly Dictionary<Vector2Int, GameObject> _activeCells = new();
        private Vector2Int _lastCenterCell;
        private bool _hasInitializedGrid = false;
        private bool _isInitialized = false;

        public bool IsLoaded => _isInitialized;
        


        private void OnDisable()
        {
            DespawnAll();
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            Vector2Int currentCenter = WorldToCell(_ship.position);
            if (!_hasInitializedGrid || currentCenter != _lastCenterCell)
            {
                RefreshGrid(force: false);
                _lastCenterCell = currentCenter;
                _hasInitializedGrid = true;
            }
        }

        public void Initialize()
        {
            TryResolveReferences();
            RefreshGrid(force: true);
            _isInitialized = true;
        }

        private bool TryResolveReferences()
        {
            if (_ship == null && GameManager.Instance != null)
                _ship = GameManager.Instance.Boat != null ? GameManager.Instance.Boat.transform : null;

            if (_poolManager == null)
                _poolManager = PoolManagerMono.Instance;

            if (_waterVolume == null && GameManager.Instance != null)
                _waterVolume = GameManager.Instance.WaterVolumeTransforms;

            return _ship != null && _poolManager != null && _waterVolume != null;
        }

        private void RefreshGrid(bool force)
        {
            if (_ship == null || _poolManager == null)
                return;

            Vector2Int centerCell = WorldToCell(_ship.position);
            HashSet<Vector2Int> desiredCells = new();

            for (int dx = -_gridRadius.x; dx <= _gridRadius.x; dx++)
            {
                for (int dz = -_gridRadius.y; dz <= _gridRadius.y; dz++)
                {
                    Vector2Int cell = new Vector2Int(centerCell.x + dx, centerCell.y + dz);
                    desiredCells.Add(cell);

                    if (force || !_activeCells.ContainsKey(cell))
                        SpawnCell(cell);
                }
            }

            List<Vector2Int> cellsToRemove = new();
            foreach (KeyValuePair<Vector2Int, GameObject> kvp in _activeCells)
            {
                if (!desiredCells.Contains(kvp.Key))
                    cellsToRemove.Add(kvp.Key);
            }

            foreach (Vector2Int cell in cellsToRemove)
                DespawnCell(cell);
        }

        private void SpawnCell(Vector2Int cell)
        {
            if (_activeCells.ContainsKey(cell) || _poolManager == null)
                return;

            GameObject pooled = _poolManager.GetObjectFromPool(PoolManagerMono.PoolType.WaterCube);
            if (pooled == null)
                return;

            pooled.transform.SetPositionAndRotation(CellToWorld(cell), Quaternion.identity);
            pooled.transform.SetParent(_waterVolume.transform, worldPositionStays: true);
            pooled.SetActive(true);

            _activeCells[cell] = pooled;
        }

        private void DespawnCell(Vector2Int cell)
        {
            if (!_activeCells.TryGetValue(cell, out GameObject obj) || _poolManager == null)
                return;

            _poolManager.ReturnObjectToPool(PoolManagerMono.PoolType.WaterCube, obj);
            obj.transform.SetParent(PoolManagerMono.Instance.transform, worldPositionStays: false);
            _activeCells.Remove(cell);
        }

        private void DespawnAll()
        {
            if (_poolManager == null)
                return;

            foreach (GameObject obj in _activeCells.Values)
                _poolManager.ReturnObjectToPool(PoolManagerMono.PoolType.WaterCube, obj);

            _activeCells.Clear();
            _hasInitializedGrid = false;
        }

        private Vector2Int WorldToCell(Vector3 position)
        {
            Vector3 adjusted = position - _gridOriginOffset;

            int x = Mathf.FloorToInt(adjusted.x / _cellSize.x);
            int z = Mathf.FloorToInt(adjusted.z / _cellSize.z);

            return new Vector2Int(x, z);
        }

        private Vector3 CellToWorld(Vector2Int cell)
        {
            // Anchor is the bottom-left-back corner of each cube.
            float x = cell.x * _cellSize.x;
            float z = cell.y * _cellSize.z;
            return new Vector3(x, _fixedYLevel, z) + _gridOriginOffset;
        }

        public void Load()
        {
            
        }

    }
}
