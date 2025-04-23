using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using System.Collections;

public class InteractionUIBuilder : MonoBehaviour
{
    private GameObject canvas;
    private RectTransform panelRectTransform;
    private TextMeshProUGUI descriptionText;
    private Button interactButton;
    private TextMeshProUGUI buttonText;
    private CanvasGroup canvasGroup;
    private Camera playerCamera;
    private Camera uiCamera;
    private RenderTexture uiRenderTexture;
    private AudioSource audioSource;

    private const float FADE_DURATION = 0.3f; 
    private bool isFading;
    private Vector3 initialPosition;
    private float hoverSoundTimer;
    private float glitchTimer;

    
    public GameObject GetPanel() => canvas;
    public TextMeshProUGUI GetDescriptionText() => descriptionText;
    public Button GetInteractButton() => interactButton;
    public TextMeshProUGUI GetButtonText() => buttonText;

    public void Initialize(Camera camera)
    {
        playerCamera = camera;
        SetupUICamera();
        CreateUI();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        CreepyFogEffect fogEffect = playerCamera.GetComponent<CreepyFogEffect>();
        if (fogEffect != null)
        {
            fogEffect.uiRenderTexture = uiRenderTexture;
        }
    }

    private void SetupUICamera()
    {
        GameObject uiCameraObj = new GameObject("UICamera");
        uiCamera = uiCameraObj.AddComponent<Camera>();
        uiCamera.transform.SetParent(playerCamera.transform, false);
        uiCamera.clearFlags = CameraClearFlags.Color;
        uiCamera.backgroundColor = new Color(0, 0, 0, 0);
        uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
        uiCamera.depth = playerCamera.depth + 1;

        uiRenderTexture = new RenderTexture(Screen.width, Screen.height, 0);
        uiCamera.targetTexture = uiRenderTexture;
    }

