using UI;
using UnityEngine;

namespace Game
{
    public class CalmBorders : MonoBehaviour
    {
        [SerializeField] private GameObject _plane;

        private bool _triggered = false;
        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.Boat == null)
            {
                return;
            }

            if (CheckIfBoatIsPositionedInsideBorders() == false)
            {
                if (GameManager.Instance.CurrentWaterState == GameManager.WaterState.CALM && _triggered == false)
                {
                    _triggered = true;
                    UnityEngine.Debug.Log("Boat exited calm borders, switching to CRAZY water state.");
                    GameManager.Instance.Boat.MovementController.EnableControls(false);
                    GameManager.Instance.CameraController.ActivateCamera(GameManager.Instance.Boat.CameraTransition1, false, () =>
                    {
                        UnityEngine.Debug.Log("Transitioned to crazy water state.");
                        UIManager.Instance.ShowDialogueCanvas();
                        GameManager.Instance.DialogueManager.StartDialogueSequence(2, () =>
                        {
                            UnityEngine.Debug.Log("Dialogue sequence 2 complete.");
                            GameManager.Instance.CameraController.ActivateCamera(GameManager.Instance.Boat.CameraTransition2, false, () =>
                            {
                                UnityEngine.Debug.Log("Boat is now in CRAZY water state.");
                                GameManager.Instance.SetWaterState(GameManager.WaterState.CRAZY);
                                GameManager.Instance.CameraController.ActivateCamera(GameManager.Instance.Boat.CameraTransition3, false, () =>
                                {
                                    UnityEngine.Debug.Log("Boat controls re-enabled.");
                                    GameManager.Instance.CameraController.ActivateCamera(GameManager.Instance.Boat.CameraTransition1, false, () =>
                                    {
                                        UnityEngine.Debug.Log("Transitioned to calm water state.");
                                        GameManager.Instance.DialogueManager.StartDialogueSequence(3, () =>
                                        {
                                            UnityEngine.Debug.Log("Dialogue sequence 3 complete.");
                                            UIManager.Instance.ShowPlayCanvas();
                                            GameManager.Instance.Boat.MovementController.EnableControls(true);
                                            GameManager.Instance.CameraController.SetCameraDefaultPose();
                                        });
                                    });
                                });
                            });
                        });
                    });
                }
            }
        } 

        private bool CheckIfBoatIsPositionedInsideBorders()
        {
            var boatPos = GameManager.Instance.Boat.transform.position;
            var borderPos = _plane.transform.position;
            var borderScale = _plane.transform.localScale * 10f; // plane scale is multiplied by 10 in unity units

            if (boatPos.x > borderPos.x - borderScale.x / 2 &&
                boatPos.x < borderPos.x + borderScale.x / 2 &&
                boatPos.z > borderPos.z - borderScale.z / 2 &&
                boatPos.z < borderPos.z + borderScale.z / 2)
            {
                return true;
            }
            return false;
        }
    }

}
