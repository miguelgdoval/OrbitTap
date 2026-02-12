using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using static LogHelper;

/// <summary>
/// Sistema de "Zona de Peligro" — oleadas especiales que rompen la monotonía.
/// 
/// Funcionamiento:
/// - Se activa periódicamente (cada ~45-60s) a partir de cierta puntuación (≥25 pts).
/// - Al activarse: aviso "⚠ PELIGRO", fondo rojo pulsante, 3-4 obstáculos rápidos.
/// - Duración de la oleada: ~6 segundos.
/// - Sobrevivir la oleada da un bonus de puntos y Stellar Shards.
/// - Integra con ObstacleManager para forzar spawns rápidos.
/// </summary>
public class DangerZoneManager : MonoBehaviour
{
    public static DangerZoneManager Instance { get; private set; }
    
    // =========================================================================
    // CONFIGURACIÓN
    // =========================================================================
    
    [Header("Trigger Settings")]
    [Tooltip("Puntuación mínima para que puedan aparecer zonas de peligro")]
    public int minScoreToActivate = 10;
    [Tooltip("Tiempo mínimo entre zonas de peligro (segundos)")]
    public float minInterval = 25f;
    [Tooltip("Tiempo máximo entre zonas de peligro (segundos)")]
    public float maxInterval = 35f;
    [Tooltip("Delay inicial antes de que puedan empezar (segundos)")]
    public float initialDelay = 15f;
    
    [Header("Wave Settings")]
    [Tooltip("Duración de la oleada de peligro (segundos)")]
    public float waveDuration = 6f;
    [Tooltip("Número de obstáculos rápidos en la oleada")]
    public int waveObstacleCount = 4;
    [Tooltip("Intervalo entre obstáculos de la oleada (segundos)")]
    public float waveSpawnInterval = 1.2f;
    [Tooltip("Multiplicador de velocidad de los obstáculos durante la oleada")]
    public float waveSpeedMultiplier = 1.4f;
    
    [Header("Warning")]
    [Tooltip("Duración del aviso antes de la oleada (segundos)")]
    public float warningDuration = 2f;
    
    [Header("Rewards")]
    [Tooltip("Bonus de puntos al sobrevivir la oleada")]
    public int survivalBonusPoints = 5;
    [Tooltip("Bonus de Stellar Shards al sobrevivir la oleada")]
    public int survivalBonusShards = 3;
    
    // =========================================================================
    // ESTADO
    // =========================================================================
    
    private bool isActive = false;           // Sistema activo (partida en curso)
    private bool isInDangerZone = false;     // Oleada en curso
    private bool isWarning = false;          // Aviso previo en curso
    private float timeSinceLastWave = 0f;
    private float nextWaveTime;
    private int wavesCompleted = 0;
    private int wavesSurvived = 0;
    
    // UI
    private GameObject warningPanel;         // Panel de aviso "⚠ PELIGRO"
    private Text warningText;
    private Image dangerOverlay;             // Overlay rojo pulsante
    private Image dangerVignette;            // Viñeta roja en los bordes
    private Canvas gameCanvas;
    
    // Coroutines
    private Coroutine waveCoroutine;
    private Coroutine pulseCoroutine;
    
    // Propiedades públicas
    public bool IsInDangerZone => isInDangerZone;
    public bool IsWarning => isWarning;
    public int WavesCompleted => wavesCompleted;
    public int WavesSurvived => wavesSurvived;
    
    /// <summary>
    /// Multiplicador de velocidad de obstáculos durante la oleada.
    /// Lo consulta ObstacleMover.
    /// </summary>
    public float GetDangerSpeedMultiplier()
    {
        return isInDangerZone ? waveSpeedMultiplier : 1f;
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
        if (!isActive || isInDangerZone || isWarning) return;
        
        timeSinceLastWave += Time.deltaTime;
        
        if (timeSinceLastWave >= nextWaveTime)
        {
            // Verificar que el score sea suficiente
            ScoreManager scoreManager = FindFirstObjectByType<ScoreManager>();
            if (scoreManager != null && scoreManager.GetCurrentScore() >= minScoreToActivate)
            {
                // ¡Activar oleada!
                StartDangerWave();
            }
            else
            {
                // Aún no tiene suficiente score, postponer
                timeSinceLastWave = nextWaveTime - 5f; // Reintentar en 5s
            }
        }
    }
    
