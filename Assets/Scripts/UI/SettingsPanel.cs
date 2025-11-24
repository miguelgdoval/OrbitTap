using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de ajustes del menú principal
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [Header("Settings")]
    private bool soundEnabled = true;
    private bool vibrationEnabled = true;
    
    private GameObject panel;
    private Button closeButton;
    private Toggle soundToggle;
    private Toggle vibrationToggle;
    private Button restorePurchasesButton;
    
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
        
        // Panel de fondo (overlay)
        panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.8f);
        
        // Panel de contenido
        GameObject content = new GameObject("Content");
        content.transform.SetParent(panel.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(500, 600);
        
        Image contentBg = content.AddComponent<Image>();
        contentBg.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);
        
        // Título
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(content.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "SETTINGS";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 40;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = CosmicTheme.SoftGold;
        titleText.alignment = TextAnchor.UpperCenter;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -20);
        titleRect.sizeDelta = new Vector2(400, 50);
        
        float yPos = -80;
        
        // Sound Toggle
        yPos = CreateToggle(content.transform, "Sound", soundEnabled, yPos, (value) => {
            soundEnabled = value;
            SaveSettings();
        });
        
        // Vibration Toggle
        yPos = CreateToggle(content.transform, "Vibration", vibrationEnabled, yPos, (value) => {
            vibrationEnabled = value;
            SaveSettings();
        });
        
        // Idioma (placeholder)
        GameObject langObj = new GameObject("Language");
        langObj.transform.SetParent(content.transform, false);
        Text langText = langObj.AddComponent<Text>();
        langText.text = "Language: English";
        langText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        langText.fontSize = 24;
        langText.color = Color.white;
        langText.alignment = TextAnchor.MiddleLeft;
        
        RectTransform langRect = langObj.GetComponent<RectTransform>();
        langRect.anchorMin = new Vector2(0.5f, 1f);
        langRect.anchorMax = new Vector2(0.5f, 1f);
        langRect.anchoredPosition = new Vector2(0, yPos);
        langRect.sizeDelta = new Vector2(400, 40);
        
        yPos -= 60;
        
        // Créditos
        GameObject creditsObj = new GameObject("Credits");
        creditsObj.transform.SetParent(content.transform, false);
        Text creditsText = creditsObj.AddComponent<Text>();
        creditsText.text = "Starbound Orbit v1.0\nMade with Unity";
        creditsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        creditsText.fontSize = 18;
        creditsText.color = Color.gray;
        creditsText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform creditsRect = creditsObj.GetComponent<RectTransform>();
        creditsRect.anchorMin = new Vector2(0.5f, 0f);
        creditsRect.anchorMax = new Vector2(0.5f, 0f);
        creditsRect.anchoredPosition = new Vector2(0, 40);
        creditsRect.sizeDelta = new Vector2(400, 60);
        
        // Restore Purchases
        GameObject restoreObj = new GameObject("RestorePurchases");
        restoreObj.transform.SetParent(content.transform, false);
        restorePurchasesButton = restoreObj.AddComponent<Button>();
        Image restoreImg = restoreObj.AddComponent<Image>();
        restoreImg.color = new Color(0.2f, 0.2f, 0.3f, 1f);
        
        RectTransform restoreRect = restoreObj.GetComponent<RectTransform>();
        restoreRect.anchorMin = new Vector2(0.5f, 0f);
        restoreRect.anchorMax = new Vector2(0.5f, 0f);
        restoreRect.pivot = new Vector2(0.5f, 0f);
        restoreRect.anchoredPosition = new Vector2(0, 120);
        restoreRect.sizeDelta = new Vector2(300, 50);
        
        // Crear objeto hijo para el texto
        GameObject restoreTextObj = new GameObject("Text");
        restoreTextObj.transform.SetParent(restoreObj.transform, false);
        Text restoreText = restoreTextObj.AddComponent<Text>();
        restoreText.text = "Restore Purchases";
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont != null)
        {
            restoreText.font = defaultFont;
        }
        restoreText.fontSize = 20;
        restoreText.color = Color.white;
        restoreText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform restoreTextRect = restoreTextObj.GetComponent<RectTransform>();
        restoreTextRect.anchorMin = Vector2.zero;
        restoreTextRect.anchorMax = Vector2.one;
        restoreTextRect.sizeDelta = Vector2.zero;
        restoreTextRect.anchoredPosition = Vector2.zero;
        
        restorePurchasesButton.onClick.AddListener(() => {
            Debug.Log("Restore Purchases clicked - TODO: Implement");
        });
        
        // Botón cerrar
        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(content.transform, false);
        closeButton = closeObj.AddComponent<Button>();
        Image closeImg = closeObj.AddComponent<Image>();
        closeImg.color = new Color(0.3f, 0.1f, 0.1f, 1f);
        
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-10, -10);
        closeRect.sizeDelta = new Vector2(40, 40);
        
        // Crear objeto hijo para el texto
        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeObj.transform, false);
        Text closeText = closeTextObj.AddComponent<Text>();
        closeText.text = "×";
        Font defaultFontClose = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFontClose != null)
        {
            closeText.font = defaultFontClose;
        }
        closeText.fontSize = 30;
        closeText.color = Color.white;
        closeText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform closeTextRect = closeTextObj.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.sizeDelta = Vector2.zero;
        closeTextRect.anchoredPosition = Vector2.zero;
        
        closeButton.onClick.AddListener(Hide);
        
        panel.SetActive(false);
    }
    
    private float CreateToggle(Transform parent, string label, bool initialValue, float yPos, System.Action<bool> onValueChanged)
    {
        GameObject toggleObj = new GameObject($"Toggle_{label}");
        toggleObj.transform.SetParent(parent, false);
        RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.5f, 1f);
        toggleRect.anchorMax = new Vector2(0.5f, 1f);
        toggleRect.anchoredPosition = new Vector2(0, yPos);
        toggleRect.sizeDelta = new Vector2(400, 50);
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(toggleObj.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 24;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(0, 0.5f);
        labelRect.anchoredPosition = new Vector2(20, 0);
        labelRect.sizeDelta = new Vector2(200, 40);
        
        // Toggle
        GameObject toggleBg = new GameObject("Toggle");
        toggleBg.transform.SetParent(toggleObj.transform, false);
        Toggle toggle = toggleBg.AddComponent<Toggle>();
        Image toggleImg = toggleBg.AddComponent<Image>();
        toggleImg.color = new Color(0.2f, 0.2f, 0.3f, 1f);
        
        RectTransform toggleBgRect = toggleBg.GetComponent<RectTransform>();
        toggleBgRect.anchorMin = new Vector2(1f, 0.5f);
        toggleBgRect.anchorMax = new Vector2(1f, 0.5f);
        toggleBgRect.anchoredPosition = new Vector2(-20, 0);
        toggleBgRect.sizeDelta = new Vector2(80, 40);
        
        // Checkmark
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(toggleBg.transform, false);
        Image checkmarkImg = checkmark.AddComponent<Image>();
        checkmarkImg.color = CosmicTheme.SoftGold;
        toggle.graphic = checkmarkImg;
        
        RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = Vector2.zero;
        checkmarkRect.anchorMax = Vector2.one;
        checkmarkRect.sizeDelta = Vector2.zero;
        checkmarkRect.anchoredPosition = Vector2.zero;
        
        toggle.isOn = initialValue;
        toggle.onValueChanged.AddListener((value) => onValueChanged?.Invoke(value));
        
        return yPos - 60;
    }
    
    private void LoadSettings()
    {
        soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
        vibrationEnabled = PlayerPrefs.GetInt("VibrationEnabled", 1) == 1;
    }
    
    private void SaveSettings()
    {
        PlayerPrefs.SetInt("SoundEnabled", soundEnabled ? 1 : 0);
        PlayerPrefs.SetInt("VibrationEnabled", vibrationEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}

