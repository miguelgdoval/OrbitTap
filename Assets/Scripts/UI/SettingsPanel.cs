using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Panel de opciones mejorado con diseño Space Neon Minimal
/// Panel lateral izquierdo con secciones + Panel derecho con contenido dinámico
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("Settings State")]
    private bool soundEnabled = true;
    private bool vibrationEnabled = true;
    private string currentLanguage = "ES";
    private int graphicsQuality = 1; // 0=Low, 1=Medium, 2=High
    private bool colorBlindMode = false;
    private bool highContrastUI = false;
    private bool reduceAnimations = false;
    
    [Header("UI References")]
    private GameObject panel;
    private GameObject leftPanel; // Panel lateral con botones de sección
    private GameObject rightPanel; // Panel derecho con contenido
    private Button closeButton;
    
    private SettingsSection currentSection = SettingsSection.Sound;
    
    // Referencias a elementos de UI
    private Dictionary<SettingsSection, GameObject> sectionContent = new Dictionary<SettingsSection, GameObject>();
    
    private enum SettingsSection
    {
        Sound,
        Vibration,
        Language,
        Graphics,
        Accessibility,
        Controls,
        Legal
    }
    
    private void Start()
    {
        LoadSettings();
    }
    
    public void Show()
    {
        if (panel == null)
        {
            CreatePanel();
        }
        panel.SetActive(true);
        ShowSection(currentSection);
    }
    
    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
    
    private void CreatePanel()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        
        // Panel de fondo (overlay oscuro)
        panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.85f);
        
        // Botón para cerrar al hacer click fuera (opcional)
        Button backgroundButton = panel.AddComponent<Button>();
        backgroundButton.onClick.AddListener(Hide);
        
        // Panel principal de contenido (centrado)
        GameObject mainContent = new GameObject("MainContent");
        mainContent.transform.SetParent(panel.transform, false);
        RectTransform mainRect = mainContent.AddComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainRect.pivot = new Vector2(0.5f, 0.5f);
        mainRect.sizeDelta = new Vector2(900, 700);
        
        Image mainBg = mainContent.AddComponent<Image>();
        mainBg.color = CosmicTheme.GlassPanel;
        
        // Añadir efecto de borde luminiscente
        AddGlowEffect(mainContent, CosmicTheme.NeonCyan, 0.3f);
        
        // Botón cerrar (arriba derecha)
        CreateCloseButton(mainContent);
        
        // Panel lateral izquierdo
        CreateLeftPanel(mainContent);
        
        // Panel derecho con contenido
        CreateRightPanel(mainContent);
        
        // Crear contenido de todas las secciones
        CreateAllSections();
        
        panel.SetActive(false);
    }
    
    private void CreateCloseButton(GameObject parent)
    {
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(parent.transform, false);
        closeButton = closeObj.AddComponent<Button>();
        
        Image closeImg = closeObj.AddComponent<Image>();
        closeImg.color = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.2f);
        
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-15, -15);
        closeRect.sizeDelta = new Vector2(50, 50);
        
        // Texto X
        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeObj.transform, false);
        Text closeText = closeTextObj.AddComponent<Text>();
        closeText.text = "×";
        closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeText.fontSize = 36;
        closeText.color = CosmicTheme.NeonCyan;
        closeText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.sizeDelta = Vector2.zero;
        
        closeButton.onClick.AddListener(Hide);
    }
    
    private void CreateLeftPanel(GameObject parent)
    {
        leftPanel = new GameObject("LeftPanel");
        leftPanel.transform.SetParent(parent.transform, false);
        RectTransform leftRect = leftPanel.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0f);
        leftRect.anchorMax = new Vector2(0f, 1f);
        leftRect.pivot = new Vector2(0f, 1f);
        leftRect.anchoredPosition = new Vector2(20, -20);
        leftRect.sizeDelta = new Vector2(200, -40);
        
        Image leftBg = leftPanel.AddComponent<Image>();
        leftBg.color = new Color(0, 0, 0, 0.3f);
        
        // Crear botones de sección
        string[] sectionNames = { "Sonido", "Vibración", "Idioma", "Gráficos", "Accesibilidad", "Controles", "Legal" };
        SettingsSection[] sections = { 
            SettingsSection.Sound, 
            SettingsSection.Vibration, 
            SettingsSection.Language, 
            SettingsSection.Graphics, 
            SettingsSection.Accessibility, 
            SettingsSection.Controls, 
            SettingsSection.Legal 
        };
        
        float buttonHeight = 50f;
        float spacing = 10f;
        float startY = -30f;
        
        for (int i = 0; i < sections.Length; i++)
        {
            CreateSectionButton(sectionNames[i], sections[i], startY - i * (buttonHeight + spacing), buttonHeight);
        }
    }
    
    private void CreateSectionButton(string label, SettingsSection section, float yPos, float height)
    {
        GameObject btnObj = new GameObject($"SectionButton_{section}");
        btnObj.transform.SetParent(leftPanel.transform, false);
        Button btn = btnObj.AddComponent<Button>();
        
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.1f);
        
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0f, 1f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(0f, 1f);
        btnRect.anchoredPosition = new Vector2(10, yPos);
        btnRect.sizeDelta = new Vector2(-20, height);
        
        // Texto
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.color = CosmicTheme.SpaceWhite;
        text.alignment = TextAnchor.MiddleLeft;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = new Vector2(15, 0);
        
        btn.onClick.AddListener(() => ShowSection(section));
    }
    
    private void CreateRightPanel(GameObject parent)
    {
        rightPanel = new GameObject("RightPanel");
        rightPanel.transform.SetParent(parent.transform, false);
        RectTransform rightRect = rightPanel.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0f, 0f);
        rightRect.anchorMax = new Vector2(1f, 1f);
        rightRect.pivot = new Vector2(0f, 1f);
        rightRect.anchoredPosition = new Vector2(240, -20);
        rightRect.sizeDelta = new Vector2(-260, -40);
    }
    
    private void CreateAllSections()
    {
        CreateSoundSection();
        CreateVibrationSection();
        CreateLanguageSection();
        CreateGraphicsSection();
        CreateAccessibilitySection();
        CreateControlsSection();
        CreateLegalSection();
    }
    
    private void CreateSoundSection()
    {
        GameObject section = new GameObject("SoundSection");
        section.transform.SetParent(rightPanel.transform, false);
        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.anchorMin = Vector2.zero;
        sectionRect.anchorMax = Vector2.one;
        sectionRect.sizeDelta = Vector2.zero;
        
        float yPos = -30f;
        
        // Título
        yPos = CreateSectionTitle(section.transform, "Sonido", yPos);
        yPos -= 20f;
        
        // Master Volume
        float masterVol = 1f;
        if (AudioManager.Instance != null) masterVol = AudioManager.Instance.GetMasterVolume();
        yPos = CreateVolumeSlider(section.transform, "Master Volume", masterVol, yPos, 
            (value) => {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.SetMasterVolume(value);
            });
        
        // Music Volume
        float musicVol = 1f;
        if (AudioManager.Instance != null) musicVol = AudioManager.Instance.GetMusicVolume();
        yPos = CreateVolumeSlider(section.transform, "Music Volume", musicVol, yPos,
            (value) => {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.SetMusicVolume(value);
            });
        
        // SFX Volume
        float sfxVol = 1f;
        if (AudioManager.Instance != null) sfxVol = AudioManager.Instance.GetSFXVolume();
        yPos = CreateVolumeSlider(section.transform, "SFX Volume", sfxVol, yPos,
            (value) => {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.SetSFXVolume(value);
            });
        
        sectionContent[SettingsSection.Sound] = section;
        section.SetActive(false);
    }
    
    private void CreateVibrationSection()
    {
        GameObject section = new GameObject("VibrationSection");
        section.transform.SetParent(rightPanel.transform, false);
        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.anchorMin = Vector2.zero;
        sectionRect.anchorMax = Vector2.one;
        sectionRect.sizeDelta = Vector2.zero;
        
        float yPos = -30f;
        
        // Título
        yPos = CreateSectionTitle(section.transform, "Vibración", yPos);
        yPos -= 30f;
        
        // Toggle de vibración
        CreateNeonToggle(section.transform, "Activar Vibración", vibrationEnabled, yPos, (value) => {
            vibrationEnabled = value;
            SaveSettings();
        });
        
        sectionContent[SettingsSection.Vibration] = section;
        section.SetActive(false);
    }
    
    private void CreateLanguageSection()
    {
        GameObject section = new GameObject("LanguageSection");
        section.transform.SetParent(rightPanel.transform, false);
        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.anchorMin = Vector2.zero;
        sectionRect.anchorMax = Vector2.one;
        sectionRect.sizeDelta = Vector2.zero;
        
        float yPos = -30f;
        
        // Título
        yPos = CreateSectionTitle(section.transform, "Idioma", yPos);
        yPos -= 30f;
        
        // Dropdown de idioma
        CreateLanguageDropdown(section.transform, yPos);
        
        sectionContent[SettingsSection.Language] = section;
        section.SetActive(false);
    }
    
    private void CreateGraphicsSection()
    {
        GameObject section = new GameObject("GraphicsSection");
        section.transform.SetParent(rightPanel.transform, false);
        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.anchorMin = Vector2.zero;
        sectionRect.anchorMax = Vector2.one;
        sectionRect.sizeDelta = Vector2.zero;
        
        float yPos = -30f;
        
        // Título
        yPos = CreateSectionTitle(section.transform, "Calidad Gráfica", yPos);
        yPos -= 30f;
        
        // Dropdown de calidad
        CreateQualityDropdown(section.transform, yPos);
        
        sectionContent[SettingsSection.Graphics] = section;
        section.SetActive(false);
    }
    
    private void CreateAccessibilitySection()
    {
        GameObject section = new GameObject("AccessibilitySection");
        section.transform.SetParent(rightPanel.transform, false);
        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.anchorMin = Vector2.zero;
        sectionRect.anchorMax = Vector2.one;
        sectionRect.sizeDelta = Vector2.zero;
        
        float yPos = -30f;
        
        // Título
        yPos = CreateSectionTitle(section.transform, "Accesibilidad", yPos);
        yPos -= 30f;
        
        // Color Blind Mode
        yPos = CreateNeonToggle(section.transform, "Modo Daltónico", colorBlindMode, yPos, (value) => {
            colorBlindMode = value;
            SaveSettings();
        });
        yPos -= 20f;
        
        // High Contrast UI
        yPos = CreateNeonToggle(section.transform, "Alto Contraste UI", highContrastUI, yPos, (value) => {
            highContrastUI = value;
            SaveSettings();
        });
        yPos -= 20f;
        
        // Reduce Animations
        CreateNeonToggle(section.transform, "Reducir Animaciones", reduceAnimations, yPos, (value) => {
            reduceAnimations = value;
            SaveSettings();
        });
        
        sectionContent[SettingsSection.Accessibility] = section;
        section.SetActive(false);
    }
    
    private void CreateControlsSection()
    {
        GameObject section = new GameObject("ControlsSection");
        section.transform.SetParent(rightPanel.transform, false);
        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.anchorMin = Vector2.zero;
        sectionRect.anchorMax = Vector2.one;
        sectionRect.sizeDelta = Vector2.zero;
        
        float yPos = -30f;
        
        // Título
        yPos = CreateSectionTitle(section.transform, "Controles", yPos);
        yPos -= 30f;
        
        // Placeholder para controles futuros
        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(section.transform, false);
        Text placeholderText = placeholder.AddComponent<Text>();
        placeholderText.text = "Opciones de controles próximamente...";
        placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholderText.fontSize = 20;
        placeholderText.color = CosmicTheme.SpaceWhite;
        placeholderText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = new Vector2(0.5f, 0.5f);
        placeholderRect.anchorMax = new Vector2(0.5f, 0.5f);
        placeholderRect.pivot = new Vector2(0.5f, 0.5f);
        placeholderRect.anchoredPosition = new Vector2(0, 0);
        placeholderRect.sizeDelta = new Vector2(400, 40);
        
        sectionContent[SettingsSection.Controls] = section;
        section.SetActive(false);
    }
    
    private void CreateLegalSection()
    {
        GameObject section = new GameObject("LegalSection");
        section.transform.SetParent(rightPanel.transform, false);
        RectTransform sectionRect = section.AddComponent<RectTransform>();
        sectionRect.anchorMin = Vector2.zero;
        sectionRect.anchorMax = Vector2.one;
        sectionRect.sizeDelta = Vector2.zero;
        
        float yPos = -30f;
        
        // Título
        yPos = CreateSectionTitle(section.transform, "Legal", yPos);
        yPos -= 40f;
        
        // Versión del juego
        GameObject versionObj = new GameObject("Version");
        versionObj.transform.SetParent(section.transform, false);
        Text versionText = versionObj.AddComponent<Text>();
        versionText.text = "Starbound Orbit v0.1.3 Early Test";
        versionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        versionText.fontSize = 16;
        versionText.color = new Color(CosmicTheme.SpaceWhite.r, CosmicTheme.SpaceWhite.g, CosmicTheme.SpaceWhite.b, 0.6f);
        versionText.alignment = TextAnchor.MiddleLeft;
        
        RectTransform versionRect = versionObj.GetComponent<RectTransform>();
        versionRect.anchorMin = new Vector2(0f, 1f);
        versionRect.anchorMax = new Vector2(0f, 1f);
        versionRect.pivot = new Vector2(0f, 1f);
        versionRect.anchoredPosition = new Vector2(20, yPos);
        versionRect.sizeDelta = new Vector2(400, 30);
        yPos -= 40f;
        
        // Créditos
        GameObject creditsObj = new GameObject("Credits");
        creditsObj.transform.SetParent(section.transform, false);
        Text creditsText = creditsObj.AddComponent<Text>();
        creditsText.text = "Made with Unity\n© 2024";
        creditsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        creditsText.fontSize = 18;
        creditsText.color = CosmicTheme.SpaceWhite;
        creditsText.alignment = TextAnchor.MiddleLeft;
        
        RectTransform creditsRect = creditsObj.GetComponent<RectTransform>();
        creditsRect.anchorMin = new Vector2(0f, 1f);
        creditsRect.anchorMax = new Vector2(0f, 1f);
        creditsRect.pivot = new Vector2(0f, 1f);
        creditsRect.anchoredPosition = new Vector2(20, yPos);
        creditsRect.sizeDelta = new Vector2(400, 60);
        yPos -= 80f;
        
        // Botón Restore Purchases
        CreateNeonButton(section.transform, "Restore Purchases", yPos, () => {
            Debug.Log("Restore Purchases clicked - TODO: Implement");
        });
        
        sectionContent[SettingsSection.Legal] = section;
        section.SetActive(false);
    }
    
    private float CreateSectionTitle(Transform parent, string title, float yPos)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = title;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = CosmicTheme.NeonCyan;
        titleText.alignment = TextAnchor.MiddleLeft;
        
        // Sombra para mejor legibilidad
        Shadow shadow = titleObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(2, -2);
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(20, yPos);
        titleRect.sizeDelta = new Vector2(400, 40);
        
        return yPos - 40f;
    }
    
    private float CreateVolumeSlider(Transform parent, string label, float initialValue, float yPos, System.Action<float> onValueChanged)
    {
        GameObject sliderObj = new GameObject($"Slider_{label}");
        sliderObj.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 1f);
        sliderRect.anchorMax = new Vector2(1f, 1f);
        sliderRect.pivot = new Vector2(0f, 1f);
        sliderRect.anchoredPosition = new Vector2(20, yPos);
        sliderRect.sizeDelta = new Vector2(-40, 60);
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(sliderObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 22;
        labelText.color = CosmicTheme.SpaceWhite;
        labelText.alignment = TextAnchor.MiddleLeft;
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0, 10);
        labelRect.sizeDelta = new Vector2(200, 30);
        
        // Value text
        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(sliderObj.transform, false);
        Text valueText = valueObj.AddComponent<Text>();
        valueText.text = Mathf.RoundToInt(initialValue * 100) + "%";
        valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        valueText.fontSize = 20;
        valueText.color = CosmicTheme.NeonCyan;
        valueText.alignment = TextAnchor.MiddleRight;
        
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(1f, 0.5f);
        valueRect.anchorMax = new Vector2(1f, 0.5f);
        valueRect.pivot = new Vector2(1f, 0.5f);
        valueRect.anchoredPosition = new Vector2(0, 10);
        valueRect.sizeDelta = new Vector2(60, 30);
        
        // Slider
        GameObject sliderBg = new GameObject("Slider");
        sliderBg.transform.SetParent(sliderObj.transform, false);
        Slider slider = sliderBg.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = initialValue;
        slider.wholeNumbers = false;
        
        Image sliderBgImg = sliderBg.AddComponent<Image>();
        sliderBgImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        
        RectTransform sliderBgRect = sliderBg.GetComponent<RectTransform>();
        sliderBgRect.anchorMin = new Vector2(0f, 0f);
        sliderBgRect.anchorMax = new Vector2(1f, 0f);
        sliderBgRect.pivot = new Vector2(0f, 0f);
        sliderBgRect.anchoredPosition = new Vector2(0, 0);
        sliderBgRect.sizeDelta = new Vector2(0, 8);
        
        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderBg.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = CosmicTheme.NeonCyan;
        slider.fillRect = fill.GetComponent<RectTransform>();
        
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(initialValue, 1f);
        fillRect.sizeDelta = Vector2.zero;
        
        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(fillArea.transform, false);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = CosmicTheme.NeonMagenta;
        slider.targetGraphic = handleImg;
        
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(initialValue, 0f);
        handleRect.anchorMax = new Vector2(initialValue, 1f);
        handleRect.sizeDelta = new Vector2(16, 0);
        
        slider.onValueChanged.AddListener((value) => {
            fillRect.anchorMax = new Vector2(value, 1f);
            handleRect.anchorMin = new Vector2(value, 0f);
            handleRect.anchorMax = new Vector2(value, 1f);
            valueText.text = Mathf.RoundToInt(value * 100) + "%";
            onValueChanged?.Invoke(value);
        });
        
        return yPos - 60f;
    }
    
    private float CreateNeonToggle(Transform parent, string label, bool initialValue, float yPos, System.Action<bool> onValueChanged)
    {
        GameObject toggleObj = new GameObject($"Toggle_{label}");
        toggleObj.transform.SetParent(parent, false);
        RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(1f, 1f);
        toggleRect.pivot = new Vector2(0f, 1f);
        toggleRect.anchoredPosition = new Vector2(20, yPos);
        toggleRect.sizeDelta = new Vector2(-40, 50);
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(toggleObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 22;
        labelText.color = CosmicTheme.SpaceWhite;
        labelText.alignment = TextAnchor.MiddleLeft;
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = new Vector2(0, 0);
        
        // Toggle (pill style)
        GameObject toggleBg = new GameObject("Toggle");
        toggleBg.transform.SetParent(toggleObj.transform, false);
        Toggle toggle = toggleBg.AddComponent<Toggle>();
        Image toggleImg = toggleBg.AddComponent<Image>();
        toggleImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        
        RectTransform toggleBgRect = toggleBg.GetComponent<RectTransform>();
        toggleBgRect.anchorMin = new Vector2(1f, 0.5f);
        toggleBgRect.anchorMax = new Vector2(1f, 0.5f);
        toggleBgRect.pivot = new Vector2(1f, 0.5f);
        toggleBgRect.anchoredPosition = new Vector2(-10, 0);
        toggleBgRect.sizeDelta = new Vector2(80, 30);
        
        // Checkmark (pill)
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(toggleBg.transform, false);
        Image checkmarkImg = checkmark.AddComponent<Image>();
        checkmarkImg.color = initialValue ? CosmicTheme.NeonCyan : new Color(0.3f, 0.3f, 0.3f, 1f);
        toggle.graphic = checkmarkImg;
        
        RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = initialValue ? new Vector2(0.5f, 0f) : new Vector2(0f, 0f);
        checkmarkRect.anchorMax = initialValue ? new Vector2(1f, 1f) : new Vector2(0.5f, 1f);
        checkmarkRect.sizeDelta = Vector2.zero;
        
        toggle.isOn = initialValue;
        toggle.onValueChanged.AddListener((value) => {
            checkmarkRect.anchorMin = value ? new Vector2(0.5f, 0f) : new Vector2(0f, 0f);
            checkmarkRect.anchorMax = value ? new Vector2(1f, 1f) : new Vector2(0.5f, 1f);
            checkmarkImg.color = value ? CosmicTheme.NeonCyan : new Color(0.3f, 0.3f, 0.3f, 1f);
            onValueChanged?.Invoke(value);
        });
        
        return yPos - 50f;
    }
    
    private void CreateLanguageDropdown(Transform parent, float yPos)
    {
        GameObject dropdownObj = new GameObject("LanguageDropdown");
        dropdownObj.transform.SetParent(parent, false);
        RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0f, 1f);
        dropdownRect.anchorMax = new Vector2(1f, 1f);
        dropdownRect.pivot = new Vector2(0f, 1f);
        dropdownRect.anchoredPosition = new Vector2(20, yPos);
        dropdownRect.sizeDelta = new Vector2(-40, 50);
        
        Dropdown dropdown = dropdownObj.AddComponent<Dropdown>();
        dropdown.options.Add(new Dropdown.OptionData("Español (ES)"));
        dropdown.options.Add(new Dropdown.OptionData("English (EN)"));
        dropdown.value = currentLanguage == "ES" ? 0 : 1;
        
        Image dropdownImg = dropdownObj.AddComponent<Image>();
        dropdownImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(dropdownObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = dropdown.options[dropdown.value].text;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 22;
        labelText.color = CosmicTheme.SpaceWhite;
        labelText.alignment = TextAnchor.MiddleLeft;
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = new Vector2(-30, 0);
        labelRect.anchoredPosition = new Vector2(10, 0);
        
        dropdown.captionText = labelText;
        
        dropdown.onValueChanged.AddListener((index) => {
            currentLanguage = index == 0 ? "ES" : "EN";
            SaveSettings();
        });
    }
    
    private void CreateQualityDropdown(Transform parent, float yPos)
    {
        GameObject dropdownObj = new GameObject("QualityDropdown");
        dropdownObj.transform.SetParent(parent, false);
        RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0f, 1f);
        dropdownRect.anchorMax = new Vector2(1f, 1f);
        dropdownRect.pivot = new Vector2(0f, 1f);
        dropdownRect.anchoredPosition = new Vector2(20, yPos);
        dropdownRect.sizeDelta = new Vector2(-40, 50);
        
        Dropdown dropdown = dropdownObj.AddComponent<Dropdown>();
        dropdown.options.Add(new Dropdown.OptionData("Low"));
        dropdown.options.Add(new Dropdown.OptionData("Medium"));
        dropdown.options.Add(new Dropdown.OptionData("High"));
        dropdown.value = graphicsQuality;
        
        Image dropdownImg = dropdownObj.AddComponent<Image>();
        dropdownImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(dropdownObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = dropdown.options[dropdown.value].text;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 22;
        labelText.color = CosmicTheme.SpaceWhite;
        labelText.alignment = TextAnchor.MiddleLeft;
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = new Vector2(-30, 0);
        labelRect.anchoredPosition = new Vector2(10, 0);
        
        dropdown.captionText = labelText;
        
        dropdown.onValueChanged.AddListener((index) => {
            graphicsQuality = index;
            QualitySettings.SetQualityLevel(index, true);
            SaveSettings();
        });
    }
    
    private void CreateNeonButton(Transform parent, string label, float yPos, System.Action onClick)
    {
        GameObject btnObj = new GameObject($"Button_{label}");
        btnObj.transform.SetParent(parent, false);
        Button btn = btnObj.AddComponent<Button>();
        
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.2f);
        
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0f, 1f);
        btnRect.anchorMax = new Vector2(0f, 1f);
        btnRect.pivot = new Vector2(0f, 1f);
        btnRect.anchoredPosition = new Vector2(20, yPos);
        btnRect.sizeDelta = new Vector2(250, 45);
        
        // Texto
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.color = CosmicTheme.NeonCyan;
        text.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        btn.onClick.AddListener(() => onClick?.Invoke());
    }
    
    private void ShowSection(SettingsSection section)
    {
        currentSection = section;
        
        // Ocultar todas las secciones
        foreach (var kvp in sectionContent)
        {
            if (kvp.Value != null)
                kvp.Value.SetActive(false);
        }
        
        // Mostrar la sección seleccionada
        if (sectionContent.ContainsKey(section) && sectionContent[section] != null)
        {
            sectionContent[section].SetActive(true);
        }
    }
    
    private void AddGlowEffect(GameObject obj, Color glowColor, float intensity)
    {
        // Efecto de glow usando Shadow component
        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, intensity);
        shadow.effectDistance = new Vector2(0, 0);
        
        // Añadir Outline para más efecto glow
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, intensity * 0.5f);
        outline.effectDistance = new Vector2(3, 3);
    }
    
    private void LoadSettings()
    {
        soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
        vibrationEnabled = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
        currentLanguage = PlayerPrefs.GetString("Language", "ES");
        graphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", 1);
        colorBlindMode = PlayerPrefs.GetInt("ColorBlindMode", 0) == 1;
        highContrastUI = PlayerPrefs.GetInt("HighContrastUI", 0) == 1;
        reduceAnimations = PlayerPrefs.GetInt("ReduceAnimations", 0) == 1;
    }
    
    private void SaveSettings()
    {
        PlayerPrefs.SetInt("SoundEnabled", soundEnabled ? 1 : 0);
        PlayerPrefs.SetInt("VibrationEnabled", vibrationEnabled ? 1 : 0);
        PlayerPrefs.SetString("Language", currentLanguage);
        PlayerPrefs.SetInt("GraphicsQuality", graphicsQuality);
        PlayerPrefs.SetInt("ColorBlindMode", colorBlindMode ? 1 : 0);
        PlayerPrefs.SetInt("HighContrastUI", highContrastUI ? 1 : 0);
        PlayerPrefs.SetInt("ReduceAnimations", reduceAnimations ? 1 : 0);
        PlayerPrefs.Save();
    }
}