    // =========================================================================
    // API PÚBLICA
    // =========================================================================
    
    /// <summary>
    /// Inicia el sistema para una nueva partida
    /// </summary>
    public void StartSystem()
    {
        isActive = true;
        isInDangerZone = false;
        isWarning = false;
        wavesCompleted = 0;
        wavesSurvived = 0;
        timeSinceLastWave = -initialDelay; // Delay negativo = esperar
        nextWaveTime = Random.Range(minInterval, maxInterval);
        
        // Encontrar o crear UI
        EnsureUI();
        
        Log("[DangerZoneManager] Sistema iniciado");
    }
    
    /// <summary>
    /// Detiene el sistema (game over)
    /// </summary>
    public void StopSystem()
    {
        isActive = false;
        isInDangerZone = false;
        isWarning = false;
        
        if (waveCoroutine != null) StopCoroutine(waveCoroutine);
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        
        // Ocultar UI
        HideDangerUI();
        
        // Guardar stats en PlayerPrefs para Game Over
        PlayerPrefs.SetInt("LastDangerWavesCompleted", wavesCompleted);
        PlayerPrefs.SetInt("LastDangerWavesSurvived", wavesSurvived);
        
        Log($"[DangerZoneManager] Sistema detenido. Oleadas: {wavesCompleted}, Sobrevividas: {wavesSurvived}");
    }
    
    /// <summary>
    /// Resetea para una nueva partida
    /// </summary>
    public void ResetSession()
    {
        StopSystem();
        wavesCompleted = 0;
        wavesSurvived = 0;
        
        // Destruir UI previa si existe
        if (warningPanel != null) Destroy(warningPanel);
        if (dangerOverlay != null) Destroy(dangerOverlay.gameObject);
        if (dangerVignette != null) Destroy(dangerVignette.gameObject);
        warningPanel = null;
        dangerOverlay = null;
        dangerVignette = null;
    }
    
    /// <summary>
    /// Llamado cuando el jugador muere DURANTE una oleada.
    /// La oleada se considera no sobrevivida.
    /// </summary>
    public void OnPlayerDeath()
    {
        if (isInDangerZone)
        {
            Log("[DangerZoneManager] Jugador murió durante oleada de peligro");
        }
        StopSystem();
    }
    
    // =========================================================================
    // OLEADA
    // =========================================================================
    
    private void StartDangerWave()
    {
        if (isInDangerZone || isWarning) return;
        
        waveCoroutine = StartCoroutine(DangerWaveSequence());
    }
    
