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
        [SerializeField] private bool _lookAtTarget = true;
        [SerializeField, Range(0.01f, 1f)] private float _rotationLerp = 0.15f;

        private Vector3 _velocity;

        private Transform Target => _targetOverride != null ? _targetOverride : GetBoatTransform();

        private void LateUpdate()
        {
            Transform target = Target;
            if (target == null)
                return;

            Vector3 desiredPosition = target.TransformPoint(_offset);
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

            if (_lookAtTarget)
            {
                Vector3 lookDirection = target.position - transform.position;
                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, _rotationLerp);
                }
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
    }
}
