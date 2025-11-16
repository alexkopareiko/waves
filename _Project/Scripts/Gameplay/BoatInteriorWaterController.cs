using Bitgem.VFX.StylisedWater;
using UnityEngine;

namespace Game
{
    public class BoatInteriorWaterController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _waterSurface = null;
        [SerializeField] private Transform _samplePoint = null;
        [SerializeField] private WaterVolumeHelper _waterVolumeHelper = null;

        [Header("Flooding")]
        [SerializeField] private bool _matchGlobalWaterHeight = true;
        [SerializeField] private float _minOffset = -0.1f;
        [SerializeField] private float _maxOffset = 0.5f;
        [SerializeField] [Range(0f, 1f)] private float _fillAmount = 0f;
        [SerializeField] private float _heightSmoothing = 0.15f;

        private float _heightVelocity = 0f;

        public float FillAmount
        {
            get => _fillAmount;
            set => SetFillAmount(value);
        }

        private void Awake()
        {
            if (!_waterVolumeHelper)
            {
                _waterVolumeHelper = WaterVolumeHelper.Instance;
            }
        }

        private void LateUpdate()
        {
            if (!_waterSurface)
            {
                return;
            }

            var offset = Mathf.Lerp(_minOffset, _maxOffset, _fillAmount);

            if (_matchGlobalWaterHeight)
            {
                var samplePosition = _samplePoint ? _samplePoint.position : _waterSurface.position;
                var targetWorldHeight = GetSeaSurfaceHeight(samplePosition) + offset;
                var worldPos = _waterSurface.position;
                worldPos.y = Mathf.SmoothDamp(worldPos.y, targetWorldHeight, ref _heightVelocity, Mathf.Max(0.01f, _heightSmoothing));
                _waterSurface.position = worldPos;
            }
            else
            {
                var local = _waterSurface.localPosition;
                local.y = offset;
                _waterSurface.localPosition = local;
            }
        }

        public void SetFillAmount(float normalizedAmount)
        {
            _fillAmount = Mathf.Clamp01(normalizedAmount);
        }

        public void AddFill(float deltaNormalized)
        {
            SetFillAmount(_fillAmount + deltaNormalized);
        }

        private float GetSeaSurfaceHeight(Vector3 samplePosition)
        {
            if (!_waterVolumeHelper)
            {
                return samplePosition.y;
            }

            return _waterVolumeHelper.GetHeight(samplePosition) ?? samplePosition.y;
        }
    }
}
