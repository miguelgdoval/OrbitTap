using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static LogHelper;

/// <summary>
/// Sección del leaderboard en el menú principal
/// </summary>
public class LeaderboardSection : BaseMenuSection
{
    public override MenuSection SectionType => MenuSection.Leaderboard;
    
    private GameObject contentPanel;
    private ScrollRect scrollRect;
    private Text titleText;
    private int playerHighScore = 0;
    private bool isInitialized = false;
    
    private void Start()
    {
        InitializeIfNeeded();
    }
    
    private void OnEnable()
    {
        if (!isInitialized)
        {
            InitializeIfNeeded();
        }
        else if (contentPanel == null)
        {
            CreateUI();
            RefreshLeaderboard();
        }
        else
        {
            RefreshLeaderboard();
        }
    }
    
    private void InitializeIfNeeded()
    {
        if (isInitialized && contentPanel != null) return;
        
        // Asegurar que LocalLeaderboardManager existe
        if (LocalLeaderboardManager.Instance == null)
        {
            GameObject leaderboardObj = new GameObject("LocalLeaderboardManager");
            leaderboardObj.AddComponent<LocalLeaderboardManager>();
        }
        
        // Obtener high score del jugador desde PlayerPrefs
        // Usamos la misma clave que ScoreManager (HIGH_SCORE_KEY = "HighScore")
        playerHighScore = PlayerPrefs.GetInt("HighScore", 0);
        
        CreateUI();
        RefreshLeaderboard();
        
        isInitialized = true;
    }
    
    private void CreateUI()
    {
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        // Título
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(transform, false);
        titleText = titleObj.AddComponent<Text>();
        titleText.text = "CLASIFICACIÓN";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 48;
        titleText.color = CosmicTheme.NeonCyan;
        titleText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(800, 80);
        titleRect.anchoredPosition = new Vector2(0, -80);
        
        // ScrollView para la lista
        GameObject scrollViewObj = new GameObject("ScrollView");
        scrollViewObj.transform.SetParent(transform, false);
        scrollRect = scrollViewObj.AddComponent<ScrollRect>();
        
        RectTransform scrollRectTransform = scrollViewObj.GetComponent<RectTransform>();
        // Anclar como en MissionsSection: ocupar casi toda la pantalla con márgenes
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        scrollRectTransform.sizeDelta = Vector2.zero;
        scrollRectTransform.anchoredPosition = Vector2.zero;
        
        // Márgenes similares a misiones (izquierda/derecha/arriba/abajo)
        scrollRectTransform.offsetMin = new Vector2(200, 140);   // left, bottom
        scrollRectTransform.offsetMax = new Vector2(-200, -250); // right, top
        
        // Viewport
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;
        
        Image viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = new Color(0, 0, 0, 0.01f);
        Mask mask = viewportObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        scrollRect.viewport = viewportRect;
        
        // Content Panel
        contentPanel = new GameObject("Content");
        contentPanel.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        // Hacer que el contenido se estire horizontalmente dentro del viewport (como en misiones)
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0, 100);
        contentRect.anchoredPosition = Vector2.zero;
        
        VerticalLayoutGroup layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 15f;
        layout.padding = new RectOffset(50, 50, 20, 20);
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = false;
        
        ContentSizeFitter sizeFitter = contentPanel.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollRect.content = contentRect;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        
        // Scrollbar
        GameObject scrollbarObj = new GameObject("Scrollbar");
        scrollbarObj.transform.SetParent(scrollViewObj.transform, false);
        Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
        
        RectTransform scrollbarRect = scrollbarObj.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(0.5f, 0.02f);
        scrollbarRect.anchorMax = new Vector2(0.5f, 0.98f);
        scrollbarRect.pivot = new Vector2(0.5f, 0.5f);
        scrollbarRect.anchoredPosition = new Vector2(310, 0);
        scrollbarRect.sizeDelta = new Vector2(15, 0);
        
        // Background del scrollbar
        GameObject scrollbarBg = new GameObject("Background");
        scrollbarBg.transform.SetParent(scrollbarObj.transform, false);
        Image scrollbarBgImage = scrollbarBg.AddComponent<Image>();
        scrollbarBgImage.color = new Color(0.1f, 0.1f, 0.2f, 0.8f);
        RectTransform scrollbarBgRect = scrollbarBg.GetComponent<RectTransform>();
        scrollbarBgRect.anchorMin = Vector2.zero;
        scrollbarBgRect.anchorMax = Vector2.one;
        scrollbarBgRect.sizeDelta = Vector2.zero;
        scrollbar.targetGraphic = scrollbarBgImage;
        
