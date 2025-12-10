using UnityEngine;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    public float radius = 2f;
    public float angle = 0f;
    public float angularSpeed = 2f;
    public Transform center;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public FlashEffect flashEffect;

    private void Start()
    {
        if (!Application.isPlaying) return;
        
        if (center == null)
        {
            GameObject centerObj = GameObject.Find("Center");
            if (centerObj != null)
            {
                center = centerObj.transform;
            }
            else
            {
                center = new GameObject("Center").transform;
                center.position = Vector3.zero;
            }
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Create sprite if none exists - Asteroide Errante
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = LoadPlayerSprite();
            if (spriteRenderer.sprite == null)
            {
                // Fallback a estrella si no se encuentra el sprite
                spriteRenderer.sprite = SpriteGenerator.CreateStarSprite(0.3f, CosmicTheme.EtherealLila);
                spriteRenderer.color = CosmicTheme.EtherealLila;
            }
            else
            {
                spriteRenderer.color = Color.white; // Color blanco para mantener los colores originales
            }
        }
        
        // Agregar cola de partículas
        if (GetComponent<StarParticleTrail>() == null)
        {
            gameObject.AddComponent<StarParticleTrail>();
        }

        if (flashEffect == null)
        {
            flashEffect = GetComponent<FlashEffect>();
        }
    }

    private void Update()
    {
        if (center == null) return;

        // Update angle based on angular speed
        angle += angularSpeed * Time.deltaTime;

        // Calculate position using circular motion
        float x = center.position.x + Mathf.Cos(angle) * radius;
        float y = center.position.y + Mathf.Sin(angle) * radius;

        transform.position = new Vector3(x, y, 0f);
    }

    public void ToggleDirection()
    {
        angularSpeed = -angularSpeed;
        
        // Trigger flash effect
        if (flashEffect != null)
        {
            flashEffect.TriggerFlash();
        }
    }
    
    /// <summary>
    /// Obtiene el tamaño de referencia del Asteroide Errante (el tamaño correcto)
    /// </summary>
    private float GetReferencePlanetSize()
    {
        // Cargar el sprite del Asteroide Errante como referencia
        Sprite referenceSprite = Resources.Load<Sprite>("Art/Protagonist/AsteroideErrante");
        if (referenceSprite != null)
        {
            // Usar bounds.size que da el tamaño real en unidades del mundo (considera el rect visible del sprite)
            float worldSize = Mathf.Max(referenceSprite.bounds.size.x, referenceSprite.bounds.size.y);
            return worldSize;
        }
        
        #if UNITY_EDITOR
        // Fallback en editor
        try
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("AsteroideErrante t:Sprite");
            if (guids.Length == 0)
            {
                guids = UnityEditor.AssetDatabase.FindAssets("AsteroideErrante t:Texture2D");
            }
            
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                referenceSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (referenceSprite != null)
                {
                    float worldSize = Mathf.Max(referenceSprite.bounds.size.x, referenceSprite.bounds.size.y);
                    return worldSize;
                }
            }
        }
        catch { }
        #endif
        
        // Valor por defecto si no se encuentra (aproximado)
        return 1.0f;
    }
    
    /// <summary>
    /// Normaliza un sprite para que tenga el mismo tamaño visual que el Asteroide Errante
    /// </summary>
    private Sprite NormalizePlanetSize(Sprite originalSprite, float targetWorldSize)
    {
        if (originalSprite == null || originalSprite.texture == null) return originalSprite;
        
        // Calcular el tamaño actual del sprite en unidades del mundo usando bounds (tamaño visual real)
        float currentWorldSize = Mathf.Max(originalSprite.bounds.size.x, originalSprite.bounds.size.y);
        
        // Si ya tiene el tamaño correcto (con un margen de error pequeño), no hacer nada
        if (Mathf.Abs(currentWorldSize - targetWorldSize) < 0.01f)
        {
            return originalSprite;
        }
        
        // Calcular el nuevo pixelsPerUnit para que el sprite tenga el tamaño objetivo
        // Usamos el tamaño del rect en píxeles dividido por el tamaño objetivo en unidades del mundo
        float newPixelsPerUnit = Mathf.Max(originalSprite.rect.width, originalSprite.rect.height) / targetWorldSize;
        
        // Crear un nuevo sprite con el pixelsPerUnit ajustado
        return Sprite.Create(
            originalSprite.texture,
            originalSprite.rect,
            originalSprite.pivot,
            newPixelsPerUnit
        );
    }
    
    #if UNITY_EDITOR
    private Sprite LoadPlayerSprite()
    {
        if (!Application.isPlaying) return null;
        
        // Obtener el tamaño de referencia del Asteroide Errante
        float referenceSize = GetReferencePlanetSize();
        
        // Cargar planeta seleccionado guardado
        string selectedPlanet = PlayerPrefs.GetString("SelectedPlanet", "AsteroideErrante");
        
        // Mapeo de nombres de código a nombres reales de archivos (para caracteres especiales)
        Dictionary<string, string> planetNameMapping = new Dictionary<string, string>
        {
            { "PlanetaOceanico", "PlanetaOceánico" }  // Mapear código sin acento a archivo con acento
        };
        
        // Si hay un mapeo, intentar primero con el nombre mapeado
        string actualFileName = planetNameMapping.ContainsKey(selectedPlanet) ? planetNameMapping[selectedPlanet] : selectedPlanet;
        
        try
        {
            // Buscar el sprite del planeta seleccionado
            string[] guids = AssetDatabase.FindAssets($"{actualFileName} t:Sprite");
            if (guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets($"{actualFileName} t:Texture2D");
            }
            
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    // No normalizar - usar el sprite tal como está configurado en Unity
                    return sprite;
                }
                
                // Si no se encuentra como Sprite, intentar como Texture2D
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    // Usar un pixelsPerUnit por defecto razonable (100 es común en Unity)
                    float pixelsPerUnit = 100f;
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                }
            }
            
            // Si no se encuentra con el nombre mapeado, intentar con el nombre original
            if (actualFileName != selectedPlanet)
            {
                guids = AssetDatabase.FindAssets($"{selectedPlanet} t:Sprite");
                if (guids.Length == 0)
                {
                    guids = AssetDatabase.FindAssets($"{selectedPlanet} t:Texture2D");
                }
                
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null)
                    {
                        // No normalizar - usar el sprite tal como está configurado en Unity
                        return sprite;
                    }
                    
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture != null)
                    {
                        // Usar un pixelsPerUnit por defecto razonable (100 es común en Unity)
                        float pixelsPerUnit = 100f;
                        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo cargar el sprite del planeta: {e.Message}");
        }
        return null;
    }
    #else
    private Sprite LoadPlayerSprite()
    {
        // Obtener el tamaño de referencia del Asteroide Errante
        float referenceSize = GetReferencePlanetSize();
        
        // Cargar planeta seleccionado guardado
        string selectedPlanet = PlayerPrefs.GetString("SelectedPlanet", "AsteroideErrante");
        
        // Mapeo de nombres de código a nombres reales de archivos (para caracteres especiales)
        Dictionary<string, string> planetNameMapping = new Dictionary<string, string>
        {
            { "PlanetaOceanico", "PlanetaOceánico" }  // Mapear código sin acento a archivo con acento
        };
        
        // Si hay un mapeo, intentar primero con el nombre mapeado
        string actualFileName = planetNameMapping.ContainsKey(selectedPlanet) ? planetNameMapping[selectedPlanet] : selectedPlanet;
        
        // En build, intentar cargar desde Resources
        Sprite sprite = Resources.Load<Sprite>($"Art/Protagonist/{actualFileName}");
        if (sprite != null)
        {
            // No normalizar - usar el sprite tal como está configurado en Unity
            return sprite;
        }
        
        // Si falla, intentar con el nombre original
        if (actualFileName != selectedPlanet)
        {
            sprite = Resources.Load<Sprite>($"Art/Protagonist/{selectedPlanet}");
            if (sprite != null)
            {
                // No normalizar - usar el sprite tal como está configurado en Unity
                return sprite;
            }
        }
        
        // Si aún falla, intentar cargar todos los sprites y buscar por nombre normalizado
        Object[] allSprites = Resources.LoadAll("Art/Protagonist", typeof(Sprite));
        System.Func<string, string> normalizeName = (name) => {
            if (string.IsNullOrEmpty(name)) return "";
            string lower = name.ToLowerInvariant();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in lower)
            {
                int charCode = (int)c;
                // Ignorar caracteres combinados (diacríticos) - códigos 768-879
                if (charCode >= 768 && charCode <= 879) continue;
                char normalizedChar = c;
                if (charCode >= 224 && charCode <= 230) normalizedChar = 'a';
                else if (charCode >= 232 && charCode <= 235) normalizedChar = 'e';
                else if (charCode >= 236 && charCode <= 239) normalizedChar = 'i';
                else if (charCode >= 242 && charCode <= 246) normalizedChar = 'o';
                else if (charCode >= 249 && charCode <= 252) normalizedChar = 'u';
                else if (charCode == 241) normalizedChar = 'n';
                else if (charCode == 231) normalizedChar = 'c';
                sb.Append(normalizedChar);
            }
            return sb.ToString();
        };
        
        string normalizedSelectedPlanet = normalizeName(selectedPlanet);
        foreach (Object obj in allSprites)
        {
            if (obj is Sprite foundSprite)
            {
                string spriteName = foundSprite.name;
                string normalizedSpriteName = normalizeName(spriteName);
                if (normalizedSpriteName == normalizedSelectedPlanet)
                {
                    // No normalizar - usar el sprite tal como está configurado en Unity
                    return foundSprite;
                }
            }
        }
        
        return null;
    }
    #endif
}

