using UnityEngine;
using static LogHelper;

/// <summary>
/// Inicializador centralizado de managers del juego.
/// Elimina duplicación de código entre GameInitializer y MainMenuController.
/// Garantiza el orden correcto de inicialización (SaveDataManager primero).
/// </summary>
public static class ManagerInitializer
{
    /// <summary>
    /// Inicializa todos los managers básicos del juego en el orden correcto.
    /// Debe llamarse al inicio de cualquier escena que necesite managers.
    /// </summary>
    public static void InitializeCoreManagers()
    {
        // 1. SaveDataManager PRIMERO (otros managers dependen de él)
        EnsureManager<SaveDataManager>("SaveDataManager");
        
        // 2. Cargar configuración del juego (necesaria para otros managers)
        GameConfig.LoadConfig();
        
        // 3. Managers de datos y persistencia
        EnsureManager<StatisticsManager>("StatisticsManager");
        EnsureManager<CurrencyManager>("CurrencyManager");
        EnsureManager<LocalLeaderboardManager>("LocalLeaderboardManager");
        
        // 4. Managers de sistema
        EnsureManager<MissionManager>("MissionManager");
        EnsureManager<NotificationManager>("NotificationManager");
        EnsureManager<AccessibilityManager>("AccessibilityManager");
        EnsureManager<PauseManager>("PauseManager");
        EnsureManager<TutorialManager>("TutorialManager");
        EnsureManager<ReviveManager>("ReviveManager");
        EnsureManager<PowerUpManager>("PowerUpManager");
        EnsureManager<ComboManager>("ComboManager");
        EnsureManager<GameFeedbackManager>("GameFeedbackManager");
        
        // 5. Managers de servicios externos
        EnsureManager<AdManager>("AdManager");
        EnsureManager<AnalyticsManager>("AnalyticsManager");
        EnsureManager<IAPManager>("IAPManager");
        EnsureManager<AudioManager>("AudioManager");
        
        // 6. Managers específicos de UI (solo en menú)
        EnsureManager<PrivacyPolicyManager>("PrivacyPolicyManager");
        EnsureManager<SocialShareManager>("SocialShareManager");
        
        Log("[ManagerInitializer] Todos los managers core inicializados");
    }
    
    /// <summary>
    /// Inicializa managers específicos de la escena de juego (no del menú).
    /// </summary>
    public static void InitializeGameManagers()
    {
        // Estos managers solo se necesitan durante el juego, no en el menú
        // Se crean dinámicamente cuando se inicia una partida
        // (ObstacleManager, ScoreManager, etc. se crean en GameInitializer)
        
        Log("[ManagerInitializer] Game managers se inicializan en GameInitializer");
    }
    
    /// <summary>
    /// Asegura que un manager singleton existe, creándolo si es necesario.
    /// </summary>
    private static void EnsureManager<T>(string managerName) where T : MonoBehaviour
    {
        // Intentar obtener la instancia usando reflexión
        var instanceProperty = typeof(T).GetProperty("Instance", 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Static);
        
        if (instanceProperty != null)
        {
            var instance = instanceProperty.GetValue(null) as T;
            if (instance != null)
            {
                // Ya existe, no hacer nada
                return;
            }
        }
        
        // Crear el manager
        GameObject managerObj = new GameObject(managerName);
        managerObj.AddComponent<T>();
        Log($"[ManagerInitializer] Creado {managerName}");
    }
    
    /// <summary>
    /// Verifica si todos los managers core están inicializados.
    /// Útil para debugging.
    /// </summary>
    public static bool AreCoreManagersReady()
    {
        return SaveDataManager.Instance != null &&
               StatisticsManager.Instance != null &&
               CurrencyManager.Instance != null &&
               MissionManager.Instance != null &&
               NotificationManager.Instance != null;
    }
}
