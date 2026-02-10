using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using static LogHelper;

/// <summary>
/// Sistema de Fever Mode — se activa cuando el multiplicador de combo llega a ×2.0+.
/// 
/// Efectos:
/// - La estela del jugador se vuelve más intensa y cambia de color.
/// - El fondo pulsa con un brillo dorado sutil.
/// - Los Stellar Shards recogidos valen el doble.
/// - HUD muestra indicador "FEVER!" parpadeante.
/// - Si pierdes la racha (muerte), el Fever se desactiva.
/// 
/// Integración:
/// - ComboManager → activa/desactiva Fever según el multiplicador.
/// - CollectibleManager → consulta GetShardValueMultiplier().
/// - StarParticleTrail → consulta GetTrailColor() y GetTrailIntensity().
/// </summary>
public class FeverModeManager : MonoBehaviour
{
    public static FeverModeManager Instance { get; private set; }
    
    // =========================================================================
    // CONFIGURACIÓN
    // =========================================================================
    
    [Header("Activation")]
    [Tooltip("Multiplicador mínimo del combo para activar Fever Mode")]
    public float activationMultiplier = 1.5f;
    [Tooltip("Multiplicador para desactivar Fever (debajo de este se desactiva)")]
    public float deactivationMultiplier = 1.3f; // Histéresis para evitar flicker
    
    [Header("Shard Bonus")]
    [Tooltip("Multiplicador de valor de shards durante Fever Mode")]
    public float shardValueMultiplier = 2f;
    
    [Header("Visual - Trail")]
    [Tooltip("Color de la estela durante Fever Mode")]
    public Color feverTrailColor = new Color(1f, 0.7f, 0.1f, 1f); // Dorado intenso
    [Tooltip("Opacidad máxima de la estela en Fever")]
    public float feverTrailOpacity = 0.6f; // 0.3 normal → 0.6 fever
    
    [Header("Visual - Background Pulse")]
    [Tooltip("Velocidad del pulso de fondo en Fever")]
    public float bgPulseSpeed = 2f;
    [Tooltip("Intensidad del pulso de fondo")]
    public float bgPulseIntensity = 0.06f;
    
    [Header("Visual - HUD")]
    [Tooltip("Tamaño del texto FEVER")]
    public int feverFontSize = 22;
    
    // =========================================================================
    // ESTADO
    // =========================================================================
    
    private bool isActive = false;       // Partida en curso
    private bool isFeverActive = false;  // Fever mode activo
    private float feverTimer = 0f;       // Tiempo en Fever (para estadísticas)
    private float totalFeverTime = 0f;   // Tiempo total en Fever en la sesión
    private int feverActivations = 0;    // Veces que se activó Fever en la sesión
    
    // UI
    private GameObject feverHUD;         // Indicador "FEVER!" en el HUD
    private Text feverText;
    private Image feverBgPulse;          // Overlay dorado de fondo
    private Canvas gameCanvas;
    
    // Trail
    private StarParticleTrail playerTrail;
    private Color originalTrailColor;
    private bool hasOriginalColor = false;
    
    // Coroutines
    private Coroutine pulseCoroutine;
    private Coroutine hudBlinkCoroutine;
    
    // Propiedades públicas
    public bool IsFeverActive => isFeverActive;
    public float TotalFeverTime => totalFeverTime;
    public int FeverActivations => feverActivations;
    
    // =========================================================================
    // API PÚBLICA
    // =========================================================================
    
    /// <summary>
    /// Multiplicador de valor de shards. Lo consulta CollectibleShard.
    /// </summary>
    public float GetShardValueMultiplier()
    {
        return isFeverActive ? shardValueMultiplier : 1f;
    }
    
    /// <summary>
    /// Color actual de la estela. Lo consulta StarParticleTrail.
    /// </summary>
    public Color GetTrailColor()
    {
        if (!isFeverActive) return Color.clear; // Señal de "usar el color normal"
        
        // Pulsar el color durante Fever
        float pulse = Mathf.Sin(Time.time * 4f) * 0.15f + 0.85f;
        return new Color(
            feverTrailColor.r * pulse,
            feverTrailColor.g * pulse,
            feverTrailColor.b * pulse,
            feverTrailColor.a
        );
    }
    
    /// <summary>
    /// Intensidad de la estela. Lo consulta StarParticleTrail.
    /// </summary>
    public float GetTrailOpacity()
    {
        return isFeverActive ? feverTrailOpacity : 0.3f; // 0.3 es el valor normal
    }
    
    /// <summary>
    /// Llamado por ComboManager cada vez que cambia el multiplicador.
    /// </summary>
    public void OnMultiplierChanged(float currentMultiplier)
    {
        if (!isActive) return;
        
        if (!isFeverActive && currentMultiplier >= activationMultiplier)
        {
            ActivateFever();
        }
        else if (isFeverActive && currentMultiplier < deactivationMultiplier)
        {
            DeactivateFever();
        }
    }
    
