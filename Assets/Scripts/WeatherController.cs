using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class WeatherController : MonoBehaviour
{
    [Header("Префабы погоды")]
    [SerializeField] private GameObject rainPrefab;
    [SerializeField] private GameObject snowPrefab;

    [Header("Настройки")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float particleHeightOffset = 10f;
    [SerializeField] private Vector3 particleSystemSize = new Vector3(50f, 1f, 50f);
    [SerializeField] private float transitionDuration = 2f; // 2 секунды переход

    private ParticleSystem currentWeatherSystem;
    private WeatherZone currentZone;
    private Dictionary<WeatherZone.WeatherType, GameObject> weatherPrefabs;
    private float targetIntensity = 0f;
    private float currentIntensity = 0f;
    private Coroutine transitionCoroutine;

    private void Awake()
    {
        weatherPrefabs = new Dictionary<WeatherZone.WeatherType, GameObject>
        {
            { WeatherZone.WeatherType.Rain, rainPrefab },
            { WeatherZone.WeatherType.Snow, snowPrefab }
        };
    }

    private void Update()
    {
        if (currentWeatherSystem)
        {
            currentWeatherSystem.transform.position = playerTransform.position + Vector3.up * particleHeightOffset;
            UpdateParticleEmission();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WeatherZone"))
        {
            WeatherZone zone = other.GetComponent<WeatherZone>();
            if (zone && zone.GetWeatherType() != WeatherZone.WeatherType.None)
            {
                SwitchWeather(zone);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WeatherZone"))
        {
            StopWeather();
        }
    }

    private void SwitchWeather(WeatherZone zone)
    {
        if (currentZone == zone) return;

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        if (currentWeatherSystem)
        {
            Destroy(currentWeatherSystem.gameObject);
            currentWeatherSystem = null;
        }

        currentZone = zone;
        GameObject prefab = weatherPrefabs[zone.GetWeatherType()];
        GameObject weatherObj = Instantiate(prefab, playerTransform.position + Vector3.up * particleHeightOffset, Quaternion.identity);
        currentWeatherSystem = weatherObj.GetComponent<ParticleSystem>();

        var shape = currentWeatherSystem.shape;
        shape.scale = particleSystemSize;

        transitionCoroutine = StartCoroutine(TransitionWeather(zone.GetIntensity()));
    }

    private void StopWeather()
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        currentZone = null;
        transitionCoroutine = StartCoroutine(TransitionWeather(0f));
    }

    private IEnumerator TransitionWeather(float target)
    {
        targetIntensity = target;
        float startIntensity = currentIntensity;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            currentIntensity = Mathf.Lerp(startIntensity, targetIntensity, elapsed / transitionDuration);
            UpdateParticleEmission();
            yield return null;
        }

        currentIntensity = targetIntensity;
        UpdateParticleEmission();
        
        if (currentIntensity == 0f && currentWeatherSystem)
        {
            Destroy(currentWeatherSystem.gameObject);
            currentWeatherSystem = null;
        }
    }

    private void UpdateParticleEmission()
    {
        if (!currentWeatherSystem) return;

        var emission = currentWeatherSystem.emission;
        float maxRate = currentZone != null
            ? (currentZone.GetWeatherType() == WeatherZone.WeatherType.Rain ? 200f : 100f)
            : 0f;
        emission.rateOverTime = currentIntensity * maxRate;
    }

    public WeatherZone.WeatherType GetCurrentWeatherType() => currentZone ? currentZone.GetWeatherType() : WeatherZone.WeatherType.None;
    public float GetCurrentWeatherIntensity() => currentIntensity;
}