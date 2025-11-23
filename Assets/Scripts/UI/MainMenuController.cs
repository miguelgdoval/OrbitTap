using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Settings")]
    public bool createUIAtRuntime = true;
    
    private GameObject canvas;
    private Text titleText;
    private Text tapToStartText;

    private void Start()
    {
        // Configurar fondo cósmico
        if (GetComponent<CosmicBackground>() == null)
        {
            gameObject.AddComponent<CosmicBackground>();
        }

        if (createUIAtRuntime)
        {
            CreateUI();
        }
    }

    private void CreateUI()
    {
        // Create Canvas
        canvas = new GameObject("Canvas");
        Canvas canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        canvas.layer = 5; // UI layer

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.sizeDelta = Vector2.zero;

        // Create Title Text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(canvas.transform, false);
        titleText = titleObj.AddComponent<Text>();
        titleText.text = "Starbound Orbit";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 60;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = CosmicTheme.SoftGold; // Dorado suave para el título
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow; // Permitir que el texto se muestre completo
        titleText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0, 100);
        titleRect.sizeDelta = new Vector2(600, 100); // Aumentar ancho para que quepa "Starbound Orbit"

        // Create Tap to Start Text
        GameObject tapObj = new GameObject("TapToStartText");
        tapObj.transform.SetParent(canvas.transform, false);
        tapToStartText = tapObj.AddComponent<Text>();
        tapToStartText.text = "Toca para comenzar";
        tapToStartText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tapToStartText.fontSize = 30;
        tapToStartText.color = CosmicTheme.CelestialBlue; // Azul celeste luminoso
        tapToStartText.alignment = TextAnchor.MiddleCenter;

        RectTransform tapRect = tapObj.GetComponent<RectTransform>();
        tapRect.anchorMin = new Vector2(0.5f, 0.5f);
        tapRect.anchorMax = new Vector2(0.5f, 0.5f);
        tapRect.anchoredPosition = new Vector2(0, -50);
        tapRect.sizeDelta = new Vector2(300, 50);
    }

    private void Update()
    {
        // Detect touch or click
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                LoadGame();
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            LoadGame();
        }
    }

    public void LoadGame()
    {
        SceneManager.LoadScene("Game");
    }
}

