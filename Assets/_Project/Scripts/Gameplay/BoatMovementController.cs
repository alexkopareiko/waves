using UnityEngine;

namespace Game
{
    /// <summary>
    /// Handles player driven rowing by mapping key presses to per-oar thrust
    /// and simple oar visuals.
    /// </summary>
    public class BoatMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Boat _boat = null;
        [SerializeField] private Rigidbody _rigidbody = null;
        [SerializeField] private Transform _leftOar = null;
        [SerializeField] private Transform _rightOar = null;

        [Header("Input")]
        [SerializeField] private KeyCode _leftPrimaryKey = KeyCode.A;
        [SerializeField] private KeyCode _leftAltKey = KeyCode.LeftArrow;
        [SerializeField] private KeyCode _rightPrimaryKey = KeyCode.D;
        [SerializeField] private KeyCode _rightAltKey = KeyCode.RightArrow;

        [Header("Movement")]
        [SerializeField] private float _strokeForce = 6f;
        [SerializeField] private float _turnForce = 4f;
        [SerializeField] private float _maxSpeed = 5f;
        [SerializeField] private float _linearDrag = 0.4f;
        [SerializeField] private float _angularDrag = 0.75f;
        [SerializeField] private float _strokeCooldown = 0.75f;

        [Header("Oar Animation")]
        [SerializeField] private Vector3 _oarRotationAxis = Vector3.right;
        [SerializeField] private float _oarStrokeAngle = 45f;
        [SerializeField] private float _oarRecoveryAngle = -20f;
        [SerializeField] private float _oarAnimationSpeed = 6f;

        private Quaternion _leftOarRestRotation;
        private Quaternion _rightOarRestRotation;
        private float _leftOarBlend = 0f;
        private float _rightOarBlend = 0f;
        private bool _controlsEnabled = true;
        private bool _leftInput;
        private bool _rightInput;
        private int _leftStrokesQueued;
        private int _rightStrokesQueued;
        private float _leftNextStrokeTime = 0f;
        private float _rightNextStrokeTime = 0f;

        public void EnableControls(bool enabled)
        {
            _controlsEnabled = enabled;
            if (!enabled)
            {
                _leftInput = false;
                _rightInput = false;
                _leftStrokesQueued = 0;
                _rightStrokesQueued = 0;
                _leftNextStrokeTime = 0f;
                _rightNextStrokeTime = 0f;
            }
        }

        private void Awake()
        {
            if (!_boat)
            {
                _boat = GetComponent<Boat>();
            }

            if (!_rigidbody)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            CacheRestRotations();

            if (_oarRotationAxis == Vector3.zero)
            {
                _oarRotationAxis = Vector3.right;
            }
            else
            {
                _oarRotationAxis.Normalize();
            }
        }

        private void CacheRestRotations()
        {
            if (_leftOar)
            {
                _leftOarRestRotation = _leftOar.localRotation;
            }

            if (_rightOar)
            {
                _rightOarRestRotation = _rightOar.localRotation;
            }
        }

        private void Update()
        {
            if (!_controlsEnabled || GameManager.isPaused)
            {
                _leftInput = false;
                _rightInput = false;
                _leftStrokesQueued = 0;
                _rightStrokesQueued = 0;
                _leftNextStrokeTime = 0f;
                _rightNextStrokeTime = 0f;
                UpdateOarVisual(_leftOar, _leftOarRestRotation, ref _leftOarBlend, 0f);
                UpdateOarVisual(_rightOar, _rightOarRestRotation, ref _rightOarBlend, 0f);
                return;
            }

            _leftInput = Input.GetKey(_leftPrimaryKey) || Input.GetKey(_leftAltKey);
            _rightInput = Input.GetKey(_rightPrimaryKey) || Input.GetKey(_rightAltKey);
            TryQueueStroke(_leftPrimaryKey, _leftAltKey, _leftInput, ref _leftStrokesQueued, ref _leftNextStrokeTime);
            TryQueueStroke(_rightPrimaryKey, _rightAltKey, _rightInput, ref _rightStrokesQueued, ref _rightNextStrokeTime);

            UpdateOarVisual(_leftOar, _leftOarRestRotation, ref _leftOarBlend, _leftInput ? 1f : 0f);
            UpdateOarVisual(_rightOar, _rightOarRestRotation, ref _rightOarBlend, _rightInput ? 1f : 0f);
        }

