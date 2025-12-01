using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI;

namespace Game
{
    public class PlayCanvas : UISubCanvas
    {
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _menuButton;

        [Header("References")]
        [SerializeField] private GearboxUI _gearboxUI;
        [SerializeField] private CompassUI _compassUI;
        [SerializeField] private TMP_Text _helperText;
        

        public void ShowHelperTextForDuration(string text, float duration)
        {
            if (_helperText == null)
                return;

            StopAllCoroutines();
            StartCoroutine(ShowTextForDurationCoroutine(_helperText, text, duration));
        }

        private System.Collections.IEnumerator ShowTextForDurationCoroutine(TMP_Text textComponent, string text, float duration)
        {
            textComponent.text = text;
            textComponent.gameObject.SetActive(true);

            yield return new WaitForSeconds(duration);

            textComponent.gameObject.SetActive(false);
        }


        public void Initialize()
        {
            if (_gearboxUI != null)
            {
                _gearboxUI.Initialize();
            }
            if (_compassUI != null)
            {
                _compassUI.Initialize();
            }
        }

        private void OnEnable()
        {
            if (_pauseButton != null)
                _pauseButton.onClick.AddListener(OnClickPause);

            if (_menuButton != null)
                _menuButton.onClick.AddListener(OnClickMenu);

        }

        private void OnDisable()
        {
            if (_pauseButton != null)
                _pauseButton.onClick.RemoveListener(OnClickPause);

            if (_menuButton != null)
                _menuButton.onClick.RemoveListener(OnClickMenu);
        }


        public void OnClickPause()
        {
            bool toPause = !GameManager.isPaused;
            GameManager.Pause(toPause);

            if (toPause)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayButtonSound(SoundManager.ButtonUIType.cancel);
                }
            }
            else
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayButtonSound(SoundManager.ButtonUIType.cancel);
                }
            }
        }

        public void OnClickMenu()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayButtonSound(SoundManager.ButtonUIType.confirm);
            }

            if (_menuButton != null)
            {
                _menuButton.interactable = false;
            }

            LoadMenuScene();
        }

        private void LoadMenuScene()
        {
            // Ensure we are not time-scaled when switching scenes
            GameManager.Pause(false);

            // Prefer transition if available; fall back to SceneManager
            if (MaskTransitions.TransitionManager.Instance != null)
                MaskTransitions.TransitionManager.Instance.LoadLevel("Menu", 0f);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        }

    }
}
