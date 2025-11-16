#region Using statements

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Bitgem.VFX.StylisedWater
{
    public class WateverVolumeFloater : MonoBehaviour
    {
        #region Public fields

        public WaterVolumeHelper WaterVolumeHelper = null;
        [Tooltip("Distance used to probe the surface for slope estimation.")]
        public float SurfaceSampleOffset = 0.5f;
        [Tooltip("How quickly the floater rotates to follow the wave normal.")]
        public float RotationLerpSpeed = 5f;
        [Tooltip("Enable rotation alignment so the floater rolls with the waves.")]
        public bool AlignRotation = true;

        #endregion

        #region MonoBehaviour events

        void Update()
        {
            var instance = WaterVolumeHelper ? WaterVolumeHelper : WaterVolumeHelper.Instance;
            if (!instance)
            {
                return;
            }

            var currentPosition = transform.position;
            var surfaceHeight = instance.GetHeight(currentPosition);
            if (!surfaceHeight.HasValue)
            {
                return;
            }

            // Vertical bobbing as before.
            currentPosition.y = surfaceHeight.Value;
            transform.position = currentPosition;

            if (!AlignRotation)
            {
                return;
            }

            // Sample neighbouring heights to build a normal that represents the local slope.
            var sampleXPos = currentPosition + Vector3.right * SurfaceSampleOffset;
            var sampleZPos = currentPosition + Vector3.forward * SurfaceSampleOffset;
            var sampleHeightX = instance.GetHeight(sampleXPos) ?? surfaceHeight.Value;
            var sampleHeightZ = instance.GetHeight(sampleZPos) ?? surfaceHeight.Value;

            var tangentX = new Vector3(SurfaceSampleOffset, sampleHeightX - surfaceHeight.Value, 0f);
            var tangentZ = new Vector3(0f, sampleHeightZ - surfaceHeight.Value, SurfaceSampleOffset);
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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationLerpSpeed * Time.deltaTime);
        }

        #endregion
    }
}
