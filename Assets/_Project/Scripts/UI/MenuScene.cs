using UnityEngine;
using MaskTransitions;
using UnityEngine.SceneManagement;
using Game;

namespace UI
{
    public class MenuScene : MonoBehaviour
    {
        void OnEnable()
        {
            GameManager.SetIsDied(false);
        }
        
        public void StartGame()
        {
            // Prefer TransitionManager if present, else fall back to SceneManager
            if (TransitionManager.Instance != null)
                TransitionManager.Instance.LoadLevel("Game", 0f);
            else
                SceneManager.LoadScene("Game");
        }
    }
}
