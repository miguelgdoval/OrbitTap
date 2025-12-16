using UnityEngine;
#if UNITY_ADS
using UnityEngine.Advertisements;
#endif

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
    [SerializeField] private string iosGameId = "YOUR_IOS_GAME_ID";
    [SerializeField] private bool testMode = true; // Cambiar a false en producción
    
    private const string REMOVE_ADS_KEY = "RemoveAdsPurchased";
    private const string INTERSTITIAL_AD_ID = "Interstitial_Android"; // O "Interstitial_iOS"
    private const string REWARDED_AD_ID = "Rewarded_Android"; // O "Rewarded_iOS"
    
    private bool isInitialized = false;
    private bool isInterstitialReady = false;
    private bool isRewardedReady = false;
    
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
        
        Debug.Log($"[AdManager] Inicializando Unity Ads con Game ID: {gameId}");
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
        Debug.Log("[AdManager] Remove Ads comprado. Los anuncios ya no se mostrarán.");
    }
    
    /// <summary>
    /// Carga un anuncio intersticial
    /// </summary>
    public void LoadInterstitialAd()
    {
        if (HasRemovedAds())
        {
            Debug.Log("[AdManager] Anuncios desactivados (Remove Ads comprado)");
            return;
        }
        
#if UNITY_ADS
        if (!isInitialized)
        {
            Debug.LogWarning("[AdManager] Ads no inicializados aún");
            return;
        }
        
        Debug.Log("[AdManager] Cargando anuncio intersticial...");
        Advertisement.Load(INTERSTITIAL_AD_ID, this);
#else
        Debug.LogWarning("[AdManager] Unity Ads no está instalado.");
#endif
    }
    
    /// <summary>
    /// Muestra un anuncio intersticial
    /// </summary>
    public void ShowInterstitialAd()
    {
        if (HasRemovedAds())
        {
            Debug.Log("[AdManager] Anuncios desactivados (Remove Ads comprado)");
            return;
        }
        
#if UNITY_ADS
        if (!isInterstitialReady)
        {
            Debug.LogWarning("[AdManager] Anuncio intersticial no está listo. Intentando cargar...");
            LoadInterstitialAd();
            return;
        }
        
        Debug.Log("[AdManager] Mostrando anuncio intersticial...");
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
            Debug.Log("[AdManager] Anuncios desactivados (Remove Ads comprado)");
            return;
        }
        
#if UNITY_ADS
        if (!isInitialized)
        {
            Debug.LogWarning("[AdManager] Ads no inicializados aún");
            return;
        }
        
        Debug.Log("[AdManager] Cargando anuncio con recompensa...");
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
            Debug.Log("[AdManager] Anuncios desactivados (Remove Ads comprado)");
            return;
        }
        
#if UNITY_ADS
        if (!isRewardedReady)
        {
            Debug.LogWarning("[AdManager] Anuncio con recompensa no está listo. Intentando cargar...");
            LoadRewardedAd();
            return;
        }
        
        Debug.Log("[AdManager] Mostrando anuncio con recompensa...");
        Advertisement.Show(REWARDED_AD_ID, this);
#else
        Debug.LogWarning("[AdManager] Unity Ads no está instalado.");
#endif
    }
    
#if UNITY_ADS
    // ========== IUnityAdsInitializationListener ==========
    
    public void OnInitializationComplete()
    {
        Debug.Log("[AdManager] Unity Ads inicializado correctamente");
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
        Debug.Log($"[AdManager] Anuncio cargado: {adUnitId}");
        
        if (adUnitId == INTERSTITIAL_AD_ID)
        {
            isInterstitialReady = true;
            OnInterstitialAdReady?.Invoke();
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
        Debug.Log($"[AdManager] Anuncio mostrado: {adUnitId}");
        
        // Pausar el juego si es necesario
        Time.timeScale = 0f;
    }
    
    public void OnUnityAdsShowClick(string adUnitId)
    {
        Debug.Log($"[AdManager] Anuncio clickeado: {adUnitId}");
    }
    
    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"[AdManager] Anuncio completado: {adUnitId}, Estado: {showCompletionState}");
        
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
        Debug.Log("[AdManager] Recompensa otorgada por ver anuncio");
        // Aquí puedes dar monedas, vidas extra, etc.
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(50); // Ejemplo: 50 monedas por ver anuncio
        }
    }
}

