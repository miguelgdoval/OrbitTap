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
    private GameObject leftColumn;
    private GameObject rightColumn;
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
        
        // Contenedor principal (sin scroll)
        contentPanel = new GameObject("Content");
        contentPanel.transform.SetParent(transform, false);
        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(1000, 600);
        contentRect.anchoredPosition = new Vector2(0, -50);
        
        // Layout horizontal para las dos columnas
        HorizontalLayoutGroup mainLayout = contentPanel.AddComponent<HorizontalLayoutGroup>();
        mainLayout.spacing = 40f;
        mainLayout.padding = new RectOffset(50, 50, 20, 20);
        mainLayout.childControlHeight = true;
        mainLayout.childControlWidth = false;
        mainLayout.childForceExpandHeight = true;
        mainLayout.childForceExpandWidth = false;
        mainLayout.childAlignment = TextAnchor.MiddleCenter;
        
        // Columna izquierda (posiciones 1-5)
        leftColumn = new GameObject("LeftColumn");
        leftColumn.transform.SetParent(contentPanel.transform, false);
        RectTransform leftColumnRect = leftColumn.AddComponent<RectTransform>();
        leftColumnRect.sizeDelta = new Vector2(450, 0);
        
        VerticalLayoutGroup leftLayout = leftColumn.AddComponent<VerticalLayoutGroup>();
        leftLayout.spacing = 15f;
        leftLayout.padding = new RectOffset(0, 0, 0, 0);
        leftLayout.childControlHeight = false;
        leftLayout.childControlWidth = true;
        leftLayout.childForceExpandWidth = true;
        leftLayout.childForceExpandHeight = false;
        
        LayoutElement leftLayoutElement = leftColumn.AddComponent<LayoutElement>();
        leftLayoutElement.preferredWidth = 450;
        leftLayoutElement.flexibleWidth = 0;
        
        // Columna derecha (posiciones 6-10)
        rightColumn = new GameObject("RightColumn");
        rightColumn.transform.SetParent(contentPanel.transform, false);
        RectTransform rightColumnRect = rightColumn.AddComponent<RectTransform>();
        rightColumnRect.sizeDelta = new Vector2(450, 0);
        
        VerticalLayoutGroup rightLayout = rightColumn.AddComponent<VerticalLayoutGroup>();
        rightLayout.spacing = 15f;
        rightLayout.padding = new RectOffset(0, 0, 0, 0);
        rightLayout.childControlHeight = false;
        rightLayout.childControlWidth = true;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;
        
        LayoutElement rightLayoutElement = rightColumn.AddComponent<LayoutElement>();
        rightLayoutElement.preferredWidth = 450;
        rightLayoutElement.flexibleWidth = 0;
    }
    
    private void RefreshLeaderboard()
    {
        if (contentPanel == null || leftColumn == null || rightColumn == null)
        {
            LogWarning("[LeaderboardSection] RefreshLeaderboard llamado pero paneles son null");
            return;
        }
        
        // Limpiar entradas existentes
        foreach (Transform child in leftColumn.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in rightColumn.transform)
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
            // Mostrar mensaje si no hay puntuaciones (en la columna izquierda)
            GameObject noScoresObj = new GameObject("NoScores");
            noScoresObj.transform.SetParent(leftColumn.transform, false);
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
        
        // Crear entradas distribuidas en dos columnas
        for (int i = 0; i < scores.Count; i++)
        {
            int rank = i + 1;
            Transform parentColumn = (rank <= 5) ? leftColumn.transform : rightColumn.transform;
            
            CreateLeaderboardEntry(rank, scores[i], parentColumn);
        }

        Log($"[LeaderboardSection] Después de crear entradas, izquierda: {leftColumn.transform.childCount}, derecha: {rightColumn.transform.childCount}");
    }
    
    private void CreateLeaderboardEntry(int rank, LeaderboardEntry entry, Transform parent)
    {
        GameObject entryObj = new GameObject($"Entry_{rank}");
        entryObj.transform.SetParent(parent, false);
        Log($"[LeaderboardSection] Creando entrada #{rank} con score {entry.score}");
        
        RectTransform entryRect = entryObj.AddComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(0, 80);
        
        LayoutElement entryLayout = entryObj.AddComponent<LayoutElement>();
        entryLayout.preferredHeight = 80;
        
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

