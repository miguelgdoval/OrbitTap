using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using static LogHelper;

/// <summary>
/// Manager central de feedback visual y háptico del juego.
/// 
/// Gestiona:
/// - Screen flash (near miss, shield block, milestone)
/// - Screen shake (death, shield block, combo milestone)
/// - Floating notifications ("¡CERCA!", "¡50 pts!", "¡Nuevo récord!")
/// - Haptic feedback (vibración en eventos clave)
/// - Score milestones (10, 25, 50, 75, 100, 150, 200, 300, 500)
/// </summary>
public class GameFeedbackManager : MonoBehaviour
{
    public static GameFeedbackManager Instance { get; private set; }
    
    // =========================================================================
    // CONFIGURACIÓN
    // =========================================================================
    
    [Header("Screen Flash")]
    [Tooltip("Duración del flash de near miss")]
    public float nearMissFlashDuration = 0.15f;
    
    [Header("Screen Shake")]
    [Tooltip("Intensidad del shake al morir")]
    public float deathShakeIntensity = 0.4f;
    [Tooltip("Duración del shake al morir")]
    public float deathShakeDuration = 0.3f;
    [Tooltip("Intensidad del shake al bloquear con escudo")]
    public float shieldShakeIntensity = 0.2f;
    [Tooltip("Duración del shake del escudo")]
    public float shieldShakeDuration = 0.15f;
    
    [Header("Milestones")]
    [Tooltip("Puntuaciones que activan notificación")]
    public int[] scoreMilestones = { 10, 25, 50, 75, 100, 150, 200, 300, 500 };
    
    [Header("Haptic")]
    [Tooltip("¿Está habilitada la vibración?")]
    public bool hapticsEnabled = true;
    
    // =========================================================================
    // ESTADO INTERNO
    // =========================================================================
    
    // Flash overlay
    private Image flashOverlay;
    private Coroutine flashCoroutine;
    
    // Shake
    private Camera mainCamera;
    private Vector3 cameraOriginalPos;
    private Coroutine shakeCoroutine;
    
    // Notifications
    private List<GameObject> activeNotifications = new List<GameObject>();
    private Canvas gameCanvas;
    
    // Milestones
    private int lastMilestoneReached = 0;
    private bool hasNotifiedNewRecord = false;
    private int sessionHighScore = 0;
    
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
    
    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        if (mainCamera != null)
        {
            cameraOriginalPos = mainCamera.transform.position;
        }
        
        // Cargar preferencia de vibración
        LoadHapticPreference();
        
        // Crear overlay de flash
        CreateFlashOverlay();
        
