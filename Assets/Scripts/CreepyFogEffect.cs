using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CreepyFogEffect : MonoBehaviour
{
    [Header("Настройки тумана")]
    [SerializeField] [Range(0f, 1f)] private float intensity = 0.75f;
    [SerializeField] [ColorUsage(true, true)] private Color fogColor = new Color(0.12f, 0.18f, 0.22f, 1f);
    [SerializeField] [Range(0.03f, 0.12f)] private float density = 0.07f;
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

    [Header("Вспышки цвета")]
    [SerializeField] private float flashChance = 0.01f;
    [SerializeField] private Color[] flashColors = new Color[]
    {
        new Color(0.65f, 0.12f, 0.12f, 1f),
        new Color(0.12f, 0.45f, 0.18f, 1f)
    };
    [SerializeField] private float flashDuration = 0.4f;

    [Header("Интеграция с погодой")]
    [SerializeField] private WeatherController weatherController;
    [SerializeField] private float rainIntensityBoost = 0.2f;
    [SerializeField] private float snowIntensityBoost = 0.1f;
    [SerializeField] private float weatherLerpSpeed = 0.5f; 

    private Material material;
    private float time;
    private bool isFlashing;
    private Color currentColor;
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

        mainCamera.depthTextureMode = DepthTextureMode.Depth;
        currentColor = fogColor;
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

        float dynamicIntensity = intensity;
        float dynamicDensity = density;
        if (playerMovement)
        {
            if (Input.GetKey(playerMovement.GetRunKey()))
            {
                dynamicIntensity *= runBoost;
                dynamicDensity *= runBoost;
            }
            float movement = Mathf.Abs(Input.GetAxis("Horizontal")) + Mathf.Abs(Input.GetAxis("Vertical"));
            dynamicDensity += movement * moveBoost;
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

        if (!isFlashing && Random.value < flashChance * Time.deltaTime)
        {
            StartCoroutine(DoFlash());
        }

        currentColor = Color.Lerp(currentColor, isFlashing ? currentColor : fogColor, Time.deltaTime * 0.5f);

        material.SetFloat("_Intensity", dynamicIntensity);
        material.SetFloat("_Density", dynamicDensity);
        material.SetColor("_FogColor", currentColor);
        material.SetFloat("_SwirlScale", swirlScale);
        material.SetVector("_SwirlSpeed", swirlSpeed);
        material.SetFloat("_WeatherIntensity", weatherController ? weatherController.GetCurrentWeatherIntensity() : 0f);
    }

    private IEnumerator DoFlash()
    {
        isFlashing = true;
        currentColor = flashColors[Random.Range(0, flashColors.Length)];
        yield return new WaitForSeconds(flashDuration);
        isFlashing = false;
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
}