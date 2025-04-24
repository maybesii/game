using UnityEngine;
using System.Collections;
using HorrorGame;

namespace HorrorGame
{
    public class InteractionManager : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private InteractionUI interactionUI;
        [SerializeField] private InteractionUIBuilder uiBuilder;
        [SerializeField] private float interactionDistance = 10f;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private CreepyFogEffect creepyFogEffect;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private AudioClip horrorWhisperClip;
        [SerializeField] private AudioClip[] ambientSounds;
        [SerializeField] private float minSoundInterval = 5f;
        [SerializeField] private float maxSoundInterval = 15f;

        private InteractablePoint currentPoint;
        private AudioSource audioSource;
        private float hoverSoundTimer;
        private bool isPlayingEcho;
        private float raycastTimer;
        private float raycastInterval = 0.1f;
        private float ambientSoundTimer;

        [Header("Echo Settings")]
        [SerializeField] private float echoDelay = 0.3f;
        [SerializeField] private int echoCount = 3;
        [SerializeField] private float echoVolumeReduction = 0.2f; // Уменьшено с 0.3 до 0.2
        [SerializeField] private float pitchVariation = 0.2f;
        [SerializeField] private float initialEchoVolume = 0.5f; // Новое поле для начальной громкости эха

        private void Awake()
        {
            if (!playerCamera) playerCamera = Camera.main;

            if (uiBuilder != null)
            {
                uiBuilder.Initialize(playerCamera);
                if (interactionUI != null)
                {
                    interactionUI.Initialize(uiBuilder);
                }
            }
            else
            {
                DebugLogger.LogError("InteractionUIBuilder is not assigned in InteractionManager!");
            }

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f;
            audioSource.volume = 1f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 20f;
            audioSource.spread = 180f;
            audioSource.reverbZoneMix = 1.5f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;

            if (!creepyFogEffect) creepyFogEffect = playerCamera.GetComponent<CreepyFogEffect>();
            if (!playerMovement) playerMovement = FindObjectOfType<PlayerMovement>();

            ambientSoundTimer = Random.Range(minSoundInterval, maxSoundInterval);
        }

        private void Update()
        {
            raycastTimer -= Time.deltaTime;
            if (raycastTimer <= 0)
            {
                PerformRaycast();
                raycastTimer = raycastInterval;
            }

            if (Input.GetKeyDown(KeyCode.E) && currentPoint != null)
            {
                currentPoint.Interact(this);
            }

            if (hoverSoundTimer > 0)
            {
                hoverSoundTimer -= Time.deltaTime;
            }

            ambientSoundTimer -= Time.deltaTime;
            if (ambientSoundTimer <= 0)
            {
                PlayAmbientSound();
                ambientSoundTimer = Random.Range(minSoundInterval, maxSoundInterval);
            }
        }

        private void PerformRaycast()
        {
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
            {
                InteractablePoint point = hit.collider.GetComponent<InteractablePoint>();
                if (point && point != currentPoint)
                {
                    currentPoint = point;
                    interactionUI.Show(point);
                }
            }
            else if (currentPoint != null)
            {
                interactionUI.Hide();
                currentPoint = null;
            }
        }

        public void HandleInteraction(InteractionConfig config, GameObject interactableObject)
        {
            if (config == null)
            {
                DebugLogger.LogError("InteractionConfig is null in HandleInteraction!");
                return;
            }

            // Проигрываем horrorWhisperClip при взаимодействии
            PlayHoverSound(horrorWhisperClip);

            if (config.interactSound != null)
            {
                audioSource.transform.position = interactableObject.transform.position;
                audioSource.PlayOneShot(config.interactSound, 1f);
            }

            switch (config.interactionType)
            {
                case InteractionType.OpenDoor:
                    StartCoroutine(OpenDoor(config.targetObject, config.moveToPosition, config.moveDuration));
                    break;
                case InteractionType.PickUpItem:
                    PickUpItem(interactableObject);
                    break;
                case InteractionType.ActivateTrap:
                    ActivateTrap(config.targetObject);
                    ShakeCamera(0.2f, 0.3f);
                    break;
                case InteractionType.ToggleSwitch:
                    ToggleSwitch(config.targetObject);
                    break;
                case InteractionType.ShowNarrative:
                    ShowNarrativeText(config.narrativeText);
                    break;
                case InteractionType.ApplyHorrorEffect:
                    ApplyHorrorEffects(config.horrorEffectIntensity);
                    break;
                case InteractionType.None:
                default:
                    DebugLogger.LogWarning("No interaction type defined!");
                    break;
            }

            config.onInteract?.Invoke();
        }

