using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

/// <summary>
/// Panel de estadísticas del jugador - Diseño simple y directo
/// Se monta directamente en el statisticsSection del MainMenuController
/// </summary>
public class StatisticsPanel : MonoBehaviour
{
    // UI Elements
    public GameObject panel; // Referencia pública para MainMenuController
    private GameObject contentArea; // Área donde van las stat cards (dentro del scroll)
    private ScrollRect scrollRect;
    private bool uiCreated = false;
    
    // Tabs
    private Button[] tabButtons = new Button[2];
    private string[] tabNames = { "General", "Historial" };
    private int currentTab = 0;
    
    // =========================================
    // API PÚBLICA
    // =========================================
    
    public void Show()
    {
        Debug.Log("[StatisticsPanel] Show() llamado");
        
        if (!uiCreated)
        {
            BuildUI();
        }
        
        if (panel != null)
        {
            panel.SetActive(true);
        }
        
        currentTab = 0;
        UpdateTabVisuals();
        PopulateContent();
    }
    
    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
    
    // =========================================
    // CONSTRUCCIÓN DE LA UI (una sola vez)
    // =========================================
    
    private void BuildUI()
    {
        if (uiCreated) return;
        
        // Buscar el Canvas principal
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        if (canvas == null)
        {
            Debug.LogError("[StatisticsPanel] No se encontró Canvas!");
            return;
        }
        
        GameObject canvasObj = canvas.gameObject;
        
        // Asegurar EventSystem
        if (EventSystem.current == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }
        
        // Asegurar GraphicRaycaster
        if (canvasObj.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // ---- OVERLAY (fondo oscuro, cubre toda la pantalla) ----
        panel = new GameObject("StatsOverlay");
        panel.transform.SetParent(canvasObj.transform, false);
        panel.transform.SetAsLastSibling();
        
        RectTransform overlayRT = panel.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.sizeDelta = Vector2.zero;
        overlayRT.anchoredPosition = Vector2.zero;
        
        Image overlayBg = panel.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.85f);
        overlayBg.raycastTarget = true;
        
        // Click en overlay (fuera del contenido) cierra el panel
        Button overlayBtn = panel.AddComponent<Button>();
        overlayBtn.transition = Selectable.Transition.None;
        overlayBtn.onClick.AddListener(() =>
        {
            Hide();
            if (MainMenuController.Instance != null)
                MainMenuController.Instance.NavigateTo(MenuSection.Play);
        });
        
        // ---- VENTANA PRINCIPAL (centrada) ----
        GameObject window = CreateChild(panel, "StatsWindow");
        RectTransform windowRT = window.GetComponent<RectTransform>();
        windowRT.anchorMin = new Vector2(0.5f, 0.5f);
        windowRT.anchorMax = new Vector2(0.5f, 0.5f);
        windowRT.pivot = new Vector2(0.5f, 0.5f);
        windowRT.sizeDelta = new Vector2(1000, 800);
        
        Image windowBg = window.AddComponent<Image>();
        windowBg.color = CosmicTheme.GlassPanel;
        windowBg.raycastTarget = true; // Bloquea clicks al overlay
        
        Outline windowOutline = window.AddComponent<Outline>();
        windowOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.5f);
        windowOutline.effectDistance = new Vector2(3, 3);
        
