using Bitgem.VFX.StylisedWater;
using System.Collections;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Keeps the camera following the boat with configurable position and rotation smoothing.
    /// Attach to the main camera and assign offsets in the inspector.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class BoatCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _targetOverride = null;
        [SerializeField] private bool _autoAssignBoat = true;

        [Header("Position")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 6f, -10f);
        [SerializeField, Min(0.01f)] private float _positionSmoothTime = 0.2f;
        [SerializeField] private bool _clampToWaterSurface = true;
        [SerializeField] private float _waterSurfaceOffset = 0.5f;

        [Header("Rotation")]
        [SerializeField] private float _rotationSpeed = 120f;
        [SerializeField] private Vector2 _pitchLimits = new Vector2(-25f, 75f);
        [SerializeField] private bool _autoFaceMovement = true;
        [SerializeField, Min(0f)] private float _movementAlignSpeed = 90f;
        [SerializeField, Min(0f)] private float _movementThreshold = 0.05f;

        [Header("Zoom")]
        [SerializeField] private float _zoomSpeed = 8f;
        [SerializeField] private Vector2 _zoomLimits = new Vector2(4f, 20f);

        private Vector3 _velocity;
        private float _yaw;
        private float _pitch;
        private float _distance;
        private bool _initializedAngles;
        private bool _isDragging;
        private bool _isZooming;
        private bool _hasLastTargetPosition;
        private Vector3 _lastTargetPosition;
        private Coroutine _shakeRoutine;
        private Vector3 _shakeOffset;

        private Transform Target => _targetOverride != null ? _targetOverride : GetBoatTransform();

        private void Start()
        {
            InitializeOrbitFromOffset();
        }

        private void LateUpdate()
        {
            Transform target = Target;
            if (target == null)
                return;

            HandleInput();

            bool hasInput = _isDragging || _isZooming;
            TryAlignWithMovement(target, hasInput);

            Quaternion orbitRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredOffset = orbitRotation * new Vector3(0f, 0f, -_distance);
            Vector3 desiredPosition = target.position + desiredOffset;

            if (!hasInput)
            {
                desiredPosition.y = transform.position.y;
            }

            float? minWaterHeight = null;
            if (_clampToWaterSurface && WaterVolumeHelper.Instance != null)
            {
                float? waterHeight = WaterVolumeHelper.Instance.GetHeight(desiredPosition);
                if (waterHeight.HasValue)
                {
                    minWaterHeight = waterHeight.Value + _waterSurfaceOffset;
                    if (desiredPosition.y < minWaterHeight.Value)
                        desiredPosition.y = minWaterHeight.Value;
                }
            }

            Vector3 smoothedPosition = _isDragging
                ? desiredPosition
                : Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, _positionSmoothTime);
            if (minWaterHeight.HasValue && smoothedPosition.y < minWaterHeight.Value)
                smoothedPosition.y = minWaterHeight.Value;

            transform.position = smoothedPosition;
            transform.position += _shakeOffset;

            Vector3 lookDirection = target.position - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                transform.rotation = desiredRotation;
            }

            _lastTargetPosition = target.position;
            _hasLastTargetPosition = true;
        }

        public void ShakeOnce(float duration, float strength)
        {
            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
                _shakeOffset = Vector3.zero;
            }
            _shakeRoutine = StartCoroutine(CameraShakeRoutine(duration, strength));
        }

        private IEnumerator CameraShakeRoutine(float duration, float strength)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float damper = duration > 0f ? 1f - (elapsed / duration) : 0f;
                _shakeOffset = Random.insideUnitSphere * strength * damper;
                _shakeOffset.z = 0f;
                elapsed += Time.deltaTime;
                yield return null;
            }

            _shakeOffset = Vector3.zero;
            _shakeRoutine = null;
        }

        private Transform GetBoatTransform()
        {
            if (!_autoAssignBoat)
                return null;

            if (GameManager.Instance == null)
                return null;

            Transform anchor = GameManager.Instance.Boat.CameraAnchor;
            return anchor;
        }

        private void InitializeOrbitFromOffset()
        {
            _distance = Mathf.Max(0.01f, _offset.magnitude);
            Vector3 direction = _offset.sqrMagnitude > 0.0001f ? _offset.normalized : new Vector3(0f, 0f, -1f);
            _pitch = Mathf.Asin(direction.y) * Mathf.Rad2Deg;
            _yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            _yaw = _yaw < -180f ? _yaw + 360f : _yaw;
            _initializedAngles = true;
        }

        private void HandleInput()
        {
            if (!_initializedAngles)
                InitializeOrbitFromOffset();

            _isDragging = Input.GetMouseButton(0);
            _isZooming = false;

            if (_isDragging)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                _yaw += mouseX * _rotationSpeed * Time.deltaTime;
                _pitch -= mouseY * _rotationSpeed * Time.deltaTime;
                _pitch = Mathf.Clamp(_pitch, _pitchLimits.x, _pitchLimits.y);
            }
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (!Mathf.Approximately(scroll, 0f))
            {
                _distance -= scroll * _zoomSpeed;
                _distance = Mathf.Clamp(_distance, _zoomLimits.x, _zoomLimits.y);
                _isZooming = true;
            }
        }

        private void TryAlignWithMovement(Transform target, bool hasInput)
        {
            if (!_autoFaceMovement || hasInput)
                return;

            if (!_hasLastTargetPosition)
            {
                _lastTargetPosition = target.position;
                _hasLastTargetPosition = true;
                return;
            }

            Vector3 displacement = target.position - _lastTargetPosition;
            displacement.y = 0f; // ignore vertical displacement when computing heading
            float sqrMagnitude = displacement.sqrMagnitude;
            if (sqrMagnitude < _movementThreshold * _movementThreshold)
                return;

            float targetYaw = Mathf.Atan2(displacement.x, displacement.z) * Mathf.Rad2Deg;
            _yaw = Mathf.MoveTowardsAngle(_yaw, targetYaw, _movementAlignSpeed * Time.deltaTime);
        }
    }
}
