using MaskTransitions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game
{
    public class WinCanvas : UISubCanvas
    {
        [Header("UI")]
        [SerializeField] private Button _reloadButton;

        private void OnEnable()
        {
            if (_reloadButton != null)
                _reloadButton.onClick.AddListener(OnClickReload);
        }

        private void OnDisable()
        {
            if (_reloadButton != null)
                _reloadButton.onClick.RemoveListener(OnClickReload);
        }

        public void OnClickReload()
        {
            if (SoundManager.Instance != null)
            {
            }

            if (_reloadButton != null)
                _reloadButton.interactable = false;

            LoadGameScene();
        }

        private void LoadGameScene()
        {
            GameManager.Pause(false);

            if (TransitionManager.Instance != null)
                TransitionManager.Instance.LoadLevel("Menu", 0f);
            else
                SceneManager.LoadScene("Menu"); 
        }
    }
}