    private void CreateUI()
    {
        canvas = new GameObject("InteractionCanvas");
        Canvas canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.WorldSpace;
        canvasComponent.worldCamera = uiCamera;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(0.2f, 0.2f); 
        canvasRect.localScale = new Vector3(0.003f, 0.003f, 0.003f); 

        canvas.layer = LayerMask.NameToLayer("UI");
        canvasComponent.sortingLayerName = "UIOverlay";
        canvasComponent.sortingOrder = 1000;

        canvasGroup = canvas.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        
        GameObject panel = new GameObject("InteractionPanel");
        panel.transform.SetParent(canvas.transform, false);
        panel.layer = LayerMask.NameToLayer("UI");
        panelRectTransform = panel.AddComponent<RectTransform>();
        panelRectTransform.sizeDelta = new Vector2(180, 100); 
        panelRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        panelRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panelRectTransform.pivot = new Vector2(0.5f, 0.5f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.03f, 0.03f, 0.08f, 0.8f); 
        panelImage.sprite = CreateJaggedSprite(); 
        panelImage.type = Image.Type.Sliced;

        
        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.3f, 0.3f, 0.7f, 0.85f); 
        panelOutline.effectDistance = new Vector2(1.5f, 1.5f);

        
        GameObject descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(panel.transform, false);
        descObj.layer = LayerMask.NameToLayer("UI");
        descriptionText = descObj.AddComponent<TextMeshProUGUI>();
        descriptionText.rectTransform.sizeDelta = new Vector2(160, 50);
        descriptionText.rectTransform.anchoredPosition = new Vector2(0, 10);
        descriptionText.fontSize = 16; 
        descriptionText.color = new Color(0.7f, 0.7f, 0.9f, 1f); 
        descriptionText.alignment = TextAlignmentOptions.Center;
        descriptionText.text = "Interact with this point";
        descriptionText.fontMaterial.EnableKeyword("GLOW_ON");
        descriptionText.fontMaterial.SetColor("_GlowColor", new Color(0.4f, 0.4f, 0.8f, 0.4f)); 

        
        GameObject buttonObj = new GameObject("InteractButton");
        buttonObj.transform.SetParent(panel.transform, false);
        buttonObj.layer = LayerMask.NameToLayer("UI");
        interactButton = buttonObj.AddComponent<Button>();
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(90, 30); 
        buttonRect.anchoredPosition = new Vector2(0, -25);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.1f, 0.1f, 0.15f, 1f); 
        buttonImage.sprite = CreateJaggedSprite(); 
        buttonImage.type = Image.Type.Sliced;

        
        GameObject buttonTextObj = new GameObject("ButtonText");
        buttonTextObj.transform.SetParent(buttonObj.transform, false);
        buttonTextObj.layer = LayerMask.NameToLayer("UI");
        buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.rectTransform.sizeDelta = new Vector2(80, 25);
        buttonText.fontSize = 14;
        buttonText.color = new Color(0.7f, 0.7f, 0.9f, 1f);
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.text = "Interact";
        buttonText.fontMaterial.EnableKeyword("GLOW_ON");
        buttonText.fontMaterial.SetColor("_GlowColor", new Color(0.4f, 0.4f, 0.8f, 0.4f));

        AddButtonAnimation(buttonObj, buttonImage);
        interactButton.transition = Selectable.Transition.None;
    }

    private Sprite CreateJaggedSprite()
    {
        Texture2D tex = new Texture2D(64, 64);
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                
                float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.2f + 0.8f;
                if (x < 4 || x > 60 || y < 4 || y > 60)
                {
                    if (Random.value > noise)
                        tex.SetPixel(x, y, new Color(1, 1, 1, 0));
                    else
                        tex.SetPixel(x, y, Color.white);
                }
                else
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100);
    }

    private void AddButtonAnimation(GameObject buttonObj, Image buttonImage)
    {
        EventTrigger trigger = buttonObj.AddComponent<EventTrigger>();
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pointerEnter.callback.AddListener((data) => {
            buttonImage.color = new Color(0.2f, 0.2f, 0.25f, 1f); 
            buttonObj.transform.localScale = Vector3.one * 1.02f;
            PlayHoverSound();
        });
        trigger.triggers.Add(pointerEnter);

        EventTrigger.Entry pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        pointerExit.callback.AddListener((data) => {
            buttonImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            buttonObj.transform.localScale = Vector3.one;
        });
        trigger.triggers.Add(pointerExit);

        EventTrigger.Entry pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        pointerDown.callback.AddListener((data) => {
            buttonObj.transform.localScale = Vector3.one * 0.9f;
        });
        trigger.triggers.Add(pointerDown);

        EventTrigger.Entry pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        pointerUp.callback.AddListener((data) => {
            buttonObj.transform.localScale = Vector3.one * 1.02f;
        });
        trigger.triggers.Add(pointerUp);
    }

    private void PlayHoverSound()
    {
        if (hoverSoundTimer <= 0)
        {
            AudioClip clip = Resources.Load<AudioClip>("HorrorWhisper");
            if (clip)
            {
                audioSource.PlayOneShot(clip);
                hoverSoundTimer = 1f;
            }
        }
    }

    public void Show(InteractablePoint point, Vector3 worldPosition)
    {
        if (!descriptionText || !buttonText || !interactButton)
        {
            return;
        }

        descriptionText.text = point.GetDisplayText();
        buttonText.text = point.GetButtonText();
        interactButton.onClick.RemoveAllListeners();
        interactButton.onClick.AddListener(point.Interact);

        Vector3 cameraPos = playerCamera.transform.position;
        Vector3 direction = (worldPosition - cameraPos).normalized;
        
        Vector3 offset = playerCamera.transform.right * 0.5f + playerCamera.transform.up * 0.05f;
        canvas.transform.position = cameraPos + direction * 1.8f + offset; 
        canvas.transform.LookAt(playerCamera.transform);
        canvas.transform.Rotate(0, 180, 0);
        initialPosition = canvas.transform.position;

        if (!isFading)
        {
            StartCoroutine(FadeIn());
        }
    }

    public void Hide()
    {
        if (!isFading)
        {
            StartCoroutine(FadeOut());
        }
    }

    private void Update()
    {
        if (canvasGroup.alpha > 0)
        {
            
            float floatOffset = Mathf.Sin(Time.time * 1.5f) * 0.005f;
            canvas.transform.position = initialPosition + new Vector3(0, floatOffset, 0);

            
            glitchTimer -= Time.deltaTime;
            if (glitchTimer <= 0)
            {
                float glitchChance = Random.value;
                if (glitchChance < 0.1f)
                {
                    canvas.transform.position += new Vector3(Random.Range(-0.01f, 0.01f), Random.Range(-0.01f, 0.01f), 0);
                    canvasGroup.alpha = Random.Range(0.75f, 1f);
                    StartCoroutine(ResetGlitch());
                }
                glitchTimer = Random.Range(0.3f, 1.2f);
            }

            
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.006f;
            canvas.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f) * pulse;

            
            float flicker = Random.Range(0.9f, 1f);
            descriptionText.color = new Color(0.7f, 0.7f, 0.9f, flicker);
            buttonText.color = new Color(0.7f, 0.7f, 0.9f, flicker);

            
            Outline panelOutline = panelRectTransform.GetComponent<Outline>();
            if (panelOutline)
            {
                panelOutline.effectColor = new Color(0.3f, 0.3f, 0.7f, 0.85f + Mathf.Sin(Time.time * 5f) * 0.1f);
            }

            if (hoverSoundTimer > 0)
            {
                hoverSoundTimer -= Time.deltaTime;
            }
        }
    }

    private IEnumerator FadeIn()
    {
        isFading = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        float elapsed = 0f;
        while (elapsed < FADE_DURATION)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / FADE_DURATION);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        isFading = false;
    }

    private IEnumerator FadeOut()
    {
        isFading = true;
        float elapsed = 0f;
        while (elapsed < FADE_DURATION)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / FADE_DURATION);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        isFading = false;
    }

    private IEnumerator ResetGlitch()
    {
        yield return new WaitForSeconds(0.07f);
        canvas.transform.position = initialPosition;
        canvasGroup.alpha = 1f;
    }
}