        // Suscribirse a eventos de misión completada
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionCompleted += HandleMissionCompleted;
        }
    }
    
    private void OnDestroy()
    {
        // Desuscribirse para evitar leaks
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionCompleted -= HandleMissionCompleted;
        }
    }
    
    private void HandleMissionCompleted(MissionData mission)
    {
        if (mission != null)
        {
            OnMissionCompleted(mission.title);
        }
    }
    
    // =========================================================================
    // API PÚBLICA — NEAR MISS
    // =========================================================================
    
    /// <summary>
    /// Feedback completo de near miss: flash + texto + vibración
    /// </summary>
    public void OnNearMiss(Vector3 playerPosition)
    {
        // Flash amarillo/dorado rápido
        ScreenFlash(new Color(1f, 0.85f, 0.2f, 0.25f), nearMissFlashDuration);
        
        // Texto flotante "¡CERCA!"
        ShowFloatingNotification("¡CERCA!", new Color(1f, 0.85f, 0.2f, 1f), 24, playerPosition + Vector3.up * 0.8f);
        
        // Vibración suave
        TriggerHaptic(HapticType.Light);
    }
    
    // =========================================================================
    // API PÚBLICA — COLECCIONES
    // =========================================================================
    
    /// <summary>
    /// Vibración al recoger un shard
    /// </summary>
    public void OnShardCollected(int value)
    {
        if (value >= 10)
            TriggerHaptic(HapticType.Medium);
        else
            TriggerHaptic(HapticType.Light);
    }
    
    /// <summary>
    /// Vibración al recoger un power-up
    /// </summary>
    public void OnPowerUpCollected()
    {
        TriggerHaptic(HapticType.Medium);
    }
    
    // =========================================================================
    // API PÚBLICA — ESCUDO
    // =========================================================================
    
    /// <summary>
    /// Feedback al bloquear con escudo: shake + flash + vibración
    /// </summary>
    public void OnShieldBlock(Vector3 impactPosition)
    {
        // Flash azul
        ScreenFlash(new Color(0.3f, 0.7f, 1f, 0.3f), 0.12f);
        
        // Shake medio
        ScreenShake(shieldShakeIntensity, shieldShakeDuration);
        
        // Vibración fuerte
        TriggerHaptic(HapticType.Heavy);
    }
    
    // =========================================================================
    // API PÚBLICA — MUERTE
    // =========================================================================
    
    /// <summary>
    /// Feedback al morir: shake fuerte + flash rojo + vibración larga
    /// </summary>
    public void OnPlayerDeath()
    {
        // Flash rojo intenso
        ScreenFlash(new Color(1f, 0.15f, 0.1f, 0.4f), 0.25f);
        
        // Shake fuerte
        ScreenShake(deathShakeIntensity, deathShakeDuration);
        
        // Vibración larga y fuerte
        TriggerHaptic(HapticType.Death);
    }
    
    // =========================================================================
    // API PÚBLICA — MILESTONES
    // =========================================================================
    
    /// <summary>
    /// Llamar cada frame con el score actual para verificar milestones.
    /// Optimizado: solo compara contra el siguiente milestone.
    /// </summary>
    public void CheckScoreMilestones(int currentScore, int highScore)
    {
        // Guardar high score de la sesión
        if (sessionHighScore == 0)
        {
            sessionHighScore = highScore;
        }
        
        // Verificar nuevo récord (solo una vez)
        if (!hasNotifiedNewRecord && currentScore > sessionHighScore && sessionHighScore > 0)
        {
            hasNotifiedNewRecord = true;
            ShowMilestoneNotification("¡NUEVO RÉCORD!", new Color(1f, 0.85f, 0.2f, 1f), 32);
            TriggerHaptic(HapticType.Medium);
            ScreenFlash(new Color(1f, 0.85f, 0.2f, 0.2f), 0.3f);
            Log($"[GameFeedbackManager] ¡Nuevo récord! {currentScore} > {sessionHighScore}");
        }
        
        // Verificar milestones de score
        for (int i = 0; i < scoreMilestones.Length; i++)
        {
            int milestone = scoreMilestones[i];
            if (milestone > lastMilestoneReached && currentScore >= milestone)
            {
                lastMilestoneReached = milestone;
                ShowMilestoneNotification($"¡{milestone} pts!", GetMilestoneColor(milestone), 28);
                TriggerHaptic(HapticType.Light);
                Log($"[GameFeedbackManager] Milestone alcanzado: {milestone} pts");
                break; // Solo un milestone por frame
            }
        }
    }
    
    /// <summary>
    /// Notificación de racha de combo (llamar desde ComboManager)
    /// </summary>
    public void OnComboMilestone(int streak, float multiplier)
    {
        ShowMilestoneNotification($"¡Racha ×{streak}!", new Color(0.5f, 1f, 0.5f, 1f), 26);
        TriggerHaptic(HapticType.Light);
    }
    
    /// <summary>
    /// Notificación de misión completada durante la partida
    /// </summary>
    public void OnMissionCompleted(string missionTitle)
    {
        ShowMilestoneNotification($"✓ {missionTitle}", new Color(0.4f, 0.9f, 1f, 1f), 22);
        TriggerHaptic(HapticType.Medium);
        ScreenFlash(new Color(0.4f, 0.9f, 1f, 0.15f), 0.2f);
    }
    
    /// <summary>
    /// Resetear milestones para nueva partida
    /// </summary>
    public void ResetMilestones()
    {
        lastMilestoneReached = 0;
        hasNotifiedNewRecord = false;
        sessionHighScore = 0;
        
        // Limpiar notificaciones activas
        foreach (var notif in activeNotifications)
        {
            if (notif != null) Destroy(notif);
        }
        activeNotifications.Clear();
    }
    
    // =========================================================================
    // SCREEN FLASH
    // =========================================================================
    
    private void CreateFlashOverlay()
    {
        // Buscar canvas del juego
        gameCanvas = FindFirstObjectByType<Canvas>();
        if (gameCanvas == null) return;
        
        // Crear overlay
        GameObject overlayObj = new GameObject("FlashOverlay");
        overlayObj.transform.SetParent(gameCanvas.transform, false);
        overlayObj.transform.SetAsLastSibling(); // Siempre encima
        
        flashOverlay = overlayObj.AddComponent<Image>();
        flashOverlay.color = Color.clear;
        flashOverlay.raycastTarget = false;
        
        RectTransform rt = overlayObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
    
    /// <summary>
    /// Flash de pantalla con color y duración configurables
    /// </summary>
    public void ScreenFlash(Color color, float duration)
    {
        if (flashOverlay == null)
        {
            CreateFlashOverlay();
            if (flashOverlay == null) return;
        }
        
        // Asegurar que el overlay está al frente
        flashOverlay.transform.SetAsLastSibling();
        
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
    }
    
    private IEnumerator FlashCoroutine(Color color, float duration)
    {
        flashOverlay.color = color;
        
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime; // unscaled para funcionar durante pause
            float t = timer / duration;
            flashOverlay.color = Color.Lerp(color, Color.clear, t);
            yield return null;
        }
        
        flashOverlay.color = Color.clear;
    }
    
    // =========================================================================
    // SCREEN SHAKE
    // =========================================================================
    
    /// <summary>
    /// Screen shake con intensidad y duración configurables
    /// </summary>
    public void ScreenShake(float intensity, float duration)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
            cameraOriginalPos = mainCamera.transform.position;
        }
        
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
    }
    
    private IEnumerator ShakeCoroutine(float intensity, float duration)
    {
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = 1f - (timer / duration); // Decae linealmente
            
            float offsetX = Random.Range(-1f, 1f) * intensity * t;
            float offsetY = Random.Range(-1f, 1f) * intensity * t;
            
            if (mainCamera != null)
            {
                mainCamera.transform.position = cameraOriginalPos + new Vector3(offsetX, offsetY, 0f);
            }
            
            yield return null;
        }
        
        // Restaurar posición
        if (mainCamera != null)
        {
            mainCamera.transform.position = cameraOriginalPos;
        }
    }
    
    // =========================================================================
    // FLOATING NOTIFICATIONS
    // =========================================================================
    
    /// <summary>
    /// Muestra un texto flotante en una posición del mundo
    /// </summary>
    private void ShowFloatingNotification(string text, Color color, int fontSize, Vector3 worldPosition)
    {
        if (gameCanvas == null)
        {
            gameCanvas = FindFirstObjectByType<Canvas>();
            if (gameCanvas == null) return;
        }
        
        GameObject notifObj = new GameObject("FloatingNotification");
        notifObj.transform.SetParent(gameCanvas.transform, false);
        
        Text notifText = notifObj.AddComponent<Text>();
        notifText.text = text;
        notifText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        notifText.fontSize = fontSize;
        notifText.fontStyle = FontStyle.Bold;
        notifText.color = color;
        notifText.alignment = TextAnchor.MiddleCenter;
        notifText.raycastTarget = false;
        notifText.horizontalOverflow = HorizontalWrapMode.Overflow;
        
        // Outline para legibilidad
        Outline outline = notifObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(1.5f, 1.5f);
        
        // Posicionar en pantalla desde posición del mundo
        RectTransform rt = notifObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(300, 50);
        
        if (mainCamera != null)
        {
            Vector3 screenPos = mainCamera.WorldToViewportPoint(worldPosition);
            rt.anchorMin = new Vector2(screenPos.x, screenPos.y);
            rt.anchorMax = new Vector2(screenPos.x, screenPos.y);
            rt.anchoredPosition = Vector2.zero;
        }
        
        activeNotifications.Add(notifObj);
        
        StartCoroutine(AnimateFloatingNotification(notifObj, notifText, rt));
    }
    
    /// <summary>
    /// Muestra una notificación de milestone centrada en la parte superior de la pantalla
    /// </summary>
    private void ShowMilestoneNotification(string text, Color color, int fontSize)
    {
        if (gameCanvas == null)
        {
            gameCanvas = FindFirstObjectByType<Canvas>();
            if (gameCanvas == null) return;
        }
        
        GameObject notifObj = new GameObject("MilestoneNotification");
        notifObj.transform.SetParent(gameCanvas.transform, false);
        
        Text notifText = notifObj.AddComponent<Text>();
        notifText.text = text;
        notifText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        notifText.fontSize = fontSize;
        notifText.fontStyle = FontStyle.Bold;
        notifText.color = color;
        notifText.alignment = TextAnchor.MiddleCenter;
        notifText.raycastTarget = false;
        notifText.horizontalOverflow = HorizontalWrapMode.Overflow;
        
        // Outline grueso para legibilidad
        Outline outline = notifObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2f, 2f);
        
        // Shadow para profundidad
        Shadow shadow = notifObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(3f, -3f);
        
        // Posicionar arriba-centro (debajo del score)
        RectTransform rt = notifObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -150);
        rt.sizeDelta = new Vector2(500, 50);
        
        activeNotifications.Add(notifObj);
        
        StartCoroutine(AnimateMilestoneNotification(notifObj, notifText, rt));
    }
    
    private IEnumerator AnimateFloatingNotification(GameObject obj, Text text, RectTransform rt)
    {
        float duration = 0.8f;
        float timer = 0f;
        Color startColor = text.color;
        Vector2 startPos = rt.anchoredPosition;
        
        // Scale in
        obj.transform.localScale = Vector3.zero;
        
        while (timer < duration && obj != null)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            // Scale: pop rápido
            float scaleT = Mathf.Min(t * 4f, 1f); // Escala completa en 25% del tiempo
            float scale = scaleT < 1f ? Mathf.Lerp(0f, 1.3f, scaleT) : Mathf.Lerp(1.3f, 1f, (t - 0.25f) / 0.15f);
            scale = Mathf.Max(scale, 0f);
            if (t > 0.4f) scale = Mathf.Lerp(1f, 0.8f, (t - 0.4f) / 0.6f);
            
            obj.transform.localScale = Vector3.one * scale;
            
            // Mover hacia arriba
            rt.anchoredPosition = startPos + Vector2.up * (t * 60f);
            
            // Fade out en el último 40%
            if (t > 0.6f)
            {
                float fadeT = (t - 0.6f) / 0.4f;
                text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeT);
            }
            
            yield return null;
        }
        
        if (obj != null)
        {
            activeNotifications.Remove(obj);
            Destroy(obj);
        }
    }
    
    private IEnumerator AnimateMilestoneNotification(GameObject obj, Text text, RectTransform rt)
    {
        float duration = 1.5f;
        float timer = 0f;
        Color startColor = text.color;
        
        // Empezar invisible y grande
        obj.transform.localScale = Vector3.one * 2f;
        text.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (timer < duration && obj != null)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            if (t < 0.15f)
            {
                // Fase 1: Aparecer con pop (0-15%)
                float appearT = t / 0.15f;
                float scale = Mathf.Lerp(2f, 1.0f, appearT);
                obj.transform.localScale = Vector3.one * scale;
                text.color = new Color(startColor.r, startColor.g, startColor.b, appearT);
            }
            else if (t < 0.7f)
            {
                // Fase 2: Mantener visible con pulso sutil (15-70%)
                float pulseT = (t - 0.15f) / 0.55f;
                float pulse = 1f + Mathf.Sin(pulseT * Mathf.PI * 3f) * 0.05f;
                obj.transform.localScale = Vector3.one * pulse;
                text.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
            }
            else
            {
                // Fase 3: Desaparecer (70-100%)
                float fadeT = (t - 0.7f) / 0.3f;
                float alpha = 1f - fadeT;
                text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                float scale = Mathf.Lerp(1f, 0.7f, fadeT);
                obj.transform.localScale = Vector3.one * scale;
                // Subir ligeramente
                rt.anchoredPosition += Vector2.up * Time.deltaTime * 30f;
            }
            
            yield return null;
        }
        
        if (obj != null)
        {
            activeNotifications.Remove(obj);
            Destroy(obj);
        }
    }
    
    // =========================================================================
    // HAPTIC FEEDBACK
    // =========================================================================
    
    public enum HapticType
    {
        Light,   // Tap suave: recoger shard, near miss, milestone
        Medium,  // Tap medio: recoger power-up, nuevo récord
        Heavy,   // Fuerte: escudo bloquea golpe
        Death    // Vibración larga: muerte
    }
    
    /// <summary>
    /// Activa la vibración háptica según el tipo
    /// </summary>
    public void TriggerHaptic(HapticType type)
    {
        if (!hapticsEnabled) return;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        TriggerAndroidHaptic(type);
#elif UNITY_IOS && !UNITY_EDITOR
        TriggerIOSHaptic(type);
#endif
    }
    
