using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MaskTransitions;

namespace Game
{
    public class DieCanvas : UISubCanvas
    {
        [Header("UI")]
        [SerializeField] private Button _reloadButton;

        private void OnEnable()
        {
            // Ensure listeners are attached once when shown
            if (_reloadButton != null)
                _reloadButton.onClick.AddListener(OnClickReload);
        }



        private void OnDisable()
        {
            // Clean up listeners to avoid duplicate subscriptions
            if (_reloadButton != null)
                _reloadButton.onClick.RemoveListener(OnClickReload);

        }

        public void OnClickReload()
        {
            if (_reloadButton != null) _reloadButton.interactable = false;

            LoadGameScene();
        }


        private void LoadGameScene()
        {
            GameManager.Pause(false);

            // Prefer TransitionManager if present, else fall back to SceneManager
            if (TransitionManager.Instance != null)
                TransitionManager.Instance.LoadLevel("Game", 0f);
            else
                SceneManager.LoadScene("Game");
        }
    }
}

