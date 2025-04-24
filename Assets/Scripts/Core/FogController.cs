using UnityEngine;

namespace HorrorGame
{
    [ExecuteAlways]
    public class FogController : MonoBehaviour
    {
        [Header("Основные настройки")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] [ColorUsage(true, true)] private Color fogColor = new Color(0.15f, 0.2f, 0.25f, 1f);
        [SerializeField] [Range(0.01f, 0.08f)] private float fogDensity = 0.04f;

        [Header("Эффекты пульсации")]
        [SerializeField] private bool enablePulsation = true;
        [SerializeField] [Range(0f, 0.015f)] private float pulsationAmplitude = 0.008f;
        [SerializeField] [Range(0.5f, 1.5f)] private float pulsationFrequency = 0.8f;

        [Header("Взаимодействие с игроком")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private float runDensityMultiplier = 1.4f;
        [SerializeField] private float movementDensityBoost = 0.01f;

        [Header("Настройка камеры")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float cameraFarPlane = 150f;

        private float timeAccumulator;

        private void Awake()
        {
            InitializeCamera();
            ApplySettings();
        }

        private void OnValidate()
        {
            ApplySettings();
        }

        private void InitializeCamera()
        {
            if (!mainCamera) mainCamera = Camera.main;
            if (mainCamera) mainCamera.depthTextureMode = DepthTextureMode.Depth;
        }

        private void ApplySettings()
        {
            RenderSettings.fog = enableFog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogColor = fogColor;

            if (mainCamera)
            {
                mainCamera.farClipPlane = cameraFarPlane;
                mainCamera.usePhysicalProperties = false;
            }
        }

        private void Update()
        {
            timeAccumulator += Time.deltaTime;

            float dynamicDensity = fogDensity;
            if (playerMovement)
            {
                if (Input.GetKey(playerMovement.GetRunKey()))
                {
                    dynamicDensity *= runDensityMultiplier;
                }
                float movement = Mathf.Abs(Input.GetAxis("Horizontal")) + Mathf.Abs(Input.GetAxis("Vertical"));
                dynamicDensity += movement * movementDensityBoost;
            }

            if (enablePulsation)
            {
                RenderSettings.fogDensity = dynamicDensity + Mathf.Sin(timeAccumulator * pulsationFrequency) * pulsationAmplitude;
            }
            else
            {
                RenderSettings.fogDensity = dynamicDensity;
            }

            RenderSettings.fogColor = fogColor;
        }

        public void SetFogColor(Color newColor) => fogColor = newColor;
        public void SetFogDensity(float density) => fogDensity = Mathf.Clamp(density, 0.01f, 0.08f);
        public void ToggleFog(bool state) => enableFog = state;
    }
}