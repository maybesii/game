using UnityEngine;

[RequireComponent(typeof(Light))]
public class CandleFlicker : MonoBehaviour
{
    [Header("Base Flicker Settings")]
    [SerializeField, Range(0.1f, 2f)] private float minIntensity = 0.7f;
    [SerializeField, Range(0.1f, 5f)] private float maxIntensity = 1.5f;
    [SerializeField, Range(0.1f, 20f)] private float flickerSpeed = 3f;
    [SerializeField, Range(0f, 1f)] private float intensitySmoothness = 0.5f;

    [Header("Color Settings")]
    [SerializeField] private Gradient lightColorGradient;
    [SerializeField, Range(0f, 1f)] private float colorChangeSpeed = 0.3f;

    [Header("Burst Effects")]
    [SerializeField] private float burstChance = 0.02f;
    [SerializeField] private float burstMultiplier = 2f;
    [SerializeField] private float burstDuration = 0.15f;

    private Light candleLight;
    private float randomOffset;
    private float targetIntensity;
    private float currentIntensity;
    private float burstTimer;
    private Color targetColor;

    void Awake()
    {
        candleLight = GetComponent<Light>();
        randomOffset = Random.Range(0f, 100f);
        currentIntensity = candleLight.intensity;
        targetIntensity = currentIntensity;
        targetColor = candleLight.color;
    }

    void Update()
    {
        HandleBurstEffect();
        UpdateLightIntensity();
        UpdateLightColor();
    }

    private void HandleBurstEffect()
    {
        if (burstTimer <= 0 && Random.value < burstChance * Time.deltaTime)
        {
            burstTimer = burstDuration;
        }
        burstTimer -= Time.deltaTime;
    }

    private void UpdateLightIntensity()
    {
        float noise = Mathf.PerlinNoise(randomOffset + Time.time * flickerSpeed * 0.3f, randomOffset * 0.5f);
        targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

        if (burstTimer > 0)
        {
            currentIntensity = targetIntensity * burstMultiplier;
        }
        else
        {
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, intensitySmoothness * Time.deltaTime * 10f);
        }

        candleLight.intensity = currentIntensity;
    }

    private void UpdateLightColor()
    {
        float colorNoise = Mathf.PerlinNoise(randomOffset * 0.7f, Time.time * colorChangeSpeed);
        targetColor = lightColorGradient.Evaluate(colorNoise);
        candleLight.color = Color.Lerp(candleLight.color, targetColor, Time.deltaTime * 5f);
    }
}