        private void FixedUpdate()
        {
            if (!_controlsEnabled || GameManager.isPaused)
            {
                ApplyPassiveDrag();
                return;
            }

            var leftStroke = ConsumeStroke(ref _leftStrokesQueued);
            var rightStroke = ConsumeStroke(ref _rightStrokesQueued);
            ApplyStrokeMovement(leftStroke, rightStroke);
        }

        private bool ConsumeStroke(ref int queuedStrokeCount)
        {
            if (queuedStrokeCount <= 0)
            {
                return false;
            }

            queuedStrokeCount--;
            return true;
        }

        private void ApplyStrokeMovement(bool leftStroke, bool rightStroke)
        {
            if (!leftStroke && !rightStroke)
            {
                ApplyPassiveDrag();
                return;
            }

            var forwardStrokes = 0;
            if (leftStroke)
            {
                forwardStrokes++;
            }
            if (rightStroke)
            {
                forwardStrokes++;
            }

            if (_rigidbody)
            {
                if (forwardStrokes > 0)
                {
                    var forwardImpulse = transform.forward * (_strokeForce * (forwardStrokes * 0.5f));
                    _rigidbody.AddForce(forwardImpulse, ForceMode.Impulse);
                }

                if (leftStroke ^ rightStroke)
                {
                    var turnDirection = rightStroke ? 1f : -1f;
                    _rigidbody.AddTorque(Vector3.up * (turnDirection * _turnForce), ForceMode.Impulse);
                }

                ApplyPassiveDrag();

                if (_maxSpeed > 0f)
                {
                    var velocity = _rigidbody.linearVelocity;
                    var maxSpeedSq = _maxSpeed * _maxSpeed;
                    if (velocity.sqrMagnitude > maxSpeedSq)
                    {
                        _rigidbody.linearVelocity = velocity.normalized * _maxSpeed;
                    }
                }
            }
            else
            {
                // Fallback to manual transform changes if a rigidbody is not available.
                if (forwardStrokes > 0)
                {
                    var delta = transform.forward * (_strokeForce * (forwardStrokes * 0.5f)) * Time.fixedDeltaTime;
                    transform.position += delta;
                }

                if (leftStroke ^ rightStroke)
                {
                    var turnDirection = rightStroke ? 1f : -1f;
                    var angle = turnDirection * _turnForce * Time.fixedDeltaTime;
                    transform.Rotate(Vector3.up, angle, Space.World);
                }

                ApplyPassiveDrag();
            }
        }

        private void TryQueueStroke(KeyCode primaryKey, KeyCode altKey, bool inputHeld, ref int strokeQueue, ref float nextStrokeTime)
        {
            if (!inputHeld)
            {
                return;
            }

            var strokeStarted = Input.GetKeyDown(primaryKey) || Input.GetKeyDown(altKey);
            if (!strokeStarted || Time.time < nextStrokeTime)
            {
                return;
            }

            strokeQueue++;
            nextStrokeTime = Time.time + _strokeCooldown;
        }

        private void ApplyPassiveDrag()
        {
            if (_rigidbody)
            {
                if (_linearDrag > 0f)
                {
                    _rigidbody.AddForce(-_rigidbody.linearVelocity * _linearDrag, ForceMode.Acceleration);
                }

                if (_angularDrag > 0f)
                {
                    _rigidbody.AddTorque(-_rigidbody.angularVelocity * _angularDrag, ForceMode.Acceleration);
                }
            }
        }

        private void UpdateOarVisual(Transform oar, Quaternion restRotation, ref float blend, float targetBlend)
        {
            if (!oar)
            {
                return;
            }

            blend = Mathf.MoveTowards(blend, targetBlend, _oarAnimationSpeed * Time.deltaTime);
            var targetAngle = Mathf.Lerp(_oarRecoveryAngle, _oarStrokeAngle, blend);
            var offset = Quaternion.AngleAxis(targetAngle, _oarRotationAxis);
            oar.localRotation = restRotation * offset;
        }
    }
}
