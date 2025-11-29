using System.Collections;
using UnityEngine;

namespace Game 
{
    public class Lighthouse : MonoBehaviour
    {
        [SerializeField] private GameObject lighthouseLight;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private Transform parkingSpot;
        private void Update()
        {
            RotateLighthouse();
        }
        private void RotateLighthouse()
        {
            lighthouseLight.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("Player has entered the lighthouse area.");
                // Additional logic for when the player enters the lighthouse area
                GameManager.Instance.Boat.MovementController.EnableControls(false);
                GameManager.Instance.SetGameState(GameManager.GameState.Win);
                
                GameManager.Instance.CameraController.ActivateCamera(GameManager.Instance.CameraController.WinSceneCameraPrefab, false, () =>
                {
                    UIManager.Instance.ShowDialogueCanvas();
                    GameManager.Instance.DialogueManager.StartDialogueSequence(4, () =>
                    {
                        UIManager.Instance.ShowWinCanvas();
                    });

                    StartCoroutine(ParkPlayerBoat(GameManager.Instance.Boat));
                });
            }
        }

        private IEnumerator ParkPlayerBoat(Boat boat)
        {
            Vector3 startPosition = boat.transform.position;
            Quaternion startRotation = boat.transform.rotation;
            Vector3 endPosition = parkingSpot.position;
            Quaternion endRotation = parkingSpot.rotation;

            float duration = 5f; // Duration of the parking animation
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                boat.transform.position = Vector3.Lerp(startPosition, endPosition, t);
                boat.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            boat.transform.position = endPosition;
            boat.transform.rotation = endRotation;
        }
    }
    
}
