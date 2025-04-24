using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace HorrorGame
{
    public class InteractionUIBuilder : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private Vector3 canvasScale = new Vector3(0.003f, 0.003f, 0.003f);
        [SerializeField] private Vector2 panelSize = new Vector2(300, 100);
        [SerializeField] private string uiLayer = "UI";
        [SerializeField] private string sortingLayer = "UIOverlay";
        [SerializeField] private int sortingOrder = 1000;

        private GameObject canvas;
        private RectTransform panelRectTransform;
        private TextMeshProUGUI descriptionText;
        private CanvasGroup canvasGroup;
        private Camera playerCamera;
        private Camera uiCamera;
        private RenderTexture uiRenderTexture;

        private bool isFading;
        private Vector3 initialPosition;
        private float glitchTimer;

        public GameObject GetPanel() => canvas;
        public TextMeshProUGUI GetDescriptionText() => descriptionText;

        public void Initialize(Camera camera)
        {
            if (camera == null)
            {
                DebugLogger.LogError("Player camera is not assigned!");
                return;
            }

            playerCamera = camera;
            SetupUICamera();
            CreateUI();

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
            uiCamera.backgroundColor = Color.clear;
            uiCamera.cullingMask = 1 << LayerMask.NameToLayer(uiLayer);
            uiCamera.depth = playerCamera.depth + 1;

            uiRenderTexture = new RenderTexture(Screen.width, Screen.height, 0);
            uiCamera.targetTexture = uiRenderTexture;
        }

        private void CreateUI()
        {
            canvas = CreateCanvas();
            canvasGroup = canvas.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            panelRectTransform = CreatePanel(canvas.transform);
            CreateKeyContainer(panelRectTransform);
            descriptionText = CreateTextContainer(panelRectTransform);
        }

        private GameObject CreateCanvas()
        {
            GameObject canvasObj = new GameObject("InteractionCanvas");
            Canvas canvasComponent = canvasObj.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.WorldSpace;
            canvasComponent.worldCamera = uiCamera;

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(0.2f, 0.2f);
            canvasRect.localScale = canvasScale;

            canvasObj.layer = LayerMask.NameToLayer(uiLayer);
            canvasComponent.sortingLayerName = sortingLayer;
            canvasComponent.sortingOrder = sortingOrder;

            return canvasObj;
        }

        private RectTransform CreatePanel(Transform parent)
        {
            GameObject panel = new GameObject("InteractionPanel");
            panel.transform.SetParent(parent, false);
            panel.layer = LayerMask.NameToLayer(uiLayer);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.sizeDelta = panelSize;
            rect.anchoredPosition = Vector2.zero;

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
            panelImage.sprite = CreateJaggedSprite();
            panelImage.type = Image.Type.Sliced;

            Outline panelOutline = panel.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.5f, 0.1f, 0.1f, 0.85f);
            panelOutline.effectDistance = new Vector2(2f, 2f);

            return rect;
        }

        private void CreateKeyContainer(RectTransform parent)
        {
            GameObject keyContainer = new GameObject("KeyContainer");
            keyContainer.transform.SetParent(parent, false);

            RectTransform keyRect = keyContainer.AddComponent<RectTransform>();
            keyRect.sizeDelta = new Vector2(50, 50);
            keyRect.anchoredPosition = new Vector2(-110, 0);

            Image keyBg = keyContainer.AddComponent<Image>();
            keyBg.sprite = CreateJaggedSprite();
            keyBg.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

            GameObject keyTextObj = new GameObject("KeyText");
            keyTextObj.transform.SetParent(keyContainer.transform, false);

            RectTransform keyTextRect = keyTextObj.AddComponent<RectTransform>();
            keyTextRect.sizeDelta = new Vector2(50, 50);
            keyTextRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI keyText = keyTextObj.AddComponent<TextMeshProUGUI>();
            keyText.text = "E";
            keyText.fontSize = 28;
            keyText.color = new Color(0.8f, 0.8f, 0.9f, 1f);
            keyText.alignment = TextAlignmentOptions.Center;
            keyText.fontMaterial?.EnableKeyword("GLOW_ON");
            keyText.fontMaterial?.SetColor("_GlowColor", new Color(0.5f, 0.1f, 0.1f, 0.4f));
        }

        private TextMeshProUGUI CreateTextContainer(RectTransform parent)
        {
            GameObject textContainer = new GameObject("TextContainer");
            textContainer.transform.SetParent(parent, false);

            RectTransform textRect = textContainer.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(180, 50);
            textRect.anchoredPosition = new Vector2(20, 0);

            TextMeshProUGUI text = textContainer.AddComponent<TextMeshProUGUI>();
            text.text = "Open Door";
            text.fontSize = 22;
            text.color = new Color(0.8f, 0.8f, 0.9f, 1f);
            text.alignment = TextAlignmentOptions.Left;
            text.fontStyle = FontStyles.Bold;
            text.fontMaterial?.EnableKeyword("GLOW_ON");
            text.fontMaterial?.SetColor("_GlowColor", new Color(0.5f, 0.1f, 0.1f, 0.4f));

            return text;
        }

        private Sprite CreateJaggedSprite()
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.2f + 0.8f;
                    bool isEdge = x < 4 || x > size - 4 || y < 4 || y > size - 4;
                    tex.SetPixel(x, y, isEdge && Random.value > noise ? Color.clear : Color.white);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        public void Show(InteractablePoint point, Vector3 worldPosition)
        {
            if (descriptionText == null || point == null) return;

            descriptionText.text = point.GetDisplayText();

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

        public void ShowNarrativeText(string narrativeText)
        {
            if (descriptionText == null) return;

            StartCoroutine(TypewriterEffect(narrativeText));

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
            if (canvasGroup.alpha <= 0) return;

            float floatOffset = Mathf.Sin(Time.time * 1.5f) * 0.005f;
            canvas.transform.position = initialPosition + new Vector3(0, floatOffset, 0);

            glitchTimer -= Time.deltaTime;
            if (glitchTimer <= 0)
            {
                if (Random.value < 0.1f)
                {
                    canvas.transform.position += new Vector3(Random.Range(-0.01f, 0.01f), Random.Range(-0.01f, 0.01f), 0);
                    canvasGroup.alpha = Random.Range(0.75f, 1f);
                    StartCoroutine(ResetGlitch());
                }
                glitchTimer = Random.Range(0.3f, 1.2f);
            }

            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.006f;
            canvas.transform.localScale = canvasScale * pulse;

            float flicker = Random.Range(0.9f, 1f);
            descriptionText.color = new Color(0.7f, 0.7f, 0.9f, flicker);

            if (panelRectTransform.TryGetComponent<Outline>(out var panelOutline))
            {
                panelOutline.effectColor = new Color(0.3f, 0.3f, 0.7f, 0.85f + Mathf.Sin(Time.time * 5f) * 0.1f);
            }
        }

        private IEnumerator FadeIn()
        {
            isFading = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            isFading = false;
        }

        private IEnumerator FadeOut()
        {
            isFading = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
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

        private IEnumerator TypewriterEffect(string text)
        {
            descriptionText.text = "";
            foreach (char c in text)
            {
                descriptionText.text += c;
                yield return new WaitForSeconds(0.05f);
            }
        }
    }
}