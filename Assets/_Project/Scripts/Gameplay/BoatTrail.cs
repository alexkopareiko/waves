using Bitgem.VFX.StylisedWater;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(Collider))]
    public class BoatTrail : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Collider _boatCollider = null;

        [Header("Dot Prefabs")]
        [SerializeField] private List<GameObject> _dotPrefabs = new();

        [Header("Trail Layout")]
        [SerializeField, Min(1)] private int _dotCount = 16;
        [SerializeField, Min(0.1f)] private float _netWidth = 4f;
        [SerializeField, Min(0.1f)] private float _netDepth = 6f;
        [SerializeField] private float _forwardOffset = -1f;
        [SerializeField, Min(0f)] private float _horizontalJitter = 0.2f;
        [SerializeField, Min(0f)] private float _forwardJitter = 0.2f;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float _lerpSpeed = 5f;

        [Header("Water Alignment")]
        [SerializeField] private float _waterHeightOffset = 0.05f;

        private readonly List<DotEntry> _dots = new();
        private WaterVolumeHelper _waterHelper;

        private void Awake()
        {
            if (_boatCollider == null)
                _boatCollider = GetComponent<Collider>();
            _waterHelper = WaterVolumeHelper.Instance;
        }

        private void Start()
        {
            BuildTrail();
        }

        private void OnDestroy()
        {
            ClearDots();
        }

        private void Update()
        {
            if (_dots.Count == 0)
                return;

            float lerpFactor = Mathf.Clamp01(_lerpSpeed * Time.deltaTime);
            foreach (DotEntry dot in _dots)
                SmoothDotToTarget(dot, lerpFactor);
        }

        [ContextMenu("Rebuild Trail")]
        private void BuildTrail()
        {
            ClearDots();

            if (_dotPrefabs == null || _dotPrefabs.Count == 0)
            {
                Debug.LogWarning($"[{nameof(BoatTrail)}] Cannot spawn trail because no prefabs were assigned.", this);
                return;
            }

            int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(_dotCount)));
            int rows = Mathf.Max(1, Mathf.CeilToInt((float)_dotCount / columns));

            Vector2 layoutSize = ComputeNetSize();
            float stepX = columns > 1 ? layoutSize.x / (columns - 1) : 0f;
            float stepZ = rows > 1 ? layoutSize.y / (rows - 1) : 0f;
            Vector3 startOffset = new Vector3(-layoutSize.x * 0.5f, 0f, -layoutSize.y * 0.5f + _forwardOffset);

            int created = 0;
            for (int row = 0; row < rows && created < _dotCount; row++)
            {
                float z = startOffset.z + row * stepZ;
                for (int col = 0; col < columns && created < _dotCount; col++)
                {
                    float x = startOffset.x + col * stepX;
                    Vector3 jitter = new Vector3(
                        Random.Range(-_horizontalJitter, _horizontalJitter),
                        0f,
                        Random.Range(-_forwardJitter, _forwardJitter)
                    );

                    Vector3 localOffset = new Vector3(x, 0f, z) + jitter;
                    GameObject prefab = PickRandomPrefab();
                    if (prefab == null)
                        continue;

                    Transform dotTransform = Instantiate(prefab).transform;
                    dotTransform.position = transform.TransformPoint(localOffset);
                    dotTransform.rotation = Quaternion.identity;

                    _dots.Add(new DotEntry(dotTransform, localOffset));
                    created++;
                }
            }

            AlignDotsToWater();
        }

        private void SmoothDotToTarget(DotEntry dot, float lerpFactor)
        {
            if (dot.Transform == null)
                return;

            Vector3 targetWorld = transform.TransformPoint(dot.LocalOffset);
            float waterY = SampleWaterHeight(targetWorld);
            Vector3 lerped = Vector3.Lerp(dot.Transform.position, targetWorld, lerpFactor);
            dot.Transform.position = new Vector3(lerped.x, waterY + _waterHeightOffset, lerped.z);
        }

        private void AlignDotsToWater()
        {
            foreach (DotEntry dot in _dots)
            {
                if (dot.Transform == null)
                    continue;

                Vector3 targetWorld = transform.TransformPoint(dot.LocalOffset);
                float waterY = SampleWaterHeight(targetWorld);
                dot.Transform.position = new Vector3(targetWorld.x, waterY + _waterHeightOffset, targetWorld.z);
            }
        }

        private Vector2 ComputeNetSize()
        {
            Vector2 size = new Vector2(_netWidth, _netDepth);
            if (_boatCollider != null)
            {
                Bounds bounds = _boatCollider.bounds;
                float width = Mathf.Abs(bounds.size.x);
                float depth = Mathf.Abs(bounds.size.z);
                size.x = Mathf.Max(size.x, width * 1.1f);
                size.y = Mathf.Max(size.y, depth * 1.1f);
            }
            return size;
        }

        private float SampleWaterHeight(Vector3 worldPosition)
        {
            if (_waterHelper == null)
                _waterHelper = WaterVolumeHelper.Instance;

            if (_waterHelper != null)
            {
                float? height = _waterHelper.GetHeight(worldPosition);
                if (height.HasValue)
                    return height.Value;
            }

            return transform.position.y;
        }

        private GameObject PickRandomPrefab()
        {
            if (_dotPrefabs == null || _dotPrefabs.Count == 0)
                return null;
            return _dotPrefabs[Random.Range(0, _dotPrefabs.Count)];
        }

        private void ClearDots()
        {
            foreach (DotEntry dot in _dots)
            {
                if (dot.Transform != null)
                    Destroy(dot.Transform.gameObject);
            }
            _dots.Clear();
        }

        private class DotEntry
        {
            public Transform Transform;
            public Vector3 LocalOffset;

            public DotEntry(Transform transform, Vector3 localOffset)
            {
                Transform = transform;
                LocalOffset = localOffset;
            }
        }

        private void OnValidate()
        {
            _dotCount = Mathf.Max(1, _dotCount);
            _netWidth = Mathf.Max(0.1f, _netWidth);
            _netDepth = Mathf.Max(0.1f, _netDepth);
            _horizontalJitter = Mathf.Max(0f, _horizontalJitter);
            _forwardJitter = Mathf.Max(0f, _forwardJitter);
            _lerpSpeed = Mathf.Max(0f, _lerpSpeed);
        }
    }
}
