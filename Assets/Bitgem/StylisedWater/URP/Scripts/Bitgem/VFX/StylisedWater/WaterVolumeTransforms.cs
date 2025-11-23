#region Using statements

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Bitgem.VFX.StylisedWater
{
    [AddComponentMenu("Bitgem/Water  Volume (Transforms)")]
    public class WaterVolumeTransforms : WaterVolumeBase
    {
        [Header("Follow Settings")]
        [SerializeField] private bool _followTarget = false;
        [SerializeField] private Transform _followTransform;
        [SerializeField] private bool _lockYToSeaLevel = true;
        [SerializeField] private float _seaLevelY = 0f;
        [SerializeField] private Vector3 _seaSquareOffset = Vector3.zero;

        /// <summary>
        /// Optional transform to keep this volume centered on (useful to only render water around a moving actor).
        /// </summary>
        public Transform FollowTransform
        {
            get => _followTransform;
            set => _followTransform = value;
        }

        /// <summary>
        /// Enable following a target at runtime (locks Y to the configured sea level when requested).
        /// </summary>
        public void SetFollowTarget(Transform target, bool lockYToSeaLevel = true)
        {
            _followTarget = target != null;
            _followTransform = target;
            _lockYToSeaLevel = lockYToSeaLevel;
            if (_lockYToSeaLevel)
            {
                _seaLevelY = transform.position.y;
            }
        }

        /// <summary>
        /// Update the cached sea level (used when locking Y).
        /// </summary>
        /// <param name="y">World Y value to lock to.</param>
        public void SetSeaLevel(float y)
        {
            _seaLevelY = y;
        }

        #region MonoBehaviour events

        private void LateUpdate()
        {
            return;
            if (!_followTarget || _followTransform == null)
            {
                return;
            }

            Vector3 targetPos = _followTransform.position;
            if (_lockYToSeaLevel)
            {
                targetPos.y = _seaLevelY;
            }
            transform.position = targetPos + _seaSquareOffset;
        }

        private void OnDrawGizmos()
        {
            if (!ShowDebug)
            {
                return;
            }

            // iterate the chldren
            for (var i = 0; i < transform.childCount; i++)
            {
                // grab the local position/scale
                var pos = transform.GetChild(i).localPosition;
                var sca = transform.GetChild(i).localScale / TileSize;

                // fix to the grid
                var x = Mathf.RoundToInt(pos.x / TileSize);
                var y = Mathf.RoundToInt(pos.y / TileSize);
                var z = Mathf.RoundToInt(pos.z / TileSize);

                var drawPos = new Vector3(x, y, z) * TileSize;
                var drawSca = new Vector3(Mathf.RoundToInt(sca.x), Mathf.RoundToInt(sca.y), Mathf.RoundToInt(sca.z)) * TileSize;
                drawPos += drawSca / 2f;
                drawPos += transform.position;
                drawPos -= new Vector3(TileSize, TileSize, TileSize);

                // render as wired volumes
                Gizmos.DrawWireCube(drawPos, drawSca);
            }
        }

        private void OnTransformChildrenChanged()
        {
            Rebuild();
        }

        #endregion

        #region Public methods

        protected override void GenerateTiles(ref bool[,,] _tiles)
        {
            // iterate the chldren
            for (var i = 0; i < transform.childCount; i++)
            {
                // grab the local position/scale
                var pos = transform.GetChild(i).localPosition;
                var sca = transform.GetChild(i).localScale / TileSize;

                // fix to the grid
                var x = Mathf.RoundToInt(pos.x / TileSize);
                var y = Mathf.RoundToInt(pos.y / TileSize);
                var z = Mathf.RoundToInt(pos.z / TileSize);

                // iterate the size of the transform
                for (var ix = x; ix < x + Mathf.RoundToInt(sca.x); ix++)
                {
                    for (var iy = y; iy < y + Mathf.RoundToInt(sca.y); iy++)
                    {
                        for (var iz = z; iz < z + Mathf.RoundToInt(sca.z); iz++)
                        {
                            // validate
                            if (ix < 0 || ix >= MAX_TILES_X || iy < 0 | iy >= MAX_TILES_Y || iz < 0 || iz >= MAX_TILES_Z)
                            {
                                continue;
                            }

                            // add the tile
                            _tiles[ix, iy, iz] = true;
                        }
                    }
                }
            }
        }

        #endregion
    }
}
