using UnityEngine;
using static LogHelper;

/// <summary>
/// Gestiona las dos monedas del juego: Stellar Shards (⭐) y Cosmic Crystals (💎)
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }
    
    // Keys para PlayerPrefs
    private const string STELLAR_SHARDS_KEY = "StellarShards";
    private const string COSMIC_CRYSTALS_KEY = "CosmicCrystals";
    
    // Monedas
    private int stellarShards = 0;      // Moneda gratuita (⭐)
    private int cosmicCrystals = 0;     // Moneda premium (💎)
    
    // Propiedades públicas
    public int StellarShards => stellarShards;
    public int CosmicCrystals => cosmicCrystals;
    
    // Eventos
    public System.Action<int> OnStellarShardsChanged;
    public System.Action<int> OnCosmicCrystalsChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCurrencies();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadCurrencies()
    {
        // Usar SaveDataManager si está disponible, sino usar PlayerPrefs (compatibilidad)
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                stellarShards = saveData.stellarShards;
                cosmicCrystals = saveData.cosmicCrystals;
                Log($"[CurrencyManager] Monedas cargadas desde SaveDataManager: Shards={stellarShards}, Crystals={cosmicCrystals}");
                return;
            }
        }
        
        // Fallback a PlayerPrefs (compatibilidad con datos antiguos)
        int loadedShards = PlayerPrefs.GetInt(STELLAR_SHARDS_KEY, 0);
        int loadedCrystals = PlayerPrefs.GetInt(COSMIC_CRYSTALS_KEY, 0);
        
        // Validar rangos razonables (máximo 999,999,999 para evitar overflow)
        const int MAX_CURRENCY = 999999999;
        const int MIN_CURRENCY = 0;
        
        if (loadedShards < MIN_CURRENCY || loadedShards > MAX_CURRENCY)
        {
            LogWarning($"[CurrencyManager] Stellar Shards fuera de rango ({loadedShards}), reseteando a 0");
            loadedShards = 0;
            PlayerPrefs.SetInt(STELLAR_SHARDS_KEY, 0);
        }
        
        if (loadedCrystals < MIN_CURRENCY || loadedCrystals > MAX_CURRENCY)
        {
            LogWarning($"[CurrencyManager] Cosmic Crystals fuera de rango ({loadedCrystals}), reseteando a 0");
            loadedCrystals = 0;
            PlayerPrefs.SetInt(COSMIC_CRYSTALS_KEY, 0);
        }
        
        stellarShards = loadedShards;
        cosmicCrystals = loadedCrystals;
        
        PlayerPrefs.Save();
    }
    
    // ========== STELLAR SHARDS (⭐) - Moneda Gratuita ==========
    
    public void AddStellarShards(int amount)
    {
        if (amount <= 0) return;
        
        stellarShards += amount;
        SaveStellarShards();
        OnStellarShardsChanged?.Invoke(stellarShards);
    }
    
    public bool SpendStellarShards(int amount)
    {
        if (amount <= 0) return false;
        if (stellarShards < amount) return false;
        
        stellarShards -= amount;
        SaveStellarShards();
        OnStellarShardsChanged?.Invoke(stellarShards);
        return true;
    }
    
    private void SaveStellarShards()
    {
        // Usar SaveDataManager si está disponible
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                saveData.stellarShards = stellarShards;
                SaveDataManager.Instance.MarkDirty();
                return;
            }
        }
        
        // Fallback a PlayerPrefs
        PlayerPrefs.SetInt(STELLAR_SHARDS_KEY, stellarShards);
        PlayerPrefs.Save();
    }
    
    // ========== COSMIC CRYSTALS (💎) - Moneda Premium ==========
    
    public void AddCosmicCrystals(int amount)
    {
        if (amount <= 0) return;
        
        cosmicCrystals += amount;
        SaveCosmicCrystals();
        OnCosmicCrystalsChanged?.Invoke(cosmicCrystals);
    }
    
    public bool SpendCosmicCrystals(int amount)
    {
        if (amount <= 0) return false;
        if (cosmicCrystals < amount) return false;
        
        cosmicCrystals -= amount;
        SaveCosmicCrystals();
        OnCosmicCrystalsChanged?.Invoke(cosmicCrystals);
        return true;
    }
    
    private void SaveCosmicCrystals()
    {
        // Usar SaveDataManager si está disponible
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                saveData.cosmicCrystals = cosmicCrystals;
                SaveDataManager.Instance.MarkDirty();
                return;
            }
        }
        
        // Fallback a PlayerPrefs
        PlayerPrefs.SetInt(COSMIC_CRYSTALS_KEY, cosmicCrystals);
        PlayerPrefs.Save();
    }
    
    // ========== MÉTODOS DE COMPATIBILIDAD (para código existente) ==========
    
    /// <summary>
    /// Método legacy - añade Stellar Shards (para compatibilidad con código existente)
    /// </summary>
    public void AddCurrency(int amount)
    {
        AddStellarShards(amount);
    }
    
    /// <summary>
    /// Método legacy - gasta Stellar Shards (para compatibilidad con código existente)
    /// </summary>
    public bool SpendCurrency(int amount)
    {
        return SpendStellarShards(amount);
    }
    
    /// <summary>
    /// Propiedad legacy - retorna Stellar Shards (para compatibilidad)
    /// </summary>
    public int CurrentCurrency => stellarShards;
    
    /// <summary>
    /// Evento legacy - se dispara cuando cambian Stellar Shards (para compatibilidad)
    /// </summary>
    public System.Action<int> OnCurrencyChanged
    {
        get => OnStellarShardsChanged;
        set => OnStellarShardsChanged = value;
    }
}

