using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Marks a world object so the compass can display a marker pointing toward it.
    /// </summary>
    public class CompassTarget : MonoBehaviour
    {
        [SerializeField] private Sprite _markerSprite = null;
        [SerializeField] private Color _markerColor = new Color(0.4f, 0.85f, 1f, 1f);
        [SerializeField, Tooltip("Maximum distance (in world units) that this target should remain visible. Set to 0 for infinite.")]
        private float _visibleDistance = 500f;

        private static readonly HashSet<CompassTarget> s_instances = new HashSet<CompassTarget>();

        public Sprite MarkerSprite => _markerSprite;
        public Color MarkerColor => _markerColor;
        public float VisibleDistance => _visibleDistance <= 0f ? float.PositiveInfinity : _visibleDistance;

        internal static ICollection<CompassTarget> AllTargets => s_instances;

        private void OnEnable()
        {
            s_instances.Add(this);
        }

        private void OnDisable()
        {
            s_instances.Remove(this);
        }

        private void OnDestroy()
        {
            s_instances.Remove(this);
        }
    }
}
