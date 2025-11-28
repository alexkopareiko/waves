using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class Tentacles : MonoBehaviour
    {
        private enum TentacleState
        {
            Rising,
            Waiting,
            Falling,
        }

        [System.Serializable]
        private class TentacleInstance
        {
            public GameObject Root;
            public Animator Animator;

            [HideInInspector] public float StartY;
            [HideInInspector] public float TargetY;
            [HideInInspector] public TentacleState State;
            [HideInInspector] public float WaitTimer;
            [HideInInspector] public Vector2 SpawnXZ;
            [HideInInspector] public bool HasPlayedRiseSound;
        }

        [Header("References")]
        [SerializeField] private Transform _boat;
        [SerializeField] private PoolManagerMono _poolManager;

        [Header("Spawn")]
        [SerializeField] private int _tentacleCount = 4;
        [SerializeField, Min(0f)] private float _spawnRadius = 5f;
        [SerializeField, Min(0f)] private float _spawnRadiusVariance = 1f;
        [SerializeField, Min(0f)] private float _minimumSpacing = 4f;
        [SerializeField] private float _yOffsetFromBoat = -0.8f;
        [SerializeField, Min(1)] private int _maxPositionAttempts = 12;
        [SerializeField] private LayerMask _obstacleLayerMask = 0;
        [SerializeField, Min(0f)] private float _obstacleCheckRadius = 1f;
        [SerializeField, Min(0.1f)] private float _spawnInterval = 2f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _riseHeight = 0.4f;
        [SerializeField, Min(0f)] private float _riseSpeed = 0.35f;
        [SerializeField, Min(0f)] private float _stayDuration = 2f;
        [SerializeField, Min(0f)] private float _fallSpeed = 0.25f;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _riseAudioClip;
        [SerializeField, Range(0f, 1f)] private float _riseAudioVolume = 1f;

        [Header("Behavior")]
        [SerializeField] private bool _spawnOnEnable = true;
        private readonly List<TentacleInstance> _tentacles = new();
        private readonly List<Vector2> _spawnedPositions = new();
        private Coroutine _crazySpawnRoutine;

        public void Initialize()
        {
            SimpleEventManager.Subscribe(GameEvents.WaterStateChanged, OnWaterStateChanged);

            if (_spawnOnEnable)
            {
                TrySpawnTentacles();
            }

            if (GameManager.Instance != null)
            {
                OnWaterStateChanged(GameManager.Instance.CurrentWaterState);
            }
        }

        private void OnDisable()
        {
            StopCrazySpawning();
            SimpleEventManager.Unsubscribe(GameEvents.WaterStateChanged, OnWaterStateChanged);
        }

        private void Update()
        {
            if (_tentacles.Count == 0)
                return;

            UpdateTentacles(Time.deltaTime);
        }

        public void TrySpawnTentacles()
        {
            if (!ResolveReferences())
                return;

            if (_tentacles.Count > 0)
                return;

            Vector3 boatPosition = _boat.position;
            _spawnedPositions.Clear();

            for (int i = 0; i < _tentacleCount; i++)
            {
                if (!TryGetSpawnPosition(boatPosition, out Vector3 spawnPosition))
                    continue;

                GameObject tentacleRoot = _poolManager.GetObjectFromPool(PoolManagerMono.PoolType.Tentacles);
                if (tentacleRoot == null)
                    continue;

                tentacleRoot.transform.SetParent(transform, worldPositionStays: true);
                tentacleRoot.transform.rotation = Quaternion.identity;
                tentacleRoot.transform.position = spawnPosition;
                tentacleRoot.SetActive(true);

                Animator animator = tentacleRoot.GetComponentInChildren<Animator>(includeInactive: true);
                SetRandomAnimationFrame(animator);

                TentacleInstance instance = new()
                {
                    Root = tentacleRoot,
                    Animator = animator,
                    StartY = spawnPosition.y,
                    TargetY = spawnPosition.y + _riseHeight,
                    State = TentacleState.Rising,
                    HasPlayedRiseSound = false,
                };

                _tentacles.Add(instance);
                Vector2 spawnXZ = new(spawnPosition.x, spawnPosition.z);
                instance.SpawnXZ = spawnXZ;
                _spawnedPositions.Add(spawnXZ);
            }
        }

        public void ReturnAllTentacles()
        {
            for (int i = _tentacles.Count - 1; i >= 0; i--)
            {
                ReturnTentacleToPool(_tentacles[i]);
            }

            _tentacles.Clear();
            _spawnedPositions.Clear();
        }

        private void StartCrazySpawning()
        {
            if (_crazySpawnRoutine != null)
                return;

            _crazySpawnRoutine = StartCoroutine(CrazySpawnRoutine());
        }

        private void StopCrazySpawning()
        {
            if (_crazySpawnRoutine != null)
            {
                StopCoroutine(_crazySpawnRoutine);
                _crazySpawnRoutine = null;
            }

            ReturnAllTentacles();
        }

        private IEnumerator CrazySpawnRoutine()
        {
            WaitForSeconds wait = new(_spawnInterval);
            while (true)
            {
                TrySpawnTentacles();
                yield return wait;
            }
        }

        private void UpdateTentacles(float deltaTime)
        {
            const float positionTolerance = 0.01f;

            for (int i = _tentacles.Count - 1; i >= 0; i--)
            {
                TentacleInstance instance = _tentacles[i];
                if (instance?.Root == null)
                {
                    _tentacles.RemoveAt(i);
                    continue;
                }

                Transform rootTransform = instance.Root.transform;
                float currentY = rootTransform.position.y;

                switch (instance.State)
                {
                    case TentacleState.Rising:
                    {
                        float nextY = Mathf.MoveTowards(currentY, instance.TargetY, _riseSpeed * deltaTime);
                        SetTentacleY(rootTransform, nextY);

                        if (!instance.HasPlayedRiseSound)
                        {
                            PlayRiseSound();
                            instance.HasPlayedRiseSound = true;
                        }

                        if (Mathf.Abs(nextY - instance.TargetY) <= positionTolerance)
                        {
                            instance.State = TentacleState.Waiting;
                            instance.WaitTimer = 0f;
                        }

                        break;
                    }
                    case TentacleState.Waiting:
                    {
                        instance.WaitTimer += deltaTime;
                        if (instance.WaitTimer >= _stayDuration)
                        {
                            instance.State = TentacleState.Falling;
                        }

                        break;
                    }
                    case TentacleState.Falling:
                    {
                        float nextY = Mathf.MoveTowards(currentY, instance.StartY, _fallSpeed * deltaTime);
                        SetTentacleY(rootTransform, nextY);

                        if (Mathf.Abs(nextY - instance.StartY) <= positionTolerance)
                        {
                            ReturnTentacleToPool(instance);
                            _tentacles.RemoveAt(i);
                        }

                        break;
                    }
                }
            }
        }

        private void OnWaterStateChanged(object payload)
        {
            if (payload is not GameManager.WaterState newState)
                return;

            if (newState == GameManager.WaterState.CRAZY)
            {
                StartCrazySpawning();
            }
            else
            {
                StopCrazySpawning();
            }
        }

        private void ReturnTentacleToPool(TentacleInstance instance)
        {
            if (instance?.Root == null || _poolManager == null)
                return;

            _poolManager.ReturnObjectToPool(PoolManagerMono.PoolType.Tentacles, instance.Root);
            instance.Root.transform.SetParent(_poolManager.transform, worldPositionStays: false);
        }

        private void SetTentacleY(Transform rootTransform, float y)
        {
            Vector3 current = rootTransform.position;
            current.y = y;
            rootTransform.position = current;
        }

        private bool TryGetSpawnPosition(Vector3 boatPosition, out Vector3 position)
        {
            for (int attempt = 0; attempt < _maxPositionAttempts; attempt++)
            {
                float radius = Mathf.Max(0f, _spawnRadius + Random.Range(-_spawnRadiusVariance, _spawnRadiusVariance));
                Vector2 direction = Random.insideUnitCircle.normalized;
                if (direction == Vector2.zero)
                    direction = Vector2.right;

                Vector2 offset = direction * radius;
                Vector3 candidate = boatPosition + new Vector3(offset.x, _yOffsetFromBoat, offset.y);

                if (IsTooClose(candidate) || !IsAreaClear(candidate))
                    continue;

                position = candidate;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        private bool IsTooClose(Vector3 candidate)
        {
            Vector2 candidateXZ = new(candidate.x, candidate.z);

            foreach (Vector2 existingXZ in _spawnedPositions)
            {
                if (Vector2.Distance(candidateXZ, existingXZ) < _minimumSpacing)
                    return true;
            }

            return false;
        }

        private bool IsAreaClear(Vector3 position)
        {
            if (_obstacleLayerMask == 0)
                return true;

            return !Physics.CheckSphere(position, _obstacleCheckRadius, _obstacleLayerMask, QueryTriggerInteraction.Ignore);
        }

        private void SetRandomAnimationFrame(Animator animator)
        {
            if (animator == null)
                return;

            animator.Update(0f);
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            if (state.fullPathHash == 0)
                state = animator.GetNextAnimatorStateInfo(0);

            animator.Play(state.fullPathHash, 0, Random.value);
            animator.Update(0f);
        }

        private bool ResolveReferences()
        {
            if (_poolManager == null)
                _poolManager = PoolManagerMono.Instance;

            if ((_boat == null || _boat == default) && GameManager.Instance != null)
                _boat = GameManager.Instance.Boat?.transform;

            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();

            return _poolManager != null && _boat != null;
        }

        private void PlayRiseSound()
        {
            if (_audioSource == null || _riseAudioClip == null)
                return;

            _audioSource.PlayOneShot(_riseAudioClip, _riseAudioVolume);
        }
    }
}
