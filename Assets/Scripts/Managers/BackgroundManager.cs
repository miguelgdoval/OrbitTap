using UnityEngine;
using System.Collections;

/// <summary>
/// Gestiona el sistema de fondos dinámicos del juego.
/// Controla qué fondo está activo y maneja las transiciones entre fondos.
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    public static BackgroundManager Instance { get; private set; }
    
    [Header("Background Prefabs")]
    [SerializeField] private GameObject[] backgrounds;
    
    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.75f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Current State")]
    [SerializeField] private int currentBackgroundIndex = -1;
    
    private GameObject currentBackground;
    private CanvasGroup[] backgroundCanvasGroups; // Para crossfade si usamos UI
    private SpriteRenderer[][] backgroundRenderers; // Para crossfade con SpriteRenderer
    
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
            return;
        }
        
        InitializeBackgrounds();
    }
    
    private void InitializeBackgrounds()
    {
        if (backgrounds == null || backgrounds.Length == 0)
        {
            Debug.LogWarning("BackgroundManager: No backgrounds assigned!");
            return;
        }
        
        // Inicializar arrays para transiciones
        backgroundCanvasGroups = new CanvasGroup[backgrounds.Length];
        backgroundRenderers = new SpriteRenderer[backgrounds.Length][];
        
        // Instanciar todos los backgrounds y desactivarlos
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] == null)
            {
                Debug.LogWarning($"BackgroundManager: Background at index {i} is null!");
                continue;
            }
            
            // Instanciar el prefab
            GameObject bgInstance = Instantiate(backgrounds[i], transform);
            bgInstance.name = backgrounds[i].name;
            bgInstance.SetActive(false);
            backgrounds[i] = bgInstance; // Reemplazar prefab con instancia
            
            // Preparar para transiciones (SpriteRenderer)
            backgroundRenderers[i] = bgInstance.GetComponentsInChildren<SpriteRenderer>();
            
            // Preparar para transiciones (UI CanvasGroup si existe)
            CanvasGroup cg = bgInstance.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = bgInstance.AddComponent<CanvasGroup>();
            }
            backgroundCanvasGroups[i] = cg;
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        
        // Activar el primer fondo por defecto
        if (backgrounds.Length > 0)
        {
            SetBackground(0);
        }
    }
    
    /// <summary>
    /// Cambia el fondo activo inmediatamente sin transición
    /// </summary>
    public void SetBackground(int index)
    {
        if (index < 0 || index >= backgrounds.Length)
        {
            Debug.LogWarning($"BackgroundManager: Invalid background index {index}");
            return;
        }
        
        // Desactivar fondo actual
        if (currentBackground != null)
        {
            currentBackground.SetActive(false);
        }
        
        // Activar nuevo fondo
        currentBackground = backgrounds[index];
        currentBackgroundIndex = index;
        
        if (currentBackground != null)
        {
            currentBackground.SetActive(true);
            
            // Asegurar que está completamente visible
            CanvasGroup cg = backgroundCanvasGroups[index];
            if (cg != null)
            {
                cg.alpha = 1f;
            }
            
            // Asegurar que todos los SpriteRenderers tengan alpha = 1
            SpriteRenderer[] renderers = backgroundRenderers[index];
            if (renderers != null)
            {
                foreach (SpriteRenderer sr in renderers)
                {
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = 1f;
                        sr.color = c;
                    }
                }
            }
            
            // Resetear todas las capas del nuevo fondo
            BackgroundLayer[] layers = currentBackground.GetComponentsInChildren<BackgroundLayer>();
            foreach (BackgroundLayer layer in layers)
            {
                layer.ResetLayer();
            }
        }
        
        Debug.Log($"BackgroundManager: Background changed to index {index}");
    }
    
    /// <summary>
    /// Cambia el fondo con una transición suave (crossfade)
    /// </summary>
    public void SmoothTransition(int newIndex)
    {
        if (newIndex < 0 || newIndex >= backgrounds.Length)
        {
            Debug.LogWarning($"BackgroundManager: Invalid background index {newIndex}");
            return;
        }
        
        if (newIndex == currentBackgroundIndex)
        {
            return; // Ya está activo
        }
        
        StartCoroutine(TransitionCoroutine(newIndex));
    }
    
    private IEnumerator TransitionCoroutine(int newIndex)
    {
        int oldIndex = currentBackgroundIndex;
        GameObject oldBackground = currentBackground;
        
        // Activar nuevo fondo
        GameObject newBackground = backgrounds[newIndex];
        newBackground.SetActive(true);
        
        // Resetear capas del nuevo fondo
        BackgroundLayer[] newLayers = newBackground.GetComponentsInChildren<BackgroundLayer>();
        foreach (BackgroundLayer layer in newLayers)
        {
            layer.ResetLayer();
        }
        
        // Obtener componentes para fade
        CanvasGroup oldCG = oldIndex >= 0 ? backgroundCanvasGroups[oldIndex] : null;
        CanvasGroup newCG = backgroundCanvasGroups[newIndex];
        
        SpriteRenderer[] oldRenderers = oldIndex >= 0 ? backgroundRenderers[oldIndex] : null;
        SpriteRenderer[] newRenderers = backgroundRenderers[newIndex];
        
        float elapsed = 0f;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            float curveValue = fadeCurve.Evaluate(t);
            
            // Fade out del fondo anterior
            if (oldCG != null)
            {
                oldCG.alpha = 1f - curveValue;
            }
            if (oldRenderers != null)
            {
                foreach (SpriteRenderer sr in oldRenderers)
                {
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = 1f - curveValue;
                        sr.color = c;
                    }
                }
            }
            
            // Fade in del nuevo fondo
            if (newCG != null)
            {
                newCG.alpha = curveValue;
            }
            if (newRenderers != null)
            {
                foreach (SpriteRenderer sr in newRenderers)
                {
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = curveValue;
                        sr.color = c;
                    }
                }
            }
            
            yield return null;
        }
        
            // Asegurar valores finales
            if (oldCG != null) oldCG.alpha = 0f;
            if (newCG != null) newCG.alpha = 1f;
            
            if (oldRenderers != null)
            {
                foreach (SpriteRenderer sr in oldRenderers)
                {
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = 0f;
                        sr.color = c;
                    }
                }
            }
            
            if (newRenderers != null)
            {
                foreach (SpriteRenderer sr in newRenderers)
                {
                    if (sr != null)
                    {
                        Color c = sr.color;
                        c.a = 1f;
                        sr.color = c;
                    }
                }
            }
            
            // Asegurar que el nuevo fondo esté completamente visible
            if (newCG != null)
            {
                newCG.alpha = 1f;
            }
        
        // Desactivar fondo anterior
        if (oldBackground != null)
        {
            oldBackground.SetActive(false);
        }
        
        currentBackground = newBackground;
        currentBackgroundIndex = newIndex;
        
        Debug.Log($"BackgroundManager: Smooth transition completed to index {newIndex}");
    }
    
    /// <summary>
    /// Actualiza el fondo según el nivel de dificultad
    /// 0 → VoidHorizon
    /// 1 → NebulaDrift
    /// 2 → CosmicSurge
    /// 3 → SolarRift
    /// 4 → EventHorizon
    /// </summary>
    public void UpdateDifficulty(int difficultyLevel)
    {
        int backgroundIndex = Mathf.Clamp(difficultyLevel, 0, backgrounds.Length - 1);
        
        if (backgroundIndex != currentBackgroundIndex)
        {
            SmoothTransition(backgroundIndex);
        }
    }
    
    /// <summary>
    /// Actualiza el fondo según el ObstacleDifficultyLevel del juego
    /// Easy (0) → VoidHorizon
    /// Medium (1) → NebulaDrift
    /// Hard (2) → CosmicSurge
    /// VeryHard (3) → SolarRift
    /// Extra (4+) → EventHorizon
    /// </summary>
    public void UpdateDifficulty(ObstacleDifficultyLevel difficultyLevel)
    {
        int level = (int)difficultyLevel;
        
        // Mapear niveles de dificultad a fondos (puede haber más fondos que niveles)
        // Si hay 5 fondos y solo 4 niveles, el nivel 3+ usa el fondo 4
        int backgroundIndex = Mathf.Min(level, backgrounds.Length - 1);
        
        if (backgroundIndex != currentBackgroundIndex)
        {
            SmoothTransition(backgroundIndex);
        }
    }
    
    /// <summary>
    /// Obtiene el índice del fondo actual
    /// </summary>
    public int GetCurrentBackgroundIndex()
    {
        return currentBackgroundIndex;
    }
    
    /// <summary>
    /// Establece la duración de las transiciones
    /// </summary>
    public void SetTransitionDuration(float duration)
    {
        transitionDuration = Mathf.Clamp(duration, 0.1f, 5f);
    }
}

