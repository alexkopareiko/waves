using UnityEngine;

namespace Game
{
    /// <summary>
    /// Gear-based ship movement similar to Sunless Sea.
    /// W/S (Up/Down) step through -2..2 gears with inertia, A/D (Left/Right) steers while moving.
    /// </summary>
    public class BoatMovementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Boat _boat = null;
        [SerializeField] private Rigidbody _rigidbody = null;

        [Header("Input")]
        [SerializeField] private KeyCode _gearUpKey = KeyCode.W;
        [SerializeField] private KeyCode _gearUpAltKey = KeyCode.UpArrow;
        [SerializeField] private KeyCode _gearDownKey = KeyCode.S;
        [SerializeField] private KeyCode _gearDownAltKey = KeyCode.DownArrow;
        [SerializeField] private KeyCode _turnLeftKey = KeyCode.A;
        [SerializeField] private KeyCode _turnLeftAltKey = KeyCode.LeftArrow;
        [SerializeField] private KeyCode _turnRightKey = KeyCode.D;
        [SerializeField] private KeyCode _turnRightAltKey = KeyCode.RightArrow;

        [Header("Movement")]
        [SerializeField] private float _gear1Thrust = 6f;
        [SerializeField] private float _gear2Thrust = 10f;
        [SerializeField] private float _turnTorque = 3f;
        [SerializeField] private float _maxSpeed = 6f;
        [SerializeField] private float _linearDrag = 0.25f;
        [SerializeField] private float _angularDrag = 0.6f;
        [SerializeField] private float _minTurningSpeed = 0.5f;
        [SerializeField, Tooltip("How quickly thrust ramps when changing gears.")]
        private float _thrustChangeRate = 6f;
        [SerializeField, Tooltip("Residual drag applied while coasting in neutral.")]
        private float _coastLinearDrag = 0.05f;

        private bool _controlsEnabled = true;
        private int _gearState; // -2, -1, 0, 1, 2
        private float _turnInput;
        private float _currentThrust;

        public int GearState => _gearState;

        public void EnableControls(bool enabled)
        {
            _controlsEnabled = enabled;
            if (!enabled)
            {
                _gearState = 0;
                _turnInput = 0f;
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
        }

        private void Update()
        {
            if (!_controlsEnabled || GameManager.isPaused)
            {
                _turnInput = 0f;
                return;
            }

            HandleGearInput();
            HandleTurnInput();
        }

        private void FixedUpdate()
        {
            if (!_controlsEnabled || GameManager.isPaused)
            {
                ApplyPassiveDrag();
                return;
            }

            ApplyThrust();
            ApplyTurn();
            ApplyPassiveDrag();
            ClampSpeed();
        }

        private void HandleGearInput()
        {
            var gearUpPressed = Input.GetKeyDown(_gearUpKey) || Input.GetKeyDown(_gearUpAltKey);
            var gearDownPressed = Input.GetKeyDown(_gearDownKey) || Input.GetKeyDown(_gearDownAltKey);

            if (gearUpPressed)
            {
                _gearState = Mathf.Clamp(_gearState + 1, -2, 2);
            }
            else if (gearDownPressed)
            {
                _gearState = Mathf.Clamp(_gearState - 1, -2, 2);
            }
        }

        private void HandleTurnInput()
        {
            var left = Input.GetKey(_turnLeftKey) || Input.GetKey(_turnLeftAltKey);
            var right = Input.GetKey(_turnRightKey) || Input.GetKey(_turnRightAltKey);
            _turnInput = 0f;
            if (left && !right)
            {
                _turnInput = -1f;
            }
            else if (right && !left)
            {
                _turnInput = 1f;
            }
        }

        private void ApplyThrust()
        {
            if (!_rigidbody)
            {
                return;
            }

            var targetThrust = 0f;
            if (_gearState != 0)
            {
                targetThrust = (Mathf.Abs(_gearState) == 1 ? _gear1Thrust : _gear2Thrust) * Mathf.Sign(_gearState);
            }

            // Smoothly ramp toward the new gear thrust so gear changes feel weighty.
            _currentThrust = Mathf.MoveTowards(_currentThrust, targetThrust, _thrustChangeRate * Time.fixedDeltaTime);

            if (!Mathf.Approximately(_currentThrust, 0f))
            {
                _rigidbody.AddForce(transform.forward * _currentThrust, ForceMode.Acceleration);
            }
        }

        private void ApplyTurn()
        {
            if (!_rigidbody || Mathf.Approximately(_turnInput, 0f))
            {
                return;
            }

            var forwardSpeed = Vector3.Dot(_rigidbody.linearVelocity, transform.forward);
            if (Mathf.Abs(forwardSpeed) < _minTurningSpeed)
            {
                return; // no turning in place
            }

            var turnDirection = _turnInput * Mathf.Sign(forwardSpeed); // reverse steering when moving backward
            _rigidbody.AddTorque(Vector3.up * (turnDirection * _turnTorque), ForceMode.Acceleration);
        }

        private void ApplyPassiveDrag()
        {
            if (_rigidbody)
            {
                // Use lighter drag when coasting in neutral so the boat drifts.
                var linearDrag = _gearState == 0 ? _coastLinearDrag : _linearDrag;

                if (linearDrag > 0f)
                {
                    _rigidbody.AddForce(-_rigidbody.linearVelocity * linearDrag, ForceMode.Acceleration);
                }

                if (_angularDrag > 0f)
                {
                    _rigidbody.AddTorque(-_rigidbody.angularVelocity * _angularDrag, ForceMode.Acceleration);
                }
            }
        }

        private void ClampSpeed()
        {
            if (!_rigidbody || _maxSpeed <= 0f)
            {
                return;
            }

            var velocity = _rigidbody.linearVelocity;
            var maxSpeedSq = _maxSpeed * _maxSpeed;
            if (velocity.sqrMagnitude > maxSpeedSq)
            {
                _rigidbody.linearVelocity = velocity.normalized * _maxSpeed;
            }
        }
    }
}
