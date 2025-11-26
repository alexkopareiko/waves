using System.Collections;
using UnityEngine;

namespace Game
{
    public class CameraController : MonoBehaviour
    {

        [Header("General References")]
        [SerializeField] private BoatCameraController _boatCameraController;
        [SerializeField] private UnityEngine.Camera _mainCamera;

        [Header("Camera Prefab References")]
        [SerializeField] private UnityEngine.Camera _introSceneCameraPrefab;
        [SerializeField] private UnityEngine.Camera _winSceneCameraPrefab;
        [SerializeField] private UnityEngine.Camera _pauseMenuCameraPrefab;
        [SerializeField, Min(0f)] private float _cameraTransitionDuration = 1f;

        public BoatCameraController BoatCameraController => _boatCameraController;

        private Coroutine _cameraTransitionRoutine;
        private Coroutine _boatCameraTransitionRoutine;
        


        public void Initialize()
        {
            SimpleEventManager.Subscribe(GameEvents.GameStateChanged, OnGameStateChanged);
        }

        void OnDisable()
        {
            SimpleEventManager.Unsubscribe(GameEvents.GameStateChanged, OnGameStateChanged);
            StopCameraTransition();
            StopBoatCameraTransition();
        }

        private void OnGameStateChanged(object gameStateObj)
        {
            GameManager.GameState gameState = (GameManager.GameState)gameStateObj;

            if (gameState == GameManager.GameState.BoatDying)
                return;

            switch (gameState)
            {
                case GameManager.GameState.IntroScene:
                    ActivateCamera(_introSceneCameraPrefab);
                    break;
                case GameManager.GameState.Win:
                    ActivateCamera(_winSceneCameraPrefab);
                    break;
                case GameManager.GameState.Paused:
                    ActivateCamera(_pauseMenuCameraPrefab);
                    break;
                case GameManager.GameState.BoatMoving:
                    StopCameraTransition();
                    StartBoatCameraTransition();
                    break;
            }
        }

        private void ActivateCamera(UnityEngine.Camera targetCameraPrefab)
        {
            if (_mainCamera == null || targetCameraPrefab == null)
                return;

            if (_boatCameraController != null)
                _boatCameraController.enabled = false;

            StopCameraTransition();
            StopBoatCameraTransition();
            _cameraTransitionRoutine = StartCoroutine(TransitionToCameraRoutine(targetCameraPrefab));
        }

        private void StartBoatCameraTransition()
        {
            if (_boatCameraController == null)
            {
                return;
            }

            if (_mainCamera == null)
            {
                _boatCameraController.enabled = true;
                return;
            }

            StopBoatCameraTransition();
            _boatCameraController.enabled = false;
            _boatCameraTransitionRoutine = StartCoroutine(TransitionToBoatCameraRoutine());
        }

        private IEnumerator TransitionToBoatCameraRoutine()
        {
            if (_boatCameraController == null || _mainCamera == null)
            {
                _boatCameraTransitionRoutine = null;
                yield break;
            }

            if (!_boatCameraController.TryGetFollowPose(out Vector3 targetPosition, out Quaternion targetRotation))
            {
                EnableBoatCameraController();
                _boatCameraTransitionRoutine = null;
                yield break;
            }

            Transform mainTransform = _mainCamera.transform;
            Vector3 startPosition = mainTransform.position;
            Quaternion startRotation = mainTransform.rotation;

            float duration = Mathf.Max(0f, _cameraTransitionDuration);
            if (duration <= 0f)
            {
                mainTransform.position = targetPosition;
                mainTransform.rotation = targetRotation;
                EnableBoatCameraController();
                _boatCameraTransitionRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                mainTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
                mainTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            mainTransform.position = targetPosition;
            mainTransform.rotation = targetRotation;
            EnableBoatCameraController();
            _boatCameraTransitionRoutine = null;
        }

        private void EnableBoatCameraController()
        {
            if (_boatCameraController != null && !_boatCameraController.enabled)
                _boatCameraController.enabled = true;
        }

        private void StopBoatCameraTransition()
        {
            if (_boatCameraTransitionRoutine != null)
            {
                StopCoroutine(_boatCameraTransitionRoutine);
                _boatCameraTransitionRoutine = null;
            }
        }

        private IEnumerator TransitionToCameraRoutine(UnityEngine.Camera referenceCamera)
        {
            Transform mainTransform = _mainCamera.transform;
            Vector3 startPosition = mainTransform.position;
            Quaternion startRotation = mainTransform.rotation;
            float startFov = _mainCamera.fieldOfView;
            Vector3 targetPosition = referenceCamera.transform.position;
            Quaternion targetRotation = referenceCamera.transform.rotation;
            float targetFov = referenceCamera.fieldOfView;

            float duration = Mathf.Max(0f, _cameraTransitionDuration);
            if (duration <= 0f)
            {
                mainTransform.position = targetPosition;
                mainTransform.rotation = targetRotation;
                _mainCamera.fieldOfView = targetFov;
                _cameraTransitionRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                mainTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
                mainTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                _mainCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            mainTransform.position = targetPosition;
            mainTransform.rotation = targetRotation;
            _mainCamera.fieldOfView = targetFov;
            _cameraTransitionRoutine = null;
        }

        private void StopCameraTransition()
        {
            if (_cameraTransitionRoutine != null)
            {
                StopCoroutine(_cameraTransitionRoutine);
                _cameraTransitionRoutine = null;
            }
        }

    }
}
