using UnityEngine;

namespace Game
{
    public class EnvironmentManager : MonoBehaviour
    {
        [SerializeField] private EnvironmentContainer environmentContainerCalm;
        [SerializeField] private EnvironmentContainer environmentContainerCrazy;


        public void Initialize()
        {
            SimpleEventManager.Subscribe(GameEvents.WaterStateChanged, OnWaterStateChanged);
            Debug.Log("EnvironmentManager initialized");
        }
        // void OnEnable()
        // {
        //     SimpleEventManager.Subscribe(GameEvents.WaterStateChanged, OnWaterStateChanged);
        // }

        private void OnDisable()
        {
            SimpleEventManager.Unsubscribe(GameEvents.WaterStateChanged, OnWaterStateChanged);
        }

        private void OnWaterStateChanged(object state)
        {
            var waterState = (GameManager.WaterState)state;
            Debug.Log($"EnvironmentManager: Water state changed to {waterState}");
            switch (waterState)
            {
                case GameManager.WaterState.CALM:
                    environmentContainerCalm.ActivateEnvironment();
                    environmentContainerCrazy.DeactivateEnvironment();
                    break;
                case GameManager.WaterState.CRAZY:
                    environmentContainerCalm.DeactivateEnvironment();
                    environmentContainerCrazy.ActivateEnvironment();
                    break;
            }
        }

    }
}

