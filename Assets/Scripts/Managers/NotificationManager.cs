using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using static LogHelper;

/// <summary>
/// Manager para mostrar notificaciones temporales al usuario (toasts)
/// </summary>
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }
    
    [Header("Notification Settings")]
    [SerializeField] private float defaultDuration = 3f;
    [SerializeField] private Color successColor = new Color(0f, 0.91f, 1f, 1f); // NeonCyan
    [SerializeField] private Color errorColor = new Color(1f, 0.31f, 0.31f, 1f); // Rojo
    [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0f, 1f); // Amarillo
    [SerializeField] private Color infoColor = new Color(0.85f, 0.95f, 1f, 1f); // SpaceWhite
    
    private GameObject canvas;
    private Canvas notificationCanvas;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateNotificationCanvas();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void CreateNotificationCanvas()
    {
        // Crear Canvas dedicado para notificaciones (siempre en la capa superior)
        canvas = new GameObject("NotificationCanvas");
        canvas.transform.SetParent(transform);
        
        notificationCanvas = canvas.AddComponent<Canvas>();
        notificationCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        notificationCanvas.sortingOrder = 9999; // Siempre encima de todo
        
        CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvas.AddComponent<GraphicRaycaster>();
        canvas.layer = 5; // UI layer
    }
    
    /// <summary>
    /// Muestra una notificación de éxito
    /// </summary>
    public void ShowSuccess(string message, float duration = -1f)
    {
        ShowNotification(message, successColor, duration);
    }
    
    /// <summary>
    /// Muestra una notificación de error
    /// </summary>
    public void ShowError(string message, float duration = -1f)
    {
        ShowNotification(message, errorColor, duration);
    }
    
    /// <summary>
    /// Muestra una notificación de advertencia
    /// </summary>
    public void ShowWarning(string message, float duration = -1f)
    {
        ShowNotification(message, warningColor, duration);
    }
    
    /// <summary>
    /// Muestra una notificación informativa
    /// </summary>
    public void ShowInfo(string message, float duration = -1f)
    {
        ShowNotification(message, infoColor, duration);
    }
    
    /// <summary>
    /// Muestra una notificación genérica
    /// </summary>
    private void ShowNotification(string message, Color color, float duration)
    {
        if (canvas == null)
        {
            CreateNotificationCanvas();
        }
        
        if (duration < 0)
        {
            duration = defaultDuration;
        }
        
        StartCoroutine(ShowNotificationCoroutine(message, color, duration));
    }
    
    private IEnumerator ShowNotificationCoroutine(string message, Color color, float duration)
    {
        // Crear objeto de notificación
        GameObject notificationObj = new GameObject("Notification");
        notificationObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = notificationObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(600, 80);
        rect.anchoredPosition = new Vector2(0, -50);
        
        // Fondo semi-transparente
        Image bgImage = notificationObj.AddComponent<Image>();
        bgImage.color = new Color(0.02f, 0.024f, 0.067f, 0.95f); // SpaceBlack con alta opacidad
        
        // Borde de color
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(notificationObj.transform, false);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = color;
        
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = new Vector2(1f, 0f);
        borderRect.sizeDelta = new Vector2(0, 4f);
        borderRect.anchoredPosition = Vector2.zero;
        
        // Texto
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(notificationObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = message;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Animación de entrada (deslizar desde arriba)
        float startY = rect.anchoredPosition.y;
        float targetY = -50f;
        float elapsed = 0f;
        float fadeInDuration = 0.3f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            float currentY = Mathf.Lerp(startY - 100f, targetY, t);
            rect.anchoredPosition = new Vector2(0, currentY);
            
            Color currentColor = bgImage.color;
            currentColor.a = Mathf.Lerp(0f, 0.95f, t);
            bgImage.color = currentColor;
            
            yield return null;
        }
        
        rect.anchoredPosition = new Vector2(0, targetY);
        
        // Esperar duración
        yield return new WaitForSeconds(duration);
        
        // Animación de salida
        elapsed = 0f;
        float fadeOutDuration = 0.3f;
        startY = rect.anchoredPosition.y;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            float currentY = Mathf.Lerp(startY, startY + 100f, t);
            rect.anchoredPosition = new Vector2(0, currentY);
            
            Color currentColor = bgImage.color;
            currentColor.a = Mathf.Lerp(0.95f, 0f, t);
            bgImage.color = currentColor;
            
            Color textColor = text.color;
            textColor.a = Mathf.Lerp(1f, 0f, t);
            text.color = textColor;
            
            yield return null;
        }
        
        // Destruir notificación
        Destroy(notificationObj);
    }
}
