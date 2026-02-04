using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using static LogHelper;

/// <summary>
/// UI del tutorial con indicadores visuales y animaciones
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [Header("UI Elements")]
    private GameObject panel;
    private GameObject overlay; // Overlay oscuro de fondo
    private Text titleText;
    private Text descriptionText;
    private Button continueButton;
    private Button skipButton;
    private Text continueButtonText;
    private Text skipButtonText;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    
    private CanvasGroup panelCanvasGroup;
    private bool isVisible = false;
    
    private void Awake()
    {
        CreateUI();
    }
    
    /// <summary>
    /// Crea la UI del tutorial
    /// </summary>
    private void CreateUI()
    {
        // Buscar o crear Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Asegurar que existe un EventSystem (necesario para que los botones funcionen)
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Log("[TutorialUI] EventSystem creado");
        }
        
        // Panel principal
        panel = new GameObject("TutorialPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        // CanvasGroup para animaciones
        panelCanvasGroup = panel.AddComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.blocksRaycasts = false;
        panelCanvasGroup.interactable = false; // Inicialmente no interactivo
        
        // Overlay oscuro de fondo
        overlay = new GameObject("Overlay");
        overlay.transform.SetParent(panel.transform, false);
        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0.7f); // Fondo oscuro semi-transparente
        overlayImage.raycastTarget = false; // No bloquear raycasts, solo visual
        
        // Contenedor del contenido
        GameObject contentContainer = new GameObject("ContentContainer");
        contentContainer.transform.SetParent(panel.transform, false);
        RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(800, 500);
        
        // Fondo del panel de contenido (con estilo Space Neon)
        Image contentBg = contentContainer.AddComponent<Image>();
        contentBg.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);
        
        // Borde con efecto neon
        GameObject border = new GameObject("Border");
        border.transform.SetParent(contentContainer.transform, false);
        RectTransform borderRect = border.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;
        
        Image borderImage = border.AddComponent<Image>();
        borderImage.color = new Color(0.4f, 0.6f, 1f, 0.8f); // Azul neon
        
        // Título
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(contentContainer.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -40);
        titleRect.sizeDelta = new Vector2(700, 80);
        
        titleText = titleObj.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(0.6f, 0.8f, 1f, 1f); // Azul claro
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.text = "Título";
        
        // Outline para el título
        Outline titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        titleOutline.effectDistance = new Vector2(2, 2);
        
        // Descripción
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(contentContainer.transform, false);
        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.5f, 0.5f);
        descRect.anchorMax = new Vector2(0.5f, 0.5f);
        descRect.pivot = new Vector2(0.5f, 0.5f);
        descRect.anchoredPosition = new Vector2(0, 20);
        descRect.sizeDelta = new Vector2(700, 200);
        
        descriptionText = descObj.AddComponent<Text>();
        descriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descriptionText.fontSize = 32;
        descriptionText.color = Color.white;
        descriptionText.alignment = TextAnchor.MiddleCenter;
        descriptionText.text = "Descripción";
        
        // Botón Continuar
        GameObject continueBtnObj = new GameObject("ContinueButton");
        continueBtnObj.transform.SetParent(contentContainer.transform, false);
        RectTransform continueBtnRect = continueBtnObj.AddComponent<RectTransform>();
        continueBtnRect.anchorMin = new Vector2(0.5f, 0f);
        continueBtnRect.anchorMax = new Vector2(0.5f, 0f);
        continueBtnRect.pivot = new Vector2(0.5f, 0f);
        continueBtnRect.anchoredPosition = new Vector2(0, 60);
        continueBtnRect.sizeDelta = new Vector2(300, 80);
        
        Image continueBtnBg = continueBtnObj.AddComponent<Image>();
        continueBtnBg.color = new Color(0.2f, 0.5f, 0.9f, 1f); // Azul
        continueBtnBg.raycastTarget = true; // Asegurar que recibe raycasts
        
        continueButton = continueBtnObj.AddComponent<Button>();
        continueButton.targetGraphic = continueBtnBg;
        continueButton.interactable = true; // Asegurar que el botón es interactivo
        
        // Texto del botón Continuar
        GameObject continueTextObj = new GameObject("Text");
        continueTextObj.transform.SetParent(continueBtnObj.transform, false);
        RectTransform continueTextRect = continueTextObj.AddComponent<RectTransform>();
        continueTextRect.anchorMin = Vector2.zero;
        continueTextRect.anchorMax = Vector2.one;
        continueTextRect.sizeDelta = Vector2.zero;
        
        continueButtonText = continueTextObj.AddComponent<Text>();
        continueButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        continueButtonText.fontSize = 36;
        continueButtonText.fontStyle = FontStyle.Bold;
        continueButtonText.color = Color.white;
        continueButtonText.alignment = TextAnchor.MiddleCenter;
        continueButtonText.text = "Continuar";
        continueButtonText.raycastTarget = false; // El texto no debe bloquear raycasts
        
        // Botón Saltar
        GameObject skipBtnObj = new GameObject("SkipButton");
        skipBtnObj.transform.SetParent(contentContainer.transform, false);
        RectTransform skipBtnRect = skipBtnObj.AddComponent<RectTransform>();
        skipBtnRect.anchorMin = new Vector2(1f, 1f);
        skipBtnRect.anchorMax = new Vector2(1f, 1f);
        skipBtnRect.pivot = new Vector2(1f, 1f);
        skipBtnRect.anchoredPosition = new Vector2(-20, -20);
        skipBtnRect.sizeDelta = new Vector2(120, 50);
        
        Image skipBtnBg = skipBtnObj.AddComponent<Image>();
        skipBtnBg.color = Color.clear; // Transparente pero recibe raycasts
        skipBtnBg.raycastTarget = true;
        
        skipButton = skipBtnObj.AddComponent<Button>();
        skipButton.targetGraphic = skipBtnBg;
        skipButton.interactable = true;
        
        // Texto del botón Saltar
        GameObject skipTextObj = new GameObject("Text");
        skipTextObj.transform.SetParent(skipBtnObj.transform, false);
        RectTransform skipTextRect = skipTextObj.AddComponent<RectTransform>();
        skipTextRect.anchorMin = Vector2.zero;
        skipTextRect.anchorMax = Vector2.one;
        skipTextRect.sizeDelta = Vector2.zero;
        
        skipButtonText = skipTextObj.AddComponent<Text>();
        skipButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skipButtonText.fontSize = 24;
        skipButtonText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        skipButtonText.alignment = TextAnchor.MiddleCenter;
        skipButtonText.text = "Saltar";
        skipButtonText.raycastTarget = false; // El texto no debe bloquear raycasts
        
        // Ocultar inicialmente
        Hide();
    }
    
    /// <summary>
    /// Muestra un paso del tutorial
    /// </summary>
    public void ShowStep(string title, string description, string buttonText)
    {
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
        if (continueButtonText != null) continueButtonText.text = buttonText;
        
        // Configurar botones ANTES de mostrar
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => {
                Log("[TutorialUI] Botón Continuar presionado");
                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.NextStep();
                }
                else
                {
                    LogError("[TutorialUI] TutorialManager.Instance es null!");
                }
            });
            continueButton.interactable = true;
        }
        else
        {
            LogError("[TutorialUI] continueButton es null!");
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() => {
                Log("[TutorialUI] Botón Saltar presionado");
                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.SkipTutorial();
                }
            });
            skipButton.interactable = true;
        }
        
        // Mostrar con animación
        Show();
    }
    
    /// <summary>
    /// Muestra la UI con animación
    /// </summary>
    private void Show()
    {
        if (isVisible) return;
        
        isVisible = true;
        panelCanvasGroup.blocksRaycasts = true;
        panelCanvasGroup.interactable = true; // Permitir interacción
        
        StartCoroutine(FadeIn());
    }
    
    /// <summary>
    /// Oculta la UI
    /// </summary>
    public void Hide()
    {
        if (!isVisible) return;
        
        isVisible = false;
        panelCanvasGroup.blocksRaycasts = false;
        panelCanvasGroup.interactable = false; // Desactivar interacción
        
        StartCoroutine(FadeOut());
    }
    
    /// <summary>
    /// Animación de fade in
    /// </summary>
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// Animación de fade out
    /// </summary>
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = panelCanvasGroup.alpha;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        panelCanvasGroup.alpha = 0f;
    }
}
