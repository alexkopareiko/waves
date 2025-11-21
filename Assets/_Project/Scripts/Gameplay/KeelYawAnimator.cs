using UnityEngine;

namespace Game
{
    /// <summary>
    /// Rotates the assigned keel transform around its local Y axis based on the current turn input.
    /// </summary>
    public class KeelYawAnimator : MonoBehaviour
    {
        [Tooltip("Keel transform that should twist when the boat turns. Defaults to this transform.")]
        [SerializeField] private Transform _keel = null;
        [Tooltip("Boat movement controller that provides the turn input direction.")]
        [SerializeField] private BoatMovementController _movementController = null;
        [Tooltip("Maximum yaw offset in degrees (positive for left turns, negative for right turns).")]
        [SerializeField, Range(0f, 90f)] private float _maxYawDegrees = 45f;
        [Tooltip("Degrees per second to smooth toward the target yaw.")]
        [SerializeField, Min(0f)] private float _smoothSpeed = 180f;

        private Vector3 _baseEuler;
        private float _currentYaw;

        private void Awake()
        {
            if (_keel == null)
            {
                _keel = transform;
            }

            _baseEuler = _keel.localEulerAngles;
            if (_movementController == null)
            {
                _movementController = GetComponent<BoatMovementController>() ?? GetComponentInParent<BoatMovementController>();
            }
        }

        private void LateUpdate()
        {
            if (_keel == null || _movementController == null)
            {
                return;
            }

            float targetYaw = -_movementController.TurnInput * _maxYawDegrees;
            _currentYaw = Mathf.MoveTowards(_currentYaw, targetYaw, _smoothSpeed * Time.deltaTime);

            Vector3 euler = _baseEuler;
            euler.y += _currentYaw;
            _keel.localEulerAngles = euler;
        }
    }
}
