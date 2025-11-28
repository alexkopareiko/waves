using Bitgem.VFX.StylisedWater;
using System.Collections;
using UnityEngine;

namespace Game
{
    public class Boat : MonoBehaviour, IGameModule
    {
        private enum MotorClipType
        {
            None,
            Idle,
            Low,
            High
        }

        [Header("FX")]
        [SerializeField] private GameObject _generalHitExplosionFX = null;
        [SerializeField] private GameObject _bigHitExplosionFX = null;
        [SerializeField] private AudioClip _obstacleHitClip = null;
        [SerializeField] private AudioClip _mineHitClip = null;
        [Header("Boat Components")]

        [SerializeField] private WateverVolumeFloater _floater = null;
        [SerializeField] private BoatInteriorWaterController _interiorWater = null;
        [SerializeField] private BoatMovementController _movementController = null;
        [SerializeField] private Transform _cameraAnchor = null;

        [Header("Motor Audio")]
        [SerializeField] private AudioSource _motorAudioSource = null;
        [SerializeField] private AudioClip _idleMotorClip = null;
        [SerializeField] private AudioClip _lowMotorClip = null;
        [SerializeField] private AudioClip _highMotorClip = null;
        [SerializeField] private AudioClip _shiftUpClip = null;
        [SerializeField] private AudioClip _shiftDownClip = null;

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
        private MotorClipType _currentMotorClip = MotorClipType.None;
        private int _lastGearState;

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
            if (_motorAudioSource != null)
            {
                _motorAudioSource.loop = true;
                _motorAudioSource.playOnAwake = false;
            }
            if (_movementController != null)
            {
                _lastGearState = _movementController.GearState;
            }
        }

        private void Update()
        {
            UpdateGearShiftSound();
            UpdateMotorSound();
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

            if (generalHit)
                SoundManager.Instance?.PlaySoundEffect(_obstacleHitClip, urgent: true);
            else
                SoundManager.Instance?.PlaySoundEffect(_mineHitClip, urgent: true);

            _rigidbody.useGravity = true;
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

        private void UpdateMotorSound()
        {
            if (_movementController == null || _motorAudioSource == null)
            {
                return;
            }

            MotorClipType desiredClip = GetMotorClipForGear(_movementController.GearState);
            AudioClip clip = GetAudioClipForMotorType(desiredClip);

            if (clip == null)
            {
                if (_motorAudioSource.isPlaying)
                {
                    _motorAudioSource.Stop();
                }
                _motorAudioSource.clip = null;
                _currentMotorClip = MotorClipType.None;
                return;
            }

            if (_currentMotorClip != desiredClip || _motorAudioSource.clip != clip)
            {
                _motorAudioSource.clip = clip;
                _motorAudioSource.loop = true;
                _motorAudioSource.Play();
                _currentMotorClip = desiredClip;
                return;
            }

            if (!_motorAudioSource.isPlaying)
            {
                _motorAudioSource.Play();
            }
        }

        private void UpdateGearShiftSound()
        {
            if (_movementController == null || _motorAudioSource == null)
            {
                return;
            }

            int gear = _movementController.GearState;
            if (gear == _lastGearState)
            {
                return;
            }

            AudioClip clip = gear > _lastGearState ? _shiftUpClip : _shiftDownClip;
            if (clip != null)
            {
                _motorAudioSource.PlayOneShot(clip);
            }

            _lastGearState = gear;
        }

        private static MotorClipType GetMotorClipForGear(int gear)
        {
            return gear switch
            {
                0 => MotorClipType.Idle,
                -1 or 1 => MotorClipType.Low,
                -2 or 2 => MotorClipType.High,
                _ => MotorClipType.None
            };
        }

        private AudioClip GetAudioClipForMotorType(MotorClipType clipType)
        {
            return clipType switch
            {
                MotorClipType.Idle => _idleMotorClip,
                MotorClipType.Low => _lowMotorClip,
                MotorClipType.High => _highMotorClip,
                _ => null
            };
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
