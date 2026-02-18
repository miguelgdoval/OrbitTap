using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using static LogHelper;

/// <summary>
/// Pausa manual durante la partida: botón en el HUD y menú (Reanudar / Salir al menú).
/// Responde también al botón Atrás (Escape) en Android.
/// </summary>
public class GamePauseController : MonoBehaviour
{
    private GameObject canvas;
    private GameObject pauseButtonObj;
    private GameObject pauseOverlayPanel;
    private bool overlayVisible => pauseOverlayPanel != null && pauseOverlayPanel.activeSelf;
    
    private const string MAIN_MENU_SCENE = "MainMenu";
    
    private void Start()
    {
        canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            LogError("[GamePauseController] No se encontró Canvas.");
            return;
        }
        CreatePauseButton();
        CreatePauseOverlay();
    }
    
    private void Update()
    {
        if (canvas == null) return;
        
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        
        if (overlayVisible)
        {
            ResumeAndHide();
            return;
        }
        
        if (ReviveManager.Instance != null && ReviveManager.Instance.IsShowingReviveUI)
            return;
        
        if (PauseManager.Instance != null && !PauseManager.Instance.IsPaused())
            ShowPauseOverlay();
    }
    
    private void CreatePauseButton()
    {
        pauseButtonObj = new GameObject("PauseButton");
        pauseButtonObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = pauseButtonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        // A la izquierda del contador de shards (ShardsContainer está en -20, 140px ancho): pausa en -170 para dejar ~10px de hueco
        rect.anchoredPosition = new Vector2(-170, -45);
        rect.sizeDelta = new Vector2(80, 80);
        
        Button btn = pauseButtonObj.AddComponent<Button>();
        Image img = pauseButtonObj.AddComponent<Image>();
        img.color = new Color(CosmicTheme.SpaceBlack.r, CosmicTheme.SpaceBlack.g, CosmicTheme.SpaceBlack.b, 0.5f);
        
        Outline outline = pauseButtonObj.AddComponent<Outline>();
        outline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.6f);
        outline.effectDistance = new Vector2(2, 2);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(pauseButtonObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = "II";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 36;
        text.fontStyle = FontStyle.Bold;
        text.color = CosmicTheme.NeonCyan;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        btn.onClick.AddListener(OnPauseButtonClicked);
    }
    
    private void OnPauseButtonClicked()
    {
        if (ReviveManager.Instance != null && ReviveManager.Instance.IsShowingReviveUI)
            return;
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused())
            return;
        ShowPauseOverlay();
    }
    
    private void ShowPauseOverlay()
    {
        if (PauseManager.Instance == null) return;
        PauseManager.Instance.PauseGame(false);
        if (pauseOverlayPanel != null)
        {
            pauseOverlayPanel.SetActive(true);
            pauseOverlayPanel.transform.SetAsLastSibling();
        }
        if (pauseButtonObj != null)
            pauseButtonObj.SetActive(false);
    }
    
    private void ResumeAndHide()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.ResumeGame(false);
        if (pauseOverlayPanel != null)
            pauseOverlayPanel.SetActive(false);
        if (pauseButtonObj != null)
            pauseButtonObj.SetActive(true);
    }
    
    private void GoToMainMenu()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.ResumeGame(false);
        if (pauseOverlayPanel != null)
            pauseOverlayPanel.SetActive(false);
        SceneManager.LoadScene(MAIN_MENU_SCENE);
    }
    
    private void CreatePauseOverlay()
    {
        pauseOverlayPanel = new GameObject("PauseOverlay");
        pauseOverlayPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform overlayRect = pauseOverlayPanel.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;
        
        Image overlayBg = pauseOverlayPanel.AddComponent<Image>();
        overlayBg.color = new Color(0, 0, 0, 0.8f);
        
        GameObject content = new GameObject("Content");
        content.transform.SetParent(pauseOverlayPanel.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(480, 320);
        contentRect.anchoredPosition = Vector2.zero;
        
        Image contentBg = content.AddComponent<Image>();
        contentBg.color = new Color(0.08f, 0.08f, 0.12f, 1f);
        
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(content.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "Pausa";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = CosmicTheme.NeonCyan;
        titleText.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(400, 60);
        titleRect.anchoredPosition = new Vector2(0, -30);
        
        CreateOverlayButton(content.transform, "Reanudar", -30, true, ResumeAndHide);
        CreateOverlayButton(content.transform, "Salir al menú", -110, false, GoToMainMenu);
        
        pauseOverlayPanel.SetActive(false);
    }
    
    private void CreateOverlayButton(Transform parent, string label, float yOffset, bool isPrimary, System.Action onClick)
    {
        GameObject btnObj = new GameObject("Button_" + label);
        btnObj.transform.SetParent(parent, false);
        Button btn = btnObj.AddComponent<Button>();
        Image img = btnObj.AddComponent<Image>();
        img.color = isPrimary ? CosmicTheme.NeonCyan : new Color(0.4f, 0.4f, 0.5f, 1f);
        
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(280, 56);
        rect.anchoredPosition = new Vector2(0, yOffset);
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        btn.onClick.AddListener(() => onClick?.Invoke());
    }
}
