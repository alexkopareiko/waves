#region Using statements

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Bitgem.VFX.StylisedWater
{
    public class WateverVolumeFloater : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Optional rigidbody to receive horizontal wave drift. Defaults to the one on this object.")]
        [SerializeField] private Rigidbody _rigidbody = null;

        #region Public fields

        public WaterVolumeHelper WaterVolumeHelper = null;
        [Tooltip("Distance used to probe the surface for slope estimation.")]
        public float SurfaceSampleOffset = 0.5f;
        [Tooltip("Vertical offset to match the floater's pivot to the water surface.")]
        public float VerticalOffset = 0f;
        [Tooltip("How quickly the floater rotates to follow the wave normal.")]
        public float RotationLerpSpeed = 5f;
        [Tooltip("How quickly the floater follows vertical wave motion. Higher is snappier, lower is smoother.")]
        public float HeightLerpSpeed = 8f;
        [Tooltip("Enable rotation alignment so the floater rolls with the waves.")]
        public bool AlignRotation = true;
        [Header("Horizontal Drift")]
        [Tooltip("Scales how strongly surface slopes push the floater horizontally.")]
        public float HorizontalDriftStrength = 1.5f;
        [Tooltip("How quickly to blend current velocity toward the target wave drift velocity.")]
        public float DriftResponsiveness = 2f;
        [Tooltip("Caps the horizontal speed introduced by the waves.")]
        public float MaxDriftSpeed = 3f;
        [SerializeField] private float _minRelativeFloadedHeight = 0.05f;
        [SerializeField] private float _maxRelativeFloadedHeight = 0.01f;

        #endregion

        private float _verticalOffsetInitial;

        private void Awake()
        {
            if (!_rigidbody)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }
        }

        void FixedUpdate()
        {
            var dt = Time.fixedDeltaTime;
            var instance = WaterVolumeHelper ? WaterVolumeHelper : WaterVolumeHelper.Instance;
            if (!instance)
            {
                return;
            }

            var currentPosition = _rigidbody ? (Vector3)_rigidbody.position : transform.position;
            var surfaceHeight = instance.GetHeight(currentPosition);
            if (!surfaceHeight.HasValue)
            {
                return;
            }

            var sampleSpacing = Mathf.Max(SurfaceSampleOffset, 0.01f);

            // Sample neighbouring heights to build a slope and normal that represents the local wave shape.
            var sampleXPos = currentPosition + Vector3.right * sampleSpacing;
            var sampleXNeg = currentPosition - Vector3.right * sampleSpacing;
            var sampleZPos = currentPosition + Vector3.forward * sampleSpacing;
            var sampleZNeg = currentPosition - Vector3.forward * sampleSpacing;
            var sampleHeightXPos = instance.GetHeight(sampleXPos) ?? surfaceHeight.Value;
            var sampleHeightXNeg = instance.GetHeight(sampleXNeg) ?? surfaceHeight.Value;
            var sampleHeightZPos = instance.GetHeight(sampleZPos) ?? surfaceHeight.Value;
            var sampleHeightZNeg = instance.GetHeight(sampleZNeg) ?? surfaceHeight.Value;

            // Calculate a downhill vector so steeper slopes push the boat more.
            var slopeX = (sampleHeightXPos - sampleHeightXNeg) / (sampleSpacing * 2f);
            var slopeZ = (sampleHeightZPos - sampleHeightZNeg) / (sampleSpacing * 2f);
            var downhill = new Vector3(-slopeX, 0f, -slopeZ);
            var slopeMagnitude = downhill.magnitude;
            var horizontalDrift = Vector3.zero;
            if (slopeMagnitude > 0.0001f)
            {
                var targetSpeed = Mathf.Min(slopeMagnitude * HorizontalDriftStrength, MaxDriftSpeed);
                horizontalDrift = downhill.normalized * targetSpeed;
            }

            // Apply horizontal drift through physics when possible so it blends with player controls.
            if (_rigidbody)
            {
                var horizontalVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
                var velocityDelta = (horizontalDrift - horizontalVelocity) * DriftResponsiveness;
                _rigidbody.AddForce(new Vector3(velocityDelta.x, 0f, velocityDelta.z), ForceMode.Acceleration);
            }
            else
            {
                currentPosition += horizontalDrift * dt;
            }

            // Vertical bobbing as before (kept separate so only Y is overwritten).
            var currentY = _rigidbody ? _rigidbody.position.y : currentPosition.y;
            var smoothedHeight = Mathf.Lerp(currentY, surfaceHeight.Value, 1f - Mathf.Exp(-HeightLerpSpeed * dt));
            smoothedHeight = Mathf.Clamp(smoothedHeight, surfaceHeight.Value - _minRelativeFloadedHeight, surfaceHeight.Value + _maxRelativeFloadedHeight);
            if (_rigidbody)
            {
                var rbPosition = _rigidbody.position;
                rbPosition.y = smoothedHeight + VerticalOffset;
                _rigidbody.MovePosition(rbPosition);
            }
            else
            {
                currentPosition = transform.position;
                currentPosition.y = smoothedHeight + VerticalOffset;
                transform.position = currentPosition;
            }

            if (!AlignRotation)
            {
                return;
            }

            var tangentX = new Vector3(sampleSpacing, sampleHeightXPos - surfaceHeight.Value, 0f);
            var tangentZ = new Vector3(0f, sampleHeightZPos - surfaceHeight.Value, sampleSpacing);
            var normal = Vector3.Cross(tangentZ, tangentX).normalized;
            if (normal == Vector3.zero)
            {
                return;
            }

            var forwardProjected = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
            if (forwardProjected == Vector3.zero)
            {
                forwardProjected = Vector3.ProjectOnPlane(Vector3.forward, normal).normalized;
            }

            var targetRotation = Quaternion.LookRotation(forwardProjected, normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationLerpSpeed * dt);
        }

        public void SetVerticalOffset(float newOffset)
        {
            if (_verticalOffsetInitial == 0f)
            {
                _verticalOffsetInitial = VerticalOffset;
            }
            VerticalOffset = newOffset;
        }

        public void ResetVerticalOffset()
        {
            VerticalOffset = _verticalOffsetInitial;
        }
    }
}
