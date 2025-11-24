using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Sección de skins del menú principal
/// </summary>
public class SkinsSection : BaseMenuSection
{
    public override MenuSection SectionType => MenuSection.Skins;
    
    [Header("UI References")]
    private GameObject contentPanel;
    private ScrollRect scrollRect;
    private Text currencyText;
    
    [Header("Skin Data")]
    private List<SkinData> availableSkins = new List<SkinData>();
    private SkinData currentEquippedSkin;
    
    private void Start()
    {
        InitializeSkins();
        CreateUI();
    }
    
    private void InitializeSkins()
    {
        // Crear skins por defecto
        availableSkins.Add(new SkinData("Default", SkinRarity.Common, 0, true, CosmicTheme.EtherealLila));
        availableSkins.Add(new SkinData("Celestial", SkinRarity.Rare, 100, false, CosmicTheme.CelestialBlue));
        availableSkins.Add(new SkinData("Constellation", SkinRarity.Epic, 500, false, CosmicTheme.ConstellationBlue));
        availableSkins.Add(new SkinData("Golden", SkinRarity.Legendary, 1500, false, CosmicTheme.SoftGold));
        availableSkins.Add(new SkinData("Premium", SkinRarity.Premium, 0, false, Color.cyan, true)); // Requiere compra
    }
    
    private void CreateUI()
    {
        // Panel de contenido con scroll horizontal
        GameObject scrollObj = new GameObject("SkinsScrollView");
        scrollObj.transform.SetParent(transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.sizeDelta = Vector2.zero;
        scrollRect.anchoredPosition = new Vector2(0, -40); // Ajustar para el título
        
        this.scrollRect = scrollObj.AddComponent<ScrollRect>();
        this.scrollRect.horizontal = true;
        this.scrollRect.vertical = false;
        
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
        this.scrollRect.viewport = viewportRect;
        
        // Content
        contentPanel = new GameObject("Content");
        contentPanel.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0.5f);
        contentRect.anchorMax = new Vector2(0, 0.5f);
        contentRect.pivot = new Vector2(0, 0.5f);
        contentRect.sizeDelta = new Vector2(availableSkins.Count * 250, 400);
        contentRect.anchoredPosition = new Vector2(20, 0);
        this.scrollRect.content = contentRect;
        
        // Crear botones de skin
        for (int i = 0; i < availableSkins.Count; i++)
        {
            CreateSkinButton(availableSkins[i], i);
        }
        
        // Título
        GameObject titleObj = new GameObject("SkinsTitle");
        titleObj.transform.SetParent(transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "SKINS";
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
    }
    
    private void CreateSkinButton(SkinData skin, int index)
    {
        GameObject skinObj = new GameObject($"Skin_{skin.name}");
        skinObj.transform.SetParent(contentPanel.transform, false);
        RectTransform skinRect = skinObj.AddComponent<RectTransform>();
        skinRect.anchorMin = new Vector2(0, 0.5f);
        skinRect.anchorMax = new Vector2(0, 0.5f);
        skinRect.pivot = new Vector2(0, 0.5f);
        skinRect.anchoredPosition = new Vector2(index * 250 + 20, 0);
        skinRect.sizeDelta = new Vector2(220, 380);
        
        // Fondo
        Image bgImg = skinObj.AddComponent<Image>();
        Color bgColor = GetRarityColor(skin.rarity);
        bgColor.a = 0.3f;
        bgImg.color = bgColor;
        
        // Preview del skin (círculo con el color)
        GameObject previewObj = new GameObject("Preview");
        previewObj.transform.SetParent(skinObj.transform, false);
        Image previewImg = previewObj.AddComponent<Image>();
        previewImg.sprite = SpriteGenerator.CreateCircleSprite(0.5f, skin.color);
        previewImg.color = skin.color;
        
        RectTransform previewRect = previewObj.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = new Vector2(0, 50);
        previewRect.sizeDelta = new Vector2(120, 120);
        
        // Nombre
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(skinObj.transform, false);
        Text nameText = nameObj.AddComponent<Text>();
        nameText.text = skin.name;
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 24;
        nameText.fontStyle = FontStyle.Bold;
        nameText.color = GetRarityColor(skin.rarity);
        nameText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0.5f);
        nameRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameRect.anchoredPosition = new Vector2(0, -20);
        nameRect.sizeDelta = new Vector2(200, 30);
        
        // Botón de acción
        GameObject btnObj = new GameObject("ActionButton");
        btnObj.transform.SetParent(skinObj.transform, false);
        Button btn = btnObj.AddComponent<Button>();
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = GetRarityColor(skin.rarity);
        
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0, -120);
        btnRect.sizeDelta = new Vector2(180, 50);
        
