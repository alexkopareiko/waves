using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(ParticleSystem))]
    public class BoatSmokeEmission : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Optional override for the movement controller that drives the gear state.")]
        private BoatMovementController _movementController = null;

        private ParticleSystem.EmissionModule _emission;
        private float _currentRate = float.NaN;

        private void Awake()
        {
            var particleSystem = GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                enabled = false;
                return;
            }

            _emission = particleSystem.emission;
            _movementController ??= GetComponentInParent<BoatMovementController>();
        }

        private void OnEnable()
        {
            _currentRate = float.NaN; // force first update
            UpdateEmissionRate();
        }

        private void Update()
        {
            UpdateEmissionRate();
        }

        private void UpdateEmissionRate()
        {
            if (_movementController == null)
            {
                return;
            }

            float targetRate = GetRateFromGear(_movementController.GearState);
            if (Mathf.Approximately(_currentRate, targetRate))
            {
                return;
            }

            _emission.rateOverTime = targetRate;
            _currentRate = targetRate;
        }

        private static float GetRateFromGear(int gear)
        {
            return gear switch
            {
                -2 => 6f,
                2 => 6f,
                -1 => 4f,
                1 => 4f,
                _ => 2f,
            };
        }
    }
}
