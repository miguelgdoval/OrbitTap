using UnityEngine;
using UnityEngine.UI;
using static LogHelper;

/// <summary>
/// Sección de tienda del menú principal
/// </summary>
public class ShopSection : BaseMenuSection
{
    public override MenuSection SectionType => MenuSection.Shop;
    
    private void Start()
    {
        CreateUI();
    }
    
    private void CreateUI()
    {
        // Título
        GameObject titleObj = new GameObject("ShopTitle");
        titleObj.transform.SetParent(transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "SHOP";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 40;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = CosmicTheme.SoftGold;
        titleText.alignment = TextAnchor.UpperCenter;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(200, 50);
        
        // Scroll View para los items
        GameObject scrollObj = new GameObject("ShopScrollView");
        scrollObj.transform.SetParent(transform, false);
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        
        RectTransform scrollRectTransform = scrollObj.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0, 0);
        scrollRectTransform.anchorMax = new Vector2(1, 1);
        scrollRectTransform.sizeDelta = new Vector2(0, -100);
        scrollRectTransform.anchoredPosition = new Vector2(0, -50);
        
        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        Mask mask = viewport.AddComponent<Mask>();
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = new Color(0, 0, 0, 0.1f);
        scrollRect.viewport = viewportRect;
        
        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0, 1200); // Altura aproximada
        contentRect.anchoredPosition = Vector2.zero;
        scrollRect.content = contentRect;
        
        float yPos = -20;
        
        // Starter Bundle
        yPos = CreateShopItem(content.transform, "STARTER BUNDLE", "Exclusive Skin + 500 ⭐", "€1.49", 
            "70% OFF", yPos, Color.cyan, () => OnStarterBundleClicked());
        
        // Remove Ads (solo mostrar si no está comprado)
        if (AdManager.Instance == null || !AdManager.Instance.HasRemovedAds())
        {
            yPos = CreateShopItem(content.transform, "REMOVE ADS", "No more interruptions", "€2.99", 
                "", yPos, CosmicTheme.SoftGold, () => OnRemoveAdsClicked());
        }
        
        // Paquetes de monedas
        yPos = CreateShopItem(content.transform, "500 ⭐", "Stellar Shards", "€0.99", 
            "", yPos, Color.white, () => OnCurrencyPackClicked(500, 0.99f));
        
        yPos = CreateShopItem(content.transform, "1500 ⭐", "Stellar Shards", "€1.99", 
            "BEST VALUE", yPos, Color.white, () => OnCurrencyPackClicked(1500, 1.99f));
        
        yPos = CreateShopItem(content.transform, "3000 ⭐", "Stellar Shards", "€3.99", 
            "", yPos, Color.white, () => OnCurrencyPackClicked(3000, 3.99f));
        
        yPos = CreateShopItem(content.transform, "7000 ⭐", "Stellar Shards", "€7.99", 
            "MEGA PACK", yPos, Color.white, () => OnCurrencyPackClicked(7000, 7.99f));
        
