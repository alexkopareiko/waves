using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class UIManager : MonoBehaviour, IGameModule
    {
        public static UIManager Instance => s_Instance;
        private static UIManager s_Instance;

        [Header("Canvases")]
        [SerializeField] private UISubCanvas _playCanvas;
        [SerializeField] private UISubCanvas _dieCanvas;
        [SerializeField] private UISubCanvas _winCanvas;
        [SerializeField] private UISubCanvas _settingsCanvas;
        [SerializeField] private UISubCanvas _dialogueCanvas;

        [Header("Other")]
        [SerializeField] private AudioClip _startClip;

        private List<UISubCanvas> _canvases = new List<UISubCanvas>();
        private bool _isInitialized = false;

        public PlayCanvas PlayCanvas => _playCanvas as PlayCanvas;
        public DieCanvas DieCanvas => _dieCanvas as DieCanvas;
        public UISubCanvas WinCanvas => _winCanvas;
        public UISubCanvas SettingsCanvas => _settingsCanvas;
        public UISubCanvas DialogueCanvas => _dialogueCanvas;

        public bool IsLoaded => _isInitialized;

        private void OnEnable()
        {
            SetupInstance();
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

        private void Start()
        {
            //SoundManager.Instance.PlaySoundEffect(_startClip);
        }

        private void ShowCanvas(UISubCanvas canvas)
        {
            foreach (var item in _canvases)
                item.gameObject.SetActive(item == canvas);
        }

        public void ShowPlayCanvas()
        {
            ShowCanvas(_playCanvas);
        }

        public void ShowDieCanvas()
        {
            ShowCanvas(_dieCanvas);
        }
        public void ShowWinCanvas()
        {
            ShowCanvas(_winCanvas);
        }

        public void ShowSettingsCanvas()
        {
            ShowCanvas(_settingsCanvas);
        }

        public void ShowDialogueCanvas()
        {
            ShowCanvas(_dialogueCanvas);
        }

        public void Load()
        {

        }

        public void Initialize()
        {
            _canvases.Add(_playCanvas);
            _canvases.Add(_dieCanvas);
            _canvases.Add(_winCanvas);
            _canvases.Add(_settingsCanvas);
            _canvases.Add(_dialogueCanvas);

            GameManager.Pause(false);
            
            PlayCanvas.Initialize();
            ShowPlayCanvas();

            _isInitialized = true;
        }
    }

}
