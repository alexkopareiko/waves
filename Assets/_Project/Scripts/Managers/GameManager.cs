using Bitgem.VFX.StylisedWater;
using UI;
using UnityEngine;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        public enum WaterState
        {
            CALM = 0,
            CRAZY = 1,
        }

        public enum GameState
        {
            IntroScene = 0,
            BoatMoving = 1,
            BoatDying = 2,
            Win = 3,
            Paused = 4,
        }


        public static GameManager Instance => s_Instance;
        private static GameManager s_Instance;

        //references to other managers/components
        private Boat _boat;
        private WaterVolumeHelper _waterVolumeHelper;
        private BoatCameraController _boatCameraController;
        private WaterVolumeTransforms _waterVolumeTransforms;
        private WaterSpawnManager _waterSpawnManager;
        private WaterState waterState = WaterState.CALM;
        private EnvironmentManager _environmentManager;
        private Tentacles _tentacles;
        private GameState _gameState = GameState.IntroScene;
        private CameraController _cameraController;
        private SoundManager _soundManager;
        private SaveManager _saveManager;
        private DialogueManager _dialogueManager;

        private static bool _isPaused = false;
        internal static bool isPaused => _isPaused;

        public Boat Boat => _boat;
        public WaterVolumeHelper WaterVolumeHelper => _waterVolumeHelper;
        public BoatCameraController BoatCameraController => _boatCameraController;
        public WaterVolumeTransforms WaterVolumeTransforms => _waterVolumeTransforms;
        public WaterSpawnManager WaterSpawnManager => _waterSpawnManager;
        public WaterState CurrentWaterState => waterState;
        public EnvironmentManager EnvironmentManager => _environmentManager;
        public Tentacles Tentacles => _tentacles;
        public GameState CurrentGameState => _gameState;
        public CameraController CameraController => _cameraController;
        public SoundManager SoundManager => _soundManager;
        public SaveManager SaveManager => _saveManager;
        public DialogueManager DialogueManager => _dialogueManager;

        private void OnEnable()
        {

            SetupInstance();

            LoadSequencer.LastModuleLoaded += Load;
        }

        void Start()
        {
        }

        void Update()
        {
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            LoadSequencer.LastModuleLoaded -= Load;
        }

        private void SetupInstance()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
        }

        public void Load()
        {
            CollectReferences();

            // Initialize components

            _boat.Initialize();
            UIManager.Instance.Initialize();
            PoolManagerMono.Instance.Initialize();
            _environmentManager.Initialize();
            _tentacles.Initialize();
            _cameraController.Initialize();
            _saveManager.Initialize();
            _soundManager.Initialize();

            SetWaterState(WaterState.CALM);
            SetGameState(GameState.IntroScene);
        }

        public void SetGameState(GameState newState)
        {
            _gameState = newState;
            SimpleEventManager.Emit(GameEvents.GameStateChanged, newState);
        }

        public void SetWaterState(WaterState newState)
        {
            waterState = newState;
            SimpleEventManager.Emit(GameEvents.WaterStateChanged, newState);
        }

        public void GameOver()
        {
            // Save max score
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.LosesCount = SaveManager.Instance.LosesCount + 1;
            }

            // Pause and show Die canvas
            // Pause(true);
            if (UIManager.Instance != null && UIManager.Instance.DieCanvas != null)
            {
                Debug.Log("Show Die Canvas");
                UIManager.Instance.ShowDieCanvas();
            }
        }

        public void Revive()
        {
            // Hide die canvas and resume
            // if (UIManager.Instance != null)
            // {
            //     UIManager.Instance.ShowPlayCanvas();
            //     if (UIManager.Instance.PlayCanvas != null)
            //         // UIManager.Instance.PlayCanvas.SetScore(_score);
            // }
            Pause(false);
        }

        public static void Pause(bool value)
        {
            _isPaused = value;
            Time.timeScale = value ? 0f : 1f;
        }


        private void CollectReferences()
        {
            _boat = FindFirstObjectByType<Boat>();
            _waterVolumeHelper = FindFirstObjectByType<WaterVolumeHelper>();
            _boatCameraController = FindFirstObjectByType<BoatCameraController>();
            _waterVolumeTransforms = FindFirstObjectByType<WaterVolumeTransforms>();
            _waterSpawnManager = FindFirstObjectByType<WaterSpawnManager>();
            _environmentManager = FindFirstObjectByType<EnvironmentManager>();
            _tentacles = FindFirstObjectByType<Tentacles>();
            _cameraController = FindFirstObjectByType<CameraController>();
            _soundManager = FindFirstObjectByType<SoundManager>();
            _saveManager = FindFirstObjectByType<SaveManager>();
            _dialogueManager = FindFirstObjectByType<DialogueManager>();
        }
    }
}
