using UnityEngine;
using static LogHelper;

/// <summary>
/// Helper para cargar recursos de forma segura con manejo de errores robusto
/// </summary>
public static class ResourceLoader
{
    /// <summary>
    /// Sprite por defecto para usar cuando falla la carga
    /// </summary>
    private static Sprite defaultSprite = null;
    
    /// <summary>
    /// Carga un Sprite de forma segura con manejo de errores
    /// </summary>
    public static Sprite LoadSprite(string resourcePath, string assetName = null, bool logErrors = true)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            if (logErrors)
                LogError($"[ResourceLoader] Ruta de recurso vacía o nula");
            return GetDefaultSprite();
        }
        
        try
        {
            // Intentar cargar como Sprite
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }
            
            // Intentar cargar como Texture2D y convertir
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                if (logErrors)
                    Log($"[ResourceLoader] Sprite creado desde Texture2D: {resourcePath}");
                return sprite;
            }
            
            #if UNITY_EDITOR
            // En el editor, intentar usar AssetDatabase como fallback
            if (!string.IsNullOrEmpty(assetName))
            {
                try
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets(assetName + " t:Sprite");
                    if (guids.Length == 0)
                    {
                        guids = UnityEditor.AssetDatabase.FindAssets(assetName + " t:Texture2D");
                    }
                    
                    if (guids.Length > 0)
                    {
                        string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                        sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        if (sprite != null)
                        {
                            if (logErrors)
                                Log($"[ResourceLoader] Sprite cargado desde AssetDatabase: {path}");
                            return sprite;
                        }
                        
                        texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        if (texture != null)
                        {
                            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                            if (logErrors)
                                Log($"[ResourceLoader] Sprite creado desde Texture2D (AssetDatabase): {path}");
                            return sprite;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    if (logErrors)
                        LogWarning($"[ResourceLoader] Error al buscar en AssetDatabase: {e.Message}");
                }
            }
            #endif
            
            if (logErrors)
                LogWarning($"[ResourceLoader] No se pudo cargar sprite desde: {resourcePath}");
            
            return GetDefaultSprite();
        }
        catch (System.Exception e)
        {
            if (logErrors)
                LogError($"[ResourceLoader] Excepción al cargar sprite {resourcePath}: {e.Message}");
            return GetDefaultSprite();
        }
    }
    
    /// <summary>
    /// Carga un GameObject (prefab) de forma segura
    /// </summary>
    public static GameObject LoadPrefab(string resourcePath, bool logErrors = true)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            if (logErrors)
                LogError($"[ResourceLoader] Ruta de recurso vacía o nula");
            return null;
        }
        
        try
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
            {
                return prefab;
            }
            
            #if UNITY_EDITOR
            // En el editor, intentar usar AssetDatabase
            try
            {
                string path = $"Assets/{resourcePath}.prefab";
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    if (logErrors)
                        Log($"[ResourceLoader] Prefab cargado desde AssetDatabase: {path}");
                    return prefab;
                }
            }
            catch (System.Exception e)
            {
                if (logErrors)
                    LogWarning($"[ResourceLoader] Error al buscar prefab en AssetDatabase: {e.Message}");
            }
            #endif
            
            if (logErrors)
                LogWarning($"[ResourceLoader] No se pudo cargar prefab desde: {resourcePath}");
            
            return null;
        }
        catch (System.Exception e)
        {
            if (logErrors)
                LogError($"[ResourceLoader] Excepción al cargar prefab {resourcePath}: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Carga un Texture2D de forma segura
    /// </summary>
    public static Texture2D LoadTexture(string resourcePath, bool logErrors = true)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            if (logErrors)
                LogError($"[ResourceLoader] Ruta de recurso vacía o nula");
            return null;
        }
        
        try
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                return texture;
            }
            
            #if UNITY_EDITOR
            // En el editor, intentar usar AssetDatabase
            try
            {
                string path = $"Assets/{resourcePath}.png";
                texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    if (logErrors)
                        Log($"[ResourceLoader] Texture cargado desde AssetDatabase: {path}");
                    return texture;
                }
            }
            catch (System.Exception e)
            {
                if (logErrors)
                    LogWarning($"[ResourceLoader] Error al buscar texture en AssetDatabase: {e.Message}");
            }
            #endif
            
            if (logErrors)
                LogWarning($"[ResourceLoader] No se pudo cargar texture desde: {resourcePath}");
            
            return null;
        }
        catch (System.Exception e)
        {
            if (logErrors)
                LogError($"[ResourceLoader] Excepción al cargar texture {resourcePath}: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Carga todos los objetos de un tipo desde una ruta
    /// </summary>
    public static T[] LoadAll<T>(string resourcePath, bool logErrors = true) where T : Object
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            if (logErrors)
                LogError($"[ResourceLoader] Ruta de recurso vacía o nula");
            return new T[0];
        }
        
        try
        {
            T[] objects = Resources.LoadAll<T>(resourcePath);
            if (objects != null && objects.Length > 0)
            {
                return objects;
            }
            
            if (logErrors)
                LogWarning($"[ResourceLoader] No se encontraron objetos de tipo {typeof(T).Name} en: {resourcePath}");
            
            return new T[0];
        }
        catch (System.Exception e)
        {
            if (logErrors)
                LogError($"[ResourceLoader] Excepción al cargar objetos desde {resourcePath}: {e.Message}");
            return new T[0];
        }
    }
    
    /// <summary>
    /// Obtiene un sprite por defecto (crea uno simple si no existe)
    /// </summary>
    private static Sprite GetDefaultSprite()
    {
        if (defaultSprite == null)
        {
            // Crear un sprite simple de 1x1 píxel blanco como fallback
            Texture2D defaultTexture = new Texture2D(1, 1);
            defaultTexture.SetPixel(0, 0, Color.white);
            defaultTexture.Apply();
            defaultSprite = Sprite.Create(defaultTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
            defaultSprite.name = "DefaultSprite";
        }
        return defaultSprite;
    }
}