        // ---- TÍTULO ----
        GameObject titleObj = CreateChild(window, "Title");
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0, -10);
        titleRT.sizeDelta = new Vector2(0, 60);
        
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "ESTADÍSTICAS";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = CosmicTheme.NeonCyan;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.raycastTarget = false;
        
        // ---- BOTÓN CERRAR ----
        GameObject closeBtnObj = CreateChild(window, "CloseBtn");
        RectTransform closeRT = closeBtnObj.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1f, 1f);
        closeRT.anchorMax = new Vector2(1f, 1f);
        closeRT.pivot = new Vector2(1f, 1f);
        closeRT.anchoredPosition = new Vector2(-10, -10);
        closeRT.sizeDelta = new Vector2(50, 50);
        
        Image closeBg = closeBtnObj.AddComponent<Image>();
        closeBg.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        
        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeBg;
        closeBtn.onClick.AddListener(() =>
        {
            Hide();
            if (MainMenuController.Instance != null)
                MainMenuController.Instance.NavigateTo(MenuSection.Play);
        });
        
        GameObject closeTextObj = CreateChild(closeBtnObj, "X");
        RectTransform closeTextRT = closeTextObj.GetComponent<RectTransform>();
        closeTextRT.anchorMin = Vector2.zero;
        closeTextRT.anchorMax = Vector2.one;
        closeTextRT.sizeDelta = Vector2.zero;
        
        Text closeText = closeTextObj.AddComponent<Text>();
        closeText.text = "✕";
        closeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeText.fontSize = 32;
        closeText.color = Color.white;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.raycastTarget = false;
        
        // ---- TABS (2 tabs: General e Historial) ----
        GameObject tabBar = CreateChild(window, "TabBar");
        RectTransform tabBarRT = tabBar.GetComponent<RectTransform>();
        tabBarRT.anchorMin = new Vector2(0f, 1f);
        tabBarRT.anchorMax = new Vector2(1f, 1f);
        tabBarRT.pivot = new Vector2(0.5f, 1f);
        tabBarRT.anchoredPosition = new Vector2(0, -75);
        tabBarRT.sizeDelta = new Vector2(-80, 45);
        
        HorizontalLayoutGroup tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 10f;
        tabLayout.childAlignment = TextAnchor.MiddleCenter;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = true;
        
        for (int i = 0; i < 2; i++)
        {
            int tabIndex = i;
            
            GameObject tabObj = CreateChild(tabBar, $"Tab_{tabNames[i]}");
            
            Image tabImg = tabObj.AddComponent<Image>();
            tabImg.color = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.1f);
            
            Button tabBtn = tabObj.AddComponent<Button>();
            tabBtn.targetGraphic = tabImg;
            tabBtn.onClick.AddListener(() =>
            {
                currentTab = tabIndex;
                UpdateTabVisuals();
                PopulateContent();
            });
            
            tabButtons[i] = tabBtn;
            
            GameObject tabTextObj = CreateChild(tabObj, "Label");
            RectTransform tabTextRT = tabTextObj.GetComponent<RectTransform>();
            tabTextRT.anchorMin = Vector2.zero;
            tabTextRT.anchorMax = Vector2.one;
            tabTextRT.sizeDelta = Vector2.zero;
            
            Text tabText = tabTextObj.AddComponent<Text>();
            tabText.text = tabNames[i];
            tabText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tabText.fontSize = 24;
            tabText.color = Color.white;
            tabText.alignment = TextAnchor.MiddleCenter;
            tabText.raycastTarget = false;
        }
        
        // ---- SCROLL VIEW ----
        GameObject scrollGO = CreateChild(window, "ScrollView");
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(20, 20);    // left, bottom
        scrollRT.offsetMax = new Vector2(-20, -130); // right, top (título + tabs)
        
        scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 25f;
        
        // Viewport (la zona visible del scroll)
        GameObject viewport = CreateChild(scrollGO, "Viewport");
        RectTransform vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;
        vpRT.pivot = new Vector2(0f, 1f);
        
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = Color.white;
        vpImg.raycastTarget = true; // Necesario para recibir eventos de drag/scroll
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        
        scrollRect.viewport = vpRT;
        
        // Content (crece verticalmente según su contenido)
        GameObject content = CreateChild(viewport, "Content");
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0, 0);
        contentRT.anchoredPosition = Vector2.zero;
        
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollRect.content = contentRT;
        contentArea = content;
        
        uiCreated = true;
        panel.SetActive(false);
        
        Debug.Log("[StatisticsPanel] UI construida correctamente");
    }
    
    // =========================================
    // POBLAR CONTENIDO
    // =========================================
    
    private void PopulateContent()
    {
        if (contentArea == null)
        {
            Debug.LogError("[StatisticsPanel] contentArea es null!");
            return;
        }
        
        if (!contentArea.activeInHierarchy)
        {
            EnsureHierarchyActive(contentArea);
        }
        
        // Limpiar contenido previo (DestroyImmediate para que se eliminen antes de añadir nuevos)
        for (int i = contentArea.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(contentArea.transform.GetChild(i).gameObject);
        }
        
        // Obtener estadísticas
        PlayerStatistics stats = GetStats();
        
        // Crear contenido según tab activa
        switch (currentTab)
        {
            case 0: CreateOverview(stats); break;
            case 1: CreateHistory(stats); break;
        }
        
        // Forzar rebuild del layout
        Canvas.ForceUpdateCanvases();
        
        RectTransform contentRT = contentArea.GetComponent<RectTransform>();
        if (contentRT != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
        }
        
        // Scroll al inicio
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
        
        Debug.Log($"[StatisticsPanel] Contenido poblado: {contentArea.transform.childCount} elementos, tab: {tabNames[currentTab]}");
    }
    
    private void EnsureHierarchyActive(GameObject obj)
    {
        List<Transform> chain = new List<Transform>();
        Transform t = obj.transform;
        while (t != null)
        {
            chain.Add(t);
            t = t.parent;
        }
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            if (!chain[i].gameObject.activeSelf)
            {
                chain[i].gameObject.SetActive(true);
            }
        }
    }
    
    private PlayerStatistics GetStats()
    {
        PlayerStatistics stats = null;
        SaveData saveData = null;
        
        // Intentar obtener de SaveDataManager
        if (SaveDataManager.Instance != null)
        {
            try
            {
                saveData = SaveDataManager.Instance.GetSaveData();
                if (saveData != null)
                {
                    if (saveData.statistics == null)
                    {
                        saveData.statistics = new PlayerStatistics();
                        SaveDataManager.Instance.MarkDirty();
                    }
                    stats = saveData.statistics;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StatisticsPanel] Error al obtener stats: {e.Message}");
            }
        }
        
        // Fallback a StatisticsManager
        if (stats == null && StatisticsManager.Instance != null)
        {
            try
            {
                stats = StatisticsManager.Instance.GetStatistics();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StatisticsPanel] Error StatisticsManager: {e.Message}");
            }
        }
        
        // Último fallback
        if (stats == null)
        {
            Debug.Log("[StatisticsPanel] Usando estadísticas vacías");
            stats = new PlayerStatistics();
        }
        
        // Sincronizar con datos globales de SaveData (por si las estadísticas
        // se añadieron después de que el jugador ya tuviera partidas)
        if (saveData != null)
        {
            if (saveData.highScore > stats.bestScore)
            {
                stats.bestScore = saveData.highScore;
            }
        }
        
        return stats;
    }
    
    // =========================================
    // CREACIÓN DE CONTENIDO POR TAB
    // =========================================
    
    private void CreateOverview(PlayerStatistics stats)
    {
        if (stats.totalGamesPlayed == 0 && stats.bestScore == 0)
        {
            AddEmptyMessage("¡Juega tu primera partida para ver tus estadísticas!");
            return;
        }
        
        AddCard("Mejor Puntuación", stats.bestScore.ToString(), CosmicTheme.NeonCyan);
        AddCard("Partidas Jugadas", stats.totalGamesPlayed.ToString(), Color.white);
        
        if (stats.longestSurvivalTime > 0)
            AddCard("Mayor Supervivencia", FormatTime(stats.longestSurvivalTime), Color.white);
        
        if (stats.totalPlayTime > 0)
            AddCard("Tiempo Total", FormatTime(stats.totalPlayTime), Color.white);
        
        if (stats.totalObstaclesAvoided > 0)
            AddCard("Obstáculos Esquivados", stats.totalObstaclesAvoided.ToString(), Color.white);
        
        int totalEncounters = stats.totalObstaclesAvoided + stats.totalCollisions;
        if (totalEncounters > 0)
        {
            float successRate = (float)stats.totalObstaclesAvoided / totalEncounters * 100f;
            AddCard("Tasa de Éxito", $"{successRate:F1}%", Color.white);
        }
        
        if (stats.bestStreak > 0)
            AddCard("Mejor Racha", stats.bestStreak.ToString(), Color.white);
    }
    
    private void CreateHistory(PlayerStatistics stats)
    {
        // Estadísticas de hoy
        AddSectionHeader("HOY");
        AddCard("Partidas Hoy", stats.gamesPlayedToday.ToString(), Color.white);
        AddCard("Mejor Hoy", stats.bestScoreToday.ToString(), CosmicTheme.NeonCyan);
        
        // Últimas partidas
        if (stats.recentScores != null && stats.recentScores.Count > 0)
        {
            AddSectionHeader("ÚLTIMAS PARTIDAS");
            int count = Mathf.Min(stats.recentScores.Count, 10);
            for (int i = stats.recentScores.Count - 1; i >= stats.recentScores.Count - count; i--)
            {
                int score = stats.recentScores[i];
                float time = (i < stats.recentPlayTimes.Count) ? stats.recentPlayTimes[i] : 0f;
                int num = stats.recentScores.Count - i;
                AddCard($"#{num}", $"{score} pts  ·  {FormatTime(time)}", new Color(0.8f, 0.8f, 0.9f));
            }
        }
        else
        {
            AddSectionHeader("");
            AddEmptyMessage("Aún no hay partidas registradas");
        }
    }
    
    // =========================================
    // COMPONENTES UI REUTILIZABLES
    // =========================================
    
    private void AddSectionHeader(string title)
    {
        GameObject obj = new GameObject($"Header_{title}");
        obj.transform.SetParent(contentArea.transform, false);
        
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 35;
        le.flexibleWidth = 1;
        
        Text text = obj.AddComponent<Text>();
        text.text = title;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26;
        text.fontStyle = FontStyle.Bold;
        text.color = CosmicTheme.NeonCyan;
        text.alignment = TextAnchor.LowerLeft;
        text.raycastTarget = false;
    }
    
    private void AddEmptyMessage(string message)
    {
        GameObject obj = new GameObject("EmptyMsg");
        obj.transform.SetParent(contentArea.transform, false);
        
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = 60;
        le.flexibleWidth = 1;
        
        Text text = obj.AddComponent<Text>();
        text.text = message;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.color = new Color(0.6f, 0.6f, 0.7f, 0.8f);
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Italic;
        text.raycastTarget = false;
    }
    
    private void AddCard(string title, string value, Color valueColor)
    {
        // Card container
        GameObject card = new GameObject($"Card_{title}");
        card.transform.SetParent(contentArea.transform, false);
        
        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.05f, 0.05f, 0.15f, 0.7f);
        cardBg.raycastTarget = false;
        
        LayoutElement cardLE = card.AddComponent<LayoutElement>();
        cardLE.preferredHeight = 70;
        cardLE.flexibleWidth = 1;
        
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.15f);
        cardOutline.effectDistance = new Vector2(1, 1);
        
        // Título (izquierda)
        GameObject titleObj = CreateChild(card, "Title");
        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0);
        titleRT.anchorMax = new Vector2(0.55f, 1f);
        titleRT.offsetMin = new Vector2(15, 5);
        titleRT.offsetMax = new Vector2(0, -5);
        
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = title;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 22;
        titleText.color = new Color(0.8f, 0.8f, 0.9f, 0.9f);
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.raycastTarget = false;
        
        // Valor (derecha)
        GameObject valueObj = CreateChild(card, "Value");
        RectTransform valueRT = valueObj.GetComponent<RectTransform>();
        valueRT.anchorMin = new Vector2(0.55f, 0);
        valueRT.anchorMax = new Vector2(1f, 1f);
        valueRT.offsetMin = new Vector2(0, 5);
        valueRT.offsetMax = new Vector2(-15, -5);
        
        Text valueText = valueObj.AddComponent<Text>();
        valueText.text = value;
        valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        valueText.fontSize = 28;
        valueText.fontStyle = FontStyle.Bold;
        valueText.color = valueColor;
        valueText.alignment = TextAnchor.MiddleRight;
        valueText.raycastTarget = false;
    }
    
    private void UpdateTabVisuals()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;
            
            Image img = tabButtons[i].GetComponent<Image>();
            Text text = tabButtons[i].GetComponentInChildren<Text>();
            
            bool selected = (i == currentTab);
            
            if (img != null)
            {
                img.color = selected
                    ? new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.35f)
                    : new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.08f);
            }
            if (text != null)
            {
                text.color = selected ? CosmicTheme.NeonCyan : new Color(0.8f, 0.8f, 0.9f, 0.7f);
                text.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }
    }
    
    // =========================================
    // HELPERS
    // =========================================
    
    private GameObject CreateChild(GameObject parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        if (obj.GetComponent<RectTransform>() == null)
        {
            obj.AddComponent<RectTransform>();
        }
        return obj;
    }
    
    private string FormatTime(float seconds)
    {
        if (seconds < 60f)
            return $"{seconds:F1}s";
        else if (seconds < 3600f)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m}m {s}s";
        }
        else
        {
            int h = Mathf.FloorToInt(seconds / 3600f);
            int m = Mathf.FloorToInt((seconds % 3600f) / 60f);
            return $"{h}h {m}m";
        }
    }
}
