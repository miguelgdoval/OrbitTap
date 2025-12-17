using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manager para manejar la política de privacidad (primera vez y acceso desde settings)
/// </summary>
public class PrivacyPolicyManager : MonoBehaviour
{
    public static PrivacyPolicyManager Instance { get; private set; }
    
    private const string PRIVACY_POLICY_ACCEPTED_KEY = "PrivacyPolicyAccepted";
    private const string PRIVACY_POLICY_URL = "https://luxuriant-wallaby-385.notion.site/Privacy-Policy-Starbound-Orbit-2ccb484c8c6e804e9bfeec32968f63a4";
    
    private GameObject privacyPanel;
    private bool hasShownFirstTime = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Verifica si se debe mostrar la política de privacidad (primera vez)
    /// </summary>
    public bool ShouldShowPrivacyPolicy()
    {
        return !PlayerPrefs.HasKey(PRIVACY_POLICY_ACCEPTED_KEY);
    }
    
    /// <summary>
    /// Muestra el panel de política de privacidad (primera vez)
    /// </summary>
    public void ShowPrivacyPolicyFirstTime()
    {
        if (hasShownFirstTime) return;
        
        if (ShouldShowPrivacyPolicy())
        {
            CreatePrivacyPolicyPanel();
            hasShownFirstTime = true;
        }
    }
    
    /// <summary>
    /// Abre la política de privacidad en el navegador
    /// </summary>
    public void OpenPrivacyPolicyURL()
    {
        Application.OpenURL(PRIVACY_POLICY_URL);
    }
    
    /// <summary>
    /// Marca que el usuario aceptó la política de privacidad
    /// </summary>
    public void AcceptPrivacyPolicy()
    {
        PlayerPrefs.SetInt(PRIVACY_POLICY_ACCEPTED_KEY, 1);
        PlayerPrefs.Save();
        
        if (privacyPanel != null)
        {
            Destroy(privacyPanel);
            privacyPanel = null;
        }
    }
    
    private void CreatePrivacyPolicyPanel()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
        }
        
        // Panel de fondo (overlay oscuro)
        privacyPanel = new GameObject("PrivacyPolicyPanel");
        privacyPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = privacyPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        Image panelBg = privacyPanel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.9f);
        
        // Panel de contenido
        GameObject contentPanel = new GameObject("Content");
        contentPanel.transform.SetParent(privacyPanel.transform, false);
        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(800, 600);
        contentRect.anchoredPosition = Vector2.zero;
        
        Image contentBg = contentPanel.AddComponent<Image>();
        contentBg.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        
        // Título
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(contentPanel.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "Política de Privacidad";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = CosmicTheme.NeonCyan;
        titleText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(750, 80);
        titleRect.anchoredPosition = new Vector2(0, -30);
        
        // Texto descriptivo
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(contentPanel.transform, false);
        Text descText = descObj.AddComponent<Text>();
        descText.text = "Para continuar, por favor lee y acepta nuestra Política de Privacidad.\n\nAl hacer clic en 'Ver Política', se abrirá en tu navegador.";
        descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descText.fontSize = 24;
        descText.color = CosmicTheme.SpaceWhite;
        descText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.5f, 0.5f);
        descRect.anchorMax = new Vector2(0.5f, 0.5f);
        descRect.pivot = new Vector2(0.5f, 0.5f);
        descRect.sizeDelta = new Vector2(750, 200);
        descRect.anchoredPosition = new Vector2(0, 50);
        
        // Botón "Ver Política"
        GameObject viewButtonObj = new GameObject("ViewPolicyButton");
        viewButtonObj.transform.SetParent(contentPanel.transform, false);
        Button viewButton = viewButtonObj.AddComponent<Button>();
        Image viewButtonImg = viewButtonObj.AddComponent<Image>();
        viewButtonImg.color = CosmicTheme.NeonCyan;
        
        RectTransform viewButtonRect = viewButtonObj.GetComponent<RectTransform>();
        viewButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
        viewButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
        viewButtonRect.pivot = new Vector2(0.5f, 0.5f);
        viewButtonRect.sizeDelta = new Vector2(300, 60);
        viewButtonRect.anchoredPosition = new Vector2(0, -50);
        
        GameObject viewButtonTextObj = new GameObject("Text");
        viewButtonTextObj.transform.SetParent(viewButtonObj.transform, false);
        Text viewButtonText = viewButtonTextObj.AddComponent<Text>();
        viewButtonText.text = "Ver Política";
        viewButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        viewButtonText.fontSize = 28;
        viewButtonText.color = Color.white;
        viewButtonText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform viewButtonTextRect = viewButtonTextObj.GetComponent<RectTransform>();
        viewButtonTextRect.anchorMin = Vector2.zero;
        viewButtonTextRect.anchorMax = Vector2.one;
        viewButtonTextRect.sizeDelta = Vector2.zero;
        
        viewButton.onClick.AddListener(() => {
            OpenPrivacyPolicyURL();
        });
        
        // Botón "Aceptar"
        GameObject acceptButtonObj = new GameObject("AcceptButton");
        acceptButtonObj.transform.SetParent(contentPanel.transform, false);
        Button acceptButton = acceptButtonObj.AddComponent<Button>();
        Image acceptButtonImg = acceptButtonObj.AddComponent<Image>();
        acceptButtonImg.color = new Color(0.2f, 1f, 0.2f, 1f); // Verde
        
        RectTransform acceptButtonRect = acceptButtonObj.GetComponent<RectTransform>();
        acceptButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
        acceptButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
        acceptButtonRect.pivot = new Vector2(0.5f, 0.5f);
        acceptButtonRect.sizeDelta = new Vector2(300, 60);
        acceptButtonRect.anchoredPosition = new Vector2(0, -150);
        
        GameObject acceptButtonTextObj = new GameObject("Text");
        acceptButtonTextObj.transform.SetParent(acceptButtonObj.transform, false);
        Text acceptButtonText = acceptButtonTextObj.AddComponent<Text>();
        acceptButtonText.text = "Aceptar";
        acceptButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        acceptButtonText.fontSize = 28;
        acceptButtonText.color = Color.white;
        acceptButtonText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform acceptButtonTextRect = acceptButtonTextObj.GetComponent<RectTransform>();
        acceptButtonTextRect.anchorMin = Vector2.zero;
        acceptButtonTextRect.anchorMax = Vector2.one;
        acceptButtonTextRect.sizeDelta = Vector2.zero;
        
        acceptButton.onClick.AddListener(() => {
            AcceptPrivacyPolicy();
        });
    }
}

