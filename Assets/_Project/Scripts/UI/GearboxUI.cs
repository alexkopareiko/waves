using System;
using UnityEngine;

namespace Game
{
    public class GearboxUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _lever = null;
        [SerializeField] private BoatMovementController _movementController = null;
        [SerializeField, Tooltip("Time to ease toward the next gear angle.")]
        private float _smoothTime = 0.1f;

        private float _smoothVelocity;
        private bool _isInitialized = false;

        private void Awake()
        {

        }

        private void OnEnable()
        {
            _smoothVelocity = 0f;
        }

        private void Update()
        {
            if (!_lever || !_isInitialized)
            {
                return;
            }

            var targetAngle = GetTargetAngle();
            var currentZ = _lever.localEulerAngles.z;
            var newZ = Mathf.SmoothDampAngle(currentZ, targetAngle, ref _smoothVelocity, _smoothTime);

            var euler = _lever.localEulerAngles;
            euler.z = newZ;
            _lever.localEulerAngles = euler;
        }

        private float GetTargetAngle()
        {
            if (_movementController == null)
            {
                return 0f;
            }

            return _movementController.GearState switch
            {
                -2 => 45f,
                -1 => 25f,
                1 => -25f,
                2 => -45f,
                _ => 0f
            };
        }

        internal void Initialize()
        {
            if (!_movementController && GameManager.Instance != null && GameManager.Instance.Boat != null)
            {
                _movementController = GameManager.Instance.Boat.MovementController;
            }
            _isInitialized = true;
            Debug.Log("GearboxUI initialized.");
        }

    }
}