        // Handle del scrollbar
        GameObject scrollbarHandle = new GameObject("Handle");
        scrollbarHandle.transform.SetParent(scrollbarBg.transform, false);
        Image scrollbarHandleImage = scrollbarHandle.AddComponent<Image>();
        scrollbarHandleImage.color = CosmicTheme.NeonCyan;
        RectTransform scrollbarHandleRect = scrollbarHandle.GetComponent<RectTransform>();
        scrollbarHandleRect.anchorMin = Vector2.zero;
        scrollbarHandleRect.anchorMax = Vector2.one;
        scrollbarHandleRect.sizeDelta = Vector2.zero;
        scrollbar.handleRect = scrollbarHandleRect;
        
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }
    
    private void RefreshLeaderboard()
    {
        if (contentPanel == null)
        {
            LogWarning("[LeaderboardSection] RefreshLeaderboard llamado pero contentPanel es null");
            return;
        }
        
        // Limpiar entradas existentes
        foreach (Transform child in contentPanel.transform)
        {
            Destroy(child.gameObject);
        }
        
        // Obtener puntuaciones del leaderboard local
        List<LeaderboardEntry> scores = LocalLeaderboardManager.Instance != null 
            ? LocalLeaderboardManager.Instance.GetTopScores() 
            : new List<LeaderboardEntry>();
        
        Log($"[LeaderboardSection] RefreshLeaderboard - entradas encontradas: {scores.Count}");
        
        if (scores.Count == 0)
        {
            Log("[LeaderboardSection] No hay puntuaciones, mostrando mensaje vacío.");
            // Mostrar mensaje si no hay puntuaciones
            GameObject noScoresObj = new GameObject("NoScores");
            noScoresObj.transform.SetParent(contentPanel.transform, false);
            Text noScoresText = noScoresObj.AddComponent<Text>();
            noScoresText.text = "Aún no hay puntuaciones.\n¡Juega para aparecer aquí!";
            noScoresText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            noScoresText.fontSize = 28;
            noScoresText.color = CosmicTheme.NeonCyan;
            noScoresText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform noScoresRect = noScoresObj.GetComponent<RectTransform>();
            noScoresRect.sizeDelta = new Vector2(0, 150);
            
            LayoutElement noScoresLayout = noScoresObj.AddComponent<LayoutElement>();
            noScoresLayout.preferredHeight = 150;
            
            return;
        }
        
        // Crear entrada para cada puntuación
        for (int i = 0; i < scores.Count; i++)
        {
            CreateLeaderboardEntry(i + 1, scores[i]);
        }

        Log($"[LeaderboardSection] Después de crear entradas, hijos en contentPanel: {contentPanel.transform.childCount}");
    }
    
    private void CreateLeaderboardEntry(int rank, LeaderboardEntry entry)
    {
        GameObject entryObj = new GameObject($"Entry_{rank}");
        entryObj.transform.SetParent(contentPanel.transform, false);
        Log($"[LeaderboardSection] Creando entrada #{rank} con score {entry.score}");
        
        RectTransform entryRect = entryObj.AddComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(0, 80);
        
        LayoutElement entryLayout = entryObj.AddComponent<LayoutElement>();
        entryLayout.preferredHeight = 80;
        entryLayout.preferredWidth = 600;
        
        HorizontalLayoutGroup layout = entryObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20f;
        layout.padding = new RectOffset(20, 20, 10, 10);
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;
        
        // Fondo
        Image bgImage = entryObj.AddComponent<Image>();
        bool isPlayerScore = (entry.score == playerHighScore);
        bgImage.color = isPlayerScore 
            ? new Color(0.2f, 0.8f, 1f, 0.3f) // Cyan para tu puntuación
            : new Color(0.1f, 0.1f, 0.2f, 0.5f); // Oscuro para otras
        
        // Rank
        GameObject rankObj = new GameObject("Rank");
        rankObj.transform.SetParent(entryObj.transform, false);
        Text rankText = rankObj.AddComponent<Text>();
        rankText.text = $"#{rank}";
        rankText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rankText.fontSize = 32;
        rankText.color = CosmicTheme.NeonCyan;
        rankText.alignment = TextAnchor.MiddleLeft;
        
        RectTransform rankRect = rankObj.GetComponent<RectTransform>();
        rankRect.sizeDelta = new Vector2(80, 0);
        
        LayoutElement rankLayout = rankObj.AddComponent<LayoutElement>();
        rankLayout.preferredWidth = 80;
        
        // Nombre
        GameObject nameObj = new GameObject("Name");
        nameObj.transform.SetParent(entryObj.transform, false);
        Text nameText = nameObj.AddComponent<Text>();
        nameText.text = entry.playerName;
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 24;
        nameText.color = Color.white;
        nameText.alignment = TextAnchor.MiddleLeft;
        
        LayoutElement nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.flexibleWidth = 1;
        
        // Score
        GameObject scoreObj = new GameObject("Score");
        scoreObj.transform.SetParent(entryObj.transform, false);
        Text scoreText = scoreObj.AddComponent<Text>();
        scoreText.text = entry.score.ToString();
        scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        scoreText.fontSize = 32;
        scoreText.color = CosmicTheme.NeonCyan;
        scoreText.alignment = TextAnchor.MiddleRight;
        scoreText.fontStyle = FontStyle.Bold;
        
        RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
        scoreRect.sizeDelta = new Vector2(150, 0);
        
        LayoutElement scoreLayout = scoreObj.AddComponent<LayoutElement>();
        scoreLayout.preferredWidth = 150;
    }
}

