using Bitgem.VFX.StylisedWater;
using System.Collections;
using UnityEngine;

namespace Game
{
    public class Boat : MonoBehaviour, IGameModule
    {
        [Header("FX")]
        [SerializeField] private GameObject _generalHitExplosionFX = null;
        [SerializeField] private GameObject _bigHitExplosionFX = null;
        [Header("Boat Components")]

        [SerializeField] private WateverVolumeFloater _floater = null;
        [SerializeField] private BoatInteriorWaterController _interiorWater = null;
        [SerializeField] private BoatMovementController _movementController = null;
        [SerializeField] private Transform _cameraAnchor = null;

        [Header("Obstacle Impact Settings")]
        [SerializeField] private LayerMask _obstacleLayers = 0;
        [SerializeField, Min(0f)] private float _rotationDuration = 1.1f;
        [SerializeField] private float _sinkDelay = 0.8f;
        [SerializeField, Min(0f)] private float _sinkDuration = 1.8f;
        [SerializeField, Min(0f)] private float _sinkDepth = 4f;
        private bool _isInitialized = false;
        private Rigidbody _rigidbody;
        private bool _isDying;
        private Coroutine _deathCoroutine;

        bool IGameModule.IsLoaded => _isInitialized;
        public WateverVolumeFloater Floater => _floater;
        public BoatInteriorWaterController InteriorWater => _interiorWater;
        public BoatMovementController MovementController => _movementController;
        public Transform CameraAnchor => _cameraAnchor;


        public void Load()
        {
        }

        public void Initialize()
        {
           _isInitialized = true;
           _movementController?.EnableControls(true);
           Debug.Log("Boat Initialized");

           SimpleEventManager.Subscribe(GameEvents.GameStateChanged, OnGameStateChanged);
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        void OnDisable()
        {
            SimpleEventManager.Unsubscribe(GameEvents.GameStateChanged, OnGameStateChanged);
            
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isDying)
                return;

            if (!IsObstacleLayer(collision.gameObject.layer))
                return;

            Vector3 impactPoint = transform.position;
            if (collision.contactCount > 0)
                impactPoint = collision.GetContact(0).point;


            if (collision.gameObject.name.Contains("Mine"))
            {
                HandleObstacleImpact(impactPoint, generalHit: false);
                Destroy(collision.gameObject);
            }
            else 
            {
                HandleObstacleImpact(impactPoint, generalHit: true);
            }


        }

        private void OnGameStateChanged(object gameStateObj)
        {
            GameManager.GameState gameState = (GameManager.GameState)gameStateObj;

            if (gameState != GameManager.GameState.BoatMoving)
            {
                // _floater.SetVerticalOffset(0.032f);
                // _rigidbody.useGravity = false;
                _movementController?.EnableControls(false);
            }
            else
            {
                if (!_isDying) {
                    // _floater.ResetVerticalOffset();
                    // _rigidbody.useGravity = true;
                    _movementController?.EnableControls(true);
                }
            }
        }

        private bool IsObstacleLayer(int layer)
        {
            return (_obstacleLayers.value & (1 << layer)) != 0;
        }

        private void HandleObstacleImpact(Vector3 impactPoint, bool generalHit)
        {
            _isDying = true;
            _movementController?.EnableControls(false);
            _floater.enabled = false;
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }

            SpawnExplosion(impactPoint, generalHit);
            GameManager.Instance?.BoatCameraController?.ShakeOnce(0.4f, 0.3f);

            if (_deathCoroutine != null)
            {
                StopCoroutine(_deathCoroutine);
            }
            _deathCoroutine = StartCoroutine(ObstacleDeathSequence());
        }

        private void SpawnExplosion(Vector3 position, bool generalHit)
        {
            if (generalHit)
            {
                GameObject explosion = Instantiate(_generalHitExplosionFX);
                explosion.transform.position = position;
                explosion.transform.rotation = Quaternion.identity;
                return;
            }
            else
            {
                GameObject explosion = Instantiate(_bigHitExplosionFX);
                explosion.transform.position = position;
                explosion.transform.rotation = Quaternion.identity;
                return;
            }
        }

        private IEnumerator ObstacleDeathSequence()
        {
            Quaternion startRotation = transform.rotation;
            Quaternion targetRotation = startRotation * Quaternion.Euler(0f, 0f, 180f);

            float elapsed = 0f;
            while (elapsed < _rotationDuration)
            {
                float t = _rotationDuration > 0f ? elapsed / _rotationDuration : 1f;
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.rotation = targetRotation;
            if (_sinkDelay > 0f)
            {
                yield return new WaitForSeconds(_sinkDelay);
            }
            float sinkStart = transform.position.y;
            float sinkTarget = sinkStart - _sinkDepth;
            elapsed = 0f;
            while (elapsed < _sinkDuration)
            {
                float t = elapsed / _sinkDuration;
                MoveBoatVertical(Mathf.Lerp(sinkStart, sinkTarget, t));
                elapsed += Time.deltaTime;
                yield return null;
            }

            MoveBoatVertical(sinkTarget);
            GameManager.Instance?.GameOver();
        }

        private void MoveBoatVertical(float y)
        {
            Vector3 position = transform.position;
            position.y = y;
            transform.position = position;
        }

        public void SetInteriorWaterLevel(float normalizedAmount)
        {
            _interiorWater?.SetFillAmount(normalizedAmount);
        }

        public void AddInteriorWater(float normalizedDelta)
        {
            _interiorWater?.AddFill(normalizedDelta);
        }
    }
}
