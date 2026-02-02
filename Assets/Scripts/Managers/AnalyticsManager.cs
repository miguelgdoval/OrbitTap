using UnityEngine;
#if UNITY_ANALYTICS
using UnityEngine.Analytics;
#endif
#if FIREBASE_ANALYTICS
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
#endif
using static LogHelper;

/// <summary>
/// Manager para Analytics y Crash Reporting
/// Soporta Unity Analytics y Firebase Analytics (recomendado para producción)
/// </summary>
public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }
    
    [Header("Analytics Settings")]
    [SerializeField] private bool enableAnalytics = true;
    [SerializeField] private AnalyticsProvider preferredProvider = AnalyticsProvider.Firebase;
    
    private bool isFirebaseInitialized = false;
    private bool isUnityAnalyticsInitialized = false;
    
    public enum AnalyticsProvider
    {
        Unity,      // Unity Analytics (básico, funciona pero limitado)
        Firebase,  // Firebase Analytics (recomendado para producción)
        Both        // Ambos (para migración o redundancia)
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAnalytics();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializeAnalytics()
    {
        if (!enableAnalytics)
        {
            LogWarning("[AnalyticsManager] Analytics está deshabilitado");
            return;
        }
        
        Log($"[AnalyticsManager] Inicializando analytics. Provider: {preferredProvider}");
        
        // En el Editor, Firebase no funciona bien (especialmente en macOS)
        // Usar Unity Analytics como fallback en el Editor
#if UNITY_EDITOR
        if (preferredProvider == AnalyticsProvider.Firebase)
        {
            Log("[AnalyticsManager] ⚠️ Editor detectado: Firebase deshabilitado, usando Unity Analytics como fallback");
            preferredProvider = AnalyticsProvider.Unity;
        }
        else if (preferredProvider == AnalyticsProvider.Both)
        {
            Log("[AnalyticsManager] ⚠️ Editor detectado: Firebase deshabilitado, usando solo Unity Analytics");
            preferredProvider = AnalyticsProvider.Unity;
        }
#endif
        
        // Inicializar Firebase Analytics (recomendado para producción)
        if (preferredProvider == AnalyticsProvider.Firebase || preferredProvider == AnalyticsProvider.Both)
        {
            Log("[AnalyticsManager] Intentando inicializar Firebase...");
            InitializeFirebase();
        }
        
        // Inicializar Unity Analytics (como respaldo o principal)
        if (preferredProvider == AnalyticsProvider.Unity || preferredProvider == AnalyticsProvider.Both)
        {
            Log("[AnalyticsManager] Intentando inicializar Unity Analytics...");
            InitializeUnityAnalytics();
        }
    }
    
    private void InitializeFirebase()
    {
#if FIREBASE_ANALYTICS
        // Firebase no funciona bien en el Editor de Unity (especialmente en macOS)
        // Solo inicializar en builds reales
#if UNITY_EDITOR
        LogWarning("[AnalyticsManager] ⚠️ Firebase Analytics está deshabilitado en el Editor (no funciona en macOS Editor)");
        LogWarning("[AnalyticsManager] Firebase funcionará correctamente en builds reales (Android/iOS)");
#else
        Log("[AnalyticsManager] ✅ Símbolo FIREBASE_ANALYTICS detectado. Inicializando Firebase...");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase está listo
                isFirebaseInitialized = true;
                Log("[AnalyticsManager] ✅ Firebase Analytics inicializado correctamente");
                
                // Opcional: Habilitar recopilación automática de eventos
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            }
            else
            {
                LogWarning($"[AnalyticsManager] ❌ Firebase no está disponible: {dependencyStatus}");
            }
        });
#endif
#else
        LogWarning("[AnalyticsManager] ❌ Símbolo FIREBASE_ANALYTICS NO detectado. Firebase Analytics no está disponible.");
        LogWarning("[AnalyticsManager] Verifica que el símbolo esté definido en Project Settings > Player > Other Settings > Scripting Define Symbols");
