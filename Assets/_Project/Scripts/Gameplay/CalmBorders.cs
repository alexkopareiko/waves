using UnityEngine;

namespace Game
{
    public class CalmBorders : MonoBehaviour
    {
        [SerializeField] private GameObject _plane;
        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.Boat == null)
            {
                return;
            }

            if (CheckIfBoatIsPositionedInsideBorders() == false)
            {
                if (GameManager.Instance.CurrentWaterState == GameManager.WaterState.CALM)
                {
                    GameManager.Instance.SetWaterState(GameManager.WaterState.CRAZY);
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
