using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Controlador principal del menú - Versión completa con todas las secciones
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    private GameObject canvas;
    private GameObject playSection;
    private GameObject skinsSection;
    private GameObject shopSection;
    private GameObject missionsSection;
    private GameObject leaderboardSection;
    private SettingsPanel settingsPanel;
    
    [Header("Play Section")]
    private Text titleText;
    private GameObject playerDemo;
    private Button playButton;
    private Text playButtonText;
    
    [Header("Top Panel")]
    private Text currencyText;
    private Button currencyButton;
    private Button settingsButton;
    
    [Header("Bottom Navigation")]
    private GameObject bottomNavPanel;
    private Button skinsNavButton;
    private Button storeNavButton;
    private Button playNavButton;
    private Button missionsNavButton;
    private Button leaderboardNavButton;
    
    private MenuSection currentSection = MenuSection.Play;
    private CurrencyManager currencyManager;
    
    private void Start()
    {
        // Configurar fondo cósmico
        if (GetComponent<CosmicBackground>() == null)
        {
            gameObject.AddComponent<CosmicBackground>();
        }
        
        // Crear o encontrar CurrencyManager
        if (CurrencyManager.Instance == null)
        {
            GameObject currencyObj = new GameObject("CurrencyManager");
            currencyManager = currencyObj.AddComponent<CurrencyManager>();
        }
        else
        {
            currencyManager = CurrencyManager.Instance;
        }
        
        CreateUI();
        CreatePlayerDemo();
    }
    
    private void CreateUI()
    {
        // Create Canvas
        canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvas.AddComponent<GraphicRaycaster>();
            canvas.layer = 5; // UI layer
            
            // Asegurar que existe un EventSystem para los botones
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.anchorMin = Vector2.zero;
                canvasRect.anchorMax = Vector2.one;
                canvasRect.sizeDelta = Vector2.zero;
            }
        }
        
        // Verificar que el canvas tiene RectTransform
        if (canvas != null && canvas.GetComponent<RectTransform>() == null)
        {
            canvas.AddComponent<RectTransform>();
        }
        
        if (canvas == null)
        {
            Debug.LogError("MainMenuController: Failed to create Canvas!");
            return;
        }
        
        CreateTopPanel();
        CreatePlaySection();
        CreateSkinsSection();
        CreateShopSection();
        CreateBottomNavigation();
        
        // Inicialmente mostrar solo Play
        ShowSection(MenuSection.Play);
    }
    
    private void CreateTopPanel()
    {
        if (canvas == null)
        {
            Debug.LogError("MainMenuController: Canvas is null in CreateTopPanel!");
            return;
        }
        
        // Panel superior
        GameObject topPanel = new GameObject("TopPanel");
        topPanel.transform.SetParent(canvas.transform, false);
        RectTransform topRect = topPanel.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0, -40); // Negativo para estar dentro de la pantalla
        topRect.sizeDelta = new Vector2(0, 80);
        
        // Botón Settings (izquierda)
        GameObject settingsObj = new GameObject("SettingsButton");
        settingsObj.transform.SetParent(topPanel.transform, false);
        settingsButton = settingsObj.AddComponent<Button>();
        Image settingsImg = settingsObj.AddComponent<Image>();
        settingsImg.color = Color.clear; // Fondo transparente
        
        RectTransform settingsRect = settingsObj.GetComponent<RectTransform>();
        settingsRect.anchorMin = new Vector2(0, 0.5f);
        settingsRect.anchorMax = new Vector2(0, 0.5f);
        settingsRect.pivot = new Vector2(0.5f, 0.5f);
        settingsRect.anchoredPosition = new Vector2(50, 0);
        settingsRect.sizeDelta = new Vector2(60, 60);
        
        // Crear objeto hijo para el icono
        GameObject settingsIconObj = new GameObject("Icon");
        settingsIconObj.transform.SetParent(settingsObj.transform, false);
        Image settingsIconImg = settingsIconObj.AddComponent<Image>();
        
        // Cargar el sprite del icono
        Sprite optionsIcon = LoadOptionsIcon();
        if (optionsIcon != null)
        {
            settingsIconImg.sprite = optionsIcon;
        }
        else
        {
            // Fallback: usar un sprite simple si no se encuentra el icono
            Debug.LogWarning("No se pudo cargar OptionsIcon, usando fallback");
        }
        
        settingsIconImg.color = CosmicTheme.NeonCyan; // Color neon cian para el icono
        settingsIconImg.preserveAspect = true; // Mantener proporción del icono
        
        // Configurar para mejor calidad de renderizado
        settingsIconImg.type = Image.Type.Simple; // Tipo Simple para mejor calidad
        settingsIconImg.useSpriteMesh = false; // Desactivar mesh para mejor calidad en UI
        
        RectTransform iconRect = settingsIconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        // Icono más grande que el contenedor (120% del tamaño)
        iconRect.sizeDelta = new Vector2(72, 72); // 60 * 1.2 = 72
        
        // Añadir animación de pulsado
        AddButtonPressAnimation(settingsButton, iconRect);
        
        settingsButton.onClick.AddListener(ShowSettings);
        
        // Monedas (derecha)
        GameObject currencyObj = new GameObject("CurrencyDisplay");
        currencyObj.transform.SetParent(topPanel.transform, false);
        currencyButton = currencyObj.AddComponent<Button>();
        Image currencyImg = currencyObj.AddComponent<Image>();
        currencyImg.color = new Color(0, 0, 0, 0.3f);
        
        RectTransform currencyRect = currencyObj.GetComponent<RectTransform>();
        currencyRect.anchorMin = new Vector2(1, 0.5f);
        currencyRect.anchorMax = new Vector2(1, 0.5f);
        currencyRect.pivot = new Vector2(1f, 0.5f);
        currencyRect.anchoredPosition = new Vector2(-100, 0);
        currencyRect.sizeDelta = new Vector2(150, 60);
        
        // Crear objeto hijo para el texto
        GameObject currencyTextObj = new GameObject("Text");
        currencyTextObj.transform.SetParent(currencyObj.transform, false);
        currencyText = currencyTextObj.AddComponent<Text>();
        if (currencyText != null)
        {
            currencyText.text = "0 ⭐";
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont != null)
            {
                currencyText.font = defaultFont;
            }
            currencyText.fontSize = 24;
            currencyText.alignment = TextAnchor.MiddleCenter;
            currencyText.color = CosmicTheme.SoftGold;
            
            RectTransform textRect = currencyTextObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
        }
        
        currencyButton.onClick.AddListener(() => NavigateTo(MenuSection.Shop));
        
        // Actualizar monedas
        if (currencyManager != null)
        {
            UpdateCurrencyDisplay();
            currencyManager.OnCurrencyChanged += (amount) => UpdateCurrencyDisplay();
        }
    }
    
    private void CreatePlaySection()
    {
        // Sección Play
        playSection = new GameObject("PlaySection");
        playSection.transform.SetParent(canvas.transform, false);
        RectTransform playRect = playSection.AddComponent<RectTransform>();
        playRect.anchorMin = Vector2.zero;
        playRect.anchorMax = Vector2.one;
        playRect.sizeDelta = Vector2.zero;
        
        // Título
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(playSection.transform, false);
        titleText = titleObj.AddComponent<Text>();
        titleText.text = "STARBOUND ORBIT";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 50;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = CosmicTheme.SoftGold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0, 200); // Más arriba para dejar espacio
        titleRect.sizeDelta = new Vector2(600, 80);
        
        // Animación de pulso para el título
        StartCoroutine(PulseTitle());
        
        // Botón Play (debajo del centro) - Estilo Space Neon Minimal
        GameObject playBtnObj = new GameObject("PlayButton");
        playBtnObj.transform.SetParent(playSection.transform, false);
        
        // Añadir RectTransform primero (se añade automáticamente al añadir UI components, pero lo hacemos explícito)
        RectTransform playBtnRect = playBtnObj.AddComponent<RectTransform>();
        playBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
        playBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
        playBtnRect.pivot = new Vector2(0.5f, 0.5f);
        playBtnRect.anchoredPosition = new Vector2(0, -50);
        playBtnRect.sizeDelta = new Vector2(350, 100);
        
        playButton = playBtnObj.AddComponent<Button>();
        
        // Fondo del botón (placa circular flotante)
        GameObject plateObj = new GameObject("Plate");
        plateObj.transform.SetParent(playBtnObj.transform, false);
        Image plateImg = plateObj.AddComponent<Image>();
        plateImg.color = new Color(CosmicTheme.SpaceBlack.r, CosmicTheme.SpaceBlack.g, CosmicTheme.SpaceBlack.b, 0.5f);
        plateImg.raycastTarget = false;
        
        RectTransform plateRect = plateObj.GetComponent<RectTransform>();
        plateRect.anchorMin = Vector2.zero;
        plateRect.anchorMax = Vector2.one;
        plateRect.sizeDelta = Vector2.zero;
        plateRect.anchoredPosition = Vector2.zero;
        
        // Glow suave en la placa
        Outline plateOutline = plateObj.AddComponent<Outline>();
        plateOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.4f);
        plateOutline.effectDistance = new Vector2(3, 3);
        
        // Icono de play (usando sprite)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(playBtnObj.transform, false);
        
        // Cargar el sprite del icono de play
        Sprite playIconSprite = LoadNavIcon("PlayButton");
        if (playIconSprite != null)
        {
            // Usar Image con sprite
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = playIconSprite;
            iconImg.color = CosmicTheme.NeonCyan;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }
        else
        {
            // Fallback a emoji si no se encuentra
            Text iconText = iconObj.AddComponent<Text>();
            iconText.text = "▶";
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 50;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = CosmicTheme.NeonCyan;
            iconText.raycastTarget = false;
        }
        
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.6f);
        iconRect.anchorMax = new Vector2(0.5f, 0.6f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(350, 60);
        
        // Glow en el icono
        Outline iconOutline = iconObj.AddComponent<Outline>();
        iconOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.5f);
        iconOutline.effectDistance = new Vector2(2, 2);
        
        // Texto debajo del icono
        GameObject playTextObj = new GameObject("Text");
        playTextObj.transform.SetParent(playBtnObj.transform, false);
        playButtonText = playTextObj.AddComponent<Text>();
            playButtonText.text = "TAP TO PLAY";
        playButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        playButtonText.fontSize = 20;
            playButtonText.fontStyle = FontStyle.Bold;
            playButtonText.alignment = TextAnchor.MiddleCenter;
        playButtonText.color = CosmicTheme.SpaceWhite;
        playButtonText.raycastTarget = false;
            
            RectTransform textRect = playTextObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.1f);
        textRect.anchorMax = new Vector2(0.5f, 0.35f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(350, 30);
        
        // Añadir Image al botón para que pueda recibir clicks
        Image btnImage = playBtnObj.AddComponent<Image>();
        btnImage.color = Color.clear; // Transparente pero recibe raycasts
        btnImage.raycastTarget = true;
        
        // Añadir efectos de interacción similares a los botones de navegación
        AddPlayButtonEffects(playButton, iconObj, plateObj);
        
        playButton.onClick.AddListener(LoadGame);
    }
    
    private void AddPlayButtonEffects(Button button, GameObject icon, GameObject plate)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // Hover: aumentar glow
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => {
            Outline iconOutline = icon.GetComponent<Outline>();
            if (iconOutline != null)
            {
                iconOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.8f);
                iconOutline.effectDistance = new Vector2(4, 4);
            }
            Outline plateOutline = plate.GetComponent<Outline>();
            if (plateOutline != null)
            {
                plateOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.6f);
                plateOutline.effectDistance = new Vector2(4, 4);
            }
        });
        trigger.triggers.Add(pointerEnter);
        
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => {
            Outline iconOutline = icon.GetComponent<Outline>();
            if (iconOutline != null)
            {
                iconOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.5f);
                iconOutline.effectDistance = new Vector2(2, 2);
            }
            Outline plateOutline = plate.GetComponent<Outline>();
            if (plateOutline != null)
            {
                plateOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.4f);
                plateOutline.effectDistance = new Vector2(3, 3);
            }
        });
        trigger.triggers.Add(pointerExit);
        
        // Tap: escala y partículas
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => {
            StartCoroutine(AnimateButtonTap(icon.transform, plate.transform));
            CreateTapParticles(button.transform.position);
        });
        trigger.triggers.Add(pointerDown);
        
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => {
            StartCoroutine(AnimateButtonRelease(icon.transform, plate.transform));
        });
        trigger.triggers.Add(pointerUp);
    }
    
    private void CreateBottomNavigation()
    {
        // Panel de navegación inferior con estilo Space Neon Minimal
        bottomNavPanel = new GameObject("BottomNavigationPanel");
        bottomNavPanel.transform.SetParent(canvas.transform, false);
        RectTransform navRect = bottomNavPanel.AddComponent<RectTransform>();
        navRect.anchorMin = new Vector2(0.5f, 0f);
        navRect.anchorMax = new Vector2(0.5f, 0f);
        navRect.pivot = new Vector2(0.5f, 0f);
        navRect.anchoredPosition = new Vector2(0, 30);
        navRect.sizeDelta = new Vector2(1500, 130); // Panel más ancho y alto
        
        // Fondo con esquinas redondeadas (simulado con Image)
        Image navBg = bottomNavPanel.AddComponent<Image>();
        navBg.color = new Color(CosmicTheme.SpaceBlack.r, CosmicTheme.SpaceBlack.g, CosmicTheme.SpaceBlack.b, 0.7f);
        navBg.raycastTarget = false; // IMPORTANTE: No bloquear raycasts para que los botones funcionen
        
        // Borde luminiscente
        Outline navOutline = bottomNavPanel.AddComponent<Outline>();
        navOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.6f);
        navOutline.effectDistance = new Vector2(1, 1);
        
        // Sombra suave
        Shadow navShadow = bottomNavPanel.AddComponent<Shadow>();
        navShadow.effectColor = new Color(0, 0.3f, 0.5f, 0.4f);
        navShadow.effectDistance = new Vector2(0, -5);
        
        // Crear contenedor para los botones
        GameObject buttonsContainer = new GameObject("ButtonsContainer");
        buttonsContainer.transform.SetParent(bottomNavPanel.transform, false);
        RectTransform containerRect = buttonsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.sizeDelta = Vector2.zero;
        containerRect.anchoredPosition = Vector2.zero;
        
        // Asegurar que el contenedor no bloquee raycasts
        // No añadir Image al contenedor para que no bloquee
        
        // Crear botones en el orden: Skins, Store, Play (centro grande), Missions, Leaderboard
        float buttonSpacing = 250f; // Más espaciado para panel más ancho
        float startX = -500f; // Ajustado para panel más ancho
        float buttonSize = 90f; // Botones más grandes (crecerán a 115px cuando estén activos)
        
        // Botón Skins (todos empiezan con el mismo tamaño, el seleccionado crecerá)
        skinsNavButton = CreateBottomNavButton("SkinsButton", "Skins", buttonsContainer.transform, startX, buttonSize, false);
        skinsNavButton.onClick.AddListener(() => NavigateTo(MenuSection.Skins));
        
        // Botón Store
        storeNavButton = CreateBottomNavButton("StoreButton", "Store", buttonsContainer.transform, startX + buttonSpacing, buttonSize, false);
        storeNavButton.onClick.AddListener(() => NavigateTo(MenuSection.Shop));
        
        // Botón Play (centro, mismo tamaño inicial)
        playNavButton = CreateBottomNavButton("PlayButton", "Play", buttonsContainer.transform, 0, buttonSize, false);
        playNavButton.onClick.AddListener(() => NavigateTo(MenuSection.Play));
        
        // Botón Missions
        missionsNavButton = CreateBottomNavButton("MissionsButton", "Missions", buttonsContainer.transform, -startX - buttonSpacing, buttonSize, false);
        missionsNavButton.onClick.AddListener(() => NavigateTo(MenuSection.Missions));
        
        // Botón Leaderboard
        leaderboardNavButton = CreateBottomNavButton("LeaderboardButton", "Leaderboard", buttonsContainer.transform, -startX, buttonSize, false);
        leaderboardNavButton.onClick.AddListener(() => NavigateTo(MenuSection.Leaderboard));
        
        // Añadir partículas sutiles detrás de la barra (al final para que no bloqueen)
        // Las partículas se crean después de los botones para que estén en el fondo
        StartCoroutine(CreateNavigationParticlesDelayed());
    }
    
    private IEnumerator CreateNavigationParticlesDelayed()
    {
        // Esperar un frame para que los botones se creen primero
        yield return null;
        CreateNavigationParticles();
    }
    
    private Button CreateBottomNavButton(string name, string label, Transform parent, float xPos, float size, bool isPlayButton)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Button btn = btnObj.AddComponent<Button>();
        
        // Añadir Image al botón para que pueda recibir clicks (Button necesita un Image)
        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = Color.clear; // Transparente pero recibe raycasts
        btnImage.raycastTarget = true; // IMPORTANTE: Debe recibir raycasts para funcionar
        
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(xPos, 0);
        btnRect.sizeDelta = new Vector2(size, size);
        
        // Placa circular flotante (fondo del botón)
        GameObject plateObj = new GameObject("Plate");
        plateObj.transform.SetParent(btnObj.transform, false);
        Image plateImg = plateObj.AddComponent<Image>();
        plateImg.color = new Color(CosmicTheme.SpaceBlack.r, CosmicTheme.SpaceBlack.g, CosmicTheme.SpaceBlack.b, 0.4f);
        plateImg.raycastTarget = false; // No bloquear raycasts
        
        RectTransform plateRect = plateObj.GetComponent<RectTransform>();
        plateRect.anchorMin = Vector2.zero;
        plateRect.anchorMax = Vector2.one;
        plateRect.sizeDelta = Vector2.zero;
        plateRect.anchoredPosition = Vector2.zero;
        
        // Glow suave en la placa
        Outline plateOutline = plateObj.AddComponent<Outline>();
        plateOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.3f);
        plateOutline.effectDistance = new Vector2(2, 2);
        
        // Indicador de sección activa (línea debajo del botón)
        GameObject indicatorObj = new GameObject("Indicator");
        indicatorObj.transform.SetParent(btnObj.transform, false);
        Image indicatorImg = indicatorObj.AddComponent<Image>();
        indicatorImg.color = CosmicTheme.NeonCyan;
        indicatorImg.raycastTarget = false;
        
        RectTransform indicatorRect = indicatorObj.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 0f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0f);
        indicatorRect.pivot = new Vector2(0.5f, 0f);
        indicatorRect.anchoredPosition = new Vector2(0, -size * 0.6f);
        indicatorRect.sizeDelta = new Vector2(size * 0.6f, 3);
        
        // Inicialmente oculto, se mostrará cuando el botón esté activo
        indicatorObj.SetActive(false);
        
        // Icono (usando sprite de imagen)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        
        // Cargar el sprite del icono según el botón
        Sprite iconSprite = LoadNavIcon(name);
        if (iconSprite != null)
        {
            // Usar Image con sprite
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.color = CosmicTheme.NeonCyan;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false; // No bloquear raycasts
        }
        else
        {
            // Fallback a emoji si no se encuentra el icono
            Text iconText = iconObj.AddComponent<Text>();
            string iconSymbol = "●";
            switch (name)
            {
                case "SkinsButton": iconSymbol = "🎨"; break;
                case "StoreButton": iconSymbol = "🛒"; break;
                case "PlayButton": iconSymbol = "▶"; break;
                case "MissionsButton": iconSymbol = "🏆"; break;
                case "LeaderboardButton": iconSymbol = "📊"; break;
            }
            iconText.text = iconSymbol;
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 50; // Iconos más grandes
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = CosmicTheme.NeonCyan;
            iconText.raycastTarget = false;
        }
        
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.6f);
        iconRect.anchorMax = new Vector2(0.5f, 0.6f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(size, size * 0.6f);
        
        // Glow mínimo en el icono
        Outline iconOutline = iconObj.AddComponent<Outline>();
        iconOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.4f);
        iconOutline.effectDistance = new Vector2(1, 1);
        
        // Texto debajo del icono
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 16; // Texto más grande
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = CosmicTheme.SpaceWhite;
        labelText.raycastTarget = false; // No bloquear raycasts
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.1f);
        labelRect.anchorMax = new Vector2(0.5f, 0.3f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(size, size * 0.2f);
        
        // Añadir efectos de interacción
        AddNavigationButtonEffects(btn, iconObj, plateObj);
        
        return btn;
    }
    
    private void AddNavigationButtonEffects(Button button, GameObject icon, GameObject plate)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // Hover/Tap: aumentar glow
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => {
            Outline iconOutline = icon.GetComponent<Outline>();
            if (iconOutline != null)
            {
                iconOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.7f);
                iconOutline.effectDistance = new Vector2(3, 3);
            }
            Outline plateOutline = plate.GetComponent<Outline>();
            if (plateOutline != null)
            {
                plateOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.5f);
            }
        });
        trigger.triggers.Add(pointerEnter);
        
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => {
            Outline iconOutline = icon.GetComponent<Outline>();
            if (iconOutline != null)
            {
                iconOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.4f);
                iconOutline.effectDistance = new Vector2(1, 1);
            }
            Outline plateOutline = plate.GetComponent<Outline>();
            if (plateOutline != null)
            {
                plateOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.3f);
            }
        });
        trigger.triggers.Add(pointerExit);
        
        // Tap: escala y partículas
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => {
            StartCoroutine(AnimateButtonTap(icon.transform, plate.transform));
            CreateTapParticles(button.transform.position);
        });
        trigger.triggers.Add(pointerDown);
        
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => {
            StartCoroutine(AnimateButtonRelease(icon.transform, plate.transform));
        });
        trigger.triggers.Add(pointerUp);
    }
    
    private IEnumerator AnimateButtonTap(Transform icon, Transform plate)
    {
        Vector3 targetScale = Vector3.one * 1.05f;
        float duration = 0.1f;
        float elapsed = 0f;
        Vector3 startScale = icon.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            icon.localScale = Vector3.Lerp(startScale, targetScale, t);
            plate.localScale = Vector3.Lerp(Vector3.one, targetScale * 0.98f, t);
            yield return null;
        }
        
        icon.localScale = targetScale;
        plate.localScale = targetScale * 0.98f;
    }
    
    private IEnumerator AnimateButtonRelease(Transform icon, Transform plate)
    {
        Vector3 targetScale = Vector3.one;
        float duration = 0.15f;
        float elapsed = 0f;
        Vector3 startScale = icon.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); // Ease out
            icon.localScale = Vector3.Lerp(startScale, targetScale, easeT);
            plate.localScale = Vector3.Lerp(plate.localScale, Vector3.one, easeT);
            yield return null;
        }
        
        icon.localScale = targetScale;
        plate.localScale = Vector3.one;
    }
    
    private void CreateTapParticles(Vector3 position)
    {
        // Crear partículas cian que salen durante 0.2s
        for (int i = 0; i < 5; i++)
        {
            GameObject particle = new GameObject("TapParticle");
            particle.transform.SetParent(canvas.transform, false);
            particle.transform.position = position;
            
            Image particleImg = particle.AddComponent<Image>();
            particleImg.color = CosmicTheme.NeonCyan;
            particleImg.sprite = SpriteGenerator.CreateStarSprite(0.1f, CosmicTheme.NeonCyan);
            particleImg.raycastTarget = false; // IMPORTANTE: No bloquear raycasts
            
            RectTransform particleRect = particle.GetComponent<RectTransform>();
            particleRect.sizeDelta = new Vector2(8, 8);
            
            StartCoroutine(AnimateParticle(particle, position));
        }
    }
    
    private IEnumerator AnimateParticle(GameObject particle, Vector3 startPos)
    {
        RectTransform rect = particle.GetComponent<RectTransform>();
        Image img = particle.GetComponent<Image>();
        
        // Asegurar que la partícula no bloquee raycasts
        if (img != null)
        {
            img.raycastTarget = false;
        }
        
        Vector2 direction = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1f)).normalized;
        float speed = 100f;
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            rect.anchoredPosition = startPos + (Vector3)(direction * speed * t);
            
            Color c = img.color;
            c.a = 1f - t;
            img.color = c;
            
            rect.localScale = Vector3.one * (1f - t * 0.5f);
            
            yield return null;
        }
        
        Destroy(particle);
    }
    
    private void CreateNavigationParticles()
    {
        // Crear 2-5 partículas sutiles detrás de la barra
        // IMPORTANTE: Estas partículas deben estar DETRÁS de los botones en la jerarquía
        // para que no bloqueen los clicks, incluso si se mueven
        for (int i = 0; i < 3; i++)
        {
            GameObject particle = new GameObject($"NavParticle_{i}");
            // Añadir al fondo del panel, NO al contenedor de botones
            particle.transform.SetParent(bottomNavPanel.transform, false);
            
            // Asegurar que la partícula esté al principio de la jerarquía (detrás)
            particle.transform.SetAsFirstSibling();
            
            Image particleImg = particle.AddComponent<Image>();
            particleImg.color = new Color(1, 1, 1, 0.3f);
            particleImg.sprite = SpriteGenerator.CreateStarSprite(0.15f, Color.white);
            particleImg.raycastTarget = false; // IMPORTANTE: No bloquear raycasts
            
            // Asegurar que el CanvasGroup no bloquee (si existe)
            CanvasGroup canvasGroup = particle.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = particle.AddComponent<CanvasGroup>();
            }
            canvasGroup.blocksRaycasts = false;
            canvasGroup.ignoreParentGroups = true;
            
            RectTransform particleRect = particle.GetComponent<RectTransform>();
            particleRect.anchorMin = new Vector2(Random.Range(0.1f, 0.9f), 0.5f);
            particleRect.anchorMax = new Vector2(Random.Range(0.1f, 0.9f), 0.5f);
            particleRect.pivot = new Vector2(0.5f, 0.5f);
            particleRect.sizeDelta = new Vector2(6, 6);
            
            StartCoroutine(AnimateNavigationParticle(particle));
        }
    }
    
    private IEnumerator AnimateNavigationParticle(GameObject particle)
    {
        RectTransform rect = particle.GetComponent<RectTransform>();
        Image img = particle.GetComponent<Image>();
        
        // Asegurar que la partícula no bloquee raycasts
        if (img != null)
        {
            img.raycastTarget = false;
        }
        
        float floatSpeed = Random.Range(0.3f, 0.6f);
        float floatRange = Random.Range(5f, 15f);
        float startY = rect.anchoredPosition.y;
        
        while (particle != null)
        {
            float time = Time.time * floatSpeed;
            float newY = startY + Mathf.Sin(time) * floatRange;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, newY);
            
            // Fade sutil
            float alpha = 0.2f + Mathf.Sin(time * 0.7f) * 0.1f;
            Color c = img.color;
            c.a = alpha;
            img.color = c;
            
            yield return null;
        }
    }
    
    private void CreateMissionsSection()
    {
        missionsSection = new GameObject("MissionsSection");
        missionsSection.transform.SetParent(canvas.transform, false);
        RectTransform missionsRect = missionsSection.AddComponent<RectTransform>();
        missionsRect.anchorMin = Vector2.zero;
        missionsRect.anchorMax = Vector2.one;
        missionsRect.sizeDelta = Vector2.zero;
        
        // Placeholder para Missions
        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(missionsSection.transform, false);
        Text placeholderText = placeholder.AddComponent<Text>();
        placeholderText.text = "Missions / Challenges\n\nPróximamente...";
        placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholderText.fontSize = 32;
        placeholderText.color = CosmicTheme.NeonCyan;
        placeholderText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = new Vector2(0.5f, 0.5f);
        placeholderRect.anchorMax = new Vector2(0.5f, 0.5f);
        placeholderRect.pivot = new Vector2(0.5f, 0.5f);
        placeholderRect.anchoredPosition = Vector2.zero;
        placeholderRect.sizeDelta = new Vector2(600, 200);
        
        missionsSection.SetActive(false);
    }
    
    private void CreateLeaderboardSection()
    {
        leaderboardSection = new GameObject("LeaderboardSection");
        leaderboardSection.transform.SetParent(canvas.transform, false);
        RectTransform leaderboardRect = leaderboardSection.AddComponent<RectTransform>();
        leaderboardRect.anchorMin = Vector2.zero;
        leaderboardRect.anchorMax = Vector2.one;
        leaderboardRect.sizeDelta = Vector2.zero;
        
        // Placeholder para Leaderboard
        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(leaderboardSection.transform, false);
        Text placeholderText = placeholder.AddComponent<Text>();
        placeholderText.text = "Leaderboard\n\nPróximamente...";
        placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholderText.fontSize = 32;
        placeholderText.color = CosmicTheme.NeonCyan;
        placeholderText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = new Vector2(0.5f, 0.5f);
        placeholderRect.anchorMax = new Vector2(0.5f, 0.5f);
        placeholderRect.pivot = new Vector2(0.5f, 0.5f);
        placeholderRect.anchoredPosition = Vector2.zero;
        placeholderRect.sizeDelta = new Vector2(600, 200);
        
        leaderboardSection.SetActive(false);
    }
    
    private void CreatePlayerDemo()
    {
        // Crear un player demo orbitando en el centro
        GameObject center = new GameObject("MenuCenter");
        center.transform.position = Vector3.zero;
        
        GameObject player = new GameObject("PlayerDemo");
        player.transform.position = new Vector3(2, 0, 0);
        player.transform.localScale = Vector3.one * 0.64f; // Más pequeño para el asteroide
        
        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = LoadPlayerSprite();
        if (sr.sprite == null)
        {
            // Fallback a estrella si no se encuentra el sprite
            sr.sprite = SpriteGenerator.CreateStarSprite(0.3f, CosmicTheme.EtherealLila);
            sr.color = CosmicTheme.EtherealLila;
        }
        else
        {
            sr.color = Color.white; // Color blanco para mantener los colores originales del sprite
        }
        sr.sortingOrder = 5;
        
        PlayerOrbit orbit = player.AddComponent<PlayerOrbit>();
        orbit.radius = 2f;
        orbit.angle = 0f;
        orbit.angularSpeed = 1f; // Más lento que en el juego
        orbit.center = center.transform;
        
        // PlanetSurface - Rotación de superficie del planeta
        PlanetSurface planetSurface = player.AddComponent<PlanetSurface>();
        planetSurface.rotationSpeed = 20f; // Misma velocidad que en el juego
        
        playerDemo = player;
    }
    
    private void CreateSkinsSection()
    {
        // Crear sección de Skins
        GameObject skinsObj = new GameObject("SkinsSection");
        skinsObj.transform.SetParent(canvas.transform, false);
        RectTransform skinsRect = skinsObj.AddComponent<RectTransform>();
        skinsRect.anchorMin = Vector2.zero;
        skinsRect.anchorMax = Vector2.one;
        skinsRect.sizeDelta = Vector2.zero;
        skinsSection = skinsObj;
        
        // Añadir componente SkinsSection
        SkinsSection skinsComponent = skinsObj.AddComponent<SkinsSection>();
        skinsSection.SetActive(false);
    }
    
    private void CreateShopSection()
    {
        // Crear sección de Shop
        GameObject shopObj = new GameObject("ShopSection");
        shopObj.transform.SetParent(canvas.transform, false);
        RectTransform shopRect = shopObj.AddComponent<RectTransform>();
        shopRect.anchorMin = Vector2.zero;
        shopRect.anchorMax = Vector2.one;
        shopRect.sizeDelta = Vector2.zero;
        shopSection = shopObj;
        
        // Añadir componente ShopSection
        ShopSection shopComponent = shopObj.AddComponent<ShopSection>();
        shopSection.SetActive(false);
    }
    
    private void ShowSection(MenuSection section)
    {
        currentSection = section;
        
        if (playSection != null) playSection.SetActive(section == MenuSection.Play);
        if (skinsSection != null) skinsSection.SetActive(section == MenuSection.Skins);
        if (shopSection != null) shopSection.SetActive(section == MenuSection.Shop);
        if (missionsSection != null) missionsSection.SetActive(section == MenuSection.Missions);
        if (leaderboardSection != null) leaderboardSection.SetActive(section == MenuSection.Leaderboard);
        
        // Actualizar estado visual de los botones de navegación
        UpdateNavigationButtons(section);
    }
    
    private void UpdateNavigationButtons(MenuSection activeSection)
    {
        // Actualizar el estado visual de cada botón según la sección activa
        SetButtonActive(skinsNavButton, activeSection == MenuSection.Skins);
        SetButtonActive(storeNavButton, activeSection == MenuSection.Shop);
        SetButtonActive(playNavButton, activeSection == MenuSection.Play);
        SetButtonActive(missionsNavButton, activeSection == MenuSection.Missions);
        SetButtonActive(leaderboardNavButton, activeSection == MenuSection.Leaderboard);
    }
    
    private void SetButtonActive(Button button, bool isActive)
    {
        if (button == null) return;
        
        RectTransform btnRect = button.GetComponent<RectTransform>();
        float targetSize = isActive ? 115f : 90f; // El botón activo es más grande
        
        // Cambiar el glow y escala del icono según si está activo
        Transform iconTransform = button.transform.Find("Icon");
        if (iconTransform != null)
        {
            Outline outline = iconTransform.GetComponent<Outline>();
            if (outline == null)
            {
                outline = iconTransform.gameObject.AddComponent<Outline>();
            }
            
            if (isActive)
            {
                outline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.8f);
                outline.effectDistance = new Vector2(3, 3);
                iconTransform.localScale = Vector3.one * 1.1f;
            }
            else
            {
                outline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.3f);
                outline.effectDistance = new Vector2(2, 2);
                iconTransform.localScale = Vector3.one;
            }
        }
        
        // Mostrar/ocultar indicador de sección activa
        Transform indicatorTransform = button.transform.Find("Indicator");
        RectTransform indicatorRect = indicatorTransform != null ? indicatorTransform.GetComponent<RectTransform>() : null;
        
        if (indicatorTransform != null)
        {
            indicatorTransform.gameObject.SetActive(isActive);
        }
        
        // Animar el cambio de tamaño del botón y actualizar el indicador
        if (btnRect != null)
        {
            StartCoroutine(AnimateButtonSize(btnRect, targetSize, indicatorRect, isActive));
        }
    }
    
    private IEnumerator AnimateButtonSize(RectTransform btnRect, float targetSize, RectTransform indicatorRect, bool isActive)
    {
        float currentSize = btnRect.sizeDelta.x;
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
            
            float newSize = Mathf.Lerp(currentSize, targetSize, easeT);
            btnRect.sizeDelta = new Vector2(newSize, newSize);
            
            // Actualizar el indicador durante la animación
            if (indicatorRect != null && isActive)
            {
                indicatorRect.sizeDelta = new Vector2(newSize * 0.6f, 3);
                indicatorRect.anchoredPosition = new Vector2(0, -newSize * 0.6f);
            }
            
            yield return null;
        }
        
        btnRect.sizeDelta = new Vector2(targetSize, targetSize);
        
        // Asegurar que el indicador tenga el tamaño final correcto
        if (indicatorRect != null && isActive)
        {
            indicatorRect.sizeDelta = new Vector2(targetSize * 0.6f, 3);
            indicatorRect.anchoredPosition = new Vector2(0, -targetSize * 0.6f);
        }
    }
    
    public void NavigateTo(MenuSection section)
    {
        ShowSection(section);
    }
    
    private void ShowSettings()
    {
        if (settingsPanel == null)
        {
            GameObject settingsObj = new GameObject("SettingsPanel");
            settingsPanel = settingsObj.AddComponent<SettingsPanel>();
        }
        settingsPanel.Show();
    }
    
    private void AddButtonPressAnimation(Button button, RectTransform iconRect)
    {
        // Crear EventTrigger para detectar cuando se presiona y suelta el botón
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // Evento: PointerDown (cuando se presiona)
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => {
            StartCoroutine(AnimateButtonPress(iconRect, true));
        });
        trigger.triggers.Add(pointerDown);
        
        // Evento: PointerUp (cuando se suelta)
        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => {
            StartCoroutine(AnimateButtonPress(iconRect, false));
        });
        trigger.triggers.Add(pointerUp);
        
        // Evento: PointerExit (si el mouse sale del botón mientras está presionado)
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => {
            StartCoroutine(AnimateButtonPress(iconRect, false));
        });
        trigger.triggers.Add(pointerExit);
    }
    
    private IEnumerator AnimateButtonPress(RectTransform iconRect, bool isPressed)
    {
        Vector3 targetScale = isPressed ? Vector3.one * 0.85f : Vector3.one;
        Vector3 currentScale = iconRect.localScale;
        float duration = 0.1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease out para suavidad
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            iconRect.localScale = Vector3.Lerp(currentScale, targetScale, easeT);
            yield return null;
        }
        
        iconRect.localScale = targetScale;
    }
    
    private void UpdateCurrencyDisplay()
    {
        if (currencyText != null && currencyManager != null)
        {
            currencyText.text = $"{currencyManager.CurrentCurrency} ⭐";
        }
    }
    
    private IEnumerator PulseTitle()
    {
        while (true)
        {
            float time = 0f;
            float duration = 2f;
            float minScale = 0.98f;
            float maxScale = 1.02f;
            
            while (time < duration)
            {
                time += Time.deltaTime;
                float scale = Mathf.Lerp(minScale, maxScale, Mathf.Sin(time / duration * Mathf.PI));
                if (titleText != null)
                {
                    titleText.transform.localScale = Vector3.one * scale;
                }
                yield return null;
            }
        }
    }
    
    private IEnumerator PulseButton(RectTransform buttonRect)
    {
        while (true)
        {
            float time = 0f;
            float duration = 1.5f;
            float minScale = 0.97f;
            float maxScale = 1.03f;
            
            while (time < duration)
            {
                time += Time.deltaTime;
                float scale = Mathf.Lerp(minScale, maxScale, Mathf.Sin(time / duration * Mathf.PI));
                if (buttonRect != null)
                {
                    buttonRect.localScale = Vector3.one * scale;
                }
                yield return null;
            }
        }
    }
    
    public void LoadGame()
    {
        SceneManager.LoadScene("Game");
    }
    
    #if UNITY_EDITOR
    private Sprite LoadPlayerSprite()
    {
        if (!Application.isPlaying) return null;
        
        try
        {
            // Buscar el sprite del asteroide errante
            string[] guids = AssetDatabase.FindAssets("AsteroideErrante t:Sprite");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
                
                // Si no se encuentra como Sprite, intentar como Texture2D
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    // Crear sprite desde texture
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo cargar el sprite del asteroide: {e.Message}");
        }
        return null;
    }
    private Sprite LoadOptionsIcon()
    {
        if (!Application.isPlaying) return null;
        
        try
        {
            // Buscar el sprite del icono de opciones
            string[] guids = AssetDatabase.FindAssets("OptionsIcon t:Sprite");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
                
                // Si no se encuentra como Sprite, intentar como Texture2D
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    // Crear sprite desde texture
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo cargar el icono de opciones: {e.Message}");
        }
        return null;
    }
    
    private Sprite LoadNavIcon(string buttonName)
    {
        if (!Application.isPlaying) return null;
        
        try
        {
            // Determinar el nombre del icono según el botón
            string iconName = "";
            switch (buttonName)
            {
                case "SkinsButton": iconName = "SkinIcon"; break;
                case "StoreButton": iconName = "StoreIcon"; break;
                case "PlayButton": iconName = "PlayIcon"; break;
                case "MissionsButton": iconName = "MissionsIcon"; break;
                case "LeaderboardButton": iconName = "LeaderboardIcon"; break;
                default: return null;
            }
            
            if (string.IsNullOrEmpty(iconName)) return null;
            
            // Buscar el sprite del icono
            string[] guids = AssetDatabase.FindAssets(iconName + " t:Sprite");
            if (guids.Length == 0)
            {
                // Intentar como Texture2D
                guids = AssetDatabase.FindAssets(iconName + " t:Texture2D");
            }
            
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
                
                // Si no se encuentra como Sprite, intentar como Texture2D
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    // Crear sprite desde texture
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo cargar el icono {buttonName}: {e.Message}");
        }
        return null;
    }
    #else
    private Sprite LoadPlayerSprite()
    {
        // En build, intentar cargar desde Resources
        return Resources.Load<Sprite>("Art/Protagonist/AsteroideErrante");
    }
    
    private Sprite LoadOptionsIcon()
    {
        // En build, intentar cargar desde Resources
        return Resources.Load<Sprite>("Art/Icons/OptionsIcon");
    }
    
    private Sprite LoadNavIcon(string buttonName)
    {
        if (!Application.isPlaying) return null;
        
        // Determinar el nombre del icono según el botón
        string iconName = "";
        switch (buttonName)
        {
            case "SkinsButton": iconName = "SkinIcon"; break;
            case "StoreButton": iconName = "StoreIcon"; break;
            case "PlayButton": iconName = "PlayIcon"; break;
            case "MissionsButton": iconName = "MissionsIcon"; break;
            case "LeaderboardButton": iconName = "LeaderboardIcon"; break;
            default: return null;
        }
        
        if (string.IsNullOrEmpty(iconName)) return null;
        
        // En build, intentar cargar desde Resources
        return Resources.Load<Sprite>($"Art/Icons/{iconName}");
    }
    #endif
}
