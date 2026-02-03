using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static LogHelper;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    private MainMenuController menuController;
    
    [Header("Planet Data")]
    private List<PlanetData> availablePlanets = new List<PlanetData>();
    private PlanetData currentEquippedPlanet;
    private const string SELECTED_PLANET_KEY = "SelectedPlanet";
    
    [Header("Skin Pricing")]
    private Dictionary<string, SkinPriceData> skinPrices = new Dictionary<string, SkinPriceData>();
    
    private bool isInitialized = false;
    
    private void Start()
    {
        // Solo inicializar si el GameObject está activo
        if (gameObject.activeInHierarchy && !isInitialized)
        {
            InitializeIfNeeded();
        }
    }
    
    private void OnEnable()
    {
        Log($"[SkinsSection] OnEnable llamado. isInitialized: {isInitialized}, activeInHierarchy: {gameObject.activeInHierarchy}");
        
        // Inicializar cuando se active el GameObject
        if (!isInitialized)
        {
            InitializeIfNeeded();
        }
        else if (contentPanel == null)
        {
            // Si ya estaba inicializado pero la UI fue destruida, recrearla
            Log("[SkinsSection] UI destruida, recreando...");
            InitializePlanets();
            CreateUI();
        }
        
        // Asegurar que los elementos sean visibles
        if (scrollRect != null && scrollRect.content != null)
        {
            // Forzar actualización del layout
            Canvas.ForceUpdateCanvases();
            Log($"[SkinsSection] ScrollRect verificado después de OnEnable. Content hijos: {scrollRect.content.childCount}");
        }
    }
    
    private void InitializeIfNeeded()
    {
        if (isInitialized) return;
        
        menuController = FindFirstObjectByType<MainMenuController>();
        InitializeSkinPrices();
        InitializePlanets();
        CreateUI();
        isInitialized = true;
        
        Log($"[SkinsSection] Inicialización completada. UI creada: {contentPanel != null}");
    }
    
    private void InitializeSkinPrices()
    {
        // Definir precios de skins según el diseño balanceado
        // Tier 1: Starter (gratis)
        skinPrices["AsteroideErrante"] = new SkinPriceData { isUnlocked = true, price = 0, currencyType = CurrencyType.StellarShards }; // Gratis por defecto
        
        // Tier 2-3: Comunes (⭐) - desbloqueables con Stellar Shards
        skinPrices["CristalCosmico"] = new SkinPriceData { isUnlocked = false, price = 100, currencyType = CurrencyType.StellarShards };
        skinPrices["PlanetaDeGas"] = new SkinPriceData { isUnlocked = false, price = 200, currencyType = CurrencyType.StellarShards };
        skinPrices["PlanetaDeLava"] = new SkinPriceData { isUnlocked = false, price = 350, currencyType = CurrencyType.StellarShards };
        skinPrices["PlanetaHelado"] = new SkinPriceData { isUnlocked = false, price = 500, currencyType = CurrencyType.StellarShards };
        
        // Tier 4: Premium (💎) - desbloqueables con Cosmic Crystals
        skinPrices["PlanetaOceanico"] = new SkinPriceData { isUnlocked = false, price = 50, currencyType = CurrencyType.CosmicCrystals };
        skinPrices["PlanetaEstelar"] = new SkinPriceData { isUnlocked = false, price = 100, currencyType = CurrencyType.CosmicCrystals };
        skinPrices["PlanetaNebulosa"] = new SkinPriceData { isUnlocked = false, price = 200, currencyType = CurrencyType.CosmicCrystals };
        
        // Cargar estado de desbloqueo guardado con validación
        foreach (var kvp in skinPrices)
        {
            string unlockKey = $"Skin_{kvp.Key}_Unlocked";
            if (PlayerPrefs.HasKey(unlockKey))
            {
                try
                {
                    int unlockedValue = PlayerPrefs.GetInt(unlockKey, 0);
                    // Validar que el valor sea 0 o 1 (solo valores booleanos válidos)
                    if (unlockedValue == 0 || unlockedValue == 1)
                    {
                        skinPrices[kvp.Key].isUnlocked = unlockedValue == 1;
                    }
                    else
                    {
                        LogWarning($"[SkinsSection] Valor inválido para skin {kvp.Key} ({unlockedValue}), reseteando a bloqueado");
                        skinPrices[kvp.Key].isUnlocked = false;
                        PlayerPrefs.SetInt(unlockKey, 0);
                        PlayerPrefs.Save();
                    }
                }
                catch (System.Exception e)
                {
                    LogError($"[SkinsSection] Error al cargar estado de desbloqueo para skin {kvp.Key}: {e.Message}. Reseteando a bloqueado.");
                    skinPrices[kvp.Key].isUnlocked = false;
                    PlayerPrefs.DeleteKey(unlockKey);
                    PlayerPrefs.Save();
                }
            }
        }
    }
    
    private void InitializePlanets()
    {
        // Limpiar lista anterior
        availablePlanets.Clear();
        
        // Lista de planetas disponibles
        // Nota: "PlanetaOceánico" tiene acento en el archivo, pero usamos "PlanetaOceanico" sin acento para el código
        string[] planetNames = {
            "AsteroideErrante",
            "CristalCosmico",
            "PlanetaDeGas",
            "PlanetaDeLava",
            "PlanetaHelado",
            "PlanetaOceanico",  // Sin acento en el código, pero el archivo tiene "PlanetaOceánico"
            "PlanetaEstelar",   // Nueva skin premium (sprite por defecto si no existe)
            "PlanetaNebulosa"   // Nueva skin premium (sprite por defecto si no existe)
        };
        
        // Mapeo de nombres de código a nombres reales de archivos (para caracteres especiales)
        Dictionary<string, string> planetNameMapping = new Dictionary<string, string>
        {
            { "PlanetaOceanico", "PlanetaOceánico" }  // Mapear código sin acento a archivo con acento
        };
        
        // Cargar planeta seleccionado guardado
        string savedPlanet = PlayerPrefs.GetString(SELECTED_PLANET_KEY, "AsteroideErrante");
        
        Log($"[SkinsSection] Inicializando planetas. Planeta guardado: {savedPlanet}");
        
        // Cargar sprite por defecto para usar como placeholder
        Sprite defaultSprite = LoadPlanetSprite("AsteroideErrante");
        
        // Primero intentar cargar desde la lista de nombres
        foreach (string planetName in planetNames)
        {
            // Si hay un mapeo para este nombre, intentar primero con el nombre mapeado
            string actualFileName = planetNameMapping.ContainsKey(planetName) ? planetNameMapping[planetName] : planetName;
            
            Sprite planetSprite = LoadPlanetSprite(actualFileName);
            if (planetSprite == null && actualFileName != planetName)
            {
                // Si falla con el nombre mapeado, intentar con el nombre original
                planetSprite = LoadPlanetSprite(planetName);
            }
            
            // Si no se encontró el sprite y es una de las nuevas skins premium, usar sprite por defecto
            if (planetSprite == null && (planetName == "PlanetaEstelar" || planetName == "PlanetaNebulosa"))
            {
                planetSprite = defaultSprite;
                if (planetSprite != null)
                {
                    LogWarning($"[SkinsSection] Sprite no encontrado para {planetName}, usando sprite por defecto (temporal)");
                }
            }
            
            if (planetSprite != null)
            {
                bool isEquipped = planetName == savedPlanet;
                PlanetData planet = new PlanetData(planetName, planetSprite, isEquipped);
                availablePlanets.Add(planet);
                Log($"[SkinsSection] Planeta cargado: {planetName} (archivo: {actualFileName})");
                
                if (isEquipped)
                {
                    currentEquippedPlanet = planet;
                }
            }
            else
            {
                LogWarning($"[SkinsSection] No se pudo cargar el sprite del planeta: {planetName} (intentó: {actualFileName})");
            }
        }
        
        // Si falta algún planeta (especialmente PlanetaOceánico con carácter especial), intentar cargar desde la carpeta
        if (availablePlanets.Count < planetNames.Length)
        {
            Log($"[SkinsSection] Faltan {planetNames.Length - availablePlanets.Count} planetas. Intentando cargar desde la carpeta...");
            LoadMissingPlanetsFromFolder(planetNames, savedPlanet);
        }
        
        Log($"[SkinsSection] Total de planetas cargados: {availablePlanets.Count}");
        
        // Si no hay planeta equipado, equipar el primero
        if (currentEquippedPlanet == null && availablePlanets.Count > 0)
        {
            currentEquippedPlanet = availablePlanets[0];
            currentEquippedPlanet.isEquipped = true;
            PlayerPrefs.SetString(SELECTED_PLANET_KEY, currentEquippedPlanet.name);
            PlayerPrefs.Save();
            Log($"[SkinsSection] Planeta por defecto equipado: {currentEquippedPlanet.name}");
        }
        
        if (availablePlanets.Count == 0)
        {
            LogError("[SkinsSection] ¡No se cargó ningún planeta! Verifica que los sprites estén en Resources/Art/Protagonist/ y estén configurados como Sprites en Unity.");
        }
    }
    
    private void LoadMissingPlanetsFromFolder(string[] expectedPlanetNames, string savedPlanet)
    {
        try
        {
            // Intentar cargar todos los sprites de la carpeta Resources/Art/Protagonist
            Sprite[] allSprites = ResourceLoader.LoadAll<Sprite>("Art/Protagonist");
            Log($"[SkinsSection] Encontrados {allSprites.Length} sprites en Resources/Art/Protagonist");
            
            // Crear un HashSet con los nombres de planetas ya cargados para evitar duplicados
            System.Collections.Generic.HashSet<string> loadedNames = new System.Collections.Generic.HashSet<string>();
            foreach (PlanetData planet in availablePlanets)
            {
                loadedNames.Add(planet.name);
            }
            
            // Función helper para normalizar nombres usando códigos Unicode
            System.Func<string, string> normalizeNameSimple = (name) => {
                if (string.IsNullOrEmpty(name)) return "";
                string lower = name.ToLowerInvariant();
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (char c in lower)
                {
                    int charCode = (int)c;
                    
                    // Ignorar caracteres combinados (diacríticos) - códigos 768-879
                    if (charCode >= 768 && charCode <= 879)
                    {
                        continue; // Ignorar el carácter combinado
                    }
                    
                    char normalizedChar = c;
                    if (charCode >= 224 && charCode <= 230) normalizedChar = 'a';
                    else if (charCode >= 232 && charCode <= 235) normalizedChar = 'e';
                    else if (charCode >= 236 && charCode <= 239) normalizedChar = 'i';
                    else if (charCode >= 242 && charCode <= 246) normalizedChar = 'o';
                    else if (charCode >= 249 && charCode <= 252) normalizedChar = 'u';
                    else if (charCode == 241) normalizedChar = 'n';
                    else if (charCode == 231) normalizedChar = 'c';
                    sb.Append(normalizedChar);
                }
                return sb.ToString();
            };
            
            // Crear un HashSet con los nombres normalizados ya cargados
            System.Collections.Generic.HashSet<string> loadedNormalizedNames = new System.Collections.Generic.HashSet<string>();
            foreach (PlanetData planet in availablePlanets)
            {
                loadedNormalizedNames.Add(normalizeNameSimple(planet.name));
            }
            
            foreach (Object obj in allSprites)
            {
                if (obj is Sprite sprite)
                {
                    string spriteName = sprite.name;
                    string normalizedSpriteName = normalizeNameSimple(spriteName);
                    
                    // Solo agregar si no está ya cargado (comparando nombres normalizados)
                    if (!loadedNormalizedNames.Contains(normalizedSpriteName))
                    {
                        // Verificar si el nombre coincide con alguno esperado (comparación flexible)
                        bool isExpected = false;
                        string matchingExpectedName = null;
                        
                        foreach (string expectedName in expectedPlanetNames)
                        {
                            string normalizedExpectedName = normalizeNameSimple(expectedName);
                            
                            // Comparación que ignora diferencias de mayúsculas/minúsculas y caracteres especiales
                            if (normalizedSpriteName == normalizedExpectedName)
                            {
                                isExpected = true;
                                matchingExpectedName = expectedName;
                                Log($"[SkinsSection] Match encontrado: '{spriteName}' (normalizado: '{normalizedSpriteName}') == '{expectedName}' (normalizado: '{normalizedExpectedName}')");
                                break;
                            }
                        }
                        
                        if (isExpected)
                        {
                            // Usar el nombre esperado para consistencia (no el nombre real del sprite)
                            string planetName = matchingExpectedName;
                            bool isEquipped = normalizeNameSimple(planetName) == normalizeNameSimple(savedPlanet);
                            
                            PlanetData planet = new PlanetData(planetName, sprite, isEquipped);
                            availablePlanets.Add(planet);
                            loadedNormalizedNames.Add(normalizedSpriteName);
                            Log($"[SkinsSection] ✓ Planeta cargado desde carpeta: {spriteName} -> {planetName}");
                            
                            if (isEquipped)
                            {
                                currentEquippedPlanet = planet;
                            }
                        }
                        else
                        {
                            Log($"[SkinsSection] Sprite '{spriteName}' (normalizado: '{normalizedSpriteName}') no coincide con ningún nombre esperado");
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            LogError($"[SkinsSection] Error al cargar planetas desde carpeta: {e.Message}");
        }
    }
    
    private void CreateUI()
    {
        // Verificar que el transform tenga Canvas
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            LogError("[SkinsSection] No se encontró Canvas en el padre! Los elementos UI no se mostrarán.");
        }
        else
        {
            Log($"[SkinsSection] Canvas encontrado: {parentCanvas.name}");
        }
        
        // Título con estilo del menú
        GameObject titleObj = new GameObject("PlanetsTitle");
        titleObj.transform.SetParent(transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "PLANETAS";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 56;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = CosmicTheme.NeonCyan;
        titleText.alignment = TextAnchor.UpperCenter;
        
        // Glow en el título
        Outline titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.8f);
        titleOutline.effectDistance = new Vector2(2, 2);
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -60);
        titleRect.sizeDelta = new Vector2(600, 80);
        
        Log($"[SkinsSection] Creando UI. Planetas disponibles: {availablePlanets.Count}");
        
        // Si no hay planetas, mostrar mensaje
        if (availablePlanets.Count == 0)
        {
            GameObject noPlanetsObj = new GameObject("NoPlanetsMessage");
            noPlanetsObj.transform.SetParent(transform, false);
            Text noPlanetsText = noPlanetsObj.AddComponent<Text>();
            noPlanetsText.text = "No se encontraron planetas.\nVerifica que los sprites estén en Resources/Art/Protagonist/";
            noPlanetsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            noPlanetsText.fontSize = 32;
            noPlanetsText.color = CosmicTheme.NeonCyan;
            noPlanetsText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform noPlanetsRect = noPlanetsObj.GetComponent<RectTransform>();
            noPlanetsRect.anchorMin = new Vector2(0.5f, 0.5f);
            noPlanetsRect.anchorMax = new Vector2(0.5f, 0.5f);
            noPlanetsRect.pivot = new Vector2(0.5f, 0.5f);
            noPlanetsRect.anchoredPosition = Vector2.zero;
            noPlanetsRect.sizeDelta = new Vector2(800, 200);
            return;
        }
        
        // Panel de contenido con scroll horizontal
        GameObject scrollObj = new GameObject("PlanetsScrollView");
        scrollObj.transform.SetParent(transform, false);
        RectTransform scrollRectTransform = scrollObj.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0, 0);
        scrollRectTransform.anchorMax = new Vector2(1, 1);
        scrollRectTransform.sizeDelta = new Vector2(0, -180); // Espacio para título y navegación
        scrollRectTransform.anchoredPosition = new Vector2(0, 90);
        
        this.scrollRect = scrollObj.AddComponent<ScrollRect>();
        this.scrollRect.horizontal = true;
        this.scrollRect.vertical = false;
        this.scrollRect.movementType = ScrollRect.MovementType.Elastic;
        this.scrollRect.elasticity = 0.1f;
        this.scrollRect.horizontalScrollbar = null; // Sin scrollbar por ahora
        this.scrollRect.verticalScrollbar = null;
        this.scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        this.scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        
        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;
        
        // Mask para el viewport (necesita una imagen con color para funcionar)
        Mask mask = viewport.AddComponent<Mask>();
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = new Color(0, 0, 0, 0.01f); // Casi transparente pero no completamente claro para que el Mask funcione
        mask.showMaskGraphic = false; // No mostrar el fondo del mask
        this.scrollRect.viewport = viewportRect;
        
        // Content
        contentPanel = new GameObject("Content");
        contentPanel.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        
        // Para scroll horizontal: anclar a la izquierda, centrar verticalmente
        contentRect.anchorMin = new Vector2(0, 0.5f);
        contentRect.anchorMax = new Vector2(0, 0.5f);
        contentRect.pivot = new Vector2(0, 0.5f);
        
        float cardWidth = 320f;
        float cardSpacing = 40f;
        float totalWidth = availablePlanets.Count * (cardWidth + cardSpacing) + cardSpacing;
        contentRect.sizeDelta = new Vector2(totalWidth, 500);
        // Posicionar el content para que el primer elemento sea visible desde el inicio
        contentRect.anchoredPosition = new Vector2(0, 0);
        
        this.scrollRect.content = contentRect;
        
        Log($"[SkinsSection] ScrollRect configurado. Viewport: {viewportRect.sizeDelta}, Content: {contentRect.sizeDelta}, Content pos: {contentRect.anchoredPosition}");
        
        // Crear botones de planeta
        Log($"[SkinsSection] Creando {availablePlanets.Count} botones de planeta. contentPanel: {contentPanel != null}");
        if (contentPanel == null)
        {
            LogError("[SkinsSection] contentPanel es null antes de crear botones!");
            return;
        }
        
        for (int i = 0; i < availablePlanets.Count; i++)
        {
            CreatePlanetButton(availablePlanets[i], i, cardWidth, cardSpacing);
        }
        
        Log($"[SkinsSection] Botones creados. Hijos de contentPanel: {contentPanel.transform.childCount}");
        
        // Forzar actualización del layout
        Canvas.ForceUpdateCanvases();
        
        // Verificar que el ScrollRect esté configurado
        if (this.scrollRect != null && this.scrollRect.content != null && this.scrollRect.viewport != null)
        {
            Log($"[SkinsSection] ScrollRect OK. Content size: {this.scrollRect.content.sizeDelta}, Viewport size: {this.scrollRect.viewport.sizeDelta}");
        }
        else
        {
            LogError($"[SkinsSection] ScrollRect mal configurado! content: {this.scrollRect?.content != null}, viewport: {this.scrollRect?.viewport != null}");
        }
    }
    
    private void CreatePlanetButton(PlanetData planet, int index, float cardWidth, float cardSpacing)
    {
        if (contentPanel == null)
        {
            LogError("[SkinsSection] contentPanel es null al crear botón de planeta!");
            return;
        }
        
        // Calcular posición: empezar desde el borde izquierdo del content
        float xPos = cardSpacing + index * (cardWidth + cardSpacing);
        
        GameObject planetObj = new GameObject($"Planet_{planet.name}");
        planetObj.transform.SetParent(contentPanel.transform, false);
        RectTransform planetRect = planetObj.AddComponent<RectTransform>();
        planetRect.anchorMin = new Vector2(0, 0.5f);
        planetRect.anchorMax = new Vector2(0, 0.5f);
        planetRect.pivot = new Vector2(0.5f, 0.5f);
        planetRect.anchoredPosition = new Vector2(xPos + cardWidth * 0.5f, 0);
        planetRect.sizeDelta = new Vector2(cardWidth, 480);
        
        Log($"[SkinsSection] Botón creado: {planet.name} en posición X: {planetRect.anchoredPosition.x}");
        
        // Fondo del card (placa con estilo del menú)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(planetObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        Color bgColor = new Color(CosmicTheme.SpaceBlack.r, CosmicTheme.SpaceBlack.g, CosmicTheme.SpaceBlack.b, 0.6f);
        bgImg.color = bgColor;
        bgImg.raycastTarget = false;
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Glow en el fondo si está equipado
        if (planet.isEquipped)
        {
            Outline bgOutline = bgObj.AddComponent<Outline>();
            bgOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.6f);
            bgOutline.effectDistance = new Vector2(4, 4);
        }
        else
        {
            Outline bgOutline = bgObj.AddComponent<Outline>();
            bgOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.2f);
            bgOutline.effectDistance = new Vector2(2, 2);
        }
        
        // Preview del planeta (sprite real)
        GameObject previewObj = new GameObject("Preview");
        previewObj.transform.SetParent(planetObj.transform, false);
        Image previewImg = previewObj.AddComponent<Image>();
        previewImg.sprite = planet.sprite;
        previewImg.color = Color.white;
        previewImg.preserveAspect = true;
        previewImg.raycastTarget = false;
        
        RectTransform previewRect = previewObj.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.anchoredPosition = new Vector2(0, 40);
        previewRect.sizeDelta = new Vector2(260, 260);
        
        // Glow suave en el preview
        Outline previewOutline = previewObj.AddComponent<Outline>();
        previewOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.4f);
        previewOutline.effectDistance = new Vector2(3, 3);
        
        // Nombre del planeta
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(planetObj.transform, false);
        Text nameText = nameObj.AddComponent<Text>();
        nameText.text = GetPlanetDisplayName(planet.name);
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 32;
        nameText.fontStyle = FontStyle.Bold;
        nameText.color = planet.isEquipped ? CosmicTheme.NeonCyan : CosmicTheme.SpaceWhite;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.raycastTarget = false;
        
        // Glow en el nombre
        Outline nameOutline = nameObj.AddComponent<Outline>();
        nameOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.6f);
        nameOutline.effectDistance = new Vector2(1, 1);
        
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0.5f);
        nameRect.anchorMax = new Vector2(0.5f, 0.5f);
        nameRect.pivot = new Vector2(0.5f, 0.5f);
        nameRect.anchoredPosition = new Vector2(0, -140);
        nameRect.sizeDelta = new Vector2(cardWidth - 40, 50);
        
        // Botón de acción
        GameObject btnObj = new GameObject("ActionButton");
        btnObj.transform.SetParent(planetObj.transform, false);
        Button btn = btnObj.AddComponent<Button>();
        
        // Verificar si el skin está desbloqueado
        bool isUnlocked = IsSkinUnlocked(planet.name);
        bool isEquipped = planet.isEquipped;
        
        // Fondo del botón
        Image btnImg = btnObj.AddComponent<Image>();
        if (isEquipped)
        {
            btnImg.color = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.3f);
        }
        else if (isUnlocked)
        {
            btnImg.color = new Color(CosmicTheme.SpaceBlack.r, CosmicTheme.SpaceBlack.g, CosmicTheme.SpaceBlack.b, 0.5f);
        }
        else
        {
            btnImg.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gris para bloqueado
        }
        
        // Glow en el botón
        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.5f);
        btnOutline.effectDistance = new Vector2(2, 2);
        
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0, -220);
        btnRect.sizeDelta = new Vector2(cardWidth - 60, 70);
        
        // Texto del botón
        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        Text btnText = btnTextObj.AddComponent<Text>();
        
        if (isEquipped)
        {
            btnText.text = "EQUIPADO";
        }
        else if (isUnlocked)
        {
            btnText.text = "EQUIPAR";
        }
        else
        {
            // Mostrar precio
            SkinPriceData priceData = GetSkinPrice(planet.name);
            if (priceData != null)
            {
                string currencyIcon = priceData.currencyType == CurrencyType.StellarShards ? "⭐" : "💎";
                btnText.text = $"{priceData.price} {currencyIcon}";
            }
            else
            {
                btnText.text = "BLOQUEADO";
            }
        }
        
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 28;
        btnText.fontStyle = FontStyle.Bold;
        btnText.color = isUnlocked ? CosmicTheme.NeonCyan : (GetSkinPrice(planet.name)?.currencyType == CurrencyType.CosmicCrystals ? CosmicTheme.SoftGold : CosmicTheme.NeonCyan);
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.raycastTarget = false;
        
        // Glow en el texto del botón
        Outline btnTextOutline = btnTextObj.AddComponent<Outline>();
        btnTextOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.8f);
        btnTextOutline.effectDistance = new Vector2(1, 1);
        
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        btnTextRect.anchoredPosition = Vector2.zero;
        
        // Efectos de interacción
        AddPlanetButtonEffects(btn, previewObj, bgObj);
        
        int planetIndex = index; // Capturar para el closure
        btn.onClick.AddListener(() => OnPlanetButtonClicked(planetIndex));
    }
    
    private void AddPlanetButtonEffects(Button button, GameObject preview, GameObject background)
    {
        UnityEngine.EventSystems.EventTrigger trigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }
        
        // Hover: aumentar glow
        UnityEngine.EventSystems.EventTrigger.Entry pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => {
            Outline previewOutline = preview.GetComponent<Outline>();
            if (previewOutline != null)
            {
                previewOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.7f);
                previewOutline.effectDistance = new Vector2(4, 4);
            }
        });
        trigger.triggers.Add(pointerEnter);
        
        UnityEngine.EventSystems.EventTrigger.Entry pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => {
            Outline previewOutline = preview.GetComponent<Outline>();
            if (previewOutline != null)
            {
                previewOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.4f);
                previewOutline.effectDistance = new Vector2(3, 3);
            }
        });
        trigger.triggers.Add(pointerExit);
    }
    
    private void OnPlanetButtonClicked(int index)
    {
        PlanetData planet = availablePlanets[index];
        
        // Si no está desbloqueado, intentar comprar
        if (!IsSkinUnlocked(planet.name))
        {
            SkinPriceData priceData = GetSkinPrice(planet.name);
            if (priceData != null && CurrencyManager.Instance != null)
            {
                bool purchased = false;
                
                if (priceData.currencyType == CurrencyType.StellarShards)
                {
                    purchased = CurrencyManager.Instance.SpendStellarShards(priceData.price);
                }
                else if (priceData.currencyType == CurrencyType.CosmicCrystals)
                {
                    purchased = CurrencyManager.Instance.SpendCosmicCrystals(priceData.price);
                }
                
                if (purchased)
                {
                    // Desbloquear skin
                    UnlockSkin(planet.name);
                    Log($"Skin desbloqueado: {planet.name}");
                }
                else
                {
                    LogWarning($"No tienes suficientes {(priceData.currencyType == CurrencyType.StellarShards ? "⭐" : "💎")}");
                    // Aquí podrías mostrar un mensaje al usuario
                    return;
                }
            }
            else
            {
                LogWarning($"Skin {planet.name} no tiene precio configurado");
                return;
            }
        }
        
        // Desequipar el planeta actual
        if (currentEquippedPlanet != null)
        {
            currentEquippedPlanet.isEquipped = false;
        }
        
        // Equipar el nuevo planeta
        planet.isEquipped = true;
        currentEquippedPlanet = planet;
        
        // Guardar selección
        PlayerPrefs.SetString(SELECTED_PLANET_KEY, planet.name);
        PlayerPrefs.Save();
        
        Log($"Planeta equipado: {planet.name}");
        
        // Actualizar player demo en el menú
        UpdatePlayerDemo();
        
        // Refrescar UI
        RefreshPlanetButtons();
    }
    
    private bool IsSkinUnlocked(string skinName)
    {
        if (skinPrices.ContainsKey(skinName))
        {
            return skinPrices[skinName].isUnlocked;
        }
        return false; // Por defecto bloqueado
    }
    
    private SkinPriceData GetSkinPrice(string skinName)
    {
        if (skinPrices.ContainsKey(skinName))
        {
            return skinPrices[skinName];
        }
        return null;
    }
    
    private void UnlockSkin(string skinName)
    {
        if (skinPrices.ContainsKey(skinName))
        {
            skinPrices[skinName].isUnlocked = true;
            PlayerPrefs.SetInt($"Skin_{skinName}_Unlocked", 1);
            PlayerPrefs.Save();
        }
    }
    
    private void RefreshPlanetButtons()
    {
        // Recrear los botones para actualizar el estado
        foreach (Transform child in contentPanel.transform)
        {
            Destroy(child.gameObject);
        }
        
        float cardWidth = 320f;
        float cardSpacing = 40f;
        
        for (int i = 0; i < availablePlanets.Count; i++)
        {
            CreatePlanetButton(availablePlanets[i], i, cardWidth, cardSpacing);
        }
    }
    
    private void UpdatePlayerDemo()
    {
        if (menuController != null && currentEquippedPlanet != null)
        {
            // Actualizar el sprite del player demo usando reflexión o método público
            GameObject playerDemo = GameObject.Find("PlayerDemo");
            if (playerDemo != null)
            {
                SpriteRenderer sr = playerDemo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = currentEquippedPlanet.sprite;
                }
            }
        }
    }
    
    private string GetPlanetDisplayName(string planetName)
    {
        // Convertir nombres técnicos a nombres de visualización
        switch (planetName)
        {
            case "AsteroideErrante": return "Asteroide Errante";
            case "CristalCosmico": return "Cristal Cósmico";
            case "PlanetaDeGas": return "Planeta de Gas";
            case "PlanetaDeLava": return "Planeta de Lava";
            case "PlanetaHelado": return "Planeta Helado";
            case "PlanetaOceanico": return "Planeta Oceanico";
            default: return planetName;
        }
    }
    
    /// <summary>
    /// Obtiene el tamaño de referencia del Asteroide Errante (el tamaño correcto)
    /// </summary>
    private float GetReferencePlanetSize()
    {
        // Cargar el sprite del Asteroide Errante como referencia
        Sprite referenceSprite = Resources.Load<Sprite>("Art/Protagonist/AsteroideErrante");
        if (referenceSprite != null)
        {
            // Usar bounds.size que da el tamaño real en unidades del mundo (considera el rect visible del sprite)
            float worldSize = Mathf.Max(referenceSprite.bounds.size.x, referenceSprite.bounds.size.y);
            Log($"[SkinsSection] Tamaño de referencia calculado: {worldSize} (Asteroide Errante bounds: {referenceSprite.bounds.size}, rect: {referenceSprite.rect.width}x{referenceSprite.rect.height}, PPU: {referenceSprite.pixelsPerUnit})");
            return worldSize;
        }
        
        #if UNITY_EDITOR
        // Fallback en editor
        try
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("AsteroideErrante t:Sprite");
            if (guids.Length == 0)
            {
                guids = UnityEditor.AssetDatabase.FindAssets("AsteroideErrante t:Texture2D");
            }
            
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                referenceSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (referenceSprite != null)
                {
                    float worldSize = Mathf.Max(referenceSprite.bounds.size.x, referenceSprite.bounds.size.y);
                    Log($"[SkinsSection] Tamaño de referencia calculado (Editor): {worldSize} (Asteroide Errante bounds: {referenceSprite.bounds.size}, rect: {referenceSprite.rect.width}x{referenceSprite.rect.height}, PPU: {referenceSprite.pixelsPerUnit})");
                    return worldSize;
                }
            }
        }
        catch { }
        #endif
        
        // Valor por defecto si no se encuentra (aproximado)
        LogWarning("[SkinsSection] No se pudo cargar Asteroide Errante como referencia, usando valor por defecto: 1.0");
        return 1.0f;
    }
    
    /// <summary>
    /// Normaliza un sprite para que tenga el mismo tamaño visual que el Asteroide Errante
    /// </summary>
    private Sprite NormalizePlanetSize(Sprite originalSprite, float targetWorldSize)
    {
        if (originalSprite == null || originalSprite.texture == null) return originalSprite;
        
        // Calcular el tamaño actual del sprite en unidades del mundo usando bounds (tamaño visual real)
        float currentWorldSize = Mathf.Max(originalSprite.bounds.size.x, originalSprite.bounds.size.y);
        
        Log($"[SkinsSection] Normalizando sprite '{originalSprite.name}': Tamaño actual (bounds): {currentWorldSize}, Objetivo: {targetWorldSize}, PPU actual: {originalSprite.pixelsPerUnit}, Rect: {originalSprite.rect.width}x{originalSprite.rect.height}, Bounds: {originalSprite.bounds.size}");
        
        // Si ya tiene el tamaño correcto (con un margen de error pequeño), no hacer nada
        if (Mathf.Abs(currentWorldSize - targetWorldSize) < 0.01f)
        {
            Log($"[SkinsSection] Sprite '{originalSprite.name}' ya tiene el tamaño correcto, no se normaliza");
            return originalSprite;
        }
        
        // Calcular el nuevo pixelsPerUnit para que el sprite tenga el tamaño objetivo
        // Usamos el tamaño del rect en píxeles dividido por el tamaño objetivo en unidades del mundo
        float newPixelsPerUnit = Mathf.Max(originalSprite.rect.width, originalSprite.rect.height) / targetWorldSize;
        
        Log($"[SkinsSection] Normalizando sprite '{originalSprite.name}': Nuevo PPU: {newPixelsPerUnit} (anterior: {originalSprite.pixelsPerUnit})");
        
        // Crear un nuevo sprite con el pixelsPerUnit ajustado
        return Sprite.Create(
            originalSprite.texture,
            originalSprite.rect,
            originalSprite.pivot,
            newPixelsPerUnit
        );
    }
    
    private Sprite LoadPlanetSprite(string planetName)
    {
        if (!Application.isPlaying) return null;
        
        // Obtener el tamaño de referencia del Asteroide Errante
        float referenceSize = GetReferencePlanetSize();
        
        // Usar ResourceLoader para carga segura
        string resourcePath = $"Art/Protagonist/{planetName}";
        Sprite sprite = ResourceLoader.LoadSprite(resourcePath, planetName);
        if (sprite != null && sprite.name != "DefaultSprite")
        {
            Log($"[SkinsSection] Sprite cargado desde Resources: {resourcePath} (PPU: {sprite.pixelsPerUnit})");
            return sprite;
        }
        
        // Si falla, intentar cargar todos los sprites y buscar por nombre normalizado
        // (útil para caracteres especiales como "í" en "PlanetaOceánico")
        Sprite[] allSprites = ResourceLoader.LoadAll<Sprite>("Art/Protagonist");
        Log($"[SkinsSection] Cargados {allSprites.Length} sprites desde Resources/Art/Protagonist para búsqueda normalizada");
        
        System.Func<string, string> normalizeName = (name) => {
            if (string.IsNullOrEmpty(name)) return "";
            
            // Convertir a minúsculas primero
            string lower = name.ToLowerInvariant();
            
            // Iterar sobre cada carácter y reemplazar acentos
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in lower)
            {
                int charCode = (int)c;
                
                // Ignorar caracteres combinados (diacríticos) - códigos 768-879
                if (charCode >= 768 && charCode <= 879)
                {
                    // Es un carácter combinado (acento), lo ignoramos
                    continue;
                }
                
                char normalizedChar = c;
                
                // Mapear caracteres acentuados precompuestos a sus equivalentes sin acento
                if (charCode >= 224 && charCode <= 230) // à, á, â, ã, ä, å, æ
                    normalizedChar = 'a';
                else if (charCode >= 232 && charCode <= 235) // è, é, ê, ë
                    normalizedChar = 'e';
                else if (charCode >= 236 && charCode <= 239) // ì, í, î, ï
                    normalizedChar = 'i';
                else if (charCode >= 242 && charCode <= 246) // ò, ó, ô, õ, ö
                    normalizedChar = 'o';
                else if (charCode >= 249 && charCode <= 252) // ù, ú, û, ü
                    normalizedChar = 'u';
                else if (charCode == 241) // ñ
                    normalizedChar = 'n';
                else if (charCode == 231) // ç
                    normalizedChar = 'c';
                
                sb.Append(normalizedChar);
            }
            
            return sb.ToString();
        };
        
        string normalizedPlanetName = normalizeName(planetName);
        Log($"[SkinsSection] Buscando planeta normalizado: '{normalizedPlanetName}' (original: '{planetName}')");
        
        foreach (Object obj in allSprites)
        {
            if (obj is Sprite foundSprite)
            {
                string spriteName = foundSprite.name;
                string normalizedSpriteName = normalizeName(spriteName);
                
                // Debug detallado para el sprite problemático
                if (spriteName.Contains("Oce") || spriteName.Contains("oce") || spriteName.Contains("í") || spriteName.Contains("Í"))
                {
                    Log($"[SkinsSection] DEBUG Oceánico - Original: '{spriteName}', Normalizado: '{normalizedSpriteName}', Buscado: '{normalizedPlanetName}'");
                    Log($"[SkinsSection] DEBUG - Caracteres en original: {string.Join("|", spriteName.Select(c => $"'{c}'({(int)c})"))}");
                    Log($"[SkinsSection] DEBUG - Caracteres en normalizado: {string.Join("|", normalizedSpriteName.Select(c => $"'{c}'({(int)c})"))}");
                    Log($"[SkinsSection] DEBUG - Caracteres en buscado: {string.Join("|", normalizedPlanetName.Select(c => $"'{c}'({(int)c})"))}");
                    Log($"[SkinsSection] DEBUG - Son iguales: {normalizedSpriteName == normalizedPlanetName}, Length: {normalizedSpriteName.Length} vs {normalizedPlanetName.Length}");
                }
                
                Log($"[SkinsSection] Comparando: '{normalizedSpriteName}' (original: '{spriteName}') con '{normalizedPlanetName}'");
                
                if (normalizedSpriteName == normalizedPlanetName)
                {
                    Log($"[SkinsSection] ✓ Sprite encontrado por nombre normalizado: {spriteName} (buscado: {planetName}, PPU: {foundSprite.pixelsPerUnit})");
                    // No normalizar - usar el sprite tal como está configurado en Unity
                    return foundSprite;
                }
            }
        }
        
        LogWarning($"[SkinsSection] No se encontró sprite normalizado para: {planetName}");
        
        #if UNITY_EDITOR
        // En el editor, intentar usar AssetDatabase como fallback
        try
        {
            // Función helper para normalizar nombres (misma lógica que la anterior)
            System.Func<string, string> normalizeNameEditor = (name) => {
                if (string.IsNullOrEmpty(name)) return "";
                
                string lower = name.ToLowerInvariant();
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (char c in lower)
                {
                    char normalizedChar = c;
                    switch ((int)c)
                    {
                        case 237: case 236: case 238: case 239: normalizedChar = 'i'; break;
                        case 225: case 224: case 226: case 227: case 228: normalizedChar = 'a'; break;
                        case 233: case 232: case 234: case 235: normalizedChar = 'e'; break;
                        case 243: case 242: case 244: case 245: case 246: normalizedChar = 'o'; break;
                        case 250: case 249: case 251: case 252: normalizedChar = 'u'; break;
                        case 241: normalizedChar = 'n'; break;
                    }
                    sb.Append(normalizedChar);
                }
                return sb.ToString();
            };
            
            // Buscar por nombre exacto
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{planetName} t:Sprite");
            if (guids.Length == 0)
            {
                guids = UnityEditor.AssetDatabase.FindAssets($"{planetName} t:Texture2D");
            }
            
            // Si no se encuentra, buscar todos los sprites en la carpeta y comparar por nombre normalizado
            if (guids.Length == 0)
            {
                string[] allGuids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Resources/Art/Protagonist" });
                string normalizedPlanetNameEditor = normalizeNameEditor(planetName);
                
                foreach (string guid in allGuids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    Sprite foundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (foundSprite != null)
                    {
                        string spriteName = foundSprite.name;
                        string normalizedSpriteName = normalizeNameEditor(spriteName);
                        if (normalizedSpriteName == normalizedPlanetNameEditor)
                        {
                            Log($"[SkinsSection] Sprite encontrado en AssetDatabase por nombre normalizado: {spriteName} (path: {path}, PPU: {foundSprite.pixelsPerUnit})");
                            // No normalizar - usar el sprite tal como está configurado en Unity
                            return foundSprite;
                        }
                    }
                }
            }
            
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                Log($"[SkinsSection] Encontrado en AssetDatabase: {path}");
                
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    Log($"[SkinsSection] Sprite cargado desde AssetDatabase: {path} (PPU: {sprite.pixelsPerUnit})");
                    // No normalizar - usar el sprite tal como está configurado en Unity
                    return sprite;
                }
                
                Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    Log($"[SkinsSection] Texture2D cargado desde AssetDatabase, creando sprite: {path}");
                    // Usar un pixelsPerUnit por defecto razonable (100 es común en Unity)
                    float pixelsPerUnit = 100f;
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                }
            }
            else
            {
                LogWarning($"[SkinsSection] No se encontró {planetName} en AssetDatabase");
            }
        }
        catch (System.Exception e)
        {
            LogWarning($"[SkinsSection] Error al cargar el sprite del planeta {planetName}: {e.Message}");
        }
        #endif
        
        LogWarning($"[SkinsSection] No se pudo cargar el sprite del planeta: {planetName} desde {resourcePath}");
        return null;
    }
}

/// <summary>
/// Datos de un planeta
/// </summary>
[System.Serializable]
public class PlanetData
{
    public string name;
    public Sprite sprite;
    public bool isEquipped;
    
    public PlanetData(string name, Sprite sprite, bool isEquipped = false)
    {
        this.name = name;
        this.sprite = sprite;
        this.isEquipped = isEquipped;
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
/// Tipo de moneda
/// </summary>
public enum CurrencyType
{
    StellarShards,  // ⭐ Moneda gratuita
    CosmicCrystals  // 💎 Moneda premium
}

/// <summary>
/// Datos de precio de un skin
/// </summary>
[System.Serializable]
public class SkinPriceData
{
    public bool isUnlocked;
    public int price;
    public CurrencyType currencyType;
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

