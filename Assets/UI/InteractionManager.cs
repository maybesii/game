using UnityEngine;
using UnityEngine.Rendering;

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

    private InteractablePoint currentPoint;
    private AudioSource audioSource;

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

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f; 
        audioSource.volume = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 20f;
        audioSource.spread = 180f;

        if (!creepyFogEffect) creepyFogEffect = playerCamera.GetComponent<CreepyFogEffect>();
        if (!playerMovement) playerMovement = FindObjectOfType<PlayerMovement>();
    }

    private void Update()
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
                if (point.GetHoverSound())
                {
                    audioSource.PlayOneShot(point.GetHoverSound());
                }
            }
            if (Input.GetKeyDown(KeyCode.E) && currentPoint != null)
            {
                currentPoint.Interact();
                PlayInteractSound();
                ApplyHorrorEffects();
            }
        }
        else if (currentPoint != null)
        {
            interactionUI.Hide();
            currentPoint = null;
        }
    }

    private void PlayInteractSound()
    {
        if (horrorWhisperClip)
        {
            audioSource.PlayOneShot(horrorWhisperClip, 1f);
        }
    }

    private void ApplyHorrorEffects()
    {
        if (creepyFogEffect != null)
        {
            float currentDensity = creepyFogEffect.GetDensity();
            creepyFogEffect.SetDensity(currentDensity + 0.02f);
        }

        if (playerMovement != null)
        {
            playerMovement.SetFearMultiplier(playerMovement.GetFearMultiplier() + 0.2f);
        }
    }
}