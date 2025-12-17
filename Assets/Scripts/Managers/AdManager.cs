using UnityEngine;
#if UNITY_ADS
using UnityEngine.Advertisements;
#endif
using static LogHelper;

/// <summary>
/// Manager para gestionar los anuncios del juego usando Unity Ads
/// </summary>
public class AdManager : MonoBehaviour
#if UNITY_ADS
    , IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
#endif
{
    public static AdManager Instance { get; private set; }
    
    [Header("Unity Ads Configuration")]
    [SerializeField] private string androidGameId = "YOUR_ANDROID_GAME_ID";
#pragma warning disable 0414 // Campo asignado pero nunca usado (reservado para iOS)
    [SerializeField] private string iosGameId = "YOUR_IOS_GAME_ID"; // Reservado para futura implementación iOS
#pragma warning restore 0414
    [SerializeField] private bool testMode = true; // Cambiar a false en producción
    
    [Header("Ad Frequency Settings")]
    [SerializeField] private int gamesBetweenAds = 3; // Mostrar cada 3 partidas
    [SerializeField] private float minTimeBetweenAds = 180f; // 3 minutos en segundos
    [SerializeField] private int minGameScore = 15; // Puntuación mínima para mostrar anuncio
    [SerializeField] private int gamesToForceAd = 5; // Después de X partidas, mostrar anuncio sí o sí (ignora minGameScore)
    
    private const string REMOVE_ADS_KEY = "RemoveAdsPurchased";
    private const string GAMES_SINCE_AD_KEY = "GamesSinceLastAd";
    private const string LAST_AD_TIME_KEY = "LastAdTimestamp";
    
    // Ad Unit IDs - se seleccionan automáticamente según la plataforma
    private string INTERSTITIAL_AD_ID
    {
        get
        {
#if UNITY_ANDROID
            return "Interstitial_Android";
#elif UNITY_IOS
            return "Interstitial_iOS";
#else
            return "Interstitial_Android"; // Fallback
#endif
        }
    }
    
    private string REWARDED_AD_ID
    {
        get
        {
#if UNITY_ANDROID
            return "Rewarded_Android";
#elif UNITY_IOS
            return "Rewarded_iOS";
#else
            return "Rewarded_Android"; // Fallback
#endif
        }
    }
    
    private bool isInitialized = false;
    private bool isInterstitialReady = false;
    private bool isRewardedReady = false;
    private bool pendingInterstitialShow = false; // Flag para mostrar anuncio cuando esté listo
    
    // Eventos
    public System.Action OnAdInitialized;
    public System.Action OnInterstitialAdReady;
    public System.Action OnRewardedAdReady;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAds();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Inicializa Unity Ads
    /// </summary>
    private void InitializeAds()
    {
#if UNITY_ADS
        string gameId = "";
        
#if UNITY_ANDROID
        gameId = androidGameId;
#elif UNITY_IOS
        gameId = iosGameId;
#endif
        
        if (string.IsNullOrEmpty(gameId) || gameId == "YOUR_ANDROID_GAME_ID" || gameId == "YOUR_IOS_GAME_ID")
        {
            Debug.LogWarning("[AdManager] Game ID no configurado. Los anuncios no funcionarán. Configura los Game IDs en el Inspector.");
            return;
        }
        
        Log($"[AdManager] Inicializando Unity Ads con Game ID: {gameId}");
        Advertisement.Initialize(gameId, testMode, this);
#else
        Debug.LogWarning("[AdManager] Unity Ads no está instalado. Instala el paquete desde Package Manager.");
#endif
    }
    
    /// <summary>
    /// Verifica si el usuario compró "Remove Ads"
    /// </summary>
    public bool HasRemovedAds()
    {
        return PlayerPrefs.GetInt(REMOVE_ADS_KEY, 0) == 1;
    }
    
    /// <summary>
    /// Marca que el usuario compró "Remove Ads"
    /// </summary>
    public void SetRemoveAdsPurchased()
    {
        PlayerPrefs.SetInt(REMOVE_ADS_KEY, 1);
        PlayerPrefs.Save();
        Log("[AdManager] Remove Ads comprado. Los anuncios ya no se mostrarán.");
    }
    
    /// <summary>
    /// Carga un anuncio intersticial
    /// </summary>
    public void LoadInterstitialAd()
    {
        if (HasRemovedAds())
        {
            Log("[AdManager] Anuncios desactivados (Remove Ads comprado)");
            return;
        }
        
#if UNITY_ADS
        if (!isInitialized)
        {
            Debug.LogWarning("[AdManager] Ads no inicializados aún");
            return;
        }
        
        Log("[AdManager] Cargando anuncio intersticial...");
        Advertisement.Load(INTERSTITIAL_AD_ID, this);
#else
        Debug.LogWarning("[AdManager] Unity Ads no está instalado.");
#endif
    }
    
    /// <summary>
    /// Verifica si se debe mostrar un anuncio intersticial basado en frecuencia y condiciones
    /// </summary>
    public bool ShouldShowInterstitialAd(int gameScore = 0)
    {
        if (HasRemovedAds())
        {
            return false;
        }
        
        // Verificar frecuencia de partidas
        int gamesPlayedSinceLastAd = PlayerPrefs.GetInt(GAMES_SINCE_AD_KEY, 0) + 1;
        
        // Si ha jugado suficientes partidas para forzar anuncio, ignorar puntuación mínima
        bool forceAd = gamesPlayedSinceLastAd >= gamesToForceAd;
        
        if (!forceAd)
        {
            // Verificar puntuación mínima solo si no se fuerza el anuncio
            if (gameScore > 0 && gameScore < minGameScore)
            {
                Log($"[AdManager] Puntuación muy baja ({gameScore}), no se muestra anuncio (partidas: {gamesPlayedSinceLastAd}/{gamesBetweenAds})");
                // Incrementar contador aunque no se muestre anuncio
                PlayerPrefs.SetInt(GAMES_SINCE_AD_KEY, gamesPlayedSinceLastAd);
                PlayerPrefs.Save();
                return false;
            }
            
            // Verificar si ha jugado suficientes partidas para el ciclo normal
            if (gamesPlayedSinceLastAd < gamesBetweenAds)
            {
                PlayerPrefs.SetInt(GAMES_SINCE_AD_KEY, gamesPlayedSinceLastAd);
                PlayerPrefs.Save();
                Log($"[AdManager] Partidas desde último anuncio: {gamesPlayedSinceLastAd}/{gamesBetweenAds}");
                return false;
            }
        }
        else
        {
            Log($"[AdManager] Forzando anuncio después de {gamesPlayedSinceLastAd} partidas (ignorando puntuación mínima)");
        }
        
        // Verificar cooldown temporal (usar timestamp Unix para persistir entre sesiones)
        double lastAdTimestamp = System.Convert.ToDouble(PlayerPrefs.GetString(LAST_AD_TIME_KEY, "0"));
        double currentTime = System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1)).TotalSeconds;
        double timeSinceLastAd = currentTime - lastAdTimestamp;
        
        if (timeSinceLastAd < minTimeBetweenAds && lastAdTimestamp > 0 && !forceAd)
        {
            float timeRemaining = (float)(minTimeBetweenAds - timeSinceLastAd);
            Log($"[AdManager] Cooldown activo. Tiempo restante: {timeRemaining:F1}s");
            // No incrementar contador si está en cooldown, para que cuente cuando pase el tiempo
            return false;
        }
        
        // NO resetear contador aquí - se reseteará cuando el anuncio realmente se muestre
        // Esto evita que se resetee si el anuncio falla al mostrarse
        Log("[AdManager] Condiciones cumplidas, se intentará mostrar anuncio intersticial");
        return true;
    }
    
    /// <summary>
    /// Resetea los contadores de anuncios (se llama cuando el anuncio realmente se muestra)
    /// </summary>
    private void ResetAdCounters()
    {
        double currentTime = System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1)).TotalSeconds;
        PlayerPrefs.SetInt(GAMES_SINCE_AD_KEY, 0);
        PlayerPrefs.SetString(LAST_AD_TIME_KEY, currentTime.ToString());
        PlayerPrefs.Save();
        Log("[AdManager] Contadores de anuncios reseteados");
    }
    
    /// <summary>
    /// Muestra un anuncio intersticial
    /// </summary>
    public void ShowInterstitialAd()
    {
        if (HasRemovedAds())
        {
            Log("[AdManager] Anuncios desactivados (Remove Ads comprado)");
            return;
        }
        
#if UNITY_ADS
        if (!isInitialized)
        {
            Debug.LogWarning("[AdManager] Ads no inicializados aún. No se puede mostrar anuncio.");
            return;
        }
        
        if (!isInterstitialReady)
        {
            Debug.LogWarning("[AdManager] Anuncio intersticial no está listo. Intentando cargar...");
            LoadInterstitialAd();
            // Marcar que hay un anuncio pendiente de mostrar
            pendingInterstitialShow = true;
            // NO resetear contador aquí porque el anuncio no se mostró
            Debug.LogWarning("[AdManager] El anuncio se cargará y se mostrará automáticamente cuando esté listo. El contador NO se resetea hasta entonces.");
            return;
        }
        
        // Limpiar flag de pendiente ya que vamos a mostrar el anuncio
        pendingInterstitialShow = false;
        Log("[AdManager] Mostrando anuncio intersticial...");
        Advertisement.Show(INTERSTITIAL_AD_ID, this);
#else
        Debug.LogWarning("[AdManager] Unity Ads no está instalado.");
#endif
    }
    
    /// <summary>
    /// Carga un anuncio con recompensa
    /// </summary>
    public void LoadRewardedAd()
    {
        if (HasRemovedAds())
        {
            Log("[AdManager] Anuncios desactivados (Remove Ads comprado)");
            return;
        }
        
#if UNITY_ADS
        if (!isInitialized)
        {
            Debug.LogWarning("[AdManager] Ads no inicializados aún");
            return;
        }
        
        Log("[AdManager] Cargando anuncio con recompensa...");
        Advertisement.Load(REWARDED_AD_ID, this);
#else
        Debug.LogWarning("[AdManager] Unity Ads no está instalado.");
#endif
    }
    
    /// <summary>
    /// Muestra un anuncio con recompensa
    /// </summary>
    public void ShowRewardedAd()
    {
        if (HasRemovedAds())
        {
            Log("[AdManager] Anuncios desactivados (Remove Ads comprado)");
            return;
        }
        
#if UNITY_ADS
        if (!isRewardedReady)
        {
            Debug.LogWarning("[AdManager] Anuncio con recompensa no está listo. Intentando cargar...");
            LoadRewardedAd();
            return;
        }
        
        Log("[AdManager] Mostrando anuncio con recompensa...");
        Advertisement.Show(REWARDED_AD_ID, this);
#else
        Debug.LogWarning("[AdManager] Unity Ads no está instalado.");
#endif
    }
    