    /// <summary>
    /// Inicia el sistema para una nueva partida
    /// </summary>
    public void StartSystem()
    {
        isActive = true;
        isFeverActive = false;
        feverTimer = 0f;
        totalFeverTime = 0f;
        feverActivations = 0;
        
        // Buscar la estela del jugador
        FindPlayerTrail();
        
        // Encontrar canvas
        gameCanvas = FindFirstObjectByType<Canvas>();
        
        Log("[FeverModeManager] Sistema iniciado");
    }
    
    /// <summary>
    /// Detiene el sistema (game over)
    /// </summary>
    public void StopSystem()
    {
        if (isFeverActive)
        {
            DeactivateFever();
        }
        isActive = false;
        
        // Guardar stats
        PlayerPrefs.SetFloat("LastFeverTime", totalFeverTime);
        PlayerPrefs.SetInt("LastFeverActivations", feverActivations);
        
        Log($"[FeverModeManager] Sistema detenido. Activaciones: {feverActivations}, Tiempo total: {totalFeverTime:F1}s");
    }
    
    /// <summary>
    /// Resetea para nueva partida
    /// </summary>
    public void ResetSession()
    {
        StopSystem();
        feverActivations = 0;
        totalFeverTime = 0f;
        
        // Limpiar UI
        if (feverHUD != null) Destroy(feverHUD);
        if (feverBgPulse != null) Destroy(feverBgPulse.gameObject);
        feverHUD = null;
        feverBgPulse = null;
    }
    
    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    
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
        