        // Skins Premium
        yPos = CreateShopItem(content.transform, "PREMIUM SKIN", "Exclusive visual + particles", "€1.99", 
            "", yPos, Color.cyan, () => OnPremiumSkinClicked());
    }
    
    private float CreateShopItem(Transform parent, string title, string description, string price, 
        string badge, float yPos, Color accentColor, System.Action onClick)
    {
        // Obtener la fuente una vez para reutilizarla
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        
        GameObject itemObj = new GameObject($"ShopItem_{title}");
        itemObj.transform.SetParent(parent, false);
        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0.5f, 1f);
        itemRect.anchorMax = new Vector2(0.5f, 1f);
        itemRect.pivot = new Vector2(0.5f, 1f);
        itemRect.anchoredPosition = new Vector2(0, yPos);
        itemRect.sizeDelta = new Vector2(600, 180);
        
        // Fondo
        Image bgImg = itemObj.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.3f);
        
        // Badge (si existe)
        if (!string.IsNullOrEmpty(badge))
        {
            GameObject badgeObj = new GameObject("Badge");
            badgeObj.transform.SetParent(itemObj.transform, false);
            Image badgeImg = badgeObj.AddComponent<Image>();
            badgeImg.color = accentColor;
            
            RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1f, 1f);
            badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.pivot = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-10, -10);
            badgeRect.sizeDelta = new Vector2(100, 30);
            
            // Crear objeto hijo para el texto del badge
            GameObject badgeTextObj = new GameObject("Text");
            badgeTextObj.transform.SetParent(badgeObj.transform, false);
            Text badgeText = badgeTextObj.AddComponent<Text>();
            badgeText.text = badge;
            if (defaultFont != null)
            {
                badgeText.font = defaultFont;
            }
            badgeText.fontSize = 16;
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.color = Color.white;
            badgeText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform badgeTextRect = badgeTextObj.GetComponent<RectTransform>();
            badgeTextRect.anchorMin = Vector2.zero;
            badgeTextRect.anchorMax = Vector2.one;
            badgeTextRect.sizeDelta = Vector2.zero;
            badgeTextRect.anchoredPosition = Vector2.zero;
        }
        
        // Título
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(itemObj.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = title;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = accentColor;
        titleText.alignment = TextAnchor.UpperLeft;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1f);
        titleRect.anchorMax = new Vector2(0, 1f);
        titleRect.anchoredPosition = new Vector2(20, -20);
        titleRect.sizeDelta = new Vector2(400, 35);
        
        // Descripción
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(itemObj.transform, false);
        Text descText = descObj.AddComponent<Text>();
        descText.text = description;
        descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descText.fontSize = 20;
        descText.color = Color.white;
        descText.alignment = TextAnchor.UpperLeft;
        
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 1f);
        descRect.anchorMax = new Vector2(0, 1f);
        descRect.anchoredPosition = new Vector2(20, -60);
        descRect.sizeDelta = new Vector2(400, 30);
        
        // Precio y botón
        GameObject priceObj = new GameObject("PriceButton");
        priceObj.transform.SetParent(itemObj.transform, false);
        Button priceBtn = priceObj.AddComponent<Button>();
        Image priceImg = priceObj.AddComponent<Image>();
        priceImg.color = accentColor;
        
        RectTransform priceRect = priceObj.GetComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(1f, 0.5f);
        priceRect.anchorMax = new Vector2(1f, 0.5f);
        priceRect.pivot = new Vector2(1f, 0.5f);
        priceRect.anchoredPosition = new Vector2(-20, 0);
        priceRect.sizeDelta = new Vector2(150, 60);
        
        // Crear objeto hijo para el texto del precio
        GameObject priceTextObj = new GameObject("Text");
        priceTextObj.transform.SetParent(priceObj.transform, false);
        Text priceText = priceTextObj.AddComponent<Text>();
        priceText.text = price;
        if (defaultFont != null)
        {
            priceText.font = defaultFont;
        }
        priceText.fontSize = 24;
        priceText.fontStyle = FontStyle.Bold;
        priceText.color = Color.white;
        priceText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform priceTextRect = priceTextObj.GetComponent<RectTransform>();
        priceTextRect.anchorMin = Vector2.zero;
        priceTextRect.anchorMax = Vector2.one;
        priceTextRect.sizeDelta = Vector2.zero;
        priceTextRect.anchoredPosition = Vector2.zero;
        
        priceBtn.onClick.AddListener(() => onClick?.Invoke());
        
        return yPos - 200; // Espacio entre items
    }
    
    private void OnStarterBundleClicked()
    {
        Log("Starter Bundle clicked - TODO: Implement purchase");
        // TODO: Implementar compra IAP
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(500);
        }
    }
    
    private void OnRemoveAdsClicked()
    {
        Log("Remove Ads clicked");
        
        // Verificar si ya tiene Remove Ads comprado
        if (AdManager.Instance != null && AdManager.Instance.HasRemovedAds())
        {
            Log("Remove Ads ya está activo");
            // Aquí podrías mostrar un mensaje al usuario
            return;
        }
        
        // TODO: Aquí irá la lógica de IAP cuando la implementes
        // Por ahora, simulamos la compra para testing
        if (AdManager.Instance != null)
        {
            AdManager.Instance.SetRemoveAdsPurchased();
            Log("Remove Ads activado (modo test - implementar IAP real)");
            
            // Mostrar mensaje al usuario (puedes crear un sistema de notificaciones o popup)
            // Por ahora solo un log
        }
        else
        {
            LogWarning("AdManager no encontrado");
        }
    }
    
    private void OnCurrencyPackClicked(int amount, float price)
    {
        Log($"Currency pack clicked: {amount} for {price} - TODO: Implement purchase");
        // TODO: Implementar compra IAP
        // Por ahora, añadir monedas directamente para testing
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(amount);
        }
    }
    
    private void OnPremiumSkinClicked()
    {
        Log("Premium Skin clicked - TODO: Implement purchase");
        // TODO: Implementar compra IAP
    }
}

