using UnityEngine;
using UnityEngine.Rendering;

namespace HorrorGame
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CreepyFogEffect : MonoBehaviour
    {
        [Header("Настройки тумана")]
        [SerializeField] [Range(0f, 1f)] private float intensity = 0.5f;
        [SerializeField] [ColorUsage(true, true)] private Color fogColor = new Color(0.12f, 0.18f, 0.22f, 1f);
        [SerializeField] [Range(0.03f, 0.12f)] private float density = 0.05f;
        [SerializeField] [Range(0.3f, 1.2f)] private float swirlScale = 0.7f;
        [SerializeField] private Vector2 swirlSpeed = new Vector2(0.25f, 0.25f);

        [Header("Динамические эффекты")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private float runBoost = 1.7f;
        [SerializeField] private float moveBoost = 0.025f;

        [Header("Пульсация")]
        [SerializeField] private bool enablePulsation = true;
        [SerializeField] [Range(0f, 0.025f)] private float pulseAmplitude = 0.018f;
        [SerializeField] [Range(0.6f, 1.3f)] private float pulseFrequency = 0.85f;

        [Header("Интеграция с погодой")]
        [SerializeField] private WeatherController weatherController;
        [SerializeField] private float rainIntensityBoost = 0.1f;
        [SerializeField] private float snowIntensityBoost = 0.05f;
        [SerializeField] private float weatherLerpSpeed = 0.5f;

        [Header("UI Render Texture")]
        [SerializeField] public RenderTexture uiRenderTexture;

        private Material material;
        private float time;
        private Camera mainCamera;
        private float currentWeatherBoost = 0f;

        private void Awake()
        {
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
            {
                enabled = false;
                return;
            }

            mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
            mainCamera.depthTextureMode = DepthTextureMode.Depth;
            InitMaterial();
            if (!weatherController) weatherController = FindObjectOfType<WeatherController>();
        }

        private void OnValidate()
        {
            InitMaterial();
        }

        private void InitMaterial()
        {
            Shader shader = Shader.Find("Hidden/CreepyFog");
            if (shader == null)
            {
                enabled = false;
                return;
            }

            if (material == null)
            {
                material = new Material(shader);
            }
        }

        private void Update()
        {
            if (material == null || mainCamera == null)
            {
                return;
            }

            time += Time.deltaTime;

            float dynamicIntensity = intensity * 0.7f;
            float dynamicDensity = density;
            if (playerMovement)
            {
                float movement = Mathf.Abs(Input.GetAxis("Horizontal")) + Mathf.Abs(Input.GetAxis("Vertical"));
                dynamicDensity += movement * moveBoost;

                if (Input.GetKey(playerMovement.GetRunKey()))
                {
                    dynamicDensity *= runBoost;
                }
            }

            if (weatherController)
            {
                var weatherType = weatherController.GetCurrentWeatherType();
                float weatherIntensity = weatherController.GetCurrentWeatherIntensity();
                float targetWeatherBoost = 0f;

                if (weatherType == WeatherZone.WeatherType.Rain)
                {
                    targetWeatherBoost = rainIntensityBoost * weatherIntensity;
                }
                else if (weatherType == WeatherZone.WeatherType.Snow)
                {
                    targetWeatherBoost = snowIntensityBoost * weatherIntensity;
                }

                currentWeatherBoost = Mathf.Lerp(currentWeatherBoost, targetWeatherBoost, Time.deltaTime * weatherLerpSpeed);
                dynamicIntensity += currentWeatherBoost;
                dynamicDensity += currentWeatherBoost * 0.5f;
            }

            if (enablePulsation)
            {
                float pulse = Mathf.Sin(time * pulseFrequency) * pulseAmplitude;
                dynamicIntensity += pulse;
                dynamicDensity += pulse * 0.5f;
            }

            material.SetFloat("_Intensity", dynamicIntensity);
            material.SetFloat("_Density", dynamicDensity);
            material.SetColor("_FogColor", fogColor);
            material.SetFloat("_SwirlScale", swirlScale);
            material.SetVector("_SwirlSpeed", swirlSpeed);
            material.SetFloat("_WeatherIntensity", weatherController ? weatherController.GetCurrentWeatherIntensity() * 0.5f : 0f);

            if (uiRenderTexture != null)
            {
                material.SetTexture("_UITex", uiRenderTexture);
            }
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (material == null)
            {
                Graphics.Blit(src, dest);
                return;
            }

            Graphics.Blit(src, dest, material);
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                DestroyImmediate(material);
            }
        }

        public float GetDensity()
        {
            return density;
        }

        public void SetDensity(float newDensity)
        {
            density = Mathf.Clamp(newDensity, 0.03f, 0.12f);
        }
    }
}