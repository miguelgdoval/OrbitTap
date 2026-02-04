using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static LogHelper;

/// <summary>
/// Manager para gestionar las opciones de accesibilidad del juego
/// </summary>
public class AccessibilityManager : MonoBehaviour
{
    public static AccessibilityManager Instance { get; private set; }
    
    private bool colorBlindMode = false;
    private bool highContrastUI = false;
    private bool reduceAnimations = false;
    
    // Colores para modo daltónico (protanopia/deuteranopia)
    private Color colorBlindObstacleColor = new Color(0.4f, 0.6f, 1f, 1f); // Azul más distinguible
    private Color colorBlindTextColor = new Color(1f, 1f, 0.8f, 1f); // Amarillo claro
    
    // Colores para alto contraste
    private Color highContrastTextColor = Color.white;
    private Color highContrastBackgroundColor = Color.black;
    
    // Lista de elementos UI que necesitan actualización
    private List<Text> uiTexts = new List<Text>();
    private List<Image> uiImages = new List<Image>();
    
    // Eventos
    public System.Action OnAccessibilitySettingsChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadSettings()
    {
        // Usar SaveDataManager si está disponible
        if (SaveDataManager.Instance != null)
        {
            SaveData saveData = SaveDataManager.Instance.GetSaveData();
            if (saveData != null)
            {
                colorBlindMode = saveData.colorBlindMode;
                highContrastUI = saveData.highContrastUI;
                reduceAnimations = saveData.reduceAnimations;
                ApplySettings();
                return;
            }
        }
        
        // Fallback a PlayerPrefs
        colorBlindMode = PlayerPrefs.GetInt("ColorBlindMode", 0) == 1;
        highContrastUI = PlayerPrefs.GetInt("HighContrastUI", 0) == 1;
        reduceAnimations = PlayerPrefs.GetInt("ReduceAnimations", 0) == 1;
        
        ApplySettings();
    }
    
    /// <summary>
    /// Establece el modo daltónico
    /// </summary>
    public void SetColorBlindMode(bool enabled)
    {
        if (colorBlindMode != enabled)
        {
            colorBlindMode = enabled;
            
            // Usar SaveDataManager si está disponible
            if (SaveDataManager.Instance != null)
            {
                SaveData saveData = SaveDataManager.Instance.GetSaveData();
                if (saveData != null)
                {
                    saveData.colorBlindMode = enabled;
                    SaveDataManager.Instance.MarkDirty();
                }
            }
            else
            {
                // Fallback a PlayerPrefs
                PlayerPrefs.SetInt("ColorBlindMode", enabled ? 1 : 0);
                PlayerPrefs.Save();
            }
            
            ApplySettings();
            OnAccessibilitySettingsChanged?.Invoke();
        }
    }
    
    /// <summary>
    /// Establece el alto contraste UI
    /// </summary>
    public void SetHighContrastUI(bool enabled)
    {
        if (highContrastUI != enabled)
        {
            highContrastUI = enabled;
            
            // Usar SaveDataManager si está disponible
            if (SaveDataManager.Instance != null)
            {
                SaveData saveData = SaveDataManager.Instance.GetSaveData();
                if (saveData != null)
                {
                    saveData.highContrastUI = enabled;
                    SaveDataManager.Instance.MarkDirty();
                }
            }
            else
            {
                // Fallback a PlayerPrefs
                PlayerPrefs.SetInt("HighContrastUI", enabled ? 1 : 0);
                PlayerPrefs.Save();
            }
            
            ApplySettings();
            OnAccessibilitySettingsChanged?.Invoke();
        }
    }
    
    /// <summary>
    /// Establece la reducción de animaciones
    /// </summary>
    public void SetReduceAnimations(bool enabled)
    {
        if (reduceAnimations != enabled)
        {
            reduceAnimations = enabled;
            
            // Usar SaveDataManager si está disponible
            if (SaveDataManager.Instance != null)
            {
                SaveData saveData = SaveDataManager.Instance.GetSaveData();
                if (saveData != null)
                {
                    saveData.reduceAnimations = enabled;
                    SaveDataManager.Instance.MarkDirty();
                }
            }
            else
            {
                // Fallback a PlayerPrefs
                PlayerPrefs.SetInt("ReduceAnimations", enabled ? 1 : 0);
                PlayerPrefs.Save();
            }
            
            ApplySettings();
            OnAccessibilitySettingsChanged?.Invoke();
        }
    }
    
    /// <summary>
    /// Aplica todas las configuraciones de accesibilidad
    /// </summary>
    private void ApplySettings()
    {
        // Actualizar colores de obstáculos si está en modo daltónico
        if (colorBlindMode)
        {
            ApplyColorBlindMode();
        }
        
        // Actualizar UI si está en alto contraste (siempre aplicar, incluso si no hay objetos aún)
        if (highContrastUI)
        {
            ApplyHighContrastUI();
        }
        
        // Reducir animaciones si está habilitado
        if (reduceAnimations)
        {
            ApplyReduceAnimations();
        }
        
        Log($"[AccessibilityManager] Configuraciones aplicadas - ColorBlind: {colorBlindMode}, HighContrast: {highContrastUI}, ReduceAnimations: {reduceAnimations}");
    }
    