        private IEnumerator OpenDoor(GameObject door, Vector3 moveToPosition, float duration)
        {
            if (door == null) yield break;

            Vector3 startPosition = door.transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                door.transform.position = Vector3.Lerp(startPosition, moveToPosition, elapsed / duration);
                yield return null;
            }
            door.transform.position = moveToPosition;
        }

        private void PickUpItem(GameObject item)
        {
            if (item == null) return;

            item.SetActive(false);
            DebugLogger.Log($"Picked up item: {item.name}");
        }

        private void ActivateTrap(GameObject trapObject)
        {
            if (trapObject == null) return;

            var trap = trapObject.GetComponent<TrapComponent>();
            if (trap != null)
            {
                trap.Activate();
            }
            DebugLogger.Log($"Activated trap: {trapObject.name}");
        }

        private void ShowNarrativeText(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                DebugLogger.Log($"Narrative text: {text}");
                interactionUI.ShowNarrativeText(text);
            }
        }

        private void ToggleSwitch(GameObject switchObject)
        {
            if (switchObject == null) return;

            var light = switchObject.GetComponent<Light>();
            if (light != null)
            {
                light.enabled = !light.enabled;
                DebugLogger.Log($"Toggled light: {light.enabled}");
            }
        }

        private void ApplyHorrorEffects(float intensity)
        {
            if (creepyFogEffect != null)
            {
                float currentDensity = creepyFogEffect.GetDensity();
                creepyFogEffect.SetDensity(currentDensity + intensity);
            }

            if (playerMovement != null)
            {
                playerMovement.SetFearMultiplier(playerMovement.GetFearMultiplier() + intensity);
            }
        }

        private void PlayHoverSound(AudioClip clip)
        {
            if (hoverSoundTimer > 0 || isPlayingEcho || !clip) return;

            DebugLogger.Log("Starting HorrorWhisper with echo");
            StartCoroutine(PlaySoundWithEcho(clip));
            hoverSoundTimer = 1f + (echoDelay * echoCount);
        }

        private IEnumerator PlaySoundWithEcho(AudioClip clip)
        {
            isPlayingEcho = true;

            // Сохраняем начальную позицию объекта
            Vector3 soundPosition = currentPoint != null ? currentPoint.transform.position : transform.position;

            audioSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
            audioSource.volume = 1f;
            audioSource.transform.position = soundPosition + Random.insideUnitSphere * 0.2f;
            audioSource.PlayOneShot(clip);
            DebugLogger.Log($"Main sound played at volume {audioSource.volume}, pitch {audioSource.pitch}, position {audioSource.transform.position}");

            for (int i = 1; i <= echoCount; i++)
            {
                yield return new WaitForSeconds(echoDelay);
                if (!gameObject.activeInHierarchy)
                {
                    isPlayingEcho = false;
                    yield break;
                }
                audioSource.pitch = Random.Range(1f - pitchVariation * (1f + i * 0.1f), 1f + pitchVariation * (1f + i * 0.1f));
                audioSource.volume = initialEchoVolume * Mathf.Pow(echoVolumeReduction, i);
                audioSource.transform.position = soundPosition + Random.insideUnitSphere * 0.3f;
                audioSource.PlayOneShot(clip);
                DebugLogger.Log($"Echo {i} played at volume {audioSource.volume}, pitch {audioSource.pitch}, position {audioSource.transform.position}");
            }

            audioSource.pitch = 1f;
            audioSource.volume = 1f;
            isPlayingEcho = false;
        }

        private void PlayAmbientSound()
        {
            if (ambientSounds.Length == 0) return;
            AudioClip clip = ambientSounds[Random.Range(0, ambientSounds.Length)];
            audioSource.PlayOneShot(clip, Random.Range(0.3f, 0.6f));
            DebugLogger.Log($"Played ambient sound: {clip.name}");
        }

        public void ShakeCamera(float intensity, float duration)
        {
            StartCoroutine(ShakeCameraCoroutine(intensity, duration));
        }

        private IEnumerator ShakeCameraCoroutine(float intensity, float duration)
        {
            Vector3 originalPos = playerCamera.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;
                playerCamera.transform.localPosition = originalPos + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }
            playerCamera.transform.localPosition = originalPos;
        }
    }
}