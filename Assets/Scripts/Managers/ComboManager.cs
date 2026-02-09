using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using static LogHelper;

/// <summary>
/// Sistema de combos/racha que recompensa al jugador por esquivar obstáculos consecutivamente.
/// 
/// - Cada obstáculo esquivado incrementa la racha.
/// - Cada 3 obstáculos, el multiplicador sube ×0.1 (máximo ×3.0).
/// - Un near miss da un boost extra de ×0.2 temporal.
/// - Al morir, la racha se resetea.
/// - El multiplicador se aplica al ScoreManager.
/// </summary>
public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }
    
    [Header("Combo Settings")]
    [Tooltip("Obstáculos necesarios para subir el multiplicador")]
    public int obstaclesPerMultiplierStep = 3;
    [Tooltip("Incremento de multiplicador por paso")]
    public float multiplierIncrement = 0.1f;
    [Tooltip("Multiplicador máximo")]
    public float maxMultiplier = 3.0f;
    [Tooltip("Boost de multiplicador por near miss")]
    public float nearMissBoost = 0.2f;
    [Tooltip("Duración del boost de near miss (segundos)")]
    public float nearMissBoostDuration = 3f;
    
    // Estado del combo
    private int currentStreak = 0;
    private float baseMultiplier = 1.0f;
    private float nearMissMultiplier = 0f;
    private float nearMissTimer = 0f;
    private bool isActive = false;
    private int bestStreak = 0;
    
    // HUD
    private Text multiplierText;
    private Text streakText;
    private GameObject hudRoot;
    private CanvasGroup hudCanvasGroup;
    
    // Animación
    private Coroutine popCoroutine;
    private Coroutine nearMissPopCoroutine;
    
    // Propiedades públicas
    public int CurrentStreak => currentStreak;
    public int BestStreak => bestStreak;
    public float CurrentMultiplier => Mathf.Min(baseMultiplier + nearMissMultiplier, maxMultiplier);
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        // Actualizar timer del near miss boost
        if (nearMissMultiplier > 0f)
        {
            nearMissTimer -= Time.deltaTime;
            if (nearMissTimer <= 0f)
            {
                nearMissMultiplier = 0f;
                UpdateHUD();
            }
        }
    }
    
    // =========================================================================
    // API PÚBLICA
    // =========================================================================
    
    /// <summary>
    /// Llamado cuando un obstáculo sale de pantalla sin colisionar (esquivado)
    /// </summary>
    public void OnObstacleDodged()
    {
        if (!isActive) return;
        
        currentStreak++;
        
        if (currentStreak > bestStreak)
        {
            bestStreak = currentStreak;
        }
        
        // Recalcular multiplicador base
        int steps = currentStreak / obstaclesPerMultiplierStep;
        baseMultiplier = 1.0f + steps * multiplierIncrement;
        baseMultiplier = Mathf.Min(baseMultiplier, maxMultiplier);
        
        UpdateHUD();
        
        // Animación de pop cuando sube el streak
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(PopMultiplierText());
        
        // Si justo alcanzamos un nuevo paso de multiplicador, efecto extra
        if (currentStreak % obstaclesPerMultiplierStep == 0 && currentStreak > 0)
        {
            if (popCoroutine != null) StopCoroutine(popCoroutine);
            popCoroutine = StartCoroutine(PopMultiplierTextBig());
            Log($"[ComboManager] ¡Multiplicador subió! ×{CurrentMultiplier:F1} (racha: {currentStreak})");
            
            // Notificación de milestone de combo (cada 6 obstáculos para no saturar)
            if (currentStreak % (obstaclesPerMultiplierStep * 2) == 0 && GameFeedbackManager.Instance != null)
            {
                GameFeedbackManager.Instance.OnComboMilestone(currentStreak, CurrentMultiplier);
            }
        }
    }
    
    /// <summary>
    /// Llamado cuando el jugador roza un obstáculo (near miss)
    /// </summary>
    public void OnNearMiss()
    {
        if (!isActive) return;
        
        // Aplicar boost temporal
        nearMissMultiplier = nearMissBoost;
        nearMissTimer = nearMissBoostDuration;
        
        UpdateHUD();
        
        // Animación especial de near miss
        if (nearMissPopCoroutine != null) StopCoroutine(nearMissPopCoroutine);
        nearMissPopCoroutine = StartCoroutine(NearMissEffect());
        
        Log($"[ComboManager] ¡Near miss! Boost ×{nearMissBoost} por {nearMissBoostDuration}s (total: ×{CurrentMultiplier:F1})");
    }
    
    /// <summary>
    /// Obtiene el multiplicador actual para el ScoreManager
    /// </summary>
    public float GetMultiplier()
    {
        if (!isActive) return 1f;
        return CurrentMultiplier;
    }
    
    /// <summary>
    /// Resetea el combo al inicio de una nueva partida
    /// </summary>
    public void ResetCombo()
    {
        currentStreak = 0;
        baseMultiplier = 1.0f;
        nearMissMultiplier = 0f;
        nearMissTimer = 0f;
        bestStreak = 0;
        isActive = true;
        
        UpdateHUD();
        
        // Mostrar HUD
        if (hudRoot != null)
        {
            hudRoot.SetActive(true);
        }
        
        Log("[ComboManager] Combo reseteado para nueva partida");
    }
    
    /// <summary>
    /// Detiene el combo (game over)
    /// </summary>
    public void StopCombo()
    {
        isActive = false;
        
        // Guardar mejor racha en PlayerPrefs para mostrar en GameOver
        PlayerPrefs.SetInt("LastComboStreak", currentStreak);
        PlayerPrefs.SetInt("LastBestStreak", bestStreak);
        PlayerPrefs.SetFloat("LastMaxMultiplier", baseMultiplier);
        
        Log($"[ComboManager] Combo detenido. Racha: {currentStreak}, Mejor: {bestStreak}, Max mult: ×{baseMultiplier:F1}");
    }
    
    // =========================================================================
    // HUD
    // =========================================================================
    
    /// <summary>
    /// Asigna las referencias de HUD creadas externamente
    /// </summary>
    public void SetHUD(GameObject root, Text multiplierTxt, Text streakTxt)
    {
        hudRoot = root;
        multiplierText = multiplierTxt;
        streakText = streakTxt;
        hudCanvasGroup = root?.GetComponent<CanvasGroup>();
        
        UpdateHUD();
    }
    
    /// <summary>
    /// Crea el HUD del combo dentro del canvas dado
    /// </summary>
    public void CreateHUDInCanvas(GameObject canvas)
    {
        if (canvas == null) return;
        
        // Contenedor principal — debajo del score a la izquierda
        hudRoot = new GameObject("ComboHUD");
        hudRoot.transform.SetParent(canvas.transform, false);
        
        RectTransform rootRect = hudRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f); // Esquina superior izquierda
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(20, -45);
        rootRect.sizeDelta = new Vector2(160, 55);
        
        // Fondo semi-transparente
        Image bgImage = hudRoot.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.3f);
        bgImage.raycastTarget = false;
        
        // Outline sutil
        Outline bgOutline = hudRoot.AddComponent<Outline>();
        bgOutline.effectColor = new Color(1f, 1f, 1f, 0.15f);
        bgOutline.effectDistance = new Vector2(1, 1);
        
        // CanvasGroup para fade
        hudCanvasGroup = hudRoot.AddComponent<CanvasGroup>();
        hudCanvasGroup.blocksRaycasts = false;
        
        // Texto del multiplicador (grande)
        GameObject multObj = new GameObject("MultiplierText");
        multObj.transform.SetParent(hudRoot.transform, false);
        
        multiplierText = multObj.AddComponent<Text>();
        multiplierText.text = "×1.0";
        multiplierText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        multiplierText.fontSize = 28;
        multiplierText.fontStyle = FontStyle.Bold;
        multiplierText.color = Color.white;
        multiplierText.alignment = TextAnchor.MiddleCenter;
        multiplierText.raycastTarget = false;
        
        Outline multOutline = multObj.AddComponent<Outline>();
        multOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        multOutline.effectDistance = new Vector2(1, 1);
        
        RectTransform multRect = multObj.GetComponent<RectTransform>();
        multRect.anchorMin = new Vector2(0f, 0.35f);
        multRect.anchorMax = new Vector2(1f, 1f);
        multRect.sizeDelta = Vector2.zero;
        multRect.anchoredPosition = Vector2.zero;
        
        // Texto de la racha (pequeño, debajo)
        GameObject streakObj = new GameObject("StreakText");
        streakObj.transform.SetParent(hudRoot.transform, false);
        
        streakText = streakObj.AddComponent<Text>();
        streakText.text = "";
        streakText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        streakText.fontSize = 14;
        streakText.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
        streakText.alignment = TextAnchor.MiddleCenter;
        streakText.raycastTarget = false;
        
        RectTransform streakRect = streakObj.GetComponent<RectTransform>();
        streakRect.anchorMin = new Vector2(0f, 0f);
        streakRect.anchorMax = new Vector2(1f, 0.35f);
        streakRect.sizeDelta = Vector2.zero;
        streakRect.anchoredPosition = Vector2.zero;
        
        hudRoot.SetActive(true);
        
        Log("[ComboManager] HUD creado");
    }
    
    private void UpdateHUD()
    {
        float mult = CurrentMultiplier;
        
        if (multiplierText != null)
        {
            multiplierText.text = $"×{mult:F1}";
            
            // Color según el multiplicador
            if (nearMissMultiplier > 0f)
            {
                // Near miss activo: color dorado brillante
                multiplierText.color = CosmicTheme.SoftGold;
            }
            else if (mult >= 2.0f)
            {
                // Alto: rojo/naranja
                multiplierText.color = new Color(1f, 0.5f, 0.2f, 1f);
            }
            else if (mult >= 1.5f)
            {
                // Medio: amarillo
                multiplierText.color = new Color(1f, 0.9f, 0.3f, 1f);
            }
            else if (mult > 1.0f)
            {
                // Bajo: verde claro
                multiplierText.color = new Color(0.5f, 1f, 0.5f, 1f);
            }
            else
            {
                // Base: blanco
                multiplierText.color = Color.white;
            }
        }
        
        if (streakText != null)
        {
            if (currentStreak > 0)
            {
                streakText.text = $"racha: {currentStreak}";
            }
            else
            {
                streakText.text = "";
            }
        }
    }
    
    // =========================================================================
    // ANIMACIONES
    // =========================================================================
    
    private IEnumerator PopMultiplierText()
    {
        if (multiplierText == null) yield break;
        
        float duration = 0.2f;
        float timer = 0f;
        float popScale = 1.2f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float scale = Mathf.Lerp(1f, popScale, Mathf.Sin(t * Mathf.PI));
            if (multiplierText != null)
                multiplierText.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        if (multiplierText != null)
            multiplierText.transform.localScale = Vector3.one;
    }
    
    private IEnumerator PopMultiplierTextBig()
    {
        if (multiplierText == null) yield break;
        
        float duration = 0.35f;
        float timer = 0f;
        float popScale = 1.5f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float scale = Mathf.Lerp(1f, popScale, Mathf.Sin(t * Mathf.PI));
            if (multiplierText != null)
                multiplierText.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        if (multiplierText != null)
            multiplierText.transform.localScale = Vector3.one;
    }
    
    private IEnumerator NearMissEffect()
    {
        if (multiplierText == null) yield break;
        
        // Flash dorado rápido
        Color originalColor = multiplierText.color;
        multiplierText.color = CosmicTheme.SoftGold;
        
        // Pop grande
        float duration = 0.3f;
        float timer = 0f;
        float popScale = 1.6f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            float scale = Mathf.Lerp(1f, popScale, Mathf.Sin(t * Mathf.PI));
            if (multiplierText != null)
                multiplierText.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        
        if (multiplierText != null)
        {
            multiplierText.transform.localScale = Vector3.one;
            UpdateHUD(); // Restaurar color correcto
        }
    }
}