#if UNITY_ADS
    // ========== IUnityAdsInitializationListener ==========
    
    public void OnInitializationComplete()
    {
        Log("[AdManager] Unity Ads inicializado correctamente");
        isInitialized = true;
        
        // Cargar anuncios después de la inicialización
        LoadInterstitialAd();
        LoadRewardedAd();
        
        OnAdInitialized?.Invoke();
    }
    
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"[AdManager] Error al inicializar Unity Ads: {error} - {message}");
    }
    
    // ========== IUnityAdsLoadListener ==========
    
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Log($"[AdManager] Anuncio cargado: {adUnitId}");
        
        if (adUnitId == INTERSTITIAL_AD_ID)
        {
            isInterstitialReady = true;
            OnInterstitialAdReady?.Invoke();
            
            // Si había un anuncio pendiente de mostrar, mostrarlo ahora
            if (pendingInterstitialShow)
            {
                Log("[AdManager] Anuncio ahora está listo, mostrándolo automáticamente...");
                pendingInterstitialShow = false;
                ShowInterstitialAd();
            }
        }
        else if (adUnitId == REWARDED_AD_ID)
        {
            isRewardedReady = true;
            OnRewardedAdReady?.Invoke();
        }
    }
    
    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"[AdManager] Error al cargar anuncio {adUnitId}: {error} - {message}");
        
        if (adUnitId == INTERSTITIAL_AD_ID)
        {
            isInterstitialReady = false;
        }
        else if (adUnitId == REWARDED_AD_ID)
        {
            isRewardedReady = false;
        }
    }
    
    // ========== IUnityAdsShowListener ==========
    
    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"[AdManager] Error al mostrar anuncio {adUnitId}: {error} - {message}");
        
        // Recargar el anuncio después de un error
        if (adUnitId == INTERSTITIAL_AD_ID)
        {
            isInterstitialReady = false;
            LoadInterstitialAd();
        }
        else if (adUnitId == REWARDED_AD_ID)
        {
            isRewardedReady = false;
            LoadRewardedAd();
        }
    }
    
    public void OnUnityAdsShowStart(string adUnitId)
    {
        Log($"[AdManager] Anuncio mostrado: {adUnitId}");
        
        // Resetear contadores cuando el anuncio realmente se muestra
        if (adUnitId == INTERSTITIAL_AD_ID)
        {
            pendingInterstitialShow = false; // Limpiar flag
            ResetAdCounters();
        }
        
        // Pausar el juego si es necesario
        Time.timeScale = 0f;
    }
    
    public void OnUnityAdsShowClick(string adUnitId)
    {
        Log($"[AdManager] Anuncio clickeado: {adUnitId}");
    }
    
    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        Log($"[AdManager] Anuncio completado: {adUnitId}, Estado: {showCompletionState}");
        
        // Reanudar el juego
        Time.timeScale = 1f;
        
        // Si es un anuncio con recompensa y se completó correctamente
        if (adUnitId == REWARDED_AD_ID && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            // Dar recompensa al jugador
            OnRewardedAdCompleted();
        }
        
        // Recargar el anuncio después de mostrarlo
        if (adUnitId == INTERSTITIAL_AD_ID)
        {
            isInterstitialReady = false;
            LoadInterstitialAd();
        }
        else if (adUnitId == REWARDED_AD_ID)
        {
            isRewardedReady = false;
            LoadRewardedAd();
        }
    }
#endif
    
    /// <summary>
    /// Se llama cuando un anuncio con recompensa se completa correctamente
    /// </summary>
    private void OnRewardedAdCompleted()
    {
        Log("[AdManager] Recompensa otorgada por ver anuncio");
        // Aquí puedes dar monedas, vidas extra, etc.
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(50); // Ejemplo: 50 monedas por ver anuncio
        }
    }
}