#if UNITY_ANDROID
    private void TriggerAndroidHaptic(HapticType type)
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator != null)
                {
                    long duration;
                    switch (type)
                    {
                        case HapticType.Light: duration = 15; break;
                        case HapticType.Medium: duration = 30; break;
                        case HapticType.Heavy: duration = 50; break;
                        case HapticType.Death: duration = 100; break;
                        default: duration = 15; break;
                    }
                    
                    // API 26+ usa VibrationEffect
                    int sdkVersion = GetAndroidSDKVersion();
                    if (sdkVersion >= 26)
                    {
                        using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                        {
                            int amplitude;
                            switch (type)
                            {
                                case HapticType.Light: amplitude = 40; break;
                                case HapticType.Medium: amplitude = 100; break;
                                case HapticType.Heavy: amplitude = 200; break;
                                case HapticType.Death: amplitude = 255; break;
                                default: amplitude = 40; break;
                            }
                            
                            AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                                "createOneShot", duration, amplitude);
                            vibrator.Call("vibrate", effect);
                        }
                    }
                    else
                    {
                        vibrator.Call("vibrate", duration);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Log($"[GameFeedbackManager] Error haptic Android: {e.Message}");
        }
    }
    
    private int GetAndroidSDKVersion()
    {
        using (AndroidJavaClass buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            return buildVersion.GetStatic<int>("SDK_INT");
        }
    }