        if (isFeverActive)
        {
            feverTimer += Time.deltaTime;
            totalFeverTime += Time.deltaTime;
            
            // Actualizar trail del jugador en tiempo real
            UpdatePlayerTrail();
        }
    }
    
    // =========================================================================
    // ACTIVACIÓN / DESACTIVACIÓN
    // =========================================================================
    
    private void ActivateFever()
    {
        if (isFeverActive) return;
        
        isFeverActive = true;
        feverActivations++;
        feverTimer = 0f;
        
        Log($"[FeverModeManager] ¡FEVER MODE ACTIVADO! (activación #{feverActivations})");
        
        // Buscar trail si no lo tenemos
        if (playerTrail == null) FindPlayerTrail();
        
        // UI: Mostrar indicador FEVER
        ShowFeverHUD();
        
        // Visual: Overlay dorado pulsante
        ShowFeverBgPulse();
        
        // Visual: Flash dorado de activación
        if (GameFeedbackManager.Instance != null)
        {
            GameFeedbackManager.Instance.ScreenFlash(new Color(1f, 0.85f, 0.2f, 0.25f), 0.3f);
            GameFeedbackManager.Instance.TriggerHaptic(GameFeedbackManager.HapticType.Medium);
        }
        
        // Trail: capturar color original y cambiar a fever
        CaptureOriginalTrailColor();
    }
    
    private void DeactivateFever()
    {
        if (!isFeverActive) return;
        
        isFeverActive = false;
        
        Log($"[FeverModeManager] Fever Mode desactivado (duró {feverTimer:F1}s)");
        
        // UI: Ocultar indicador
        HideFeverHUD();
        
        // Visual: Ocultar overlay
        HideFeverBgPulse();
        
        // Trail: restaurar color original
        RestoreOriginalTrailColor();
    }
    
    // =========================================================================
    // TRAIL DEL JUGADOR
    // =========================================================================
    
    private void FindPlayerTrail()
    {
        playerTrail = FindFirstObjectByType<StarParticleTrail>();
    }
    
    private void CaptureOriginalTrailColor()
    {
        if (hasOriginalColor) return; // Ya la tenemos
        
        if (playerTrail != null)
        {
            // El trail usa EtherealLila como color base
            originalTrailColor = CosmicTheme.EtherealLila;
            hasOriginalColor = true;
        }
    }
    
    private void RestoreOriginalTrailColor()
    {
        if (playerTrail == null) return;
        
        // Restaurar el color original a las partículas del trail
        UpdateTrailColor(originalTrailColor, 0.3f);
        hasOriginalColor = false;
    }
    
    private void UpdatePlayerTrail()
    {
        if (playerTrail == null)
        {
            FindPlayerTrail();
            if (playerTrail == null) return;
        }
        
        if (isFeverActive)
        {
            Color feverColor = GetTrailColor();
            float feverOpacity = GetTrailOpacity();
            UpdateTrailColor(feverColor, feverOpacity);
        }
    }
    
    /// <summary>
    /// Actualiza el color y opacidad de las partículas del trail.
    /// Accede a los hijos del StarParticleTrail directamente.
    /// </summary>
    private void UpdateTrailColor(Color color, float maxOpacity)
    {
        if (playerTrail == null) return;
        
        int childCount = playerTrail.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = playerTrail.transform.GetChild(i);
            if (child == null) continue;
            
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            
            // Mantener el fade natural (partículas más lejanas más tenues)
            float distanceFade = 1f - (float)i / childCount;
            float alpha = distanceFade * maxOpacity;
            
            sr.color = new Color(color.r, color.g, color.b, alpha);
        }
    }
    
    // =========================================================================
    // UI — INDICADOR FEVER
    // =========================================================================
    
    private void ShowFeverHUD()
    {
        if (gameCanvas == null)
        {
            gameCanvas = FindFirstObjectByType<Canvas>();
            if (gameCanvas == null) return;
        }
        
        if (feverHUD == null)
        {
            feverHUD = new GameObject("FeverHUD");
            feverHUD.transform.SetParent(gameCanvas.transform, false);
            
            RectTransform rt = feverHUD.AddComponent<RectTransform>();
            // Posicionar debajo del combo HUD (izquierda, debajo)
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(20, -105); // Debajo del combo HUD
            rt.sizeDelta = new Vector2(100, 30);
            
            // Fondo dorado semi-transparente
            Image bg = feverHUD.AddComponent<Image>();
            bg.color = new Color(1f, 0.7f, 0f, 0.4f);
            bg.raycastTarget = false;
            
            // Outline dorado
            Outline outline = feverHUD.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.2f, 0.6f);
            outline.effectDistance = new Vector2(1, 1);
            
            // Texto "FEVER!"
            GameObject textObj = new GameObject("FeverText");
            textObj.transform.SetParent(feverHUD.transform, false);
            
            feverText = textObj.AddComponent<Text>();
            feverText.text = "FEVER!";
            feverText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            feverText.fontSize = feverFontSize;
            feverText.fontStyle = FontStyle.Bold;
            feverText.color = new Color(1f, 0.95f, 0.6f, 1f);
            feverText.alignment = TextAnchor.MiddleCenter;
            feverText.raycastTarget = false;
            
            // Outline para legibilidad
            Outline textOutline = textObj.AddComponent<Outline>();
            textOutline.effectColor = new Color(0.5f, 0.3f, 0f, 0.8f);
            textOutline.effectDistance = new Vector2(1, 1);
            
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.anchoredPosition = Vector2.zero;
        }
        
        feverHUD.SetActive(true);
        
        // Iniciar parpadeo
        if (hudBlinkCoroutine != null) StopCoroutine(hudBlinkCoroutine);
        hudBlinkCoroutine = StartCoroutine(BlinkFeverHUD());
    }
    
    private IEnumerator BlinkFeverHUD()
    {
        while (isFeverActive && feverHUD != null && feverText != null)
        {
            float t = Time.time;
            
            // Parpadeo rápido del texto
            float blink = Mathf.Sin(t * 6f) * 0.5f + 0.5f;
            feverText.color = Color.Lerp(
                new Color(1f, 0.95f, 0.6f, 0.5f),
                new Color(1f, 0.95f, 0.6f, 1f),
                blink
            );
            
            // Pulso de escala sutil
            float pulse = 1f + Mathf.Sin(t * 3f) * 0.06f;
            feverHUD.transform.localScale = Vector3.one * pulse;
            
            // Cambiar fondo entre dorado intenso y atenuado
            Image bg = feverHUD.GetComponent<Image>();
            if (bg != null)
            {
                float bgBlink = Mathf.Sin(t * 4f) * 0.5f + 0.5f;
                bg.color = Color.Lerp(
                    new Color(1f, 0.7f, 0f, 0.25f),
                    new Color(1f, 0.7f, 0f, 0.5f),
                    bgBlink
                );
            }
            
            yield return null;
        }
    }
    
    private void HideFeverHUD()
    {
        if (hudBlinkCoroutine != null)
        {
            StopCoroutine(hudBlinkCoroutine);
            hudBlinkCoroutine = null;
        }
        
        if (feverHUD != null)
        {
            feverHUD.SetActive(false);
        }
    }
    
    // =========================================================================
    // UI — FONDO DORADO PULSANTE
    // =========================================================================
    
    private void ShowFeverBgPulse()
    {
        if (gameCanvas == null) return;
        
        if (feverBgPulse == null)
        {
            GameObject pulseObj = new GameObject("FeverBgPulse");
            pulseObj.transform.SetParent(gameCanvas.transform, false);
            // Ponerlo detrás de otros elementos de UI (pero visible)
            pulseObj.transform.SetSiblingIndex(0);
            
            feverBgPulse = pulseObj.AddComponent<Image>();
            feverBgPulse.color = Color.clear;
            feverBgPulse.raycastTarget = false;
            
            RectTransform rt = pulseObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
        
        feverBgPulse.gameObject.SetActive(true);
        
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseFeverBg());
    }
    
    private IEnumerator PulseFeverBg()
    {
        while (isFeverActive && feverBgPulse != null)
        {
            float t = Time.time;
            
            // Pulso dorado muy sutil
            float pulse = Mathf.Sin(t * bgPulseSpeed) * 0.5f + 0.5f;
            float alpha = pulse * bgPulseIntensity;
            
            feverBgPulse.color = new Color(1f, 0.85f, 0.2f, alpha);
            
            yield return null;
        }
    }
    
    private void HideFeverBgPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        if (feverBgPulse != null)
        {
            feverBgPulse.color = Color.clear;
            feverBgPulse.gameObject.SetActive(false);
        }
    }
    
    // =========================================================================
    // CLEANUP
    // =========================================================================
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
