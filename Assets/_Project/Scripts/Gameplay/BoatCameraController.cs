using Bitgem.VFX.StylisedWater;
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
        [SerializeField, Range(0.01f, 1f)] private float _rotationLerp = 0.15f;

        [Header("Zoom")]
        [SerializeField] private float _zoomSpeed = 8f;
        [SerializeField] private Vector2 _zoomLimits = new Vector2(4f, 20f);

        private Vector3 _velocity;
        private float _yaw;
        private float _pitch;
        private float _distance;
        private bool _initializedAngles;

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

            Quaternion orbitRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredOffset = orbitRotation * new Vector3(0f, 0f, -_distance);
            Vector3 desiredPosition = target.position + desiredOffset;
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

            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, _positionSmoothTime);
            if (minWaterHeight.HasValue && smoothedPosition.y < minWaterHeight.Value)
                smoothedPosition.y = minWaterHeight.Value;

            transform.position = smoothedPosition;

            Vector3 lookDirection = target.position - transform.position;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, _rotationLerp);
            }
        }

        private Transform GetBoatTransform()
        {
            if (!_autoAssignBoat)
                return null;

            if (GameManager.Instance == null)
                return null;

            Boat boat = GameManager.Instance.Boat;
            return boat != null ? boat.transform : null;
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

            bool isDragging = Input.GetMouseButton(0);

            if (isDragging)
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
            }
        }
    }
}