    private IEnumerator DangerWaveSequence()
    {
        // ===== FASE 1: AVISO =====
        isWarning = true;
        Log("[DangerZoneManager] ⚠ AVISO de peligro");
        
        ShowWarning();
        
        // Vibración de aviso
        if (GameFeedbackManager.Instance != null)
        {
            GameFeedbackManager.Instance.TriggerHaptic(GameFeedbackManager.HapticType.Medium);
        }
        
        yield return new WaitForSeconds(warningDuration);
        
        isWarning = false;
        HideWarning();
        
        // ===== FASE 2: OLEADA =====
        isInDangerZone = true;
        wavesCompleted++;
        Log($"[DangerZoneManager] ¡OLEADA DE PELIGRO #{wavesCompleted}!");
        
        // Iniciar overlay rojo pulsante
        ShowDangerOverlay();
        pulseCoroutine = StartCoroutine(PulseDangerOverlay());
        
        // Vibración fuerte al empezar
        if (GameFeedbackManager.Instance != null)
        {
            GameFeedbackManager.Instance.TriggerHaptic(GameFeedbackManager.HapticType.Heavy);
        }
        
        // Pausar brevemente el spawn normal del ObstacleManager (controlamos los spawns nosotros)
        // Spawnear obstáculos rápidos en ráfaga
        int obstaclesSpawned = 0;
        float waveTimer = 0f;
        float spawnAccumulator = 0f;
        
        while (waveTimer < waveDuration && isInDangerZone && isActive)
        {
            waveTimer += Time.deltaTime;
            spawnAccumulator += Time.deltaTime;
            
            if (spawnAccumulator >= waveSpawnInterval && obstaclesSpawned < waveObstacleCount)
            {
                // Forzar spawn a través del ObstacleManager
                if (ObstacleManager.Instance != null)
                {
                    ObstacleManager.Instance.ForceSpawnObstacle();
                    obstaclesSpawned++;
                    Log($"[DangerZoneManager] Obstáculo de oleada #{obstaclesSpawned} spawneado");
                }
                spawnAccumulator = 0f;
            }
            
            yield return null;
        }
        
        // ===== FASE 3: COMPLETADA =====
        bool survived = isActive && isInDangerZone; // Si sigue activo = sobrevivió
        
        isInDangerZone = false;
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        HideDangerOverlay();
        
        if (survived)
        {
            wavesSurvived++;
            Log($"[DangerZoneManager] ¡Oleada sobrevivida! Bonus: +{survivalBonusPoints} pts, +{survivalBonusShards}⭐");
            
            // Dar bonus
            AwardSurvivalBonus();
            
            // Notificación de éxito
            ShowSurvivalNotification();
            
            // Vibración de recompensa
            if (GameFeedbackManager.Instance != null)
            {
                GameFeedbackManager.Instance.TriggerHaptic(GameFeedbackManager.HapticType.Medium);
            }
        }
        
        // Preparar siguiente oleada
        timeSinceLastWave = 0f;
        nextWaveTime = Random.Range(minInterval, maxInterval);
        
        // Escalar dificultad: cada oleada sucesiva tiene más obstáculos (hasta un máximo de 6)
        if (wavesCompleted >= 3)
        {
            waveObstacleCount = Mathf.Min(waveObstacleCount + 1, 6);
            waveSpawnInterval = Mathf.Max(waveSpawnInterval - 0.1f, 0.8f);
        }
    }
    
    // =========================================================================
    // RECOMPENSAS
    // =========================================================================
    
