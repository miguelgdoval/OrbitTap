using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using static LogHelper;

/// <summary>
/// Manager para gestionar las compras in-app usando Unity IAP
/// </summary>
public class IAPManager : MonoBehaviour, IStoreListener
{
    public static IAPManager Instance { get; private set; }
    
    private IStoreController storeController;
    private IExtensionProvider extensionProvider;
    private bool isInitialized = false;
    
    // IDs de productos (deben coincidir con Google Play Console / App Store Connect)
    private const string STARTER_BUNDLE = "starter_bundle";
    private const string REMOVE_ADS = "remove_ads";
    private const string CRYSTALS_100 = "crystals100";
    private const string CRYSTALS_250 = "crystals250";
    private const string CRYSTALS_500 = "crystals500";
    private const string CRYSTALS_1000 = "crystals1000";
    private const string SHARDS_500 = "shards500";
    private const string SHARDS_1500 = "shards1500";
    private const string SHARDS_3000 = "shards3000";
    private const string SHARDS_7000 = "shards7000";
    
    // Eventos
    public System.Action<string> OnPurchaseCompleted;
    public System.Action<string, string> OnPurchaseFailedEvent;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePurchasing();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePurchasing()
    {
        if (isInitialized)
        {
            Log("[IAPManager] IAP ya está inicializado");
            return;
        }
        
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        
        // Añadir todos los productos
        builder.AddProduct(STARTER_BUNDLE, ProductType.NonConsumable);
        builder.AddProduct(REMOVE_ADS, ProductType.NonConsumable);
        builder.AddProduct(CRYSTALS_100, ProductType.Consumable);
        builder.AddProduct(CRYSTALS_250, ProductType.Consumable);
        builder.AddProduct(CRYSTALS_500, ProductType.Consumable);
        builder.AddProduct(CRYSTALS_1000, ProductType.Consumable);
        builder.AddProduct(SHARDS_500, ProductType.Consumable);
        builder.AddProduct(SHARDS_1500, ProductType.Consumable);
        builder.AddProduct(SHARDS_3000, ProductType.Consumable);
        builder.AddProduct(SHARDS_7000, ProductType.Consumable);
        
#pragma warning disable CS0618 // El método obsoleto sigue siendo funcional y compatible
        UnityPurchasing.Initialize(this, builder);
#pragma warning restore CS0618
    }
    
    // ========== IStoreListener ==========
    
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        Log("[IAPManager] Unity IAP inicializado correctamente");
        storeController = controller;
        extensionProvider = extensions;
        isInitialized = true;
        
