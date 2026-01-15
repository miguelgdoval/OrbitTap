using UnityEngine;

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
        stellarShards = PlayerPrefs.GetInt(STELLAR_SHARDS_KEY, 0);
        cosmicCrystals = PlayerPrefs.GetInt(COSMIC_CRYSTALS_KEY, 0);
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

