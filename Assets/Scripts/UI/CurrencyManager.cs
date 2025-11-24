using UnityEngine;

/// <summary>
/// Gestiona la moneda del juego (Stellar Shards)
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }
    
    private const string CURRENCY_KEY = "StellarShards";
    private int currentCurrency = 0;
    
    public int CurrentCurrency => currentCurrency;
    
    public System.Action<int> OnCurrencyChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCurrency();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadCurrency()
    {
        currentCurrency = PlayerPrefs.GetInt(CURRENCY_KEY, 0);
    }
    
    public void AddCurrency(int amount)
    {
        currentCurrency += amount;
        SaveCurrency();
        OnCurrencyChanged?.Invoke(currentCurrency);
    }
    
    public bool SpendCurrency(int amount)
    {
        if (currentCurrency >= amount)
        {
            currentCurrency -= amount;
            SaveCurrency();
            OnCurrencyChanged?.Invoke(currentCurrency);
            return true;
        }
        return false;
    }
    
    private void SaveCurrency()
    {
        PlayerPrefs.SetInt(CURRENCY_KEY, currentCurrency);
        PlayerPrefs.Save();
    }
}

