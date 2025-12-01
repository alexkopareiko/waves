using UnityEngine;

namespace Game
{
    public class EnvironmentManager : MonoBehaviour
    {
        [SerializeField] private EnvironmentContainer environmentContainerCalm;
        [SerializeField] private EnvironmentContainer environmentContainerCrazy;
        [SerializeField] private Material calmSkybox;
        [SerializeField] private Material crazySkybox;
        [SerializeField] private Color calmFogColor;
        [SerializeField] private Color crazyFogColor;

        private Material _defaultSkybox;
        private Color _defaultFogColor; 


        public void Initialize()
        {
            SimpleEventManager.Subscribe(GameEvents.WaterStateChanged, OnWaterStateChanged);
            Debug.Log("EnvironmentManager initialized");
        }

        private void Awake()
        {
            _defaultSkybox = RenderSettings.skybox;
            _defaultFogColor = RenderSettings.fogColor;
            RenderSettings.fog = true;
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
            ApplySkyboxForState(waterState);
            switch (waterState)
            {
                case GameManager.WaterState.CALM:
                    SetFogColor(calmFogColor);
                    environmentContainerCalm.ActivateEnvironment();
                    environmentContainerCrazy.DeactivateEnvironment();
                    break;
                case GameManager.WaterState.CRAZY:
                    SetFogColor(crazyFogColor);
                    environmentContainerCalm.DeactivateEnvironment();
                    environmentContainerCrazy.ActivateEnvironment();
                    break;
            }
        }

        private void ApplySkyboxForState(GameManager.WaterState waterState)
        {
            Material skyboxTarget = waterState switch
            {
                GameManager.WaterState.CALM => calmSkybox ?? _defaultSkybox,
                GameManager.WaterState.CRAZY => crazySkybox ?? _defaultSkybox,
                _ => _defaultSkybox,
            };

            if (skyboxTarget == null || RenderSettings.skybox == skyboxTarget)
            {
                return;
            } 

            RenderSettings.skybox = skyboxTarget;
            DynamicGI.UpdateEnvironment();
        }

        private void SetFogColor(Color color)
        {
            RenderSettings.fogColor = color;
            RenderSettings.fog = true;
        }

    }
}