    private void AwardSurvivalBonus()
    {
        // Bonus de puntos
        if (survivalBonusPoints > 0)
        {
            ScoreManager scoreManager = FindFirstObjectByType<ScoreManager>();
            if (scoreManager != null)
            {
                scoreManager.AddBonusPoints(survivalBonusPoints);
            }
        }

        // Bonus de shards
        if (survivalBonusShards > 0 && CollectibleManager.Instance != null)
        {
            // Otorgar shards directamente al CurrencyManager
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddStellarShards(survivalBonusShards);
            }
        }
    }
    
    // =========================================================================
    // UI — AVISO
    // =========================================================================
    
    private void EnsureUI()
    {
        gameCanvas = FindFirstObjectByType<Canvas>();
    }
    
    private void ShowWarning()
    {
        if (gameCanvas == null)
        {
            gameCanvas = FindFirstObjectByType<Canvas>();
            if (gameCanvas == null) return;
        }
        
        // Crear panel de aviso centrado
        if (warningPanel == null)
        {
            warningPanel = new GameObject("DangerWarning");
            warningPanel.transform.SetParent(gameCanvas.transform, false);
            
            RectTransform rt = warningPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 60);
            rt.sizeDelta = new Vector2(400, 80);
            
            // Fondo semi-transparente rojo oscuro
            Image bg = warningPanel.AddComponent<Image>();
            bg.color = new Color(0.6f, 0f, 0f, 0.5f);
            bg.raycastTarget = false;
            
            // Outline rojo
            Outline outline = warningPanel.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.2f, 0.2f, 0.7f);
            outline.effectDistance = new Vector2(2, 2);
            
            // Texto
            GameObject textObj = new GameObject("WarningText");
            textObj.transform.SetParent(warningPanel.transform, false);
            
            warningText = textObj.AddComponent<Text>();
            warningText.text = "⚠ PELIGRO ⚠";
            warningText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            warningText.fontSize = 36;
            warningText.fontStyle = FontStyle.Bold;
            warningText.color = new Color(1f, 0.3f, 0.2f, 1f);
            warningText.alignment = TextAnchor.MiddleCenter;
            warningText.raycastTarget = false;
            warningText.horizontalOverflow = HorizontalWrapMode.Overflow;
            
            // Outline para legibilidad
            Outline textOutline = textObj.AddComponent<Outline>();
            textOutline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            textOutline.effectDistance = new Vector2(2, 2);
            
            // Shadow
            Shadow textShadow = textObj.AddComponent<Shadow>();
            textShadow.effectColor = new Color(1f, 0f, 0f, 0.3f);
            textShadow.effectDistance = new Vector2(0, 0);
            
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.anchoredPosition = Vector2.zero;
        }
        
        warningPanel.SetActive(true);
        StartCoroutine(AnimateWarning());
    }
    
    private IEnumerator AnimateWarning()
    {
        if (warningPanel == null || warningText == null) yield break;
        
        float timer = 0f;
        Vector3 baseScale = Vector3.one;
        
        // Aparecer con pop
        warningPanel.transform.localScale = Vector3.one * 2f;
        
        while (timer < warningDuration && warningPanel != null)
        {
            timer += Time.deltaTime;
            float t = timer / warningDuration;
            
            if (t < 0.15f)
            {
                // Pop de entrada
                float popT = t / 0.15f;
                float scale = Mathf.Lerp(2f, 1f, popT);
                warningPanel.transform.localScale = Vector3.one * scale;
            }
            else
            {
                // Pulso constante
                float pulseT = (t - 0.15f) / 0.85f;
                float pulse = 1f + Mathf.Sin(pulseT * Mathf.PI * 6f) * 0.08f;
                warningPanel.transform.localScale = Vector3.one * pulse;
                
                // Parpadeo del texto
                float blink = Mathf.Sin(timer * 8f) * 0.5f + 0.5f;
                if (warningText != null)
                {
                    warningText.color = Color.Lerp(
                        new Color(1f, 0.3f, 0.2f, 0.6f),
                        new Color(1f, 0.3f, 0.2f, 1f),
                        blink
                    );
                }
            }
            
            yield return null;
        }
    }
    
    private void HideWarning()
    {
        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }
    }
    
    // =========================================================================
    // UI — OVERLAY ROJO PULSANTE
    // =========================================================================
    
    private void ShowDangerOverlay()
    {
        if (gameCanvas == null)
        {
            gameCanvas = FindFirstObjectByType<Canvas>();
            if (gameCanvas == null) return;
        }
        
        // Crear viñeta roja en los bordes (más sutil que un overlay completo)
        if (dangerVignette == null)
        {
            GameObject vignetteObj = new GameObject("DangerVignette");
            vignetteObj.transform.SetParent(gameCanvas.transform, false);
            vignetteObj.transform.SetAsLastSibling();
            
            dangerVignette = vignetteObj.AddComponent<Image>();
            dangerVignette.color = Color.clear;
            dangerVignette.raycastTarget = false;
            
            RectTransform rt = vignetteObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
        
        // Crear overlay tenue central
        if (dangerOverlay == null)
        {
            GameObject overlayObj = new GameObject("DangerOverlay");
            overlayObj.transform.SetParent(gameCanvas.transform, false);
            // Poner detrás de la viñeta pero encima de otros elementos
            overlayObj.transform.SetSiblingIndex(gameCanvas.transform.childCount - 2);
            
            dangerOverlay = overlayObj.AddComponent<Image>();
            dangerOverlay.color = Color.clear;
            dangerOverlay.raycastTarget = false;
            
            RectTransform rt = overlayObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
        
        dangerVignette.gameObject.SetActive(true);
        dangerOverlay.gameObject.SetActive(true);
    }
    
    private IEnumerator PulseDangerOverlay()
    {
        float timer = 0f;
        
        while (isInDangerZone && dangerOverlay != null && dangerVignette != null)
        {
            timer += Time.deltaTime;
            
            // Pulso sinusoidal para el overlay rojo
            float pulse = Mathf.Sin(timer * 4f) * 0.5f + 0.5f; // 0 a 1, ~2 pulsos/segundo
            
            // Overlay central: rojo muy tenue pulsante
            float overlayAlpha = Mathf.Lerp(0.03f, 0.12f, pulse);
            dangerOverlay.color = new Color(1f, 0.1f, 0.05f, overlayAlpha);
            
            // Viñeta de bordes: más intensa en los bordes
            float vignetteAlpha = Mathf.Lerp(0.08f, 0.2f, pulse);
            dangerVignette.color = new Color(0.8f, 0f, 0f, vignetteAlpha);
            
            yield return null;
        }
    }
    
    private void HideDangerOverlay()
    {
        if (dangerOverlay != null)
        {
            dangerOverlay.color = Color.clear;
            dangerOverlay.gameObject.SetActive(false);
        }
        if (dangerVignette != null)
        {
            dangerVignette.color = Color.clear;
            dangerVignette.gameObject.SetActive(false);
        }
    }
    
    private void HideDangerUI()
    {
        HideWarning();
        HideDangerOverlay();
    }
    
    // =========================================================================
    // UI — NOTIFICACIÓN DE SUPERVIVENCIA
    // =========================================================================
    
    private void ShowSurvivalNotification()
    {
        if (GameFeedbackManager.Instance != null)
        {
            // Flash verde de éxito
            GameFeedbackManager.Instance.ScreenFlash(new Color(0.2f, 1f, 0.3f, 0.2f), 0.3f);
        }
        
        // Mostrar texto de supervivencia
        if (gameCanvas == null) return;
        
        GameObject notifObj = new GameObject("SurvivalNotification");
        notifObj.transform.SetParent(gameCanvas.transform, false);
        
        Text notifText = notifObj.AddComponent<Text>();
        notifText.text = $"¡SOBREVIVISTE!\n+{survivalBonusShards}⭐";
        notifText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        notifText.fontSize = 32;
        notifText.fontStyle = FontStyle.Bold;
        notifText.color = new Color(0.3f, 1f, 0.4f, 1f);
        notifText.alignment = TextAnchor.MiddleCenter;
        notifText.raycastTarget = false;
        notifText.horizontalOverflow = HorizontalWrapMode.Overflow;
        notifText.verticalOverflow = VerticalWrapMode.Overflow;
        
        // Outline
        Outline outline = notifObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);
        
        // Shadow
        Shadow shadow = notifObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0.5f, 0f, 0.4f);
        shadow.effectDistance = new Vector2(0, -2);
        
        // Posicionar centrado
        RectTransform rt = notifObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 60);
        rt.sizeDelta = new Vector2(400, 100);
        
        StartCoroutine(AnimateSurvivalNotification(notifObj, notifText));
    }
    
    private IEnumerator AnimateSurvivalNotification(GameObject obj, Text text)
    {
        float duration = 2f;
        float timer = 0f;
        Color startColor = text.color;
        
        // Pop de entrada
        obj.transform.localScale = Vector3.one * 2.5f;
        
        while (timer < duration && obj != null)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            if (t < 0.15f)
            {
                // Pop de entrada
                float popT = t / 0.15f;
                float scale = Mathf.Lerp(2.5f, 1f, Mathf.SmoothStep(0f, 1f, popT));
                obj.transform.localScale = Vector3.one * scale;
                text.color = new Color(startColor.r, startColor.g, startColor.b, popT);
            }
            else if (t < 0.6f)
            {
                // Mantener con pulso sutil
                float pulse = 1f + Mathf.Sin((t - 0.15f) * Mathf.PI * 4f) * 0.04f;
                obj.transform.localScale = Vector3.one * pulse;
                text.color = startColor;
            }
            else
            {
                // Desvanecer y subir
                float fadeT = (t - 0.6f) / 0.4f;
                text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeT);
                float scale = Mathf.Lerp(1f, 0.7f, fadeT);
                obj.transform.localScale = Vector3.one * scale;
                
                RectTransform rt = obj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition += Vector2.up * Time.deltaTime * 40f;
                }
            }
            
            yield return null;
        }
        
        if (obj != null) Destroy(obj);
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
