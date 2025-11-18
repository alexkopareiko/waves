using Bitgem.VFX.StylisedWater;
using UnityEngine;

namespace Game
{
    public class Boat : MonoBehaviour, IGameModule
    {

        [SerializeField] private WateverVolumeFloater _floater = null;
        [SerializeField] private BoatInteriorWaterController _interiorWater = null;
        [SerializeField] private BoatMovementController _movementController = null;
        private bool _isInitialized = false;

        bool IGameModule.IsLoaded => _isInitialized;
        public WateverVolumeFloater Floater => _floater;
        public BoatInteriorWaterController InteriorWater => _interiorWater;
        public BoatMovementController MovementController => _movementController;

        public void Load()
        {
        }

        public void Initialize()
        {
           _isInitialized = true;
           _movementController?.EnableControls(true);
           Debug.Log("Boat Initialized");
        }

        public void SetInteriorWaterLevel(float normalizedAmount)
        {
            _interiorWater?.SetFillAmount(normalizedAmount);
        }

        public void AddInteriorWater(float normalizedDelta)
        {
            _interiorWater?.AddFill(normalizedDelta);
        }
    }
}
