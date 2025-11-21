using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Emits a Skyrim-style horizontal compass with cardinal labels and optional markers for world objects.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CompassUI : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private Transform _referenceTransform = null;
        [SerializeField] private bool _autoAssignReference = true;

        [Header("Appearance")]
        [SerializeField, Min(10f)] private float _visibleAngle = 180f;
        [SerializeField, Min(0f)] private float _markerYOffset = -18f;
        [SerializeField] private Vector2 _markerSize = new Vector2(6f, 24f);
        [SerializeField, Min(8f)] private float _labelFontSize = 16f;
        [SerializeField] private Color _cardinalColor = Color.white;
        [SerializeField] private TMP_FontAsset _labelFontAsset;
        [SerializeField, Tooltip("Fallback sprite used for any targets without their own marker.")]
        private Sprite _fallbackMarkerSprite;
        [SerializeField, Range(0f, 1f)] private float _targetDistanceFadeStart = 0.75f;
        [SerializeField, Range(0f, 0.4f)] private float _markerFadeMin = 0.15f;

        private RectTransform _rectTransform;
        private float _halfWidth;
        private float _lastKnownWidth;
        private Sprite _generatedMarkerSprite;
        private TMP_FontAsset _generatedFontAsset;
        private bool _isInitialized;
        private readonly List<DirectionEntry> _directionEntries = new();
        private readonly Dictionary<CompassTarget, TargetMarker> _targetMarkers = new();

        private static readonly (string Label, float Angle)[] s_directionDefinitions = new[]
        {
            ("N", 0f),
            // ("NE", 45f),
            ("E", 90f),
            // ("SE", 135f),
            ("S", 180f),
            // ("SW", -135f),
            ("W", -90f),
            // ("NW", -45f)
        };

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _rectTransform ??= GetComponent<RectTransform>();

            if (_autoAssignReference && _referenceTransform == null && GameManager.Instance != null)
            {
                _referenceTransform = GameManager.Instance.Boat?.transform;
            }

            if (_directionEntries.Count == 0)
            {
                BuildDirectionEntries();
            }

            _isInitialized = true;
        }

        private void LateUpdate()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            var reference = GetReferenceTransform();
            if (reference == null || _rectTransform == null)
            {
                return;
            }

            UpdateHalfWidth();
            UpdateDirectionEntries(reference);
            UpdateTargetMarkers(reference);
        }

        private Transform GetReferenceTransform()
        {
            if (_referenceTransform != null)
            {
                return _referenceTransform;
            }

            if (_autoAssignReference && GameManager.Instance != null)
            {
                _referenceTransform = GameManager.Instance.Boat?.transform;
            }

            return _referenceTransform;
        }

        private void UpdateHalfWidth()
        {
            if (_rectTransform == null)
            {
                return;
            }

            float width = _rectTransform.rect.width;
            if (width <= 0f)
            {
                return;
            }

            if (!Mathf.Approximately(width, _lastKnownWidth))
            {
                _lastKnownWidth = width;
                _halfWidth = width * 0.5f;
            }
        }

        private void BuildDirectionEntries()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            _directionEntries.Clear();
            foreach (var (label, angle) in s_directionDefinitions)
            {
                _directionEntries.Add(CreateDirectionEntry(label, angle));
            }
        }

        private DirectionEntry CreateDirectionEntry(string label, float angle)
        {
            var go = new GameObject($"CompassLabel_{label}", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.layer = gameObject.layer;

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(48f, 24f);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.font = GetLabelFontAsset();
            text.fontSize = _labelFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = _cardinalColor;
            text.raycastTarget = false;

            return new DirectionEntry(angle, rect, text);
        }

        private void UpdateDirectionEntries(Transform reference)
        {
            if (_directionEntries.Count == 0 || _halfWidth <= 0f)
            {
                return;
            }

            float heading = GetHeading(reference);
            float halfRange = Mathf.Max(5f, _visibleAngle * 0.5f);

            foreach (var entry in _directionEntries)
            {
                float delta = Mathf.DeltaAngle(heading, entry.Angle);
                float normalized = Mathf.Clamp(delta / halfRange, -1f, 1f);
                entry.Rect.anchoredPosition = new Vector2(normalized * _halfWidth, 0f);

                float fade = Mathf.Clamp01(1f - Mathf.Abs(normalized));
                float alpha = Mathf.Lerp(0.0f, 1f, fade);
                entry.Label.color = new Color(_cardinalColor.r, _cardinalColor.g, _cardinalColor.b, alpha);
            }
        }

        // Tracks targets that the compass should point toward and updates their markers.
        private void UpdateTargetMarkers(Transform reference)
        {
            if (_halfWidth <= 0f)
            {
                return;
            }

            CleanupMarkers();
            float heading = GetHeading(reference);
            float halfRange = Mathf.Max(1f, _visibleAngle * 0.5f);
            var referencePosition = reference.position;

            foreach (var target in CompassTarget.AllTargets)
            {
                if (target == null)
                {
                    continue;
                }

                var marker = GetOrCreateMarker(target);

                Vector3 direction = target.transform.position - referencePosition;
                float distance = direction.magnitude;

                if (distance <= Mathf.Epsilon)
                {
                    marker.Rect.anchoredPosition = new Vector2(0f, _markerYOffset);
                    marker.Rect.gameObject.SetActive(true);
                    marker.Image.color = new Color(marker.BaseColor.r, marker.BaseColor.g, marker.BaseColor.b, _markerFadeMin);
                    continue;
                }

                direction.y = 0f;
                if (direction.sqrMagnitude <= 0f)
                {
                    marker.Rect.gameObject.SetActive(false);
                    continue;
                }

                float maxDistance = target.VisibleDistance;
                if (!float.IsInfinity(maxDistance) && distance > maxDistance)
                {
                    marker.Rect.gameObject.SetActive(false);
                    continue;
                }

                float normalizedDistance = float.IsInfinity(maxDistance) ? 0f : Mathf.Clamp01(distance / maxDistance);
                if (!float.IsInfinity(maxDistance) && normalizedDistance >= 1f)
                {
                    marker.Rect.gameObject.SetActive(false);
                    continue;
                }

                float fadeDistance = 1f;
                float fadeStart = Mathf.Clamp01(_targetDistanceFadeStart);
                if (!float.IsInfinity(maxDistance) && maxDistance > 0f && normalizedDistance >= fadeStart)
                {
                    float denom = 1f - fadeStart;
                    float blend = denom <= 0f ? normalizedDistance : Mathf.Clamp01((normalizedDistance - fadeStart) / denom);
                    fadeDistance = Mathf.Clamp01(1f - blend);
                }

                float angleToTarget = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float delta = Mathf.DeltaAngle(heading, angleToTarget);
                float normalizedAngle = Mathf.Clamp(delta / halfRange, -1f, 1f);
                marker.Rect.anchoredPosition = new Vector2(normalizedAngle * _halfWidth, _markerYOffset);

                float angleFade = Mathf.Clamp01(1f - Mathf.Abs(delta) / halfRange);
                float alpha = Mathf.Max(_markerFadeMin, fadeDistance * angleFade);
                marker.Image.color = new Color(marker.BaseColor.r, marker.BaseColor.g, marker.BaseColor.b, alpha);
                marker.Rect.gameObject.SetActive(alpha > 0.05f);
            }
        }

        private TargetMarker GetOrCreateMarker(CompassTarget target)
        {
            if (!_targetMarkers.TryGetValue(target, out var marker))
            {
                marker = CreateMarkerForTarget(target);
                _targetMarkers[target] = marker;
            }
            return marker;
        }

        private TargetMarker CreateMarkerForTarget(CompassTarget target)
        {
            var go = new GameObject($"CompassMarker_{target.name}", typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = _markerSize;

            var image = go.AddComponent<Image>();
            image.sprite = target.MarkerSprite ?? GetFallbackMarkerSprite();
            image.color = target.MarkerColor;
            image.raycastTarget = false;

            return new TargetMarker(rect, image, target.MarkerColor);
        }

        private void CleanupMarkers()
        {
            if (_targetMarkers.Count == 0)
            {
                return;
            }

            var toRemove = new List<CompassTarget>();
            foreach (var kvp in _targetMarkers)
            {
                var target = kvp.Key;
                if (target == null || !CompassTarget.AllTargets.Contains(target))
                {
                    Destroy(kvp.Value.Rect.gameObject);
                    toRemove.Add(target);
                }
            }

            foreach (var target in toRemove)
            {
                _targetMarkers.Remove(target);
            }
        }

        private float GetHeading(Transform reference)
        {
            var forward = reference.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        private TMP_FontAsset GetLabelFontAsset()
        {
            if (_labelFontAsset != null)
            {
                return _labelFontAsset;
            }

            if (_generatedFontAsset != null)
                return _generatedFontAsset;

            var osFontSize = Mathf.Max(1, Mathf.RoundToInt(_labelFontSize));
            var fallbackFont = Font.CreateDynamicFontFromOSFont("Arial", osFontSize);
            if (fallbackFont != null)
            {
                _generatedFontAsset = TMP_FontAsset.CreateFontAsset(fallbackFont);
            }

            if (_generatedFontAsset != null)
            {
                return _generatedFontAsset;
            }

            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            Debug.LogWarning("CompassUI: No TMP font asset assigned and creation failed; assign one in the inspector.", this);
            return null;
        }

        private Sprite GetFallbackMarkerSprite()
        {
            if (_fallbackMarkerSprite != null)
            {
                return _fallbackMarkerSprite;
            }

            if (_generatedMarkerSprite != null)
            {
                return _generatedMarkerSprite;
            }

            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false)
            {
                name = "CompassMarkerFallback",
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            _generatedMarkerSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 100f);
            return _generatedMarkerSprite;
        }

        private class DirectionEntry
        {
            public float Angle { get; }
            public RectTransform Rect { get; }
            public TextMeshProUGUI Label { get; }

            public DirectionEntry(float angle, RectTransform rect, TextMeshProUGUI label)
            {
                Angle = angle;
                Rect = rect;
                Label = label;
            }
        }

        private readonly struct TargetMarker
        {
            public RectTransform Rect { get; }
            public Image Image { get; }
            public Color BaseColor { get; }

            public TargetMarker(RectTransform rect, Image image, Color baseColor)
            {
                Rect = rect;
                Image = image;
                BaseColor = baseColor;
            }
        }
    }
}
