using Bitgem.VFX.StylisedWater;
using UnityEngine;

namespace Game
{
    public class Boat : MonoBehaviour, IGameModule
    {

        [SerializeField] private WateverVolumeFloater _floater = null;
        private bool _isInitialized = false;

        bool IGameModule.IsLoaded => _isInitialized;
        public WateverVolumeFloater Floater => _floater;

        public void Load()
        {
            throw new System.NotImplementedException();
        }

        public void Initialize()
        {
           _isInitialized = true;
        }
    }
}
