using UnityEngine;

namespace Game
{
    public class Camera : MonoBehaviour, IGameModule
    {
        [SerializeField] private BoatCameraController _boatCameraController = null;

        public BoatCameraController BoatCameraController => _boatCameraController;
        private bool _isInitialized = false;

        public bool IsLoaded => throw new System.NotImplementedException();

        public void Initialize()
        {
            _isInitialized = true;
        }

        public void Load()
        {
            throw new System.NotImplementedException();
        }
    }
}