#endif
    
#if UNITY_IOS
    private void TriggerIOSHaptic(HapticType type)
    {
        try
        {
            switch (type)
            {
                case HapticType.Light:
                    // UIImpactFeedbackGenerator.FeedbackStyle.light
                    Handheld.Vibrate(); // Fallback simple
                    break;
                case HapticType.Medium:
                    Handheld.Vibrate();
                    break;
                case HapticType.Heavy:
                    Handheld.Vibrate();
                    break;
                case HapticType.Death:
                    Handheld.Vibrate();
                    break;
            }
        }
        catch (System.Exception e)
        {
            Log($"[GameFeedbackManager] Error haptic iOS: {e.Message}");
        }
    }
#endif
    
    // =========================================================================
    // PREFERENCIAS
    // =========================================================================
    
    private void LoadHapticPreference()
    {
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                hapticsEnabled = saveData.vibrationEnabled;
                return;
            }
        }
        
        hapticsEnabled = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
    }
    
    /// <summary>
    /// Habilitar/deshabilitar vibración
    /// </summary>
    public void SetHapticsEnabled(bool enabled)
    {
        hapticsEnabled = enabled;
        
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                saveData.vibrationEnabled = enabled;
                SaveDataManager.Instance.MarkDirty();
            }
        }
        else
        {
            PlayerPrefs.SetInt("VibrationEnabled", enabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
    
    // =========================================================================
    // HELPERS
    // =========================================================================
    
    private Color GetMilestoneColor(int score)
    {
        if (score >= 200) return new Color(1f, 0.5f, 0.2f, 1f);  // Naranja
        if (score >= 100) return new Color(1f, 0.85f, 0.2f, 1f);  // Dorado
        if (score >= 50)  return new Color(0.4f, 1f, 0.4f, 1f);   // Verde
        return new Color(0.7f, 0.9f, 1f, 1f);                     // Azul claro
    }
}
