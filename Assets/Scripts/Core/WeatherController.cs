using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame
{
    public class WeatherController : MonoBehaviour
    {
        [Header("Префабы погоды")]
        [SerializeField] private GameObject rainPrefab;
        [SerializeField] private GameObject snowPrefab;

        [Header("Настройки")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float particleHeightOffset = 10f;
        [SerializeField] private Vector3 particleSystemSize = new Vector3(50f, 1f, 50f);
        [SerializeField] private float transitionDuration = 2f;
        [SerializeField] private float maxParticleSize = 0.1f; // Новое поле для ограничения размера частиц
        [SerializeField] private CreepyFogEffect creepyFogEffect; // Связь с туманом

        [Header("Настройки звука дождя")]
        [SerializeField] private AudioClip rainAudioClip;
        [SerializeField] [Range(0f, 1f)] private float maxRainVolume = 0.5f;
        [SerializeField] private float audioTransitionSpeed = 2f;

        private ParticleSystem currentWeatherSystem;
        private WeatherZone currentZone;
        private Dictionary<WeatherZone.WeatherType, GameObject> weatherPrefabs;
        private float targetIntensity = 0f;
        private float currentIntensity = 0f;
        private Coroutine transitionCoroutine;
        private AudioSource rainAudioSource;
        private float targetVolume = 0f;
        private float currentVolume = 0f;

        private void Awake()
        {
            weatherPrefabs = new Dictionary<WeatherZone.WeatherType, GameObject>
            {
                { WeatherZone.WeatherType.Rain, rainPrefab },
                { WeatherZone.WeatherType.Snow, snowPrefab }
            };

            rainAudioSource = gameObject.AddComponent<AudioSource>();
            rainAudioSource.clip = rainAudioClip;
            rainAudioSource.loop = true;
            rainAudioSource.spatialBlend = 0f;
            rainAudioSource.playOnAwake = false;
            rainAudioSource.volume = 0f;

            if (!creepyFogEffect) creepyFogEffect = Camera.main.GetComponent<CreepyFogEffect>();
        }

        private void Update()
        {
            if (currentWeatherSystem)
            {
                currentWeatherSystem.transform.position = playerTransform.position + Vector3.up * particleHeightOffset;
                UpdateParticleEmission();
            }

            UpdateRainAudio();
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

            ParticleSystemRenderer renderer = currentWeatherSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material.renderQueue = 3100;
            }

            var shape = currentWeatherSystem.shape;
            shape.scale = particleSystemSize;

            var main = currentWeatherSystem.main;
            main.startSize = maxParticleSize; // Устанавливаем начальный размер частиц

            targetVolume = (zone.GetWeatherType() == WeatherZone.WeatherType.Rain) ? maxRainVolume * zone.GetIntensity() : 0f;
            if (zone.GetWeatherType() == WeatherZone.WeatherType.Rain && !rainAudioSource.isPlaying)
            {
                rainAudioSource.Play();
            }

            transitionCoroutine = StartCoroutine(TransitionWeather(zone.GetIntensity()));
        }

        private void StopWeather()
        {
            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);

            currentZone = null;
            targetVolume = 0f;
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

            if (currentIntensity == 0f && rainAudioSource.isPlaying)
            {
                StartCoroutine(FadeOutAudio());
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

            var main = currentWeatherSystem.main;
            float fogDensity = creepyFogEffect != null ? creepyFogEffect.GetDensity() : 0f;
            main.startSize = maxParticleSize * (1f - fogDensity * 0.5f); 
        }

        private void UpdateRainAudio()
        {
            if (!rainAudioSource) return;

            currentVolume = Mathf.Lerp(currentVolume, targetVolume, Time.deltaTime * audioTransitionSpeed);
            rainAudioSource.volume = currentVolume;
            rainAudioSource.transform.position = playerTransform.position;
        }

        private IEnumerator FadeOutAudio()
        {
            while (rainAudioSource.volume > 0.01f)
            {
                rainAudioSource.volume = Mathf.Lerp(rainAudioSource.volume, 0f, Time.deltaTime * audioTransitionSpeed);
                yield return null;
            }
            rainAudioSource.Stop();
            rainAudioSource.volume = 0f;
        }

        public WeatherZone.WeatherType GetCurrentWeatherType() => currentZone ? currentZone.GetWeatherType() : WeatherZone.WeatherType.None;
        public float GetCurrentWeatherIntensity() => currentIntensity;
    }
}