#endif
    }
    
    private void InitializeUnityAnalytics()
    {
#if UNITY_ANALYTICS
        // Unity Analytics se inicializa automáticamente si está habilitado en Project Settings
        Log("[AnalyticsManager] Verificando estado de Unity Analytics...");
        Log($"[AnalyticsManager] Analytics.enabled = {Analytics.enabled}");
        Log($"[AnalyticsManager] Analytics.deviceStatsEnabled = {Analytics.deviceStatsEnabled}");
        
        if (Analytics.enabled)
        {
            isUnityAnalyticsInitialized = true;
            Log("[AnalyticsManager] ✅ Unity Analytics habilitado y listo");
        }
        else
        {
            LogWarning("[AnalyticsManager] ❌ Unity Analytics no está habilitado en Project Settings");
            LogWarning("[AnalyticsManager] Ve a Edit > Project Settings > Services y habilita Unity Analytics");
        }
#else
        LogWarning("[AnalyticsManager] Unity Analytics no está disponible (módulo no instalado)");
#endif
    }
    
    /// <summary>
    /// Registra un evento personalizado
    /// </summary>
    public void TrackEvent(string eventName, System.Collections.Generic.Dictionary<string, object> parameters = null)
    {
        if (!enableAnalytics) return;
        
        // Firebase Analytics (recomendado para producción)
        if ((preferredProvider == AnalyticsProvider.Firebase || preferredProvider == AnalyticsProvider.Both) && isFirebaseInitialized)
        {
            TrackFirebaseEvent(eventName, parameters);
        }
        
        // Unity Analytics (respaldo o principal)
        if ((preferredProvider == AnalyticsProvider.Unity || preferredProvider == AnalyticsProvider.Both) && isUnityAnalyticsInitialized)
        {
            TrackUnityEvent(eventName, parameters);
        }
    }
    
    private void TrackFirebaseEvent(string eventName, System.Collections.Generic.Dictionary<string, object> parameters)
    {
#if FIREBASE_ANALYTICS
        if (parameters == null || parameters.Count == 0)
        {
            FirebaseAnalytics.LogEvent(eventName);
        }
        else
        {
            // Convertir Dictionary a Parameter[]
            var firebaseParams = new Parameter[parameters.Count];
            int index = 0;
            foreach (var param in parameters)
            {
                // Firebase solo acepta ciertos tipos
                if (param.Value is int)
                {
                    firebaseParams[index] = new Parameter(param.Key, (int)param.Value);
                }
                else if (param.Value is float || param.Value is double)
                {
                    firebaseParams[index] = new Parameter(param.Key, System.Convert.ToDouble(param.Value));
                }
                else if (param.Value is bool)
                {
                    firebaseParams[index] = new Parameter(param.Key, (bool)param.Value ? 1 : 0);
                }
                else
                {
                    firebaseParams[index] = new Parameter(param.Key, param.Value.ToString());
                }
                index++;
            }
            FirebaseAnalytics.LogEvent(eventName, firebaseParams);
        }
        Log($"[AnalyticsManager] Evento Firebase registrado: {eventName}");
#endif
    }
    
    private void TrackUnityEvent(string eventName, System.Collections.Generic.Dictionary<string, object> parameters)
    {
#if UNITY_ANALYTICS
        if (!Analytics.enabled)
        {
            LogWarning($"[AnalyticsManager] Unity Analytics no está habilitado. No se puede registrar: {eventName}");
            return;
        }
        
        if (parameters == null)
        {
            parameters = new System.Collections.Generic.Dictionary<string, object>();
        }
        
        // Log detallado para debug
        string paramsStr = parameters.Count > 0 ? $" con {parameters.Count} parámetros" : "";
        Log($"[AnalyticsManager] Intentando registrar evento Unity: {eventName}{paramsStr}");
        
        AnalyticsResult result = Analytics.CustomEvent(eventName, parameters);
        
        if (result == AnalyticsResult.Ok)
        {
            Log($"[AnalyticsManager] ✅ Evento Unity registrado exitosamente: {eventName}");
        }
        else if (result == AnalyticsResult.AnalyticsDisabled)
        {
            LogWarning($"[AnalyticsManager] ❌ Unity Analytics está deshabilitado. Evento: {eventName}");
        }
        else if (result == AnalyticsResult.InvalidData)
        {
            LogWarning($"[AnalyticsManager] ❌ Datos inválidos para evento Unity: {eventName}. Resultado: {result}");
        }
        else if (result == AnalyticsResult.TooManyItems)
        {
            LogWarning($"[AnalyticsManager] ❌ Demasiados parámetros en evento Unity: {eventName}. Resultado: {result}");
        }
        else
        {
            LogWarning($"[AnalyticsManager] ❌ Error al registrar evento Unity {eventName}: {result}");
        }
#else
        LogWarning($"[AnalyticsManager] Unity Analytics no está disponible (módulo no instalado). Evento: {eventName}");
#endif
    }
    
    // ========== Eventos del juego ==========
    
    /// <summary>
    /// Registra cuando el jugador inicia una partida
    /// </summary>
    public void TrackGameStart()
    {
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "timestamp", System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
        };
        TrackEvent("game_start", parameters);
    }
    
    /// <summary>
    /// Registra cuando el jugador termina una partida
    /// </summary>
    public void TrackGameOver(int score, int highScore, float playTime)
    {
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "score", score },
            { "high_score", highScore },
            { "play_time_seconds", playTime },
            { "is_new_record", score >= highScore }
        };
        TrackEvent("game_over", parameters);
    }
    
    /// <summary>
    /// Registra cuando el jugador ve un anuncio
    /// </summary>
    public void TrackAdShown(string adType, bool rewarded = false)
    {
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "ad_type", adType },
            { "is_rewarded", rewarded }
        };
        TrackEvent("ad_shown", parameters);
    }
    
    /// <summary>
    /// Registra cuando el jugador completa una misión
    /// </summary>
    public void TrackMissionCompleted(string missionId, string missionTitle, int rewardAmount)
    {
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "mission_id", missionId },
            { "mission_title", missionTitle },
            { "reward_amount", rewardAmount }
        };
        TrackEvent("mission_completed", parameters);
    }
    
    /// <summary>
    /// Registra cuando el jugador compra algo en la tienda
    /// </summary>
    public void TrackPurchase(string itemId, string itemName, float price, string currency = "USD")
    {
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "item_id", itemId },
            { "item_name", itemName },
            { "price", price },
            { "currency", currency }
        };
        TrackEvent("purchase", parameters);
    }
    
    /// <summary>
    /// Registra cuando el jugador compra un skin
    /// </summary>
    public void TrackSkinPurchased(string skinId, string skinName, bool isPremium)
    {
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "skin_id", skinId },
            { "skin_name", skinName },
            { "is_premium", isPremium }
        };
        TrackEvent("skin_purchased", parameters);
    }
    
    /// <summary>
    /// Registra cuando el jugador comparte su puntuación
    /// </summary>
    public void TrackShare(int score)
    {
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "score", score }
        };
        TrackEvent("share_score", parameters);
    }
    
    /// <summary>
    /// Registra errores personalizados (además del Crash Reporting automático)
    /// </summary>
    public void TrackError(string errorType, string errorMessage)
    {
        var parameters = new System.Collections.Generic.Dictionary<string, object>
        {
            { "error_type", errorType },
            { "error_message", errorMessage }
        };
        TrackEvent("error_occurred", parameters);
    }
    
    /// <summary>
    /// Método de diagnóstico: Envía un evento de test para verificar que Analytics funciona
    /// </summary>
    public void SendTestEvent()
    {
        var testParams = new System.Collections.Generic.Dictionary<string, object>
        {
            { "test_timestamp", System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
            { "test_value", 12345 },
            { "test_platform", Application.platform.ToString() }
        };
        
        TrackEvent("analytics_test", testParams);
        
        #if UNITY_ANALYTICS
        Log($"[AnalyticsManager] 🔍 Evento de test enviado. Analytics.enabled = {Analytics.enabled}");
        Log($"[AnalyticsManager] 🔍 Si no ves datos en el dashboard, considera usar Firebase Analytics");
        #endif
    }
}