        // Verificar compras previas (restore purchases)
        RestorePurchases();
    }
    
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        LogError($"[IAPManager] Error al inicializar Unity IAP: {error}");
        
        // Mostrar notificación al usuario
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowError("Error al inicializar la tienda. Las compras pueden no estar disponibles.");
        }
    }
    
    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        LogError($"[IAPManager] Error al inicializar Unity IAP: {error} - {message}");
        
        // Mostrar notificación al usuario
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowError("Error al inicializar la tienda. Las compras pueden no estar disponibles.");
        }
    }
    
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        Log($"[IAPManager] Compra procesada: {productId}");
        
        // Procesar la compra según el producto
        ProcessPurchaseForProduct(productId);
        
        // Analytics
        if (AnalyticsManager.Instance != null)
        {
            Product product = storeController.products.WithID(productId);
            if (product != null)
            {
                AnalyticsManager.Instance.TrackPurchase(
                    productId,
                    product.metadata.localizedTitle,
                    (float)product.metadata.localizedPrice
                );
            }
        }
        
        // Mostrar notificación de éxito
        if (NotificationManager.Instance != null)
        {
            Product product = storeController.products.WithID(productId);
            string productName = product != null ? product.metadata.localizedTitle : productId;
            NotificationManager.Instance.ShowSuccess($"¡Compra exitosa! {productName}");
        }
        
        OnPurchaseCompleted?.Invoke(productId);
        
        return PurchaseProcessingResult.Complete;
    }
    
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        LogError($"[IAPManager] Error al comprar {product.definition.id}: {failureReason}");
        
        // Mostrar notificación de error al usuario
        if (NotificationManager.Instance != null)
        {
            string errorMessage = GetPurchaseErrorMessage(failureReason);
            NotificationManager.Instance.ShowError($"Error en la compra: {errorMessage}");
        }
        
        OnPurchaseFailedEvent?.Invoke(product.definition.id, failureReason.ToString());
    }
    
    /// <summary>
    /// Obtiene un mensaje de error amigable para el usuario
    /// </summary>
    private string GetPurchaseErrorMessage(PurchaseFailureReason reason)
    {
        switch (reason)
        {
            case PurchaseFailureReason.PurchasingUnavailable:
                return "Las compras no están disponibles en este momento";
            case PurchaseFailureReason.ExistingPurchasePending:
                return "Ya hay una compra en proceso";
            case PurchaseFailureReason.ProductUnavailable:
                return "El producto no está disponible";
            case PurchaseFailureReason.SignatureInvalid:
                return "Error de verificación de compra";
            case PurchaseFailureReason.UserCancelled:
                return "Compra cancelada";
            case PurchaseFailureReason.PaymentDeclined:
                return "Pago rechazado";
            case PurchaseFailureReason.DuplicateTransaction:
                return "Transacción duplicada";
            default:
                return "Error desconocido";
        }
    }
    
    // ========== Métodos públicos ==========
    
    /// <summary>
    /// Inicia la compra de un producto
    /// </summary>
    public void BuyProduct(string productId)
    {
        if (!isInitialized)
        {
            LogError("[IAPManager] IAP no está inicializado aún");
            return;
        }
        
        Product product = storeController.products.WithID(productId);
        if (product != null && product.availableToPurchase)
        {
            Log($"[IAPManager] Iniciando compra: {productId}");
            storeController.InitiatePurchase(product);
        }
        else
        {
            LogError($"[IAPManager] Producto {productId} no está disponible");
            
            // Mostrar notificación al usuario
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowWarning("Este producto no está disponible en este momento");
            }
        }
    }
    
    /// <summary>
    /// Restaura compras previas (útil para iOS y para recuperar compras en nuevos dispositivos)
    /// </summary>
    public void RestorePurchases()
    {
        if (!isInitialized)
        {
            LogError("[IAPManager] IAP no está inicializado");
            return;
        }
        
        Log("[IAPManager] Restaurando compras...");
        
        #if UNITY_IOS
        extensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions((result) => {
            if (result)
            {
                Log("[IAPManager] Compras restauradas correctamente");
            }
            else
            {
                LogWarning("[IAPManager] Error al restaurar compras");
            }
        });
        #elif UNITY_ANDROID
        extensionProvider.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions((result, errorMessage) => {
            if (result)
            {
                Log("[IAPManager] Compras restauradas correctamente");
            }
            else
            {
                LogWarning($"[IAPManager] Error al restaurar compras: {errorMessage}");
            }
        });
        #else
        Log("[IAPManager] Restore Purchases no disponible en esta plataforma");
        #endif
    }
    
    /// <summary>
    /// Verifica si un producto ya fue comprado (para productos Non-Consumable)
    /// </summary>
    public bool HasProduct(string productId)
    {
        if (!isInitialized) return false;
        
        Product product = storeController.products.WithID(productId);
        return product != null && product.hasReceipt;
    }
    
    // ========== Procesamiento de compras ==========
    
    private void ProcessPurchaseForProduct(string productId)
    {
        if (CurrencyManager.Instance == null)
        {
            LogError("[IAPManager] CurrencyManager no encontrado");
            return;
        }
        
        switch (productId)
        {
            case STARTER_BUNDLE:
                CurrencyManager.Instance.AddCosmicCrystals(100);
                CurrencyManager.Instance.AddStellarShards(500);
                Log("[IAPManager] Starter Bundle aplicado: 100 💎 + 500 ⭐");
                break;
                
            case REMOVE_ADS:
                if (AdManager.Instance != null)
                {
                    AdManager.Instance.SetRemoveAdsPurchased();
                    Log("[IAPManager] Remove Ads activado");
                }
                break;
                
            case CRYSTALS_100:
                CurrencyManager.Instance.AddCosmicCrystals(100);
                Log("[IAPManager] 100 💎 añadidas");
                break;
                
            case CRYSTALS_250:
                CurrencyManager.Instance.AddCosmicCrystals(250);
                CurrencyManager.Instance.AddStellarShards(500);
                Log("[IAPManager] 250 💎 + 500 ⭐ añadidas");
                break;
                
            case CRYSTALS_500:
                CurrencyManager.Instance.AddCosmicCrystals(500);
                CurrencyManager.Instance.AddStellarShards(1500);
                Log("[IAPManager] 500 💎 + 1500 ⭐ añadidas");
                break;
                
            case CRYSTALS_1000:
                CurrencyManager.Instance.AddCosmicCrystals(1000);
                CurrencyManager.Instance.AddStellarShards(3000);
                Log("[IAPManager] 1000 💎 + 3000 ⭐ añadidas");
                break;
                
            case SHARDS_500:
                CurrencyManager.Instance.AddStellarShards(500);
                Log("[IAPManager] 500 ⭐ añadidas");
                break;
                
            case SHARDS_1500:
                CurrencyManager.Instance.AddStellarShards(1500);
                Log("[IAPManager] 1500 ⭐ añadidas");
                break;
                
            case SHARDS_3000:
                CurrencyManager.Instance.AddStellarShards(3000);
                Log("[IAPManager] 3000 ⭐ añadidas");
                break;
                
            case SHARDS_7000:
                CurrencyManager.Instance.AddStellarShards(7000);
                Log("[IAPManager] 7000 ⭐ añadidas");
                break;
                
            default:
                LogWarning($"[IAPManager] Producto desconocido: {productId}");
                break;
        }
    }
}