        // Crear objeto hijo para el texto
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        Text btnText = btnTextObj.AddComponent<Text>();
        if (skin.isUnlocked)
        {
            btnText.text = skin.isEquipped ? "EQUIPPED" : "EQUIP";
        }
        else if (skin.isPremium)
        {
            btnText.text = "BUY";
        }
        else
        {
            btnText.text = $"{skin.cost} ⭐";
        }
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont != null)
        {
            btnText.font = defaultFont;
        }
        btnText.fontSize = 20;
        btnText.fontStyle = FontStyle.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        btnTextRect.anchoredPosition = Vector2.zero;
        
        int skinIndex = index; // Capturar para el closure
        btn.onClick.AddListener(() => OnSkinButtonClicked(skinIndex));
    }
    
    private void OnSkinButtonClicked(int index)
    {
        SkinData skin = availableSkins[index];
        
        if (skin.isUnlocked)
        {
            // Equipar
            if (currentEquippedSkin != null)
            {
                currentEquippedSkin.isEquipped = false;
            }
            skin.isEquipped = true;
            currentEquippedSkin = skin;
            Debug.Log($"Equipped skin: {skin.name}");
        }
        else if (skin.isPremium)
        {
            // Ir a la tienda
            // TODO: Navegar a shop section
            Debug.Log("Premium skin - redirect to shop");
        }
        else
        {
            // Comprar con monedas
            if (CurrencyManager.Instance != null && CurrencyManager.Instance.SpendCurrency(skin.cost))
            {
                skin.isUnlocked = true;
                Debug.Log($"Unlocked skin: {skin.name}");
            }
            else
            {
                Debug.Log("Not enough currency");
            }
        }
        
        // Refrescar UI
        RefreshSkinButtons();
    }
    
    private void RefreshSkinButtons()
    {
        // Recrear los botones para actualizar el estado
        foreach (Transform child in contentPanel.transform)
        {
            Destroy(child.gameObject);
        }
        
        for (int i = 0; i < availableSkins.Count; i++)
        {
            CreateSkinButton(availableSkins[i], i);
        }
    }
    
    private Color GetRarityColor(SkinRarity rarity)
    {
        switch (rarity)
        {
            case SkinRarity.Common:
                return Color.gray;
            case SkinRarity.Rare:
                return Color.blue;
            case SkinRarity.Epic:
                return new Color(0.5f, 0f, 0.5f); // Púrpura
            case SkinRarity.Legendary:
                return CosmicTheme.SoftGold;
            case SkinRarity.Premium:
                return Color.cyan;
            default:
                return Color.white;
        }
    }
}

/// <summary>
/// Datos de un skin
/// </summary>
[System.Serializable]
public class SkinData
{
    public string name;
    public SkinRarity rarity;
    public int cost;
    public bool isUnlocked;
    public bool isEquipped;
    public Color color;
    public bool isPremium;
    
    public SkinData(string name, SkinRarity rarity, int cost, bool isUnlocked, Color color, bool isPremium = false)
    {
        this.name = name;
        this.rarity = rarity;
        this.cost = cost;
        this.isUnlocked = isUnlocked;
        this.isEquipped = false;
        this.color = color;
        this.isPremium = isPremium;
    }
}

/// <summary>
/// Rareza de los skins
/// </summary>
public enum SkinRarity
{
    Common,
    Rare,
    Epic,
    Legendary,
    Premium
}

