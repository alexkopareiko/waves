using UnityEngine;
using MaskTransitions;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MenuScene : MonoBehaviour
    {
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