    /// <summary>
    /// Aplica el modo daltónico a los obstáculos
    /// </summary>
    private void ApplyColorBlindMode()
    {
        // Buscar todos los obstáculos y cambiar sus colores
        ObstacleDestructionController[] obstacles = FindObjectsByType<ObstacleDestructionController>(FindObjectsSortMode.None);
        int obstaclesUpdated = 0;
        foreach (var obstacle in obstacles)
        {
            SpriteRenderer[] renderers = obstacle.GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                // Cambiar a colores más distinguibles para daltónicos
                if (renderer.color.r > 0.5f && renderer.color.g < 0.5f)
                {
                    // Si es rojo/magenta, cambiar a azul
                    renderer.color = colorBlindObstacleColor;
                    obstaclesUpdated++;
                }
            }
        }
        
        Log($"[AccessibilityManager] Modo daltónico aplicado a {obstaclesUpdated} renderers (obstáculos encontrados: {obstacles.Length})");
        
        // Si no hay obstáculos, el modo se aplicará cuando se spawneen
        if (obstacles.Length == 0)
        {
            Log("[AccessibilityManager] No hay obstáculos en la escena. El modo daltónico se aplicará cuando se spawneen.");
        }
    }
    
    /// <summary>
    /// Aplica alto contraste a la UI
    /// </summary>
    private void ApplyHighContrastUI()
    {
        // Buscar todos los textos de UI y aumentar contraste
        Text[] allTexts = FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int textsUpdated = 0;
        foreach (var text in allTexts)
        {
            // Aplicar alto contraste a todos los textos (no solo los oscuros)
            // Para mejor visibilidad, usar blanco con outline negro
            text.color = highContrastTextColor;
            
            // Añadir o actualizar outline para mejor visibilidad
            Outline outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, 2);
            textsUpdated++;
        }
        
        Log($"[AccessibilityManager] Alto contraste UI aplicado a {textsUpdated} textos");
    }
    
    /// <summary>
    /// Reduce las animaciones del juego
    /// </summary>
    private void ApplyReduceAnimations()
    {
        // Reducir velocidad de animaciones de partículas
        ParticleSystem[] particles = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var particle in particles)
        {
            var main = particle.main;
            main.simulationSpeed = 0.5f; // Reducir a la mitad
        }
        
        // Desactivar animaciones de UI innecesarias
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (var animator in animators)
        {
            // Solo desactivar animadores de UI, no de gameplay
            if (animator.gameObject.layer == 5) // UI layer
            {
                animator.speed = 0.5f; // Reducir velocidad
            }
        }
        
        Log("[AccessibilityManager] Animaciones reducidas");
    }
    
    /// <summary>
    /// Obtiene el color ajustado para modo daltónico
    /// </summary>
    public Color GetColorBlindColor(Color originalColor)
    {
        if (!colorBlindMode) return originalColor;
        
        // Convertir colores rojos/magentas a azules para mejor visibilidad
        if (originalColor.r > 0.5f && originalColor.g < 0.5f)
        {
            return colorBlindObstacleColor;
        }
        
        return originalColor;
    }
    
    /// <summary>
    /// Obtiene el color de texto ajustado para alto contraste
    /// </summary>
    public Color GetHighContrastTextColor(Color originalColor)
    {
        if (!highContrastUI) return originalColor;
        
        // Si el color es oscuro, usar blanco; si es claro, usar negro
        float brightness = originalColor.r + originalColor.g + originalColor.b;
        return brightness < 1.5f ? highContrastTextColor : Color.black;
    }
    
    /// <summary>
    /// Verifica si las animaciones deben reducirse
    /// </summary>
    public bool ShouldReduceAnimations()
    {
        return reduceAnimations;
    }
    
    // Getters
    public bool IsColorBlindModeEnabled() => colorBlindMode;
    public bool IsHighContrastUIEnabled() => highContrastUI;
    public bool IsReduceAnimationsEnabled() => reduceAnimations;
    
    // ========== Métodos públicos para aplicar a elementos nuevos ==========
    
    /// <summary>
    /// Aplica alto contraste a un texto específico (llamar cuando se crea un nuevo Text)
    /// </summary>
    public void ApplyHighContrastToText(Text text)
    {
        if (!highContrastUI || text == null) return;
        
        text.color = highContrastTextColor;
        
        // Añadir o actualizar outline para mejor visibilidad
        Outline outline = text.GetComponent<Outline>();
        if (outline == null)
        {
            outline = text.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, 2);
    }
    
    /// <summary>
    /// Aplica reducción de animaciones a un ParticleSystem específico (llamar cuando se crea uno nuevo)
    /// </summary>
    public void ApplyReduceAnimationsToParticle(ParticleSystem particle)
    {
        if (!reduceAnimations || particle == null) return;
        
        var main = particle.main;
        main.simulationSpeed = 0.5f; // Reducir a la mitad
    }
    
    /// <summary>
    /// Aplica reducción de animaciones a un Animator específico (llamar cuando se crea uno nuevo)
    /// </summary>
    public void ApplyReduceAnimationsToAnimator(Animator animator)
    {
        if (!reduceAnimations || animator == null) return;
        
        // Solo reducir velocidad de animadores de UI, no de gameplay
        if (animator.gameObject.layer == 5) // UI layer
        {
            animator.speed = 0.5f; // Reducir velocidad
        }
    }
    
    /// <summary>
    /// Aplica modo daltónico a un SpriteRenderer específico (llamar cuando se crea un obstáculo)
    /// </summary>
    public void ApplyColorBlindToRenderer(SpriteRenderer renderer)
    {
        if (!colorBlindMode || renderer == null) return;
        
        // Cambiar a colores más distinguibles para daltónicos
        if (renderer.color.r > 0.5f && renderer.color.g < 0.5f)
        {
            // Si es rojo/magenta, cambiar a azul
            renderer.color = colorBlindObstacleColor;
        }
    }
}
