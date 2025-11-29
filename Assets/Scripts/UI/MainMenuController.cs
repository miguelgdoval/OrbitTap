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
        // Forzar orientación horizontal (landscape) en móviles
        ConfigureScreenOrientation();
        
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
    
    private void ConfigureScreenOrientation()
    {
        // Configurar orientación solo para móviles
        #if UNITY_ANDROID || UNITY_IOS
        // Permitir solo rotaciones horizontales
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        
        // Forzar orientación horizontal
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        #endif
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
        topRect.sizeDelta = new Vector2(0, 160); // Duplicado de 80
        
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
        settingsRect.anchoredPosition = new Vector2(100, 0); // Duplicado de 50
        settingsRect.sizeDelta = new Vector2(120, 120); // Duplicado de 60
        
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
        iconRect.sizeDelta = new Vector2(144, 144); // Duplicado de 72
        
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
        currencyRect.anchoredPosition = new Vector2(-200, 0); // Duplicado de -100
        currencyRect.sizeDelta = new Vector2(300, 120); // Duplicado de 150x60
        
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
            currencyText.fontSize = 48; // Duplicado de 24
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
        
        // Título con estética AstroNeon
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(playSection.transform, false);
        titleText = titleObj.AddComponent<Text>();
        
        // Tracking amplio (espaciado entre letras) - simulado con espacios
        titleText.text = "S T A R B O U N D   O R B I T";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 72; // Más grande para impacto visual
        titleText.fontStyle = FontStyle.Bold;
        
        // Color principal: degradado cian-violeta (usamos cian brillante como base)
        titleText.color = new Color(0.2f, 0.9f, 1f, 1f); // Cian brillante
        
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0, 200); // Más arriba para dejar espacio
        titleRect.sizeDelta = new Vector2(1000, 100); // Más ancho para el tracking amplio
        
        // Stroke externo fino cian brillante
        Outline titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f, 0.9f, 1f, 0.8f); // Cian brillante
        titleOutline.effectDistance = new Vector2(2, 2);
        
        // Glow suave alrededor de las letras (usando Shadow para efecto de glow)
        Shadow titleGlow = titleObj.AddComponent<Shadow>();
        titleGlow.effectColor = new Color(0.2f, 0.7f, 1f, 0.4f); // Cian suave para glow
        titleGlow.effectDistance = new Vector2(0, 0);
        
        // Fondo translúcido opcional (glow muy leve detrás)
        GameObject titleBg = new GameObject("TitleBackground");
        titleBg.transform.SetParent(titleObj.transform, false);
        titleBg.transform.SetAsFirstSibling(); // Detrás del texto
        Image bgImg = titleBg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.3f, 0.5f, 0.15f); // Glow azul muy suave
        bgImg.raycastTarget = false;
        
        RectTransform bgRect = titleBg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = new Vector2(50, 30); // Más grande que el texto para el glow
        bgRect.anchoredPosition = Vector2.zero;
        
        // Animación de entrada inicial
        titleObj.transform.localScale = Vector3.one * 0.92f;
        CanvasGroup titleCanvasGroup = titleObj.AddComponent<CanvasGroup>();
        titleCanvasGroup.alpha = 0f;
        StartCoroutine(AnimateTitleEntry(titleObj.transform, titleCanvasGroup));
        
        // Animación idle (pulsación lenta)
        StartCoroutine(PulseTitle());
        
        // Partículas ascendiendo detrás del título
        StartCoroutine(CreateTitleParticles(titleObj.transform));
        
        // Botón Play (debajo del centro) - Estilo Space Neon Minimal
        GameObject playBtnObj = new GameObject("PlayButton");
        playBtnObj.transform.SetParent(playSection.transform, false);
        
        // Añadir RectTransform primero (se añade automáticamente al añadir UI components, pero lo hacemos explícito)
        RectTransform playBtnRect = playBtnObj.AddComponent<RectTransform>();
        playBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
        playBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
        playBtnRect.pivot = new Vector2(0.5f, 0.5f);
        playBtnRect.anchoredPosition = new Vector2(0, -50);
        playBtnRect.sizeDelta = new Vector2(700, 200); // Duplicado de 350x100
        
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
        plateOutline.effectDistance = new Vector2(6, 6); // Duplicado de 3
        
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
            iconText.fontSize = 100; // Duplicado de 50
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.color = CosmicTheme.NeonCyan;
            iconText.raycastTarget = false;
        }
        
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.6f);
        iconRect.anchorMax = new Vector2(0.5f, 0.6f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(700, 120); // Duplicado de 350x60
        
        // Glow en el icono
        Outline iconOutline = iconObj.AddComponent<Outline>();
        iconOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.5f);
        iconOutline.effectDistance = new Vector2(4, 4); // Duplicado de 2
        
        // Texto debajo del icono
        GameObject playTextObj = new GameObject("Text");
        playTextObj.transform.SetParent(playBtnObj.transform, false);
        playButtonText = playTextObj.AddComponent<Text>();
            playButtonText.text = "TAP TO PLAY";
        playButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        playButtonText.fontSize = 40; // Duplicado de 20
            playButtonText.fontStyle = FontStyle.Bold;
            playButtonText.alignment = TextAnchor.MiddleCenter;
        playButtonText.color = CosmicTheme.SpaceWhite;
        playButtonText.raycastTarget = false;
            
            RectTransform textRect = playTextObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.1f);
        textRect.anchorMax = new Vector2(0.5f, 0.35f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(700, 60); // Duplicado de 350x30
        
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
        navRect.anchorMin = new Vector2(0f, 0f); // Anclar a la esquina inferior izquierda
        navRect.anchorMax = new Vector2(1f, 0f); // Anclar a la esquina inferior derecha (full width)
        navRect.pivot = new Vector2(0.5f, 0f);
        navRect.anchoredPosition = new Vector2(0, 30);
        navRect.sizeDelta = new Vector2(0, 220); // Altura reducida para móvil (era 260)
        
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
        // Distribución uniforme para pantallas móviles (5 botones)
        float buttonSize = 260f; // Aumentado de 220f (botones aún más grandes)
        
        // Espaciado aumentado
        // Para 5 botones, distribuirlos uniformemente: -2x, -x, 0, x, 2x
        float buttonSpacing = 280f; // Aumentado de 240f (más espacio entre botones)
        float startOffset = -buttonSpacing * 2f; // -560 para el primer botón
        
        // Botón Skins (izquierda)
        skinsNavButton = CreateBottomNavButton("SkinsButton", "Skins", buttonsContainer.transform, startOffset, buttonSize, false);
        skinsNavButton.onClick.AddListener(() => NavigateTo(MenuSection.Skins));
        
        // Botón Store
        storeNavButton = CreateBottomNavButton("StoreButton", "Store", buttonsContainer.transform, startOffset + buttonSpacing, buttonSize, false);
        storeNavButton.onClick.AddListener(() => NavigateTo(MenuSection.Shop));
        
        // Botón Play (centro)
        playNavButton = CreateBottomNavButton("PlayButton", "Play", buttonsContainer.transform, 0, buttonSize, false);
        playNavButton.onClick.AddListener(() => NavigateTo(MenuSection.Play));
        
        // Botón Missions
        missionsNavButton = CreateBottomNavButton("MissionsButton", "Missions", buttonsContainer.transform, -startOffset - buttonSpacing, buttonSize, false);
        missionsNavButton.onClick.AddListener(() => NavigateTo(MenuSection.Missions));
        
        // Botón Leaderboard (derecha)
        leaderboardNavButton = CreateBottomNavButton("LeaderboardButton", "Leaderboard", buttonsContainer.transform, -startOffset, buttonSize, false);
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
        plateOutline.effectDistance = new Vector2(4, 4); // Duplicado de 2 para mejor visibilidad en móvil
        
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
        indicatorRect.sizeDelta = new Vector2(size * 0.6f, 6); // Duplicado de 3
        
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
            iconText.fontSize = 100; // Duplicado de 50 para móvil
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
        iconOutline.effectDistance = new Vector2(2, 2); // Duplicado de 1 para mejor visibilidad en móvil
        
        // Texto debajo del icono (más abajo para no superponerse)
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 32; // Duplicado de 16 para móvil
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = CosmicTheme.SpaceWhite;
        labelText.raycastTarget = false; // No bloquear raycasts
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f); // Movido aún más abajo (era 0.2f)
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
        
        // PlanetIdleAnimator - Animación idle del planeta (rotación, breathing, glow)
        PlanetIdleAnimator idleAnimator = player.AddComponent<PlanetIdleAnimator>();
        idleAnimator.rotationSpeed = 12f; // Rotación suave del planeta (más lenta en menú)
        idleAnimator.scaleAmplitude = 0.03f; // Breathing effect sutil
        idleAnimator.scaleFrequency = 0.4f; // Más lento: ciclo completo cada ~2.5 segundos
        idleAnimator.glowAmplitude = 0.15f; // Glow animado
        idleAnimator.glowFrequency = 1.2f;
        
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
    
    private IEnumerator AnimateTitleEntry(Transform titleTransform, CanvasGroup canvasGroup)
    {
        // Fade in + scale from 0.92 → 1 (0.4s)
        float duration = 0.4f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.92f;
        Vector3 endScale = Vector3.one;
        float startAlpha = 0f;
        float endAlpha = 1f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease out cubic para suavidad
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            
            titleTransform.localScale = Vector3.Lerp(startScale, endScale, easeT);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easeT);
            
            yield return null;
        }
        
        titleTransform.localScale = endScale;
        canvasGroup.alpha = endAlpha;
    }
    
    private IEnumerator PulseTitle()
    {
        // Esperar a que termine la animación de entrada
        yield return new WaitForSeconds(0.4f);
        
        while (true)
        {
            float time = 0f;
            float duration = 4f; // Pulsación cada 4 segundos
            float minScale = 1.00f;
            float maxScale = 1.015f; // Pulsación muy sutil
            
            while (time < duration)
            {
                time += Time.deltaTime;
                // Usar seno para movimiento suave de ida y vuelta
                float t = Mathf.Sin(time / duration * Mathf.PI * 2f) * 0.5f + 0.5f;
                float scale = Mathf.Lerp(minScale, maxScale, t);
                
                if (titleText != null)
                {
                    titleText.transform.localScale = Vector3.one * scale;
                    
                    // Parallax leve (0.5-1px de desplazamiento)
                    RectTransform titleRect = titleText.GetComponent<RectTransform>();
                    if (titleRect != null)
                    {
                        float parallaxOffset = Mathf.Sin(time * 0.5f) * 0.8f;
                        titleRect.anchoredPosition = new Vector2(parallaxOffset, 200);
                    }
                }
                yield return null;
            }
        }
    }
    
    private IEnumerator CreateTitleParticles(Transform titleParent)
    {
        // Esperar a que termine la animación de entrada
        yield return new WaitForSeconds(0.4f);
        
        while (true)
        {
            // Crear partícula cada 1-2 segundos
            yield return new WaitForSeconds(Random.Range(1f, 2f));
            
            if (titleText == null || titleParent == null) break;
            
            // Crear partícula pequeña ascendiendo
            GameObject particle = new GameObject("TitleParticle");
            particle.transform.SetParent(titleParent, false);
            particle.transform.SetAsFirstSibling(); // Detrás del texto
            
            Image particleImg = particle.AddComponent<Image>();
            particleImg.color = new Color(0.2f, 0.9f, 1f, 0.6f); // Cian brillante
            particleImg.sprite = SpriteGenerator.CreateStarSprite(0.1f, Color.white);
            particleImg.raycastTarget = false;
            
            RectTransform particleRect = particle.GetComponent<RectTransform>();
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            
            // Posición inicial: debajo del título, aleatoria en X
            float startX = Random.Range(-titleRect.sizeDelta.x * 0.4f, titleRect.sizeDelta.x * 0.4f);
            particleRect.anchorMin = new Vector2(0.5f, 0f);
            particleRect.anchorMax = new Vector2(0.5f, 0f);
            particleRect.pivot = new Vector2(0.5f, 0.5f);
            particleRect.anchoredPosition = new Vector2(startX, -60);
            particleRect.sizeDelta = new Vector2(4, 4);
            
            // Animar partícula ascendiendo
            StartCoroutine(AnimateTitleParticle(particle, titleRect.sizeDelta.y + 40));
        }
    }
    
    private IEnumerator AnimateTitleParticle(GameObject particle, float targetY)
    {
        if (particle == null) yield break;
        
        RectTransform rect = particle.GetComponent<RectTransform>();
        Image img = particle.GetComponent<Image>();
        
        float startY = rect.anchoredPosition.y;
        float duration = Random.Range(2f, 3.5f); // Ascenso lento
        float elapsed = 0f;
        
        while (elapsed < duration && particle != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Movimiento vertical suave
            float currentY = Mathf.Lerp(startY, targetY, t);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, currentY);
            
            // Fade out gradual
            if (img != null)
            {
                float alpha = Mathf.Lerp(0.6f, 0f, t);
                img.color = new Color(0.2f, 0.9f, 1f, alpha);
            }
            
            // Movimiento horizontal leve (flotación)
            float floatX = Mathf.Sin(t * Mathf.PI * 2f) * 10f;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x + floatX * Time.deltaTime, currentY);
            
            yield return null;
        }
        
        if (particle != null)
        {
            Destroy(particle);
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
    
    /// <summary>
    /// Función helper para cargar sprites que funciona tanto en editor como en builds
    /// </summary>
    private Sprite LoadSpriteResource(string resourcePath, string assetName)
    {
        if (!Application.isPlaying) return null;
        
        // Primero intentar cargar desde Resources (funciona en editor y builds si están en carpeta Resources)
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null) return sprite;
        
        // Intentar cargar como Texture2D desde Resources
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture != null)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
        
        #if UNITY_EDITOR
        // En el editor, intentar usar AssetDatabase como fallback
        try
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets(assetName + " t:Sprite");
            if (guids.Length == 0)
            {
                guids = UnityEditor.AssetDatabase.FindAssets(assetName + " t:Texture2D");
            }
            
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) return sprite;
                
                texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo cargar el sprite {assetName}: {e.Message}");
        }
        #endif
        
        return null;
    }
    
    private Sprite LoadPlayerSprite()
    {
        return LoadSpriteResource("Art/Protagonist/AsteroideErrante", "AsteroideErrante");
    }
    
    private Sprite LoadOptionsIcon()
    {
        return LoadSpriteResource("Art/Icons/OptionsIcon", "OptionsIcon");
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
        
        return LoadSpriteResource($"Art/Icons/{iconName}", iconName);
    }
}
