using UnityEngine;
using System.Collections;

[ExecuteAlways]
public class FogController : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] private bool enableFog = true;
    [SerializeField] [ColorUsage(true, true)] private Color fogColor = new Color(0.15f, 0.2f, 0.25f, 1f);
    [SerializeField] [Range(0.01f, 0.08f)] private float fogDensity = 0.04f;
    
    [Header("Pulsation Effects")]
    [SerializeField] private bool enablePulsation = true;
    [SerializeField] [Range(0f, 0.015f)] private float pulsationAmplitude = 0.008f;
    [SerializeField] [Range(0.5f, 1.5f)] private float pulsationFrequency = 0.8f;
    
    [Header("Color Effects")]
    [SerializeField] private float flashProbability = 0.015f;
    [SerializeField] private Color[] flashColors = new Color[]
    {
        new Color(0.6f, 0.1f, 0.1f, 1f), 
        new Color(0.1f, 0.4f, 0.2f, 1f)  
    };
    [SerializeField] private float flashDuration = 0.25f;
    [SerializeField] [Range(0.1f, 1f)] private float colorLerpSpeed = 0.3f;
    
    [Header("Player Interaction")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float runDensityMultiplier = 1.4f;
    [SerializeField] private float movementDensityBoost = 0.01f;

    [Header("Camera Setup")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraFarPlane = 150f;

    private float timeAccumulator;
    private bool isFlashing;
    private Color targetColor;

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

        targetColor = fogColor;
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

        if (!isFlashing && Random.value < flashProbability * Time.deltaTime)
        {
            StartCoroutine(FlashColor());
        }

        if (!isFlashing)
        {
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, targetColor, Time.deltaTime * colorLerpSpeed);
        }
    }

    private IEnumerator FlashColor()
    {
        isFlashing = true;
        Color flashColor = flashColors[Random.Range(0, flashColors.Length)];
        RenderSettings.fogColor = flashColor;
        yield return new WaitForSeconds(flashDuration);
        targetColor = fogColor;
        isFlashing = false;
    }

    public void SetFogColor(Color newColor) => fogColor = newColor;
    public void SetFogDensity(float density) => fogDensity = Mathf.Clamp(density, 0.01f, 0.08f);
    public void ToggleFog(bool state) => enableFog = state;
}