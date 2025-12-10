using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MissionsSection : MonoBehaviour
{
    private MissionManager missionManager;
    private GameObject contentPanel;
    private ScrollRect scrollRect;
    private bool isInitialized = false;
    
    private void Start()
    {
        Debug.Log("[MissionsSection] Start llamado. activeInHierarchy: " + gameObject.activeInHierarchy);
        // Solo inicializar si el GameObject está activo
        if (gameObject.activeInHierarchy && !isInitialized)
        {
            InitializeIfNeeded();
        }
    }
    
    private void OnEnable()
    {
        Debug.Log("[MissionsSection] OnEnable llamado. isInitialized: " + isInitialized);
        
        // Inicializar cuando se active el GameObject
        if (!isInitialized)
        {
            InitializeIfNeeded();
        }
        else if (contentPanel == null)
        {
            // Si ya estaba inicializado pero la UI fue destruida, recrearla
            Debug.Log("[MissionsSection] UI destruida, recreando...");
            CreateUI();
            RefreshMissions();
        }
        
        // Asegurar que los elementos sean visibles
        if (scrollRect != null && scrollRect.content != null)
        {
            // Forzar actualización del layout
            Canvas.ForceUpdateCanvases();
            Debug.Log($"[MissionsSection] ScrollRect verificado después de OnEnable. Content hijos: {scrollRect.content.childCount}");
        }
    }
    
    private void InitializeIfNeeded()
    {
        if (isInitialized && contentPanel != null) 
        {
            Debug.Log("[MissionsSection] Ya inicializado, saltando...");
            return;
        }
        
        Debug.Log("[MissionsSection] Inicializando...");
        
        missionManager = MissionManager.Instance;
        if (missionManager == null)
        {
            Debug.LogWarning("[MissionsSection] MissionManager no encontrado! Intentando crear...");
            // Intentar crear el MissionManager si no existe
            GameObject missionManagerObj = new GameObject("MissionManager");
            missionManager = missionManagerObj.AddComponent<MissionManager>();
            Debug.Log("[MissionsSection] MissionManager creado.");
        }
        
        if (missionManager == null)
        {
            Debug.LogError("[MissionsSection] No se pudo crear MissionManager!");
            CreateErrorUI();
            return;
        }
        
        Debug.Log("[MissionsSection] MissionManager encontrado. Creando UI...");
        CreateUI();
        
        Debug.Log("[MissionsSection] Refrescando misiones...");
        RefreshMissions();
        
        // Suscribirse a eventos
        if (missionManager != null)
        {
            missionManager.OnMissionProgress += OnMissionProgress;
            missionManager.OnMissionCompleted += OnMissionCompleted;
            Debug.Log("[MissionsSection] Suscrito a eventos del MissionManager.");
        }
        
        isInitialized = true;
        Debug.Log("[MissionsSection] Inicialización completada.");
    }
    
    private void CreateErrorUI()
    {
        // Crear mensaje de error
        GameObject errorObj = new GameObject("ErrorMessage");
        errorObj.transform.SetParent(transform, false);
        Text errorText = errorObj.AddComponent<Text>();
        errorText.text = "Error al cargar misiones.\nPor favor, reinicia el juego.";
        errorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        errorText.fontSize = 32;
        errorText.color = Color.red;
        errorText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform errorRect = errorObj.GetComponent<RectTransform>();
        errorRect.anchorMin = new Vector2(0.5f, 0.5f);
        errorRect.anchorMax = new Vector2(0.5f, 0.5f);
        errorRect.pivot = new Vector2(0.5f, 0.5f);
        errorRect.anchoredPosition = Vector2.zero;
        errorRect.sizeDelta = new Vector2(800, 200);
    }
    
    private void OnDisable()
    {
        if (missionManager != null)
        {
            missionManager.OnMissionProgress -= OnMissionProgress;
            missionManager.OnMissionCompleted -= OnMissionCompleted;
        }
    }
    
    private void CreateUI()
    {
        Debug.Log("[MissionsSection] CreateUI llamado.");
        
        // Título
        GameObject titleObj = new GameObject("MissionsTitle");
        titleObj.transform.SetParent(transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "MISIONES";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 56;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = CosmicTheme.NeonCyan;
        titleText.alignment = TextAnchor.UpperCenter;
        
        Outline titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.8f);
        titleOutline.effectDistance = new Vector2(2, 2);
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -40); // Más cerca del borde superior
        titleRect.sizeDelta = new Vector2(600, 80);
        
        // Scroll View con márgenes en todas las direcciones (más estrecho)
        GameObject scrollObj = new GameObject("MissionsScrollView");
        scrollObj.transform.SetParent(transform, false);
        RectTransform scrollRectTransform = scrollObj.AddComponent<RectTransform>();
        
        // Márgenes: izquierda, derecha, arriba, abajo (más anchos para hacerlo estrecho)
        float marginLeft = 200f; // Aumentado para hacerlo más estrecho
        float marginRight = 200f; // Aumentado para hacerlo más estrecho
        float marginTop = 140f; // Espacio para el título (reducido)
        float marginBottom = 250f; // Espacio para la navegación inferior (aumentado para que no tape las misiones)
        
        scrollRectTransform.anchorMin = new Vector2(0, 0);
        scrollRectTransform.anchorMax = new Vector2(1, 1);
        scrollRectTransform.sizeDelta = new Vector2(-(marginLeft + marginRight), -(marginTop + marginBottom));
        scrollRectTransform.anchoredPosition = new Vector2((marginRight - marginLeft) / 2f, (marginBottom - marginTop) / 2f);
        
        scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        
        // Viewport - Ocupa todo el espacio, la scrollbar estará posicionada después del contenido
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.anchoredPosition = Vector2.zero;
        
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0, 0, 0, 0.01f); // Casi transparente pero no completamente claro para que el Mask funcione
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        scrollRect.viewport = viewportRect;
        
        // Scrollbar vertical - Justo a la derecha del contenido de las misiones
        // El contenido tiene 600px de ancho y está centrado, así que termina en 300px desde el centro
        float contentWidth = 600f;
        float scrollbarWidth = 15f;
        float spacing = 10f; // Espacio entre el contenido y la scrollbar
        
        GameObject scrollbarObj = new GameObject("Scrollbar");
        scrollbarObj.transform.SetParent(scrollObj.transform, false);
        RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(0.5f, 0.02f); // Un poco más grande verticalmente
        scrollbarRect.anchorMax = new Vector2(0.5f, 0.98f);
        scrollbarRect.pivot = new Vector2(0, 0.5f); // Pivot a la izquierda para posicionar desde el borde derecho del contenido
        // Posición: mitad del ancho del contenido + espacio + mitad del ancho de la scrollbar
        scrollbarRect.anchoredPosition = new Vector2(contentWidth / 2f + spacing + scrollbarWidth / 2f, 0);
        scrollbarRect.sizeDelta = new Vector2(scrollbarWidth, 0);
        
        Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        
        // Background de la scrollbar
        GameObject scrollbarBg = new GameObject("Background");
        scrollbarBg.transform.SetParent(scrollbarObj.transform, false);
        Image bgImage = scrollbarBg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.3f, 0.6f);
        
        RectTransform bgRect = scrollbarBg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        scrollbar.targetGraphic = bgImage;
        
        // Handle de la scrollbar
        GameObject scrollbarHandle = new GameObject("Handle");
        scrollbarHandle.transform.SetParent(scrollbarBg.transform, false);
        Image handleImage = scrollbarHandle.AddComponent<Image>();
        handleImage.color = CosmicTheme.NeonCyan;
        
        RectTransform handleRect = scrollbarHandle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = new Vector2(1, 1);
        handleRect.sizeDelta = Vector2.zero;
        handleRect.anchoredPosition = Vector2.zero;
        
        scrollbar.handleRect = handleRect;
        
        // Asignar scrollbar al ScrollRect
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        
        // Content Panel (centrado y más estrecho)
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        // Anclar arriba centrado para scroll vertical
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(600, 100); // Ancho fijo más estrecho (600px)
        contentRect.anchoredPosition = Vector2.zero;
        
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20f;
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false; // No expandir ancho
        layout.childForceExpandHeight = false;
        
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        contentPanel = content;
        scrollRect.content = contentRect;
        
        Debug.Log($"[MissionsSection] UI creada. Content sizeDelta: {contentRect.sizeDelta}, Viewport sizeDelta: {viewportRect.sizeDelta}");
    }
    
    private void RefreshMissions()
    {
        Debug.Log("[MissionsSection] RefreshMissions llamado.");
        
        if (contentPanel == null)
        {
            Debug.LogError("[MissionsSection] contentPanel es null en RefreshMissions!");
            return;
        }
        
        if (missionManager == null)
        {
            Debug.LogError("[MissionsSection] missionManager es null en RefreshMissions!");
            return;
        }
        
        // Limpiar misiones existentes
        foreach (Transform child in contentPanel.transform)
        {
            Destroy(child.gameObject);
        }
        
        // Crear UI para cada misión activa
        List<MissionData> missions = missionManager.GetActiveMissions();
        Debug.Log($"[MissionsSection] Misiones activas encontradas: {missions.Count}");
        
        if (missions.Count == 0)
        {
            Debug.Log("[MissionsSection] No hay misiones activas, mostrando mensaje.");
            // Mostrar mensaje si no hay misiones
            GameObject noMissionsObj = new GameObject("NoMissionsMessage");
            noMissionsObj.transform.SetParent(contentPanel.transform, false);
            Text noMissionsText = noMissionsObj.AddComponent<Text>();
            noMissionsText.text = "¡Todas las misiones completadas!\nVuelve pronto para más desafíos.";
            noMissionsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            noMissionsText.fontSize = 32;
            noMissionsText.color = CosmicTheme.NeonCyan;
            noMissionsText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform noMissionsRect = noMissionsObj.GetComponent<RectTransform>();
            noMissionsRect.sizeDelta = new Vector2(0, 200);
        }
        else
        {
            Debug.Log($"[MissionsSection] Creando {missions.Count} tarjetas de misión.");
            foreach (var mission in missions)
            {
                CreateMissionCard(mission);
            }
            Debug.Log($"[MissionsSection] Tarjetas creadas. Hijos de contentPanel: {contentPanel.transform.childCount}");
        }
        
        // Forzar actualización del layout
        Canvas.ForceUpdateCanvases();
        
        // Verificar tamaños después de crear las tarjetas
        if (contentPanel != null)
        {
            RectTransform contentRect = contentPanel.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                Debug.Log($"[MissionsSection] Después de crear tarjetas - Content sizeDelta: {contentRect.sizeDelta}, anchoredPosition: {contentRect.anchoredPosition}");
            }
        }
        
        if (scrollRect != null && scrollRect.viewport != null)
        {
            Debug.Log($"[MissionsSection] Viewport sizeDelta: {scrollRect.viewport.sizeDelta}");
        }
    }
    
    private void CreateMissionCard(MissionData mission)
    {
        GameObject card = new GameObject($"MissionCard_{mission.id}");
        card.transform.SetParent(contentPanel.transform, false);
        
        // Aumentar altura de la tarjeta para mejor espaciado
        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(0, 180);
        
        // Asegurar que el card tenga un tamaño mínimo visible y ancho controlado
        LayoutElement layoutElement = card.AddComponent<LayoutElement>();
        layoutElement.minHeight = 180;
        layoutElement.preferredHeight = 180;
        layoutElement.preferredWidth = 600; // Ancho preferido para que sea consistente
        
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = mission.isCompleted ? 
            new Color(0.2f, 0.4f, 0.3f, 0.8f) : 
            new Color(0.1f, 0.1f, 0.2f, 0.8f);
        
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = mission.isCompleted ? 
            new Color(0, 1, 0.5f, 0.5f) : 
            new Color(CosmicTheme.NeonCyan.r, CosmicTheme.NeonCyan.g, CosmicTheme.NeonCyan.b, 0.3f);
        cardOutline.effectDistance = new Vector2(2, 2);
        
        // Calcular si hay botón de reclamar para ajustar el ancho disponible
        bool hasClaimButton = mission.isCompleted && !mission.isClaimed;
        float rightMargin = hasClaimButton ? 180f : 20f; // Espacio para botón + margen
        
        // Título - Parte superior
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(card.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = mission.title;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.UpperLeft;
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 20;
        titleText.resizeTextMaxSize = 28;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.65f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.pivot = new Vector2(0, 1f);
        titleRect.anchoredPosition = new Vector2(15, -10);
        titleRect.sizeDelta = new Vector2(-rightMargin - 15, 0);
        
        // Descripción - Parte media
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(card.transform, false);
        Text descText = descObj.AddComponent<Text>();
        descText.text = mission.description;
        descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descText.fontSize = 20;
        descText.color = new Color(0.8f, 0.8f, 0.8f);
        descText.alignment = TextAnchor.UpperLeft;
        descText.resizeTextForBestFit = true;
        descText.resizeTextMinSize = 16;
        descText.resizeTextMaxSize = 20;
        
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0.35f);
        descRect.anchorMax = new Vector2(1, 0.65f);
        descRect.pivot = new Vector2(0, 1f);
        descRect.anchoredPosition = new Vector2(15, -5);
        descRect.sizeDelta = new Vector2(-rightMargin - 15, 0);
        
        // Barra de progreso - Parte inferior
        GameObject progressBarBg = new GameObject("ProgressBarBg");
        progressBarBg.transform.SetParent(card.transform, false);
        Image progressBg = progressBarBg.AddComponent<Image>();
        progressBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        RectTransform progressBgRect = progressBarBg.GetComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0, 0);
        progressBgRect.anchorMax = new Vector2(1, 0.3f);
        progressBgRect.pivot = new Vector2(0, 0);
        progressBgRect.anchoredPosition = new Vector2(15, 15);
        progressBgRect.sizeDelta = new Vector2(-rightMargin - 15, 0);
        
        GameObject progressBar = new GameObject("ProgressBar");
        progressBar.transform.SetParent(progressBarBg.transform, false);
        Image progressFill = progressBar.AddComponent<Image>();
        progressFill.color = mission.isCompleted ? 
            new Color(0, 0.8f, 0.4f, 1f) : 
            CosmicTheme.NeonCyan;
        
        RectTransform progressRect = progressBar.GetComponent<RectTransform>();
        progressRect.anchorMin = Vector2.zero;
        progressRect.anchorMax = new Vector2(mission.GetProgressPercentage(), 1f);
        progressRect.sizeDelta = Vector2.zero;
        progressRect.anchoredPosition = Vector2.zero;
        
        // Texto de progreso
        GameObject progressTextObj = new GameObject("ProgressText");
        progressTextObj.transform.SetParent(progressBarBg.transform, false);
        Text progressText = progressTextObj.AddComponent<Text>();
        progressText.text = $"{mission.currentProgress} / {mission.targetValue}";
        progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        progressText.fontSize = 20;
        progressText.color = Color.white;
        progressText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform progressTextRect = progressTextObj.GetComponent<RectTransform>();
        progressTextRect.anchorMin = Vector2.zero;
        progressTextRect.anchorMax = Vector2.one;
        progressTextRect.sizeDelta = Vector2.zero;
        progressTextRect.anchoredPosition = Vector2.zero;
        
        // Recompensa - Entre descripción y barra de progreso
        GameObject rewardObj = new GameObject("Reward");
        rewardObj.transform.SetParent(card.transform, false);
        Text rewardText = rewardObj.AddComponent<Text>();
        rewardText.text = $"Recompensa: {mission.reward.description}";
        rewardText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rewardText.fontSize = 18;
        rewardText.color = Color.yellow;
        rewardText.alignment = TextAnchor.UpperLeft;
        
        RectTransform rewardRect = rewardObj.GetComponent<RectTransform>();
        rewardRect.anchorMin = new Vector2(0, 0.3f);
        rewardRect.anchorMax = new Vector2(1, 0.35f);
        rewardRect.pivot = new Vector2(0, 1f);
        rewardRect.anchoredPosition = new Vector2(15, -5);
        rewardRect.sizeDelta = new Vector2(-rightMargin - 15, 0);
        
        // Botón de reclamar - Lado derecho, a la misma altura que la barra de progreso
        if (mission.isCompleted && !mission.isClaimed)
        {
            GameObject claimBtn = new GameObject("ClaimButton");
            claimBtn.transform.SetParent(card.transform, false);
            Button button = claimBtn.AddComponent<Button>();
            Image btnImage = claimBtn.AddComponent<Image>();
            btnImage.color = new Color(0, 0.8f, 0.4f, 1f);
            
            RectTransform btnRect = claimBtn.GetComponent<RectTransform>();
            // Misma altura que la barra de progreso (0% a 30%)
            btnRect.anchorMin = new Vector2(1, 0);
            btnRect.anchorMax = new Vector2(1, 0.3f);
            btnRect.pivot = new Vector2(1, 0); // Anclar desde abajo para que el margen sea consistente
            btnRect.anchoredPosition = new Vector2(-15, 15); // Mismo margen inferior que la barra (15px)
            // Botón más pequeño: 120px de ancho
            btnRect.sizeDelta = new Vector2(120, 0);
            
            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(claimBtn.transform, false);
            Text btnText = btnTextObj.AddComponent<Text>();
            btnText.text = "RECLAMAR";
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 18;
            btnText.fontStyle = FontStyle.Bold;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.resizeTextForBestFit = true;
            btnText.resizeTextMinSize = 14;
            btnText.resizeTextMaxSize = 18;
            
            RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;
            btnTextRect.anchoredPosition = Vector2.zero;
            
            Outline btnOutline = claimBtn.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0, 1, 0.5f, 0.8f);
            btnOutline.effectDistance = new Vector2(2, 2);
            
            button.onClick.AddListener(() => OnClaimButtonClicked(mission));
        }
    }
    
    private void OnClaimButtonClicked(MissionData mission)
    {
        if (missionManager != null && missionManager.ClaimReward(mission))
        {
            RefreshMissions();
        }
    }
    
    private void OnMissionProgress(MissionData mission)
    {
        RefreshMissions();
    }
    
    private void OnMissionCompleted(MissionData mission)
    {
        RefreshMissions();
        // Aquí podrías mostrar una notificación
        Debug.Log($"¡Misión completada: {mission.title}!");
    }
}

