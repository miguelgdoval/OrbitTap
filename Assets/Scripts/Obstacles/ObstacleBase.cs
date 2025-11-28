using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObstacleBase : MonoBehaviour
{
    /// <summary>
    /// Carga un sprite de obstáculo desde Assets/Art/Obstacles/
    /// Normaliza el tamaño para que sea consistente con los sprites generados (~1.25 unidades del mundo)
    /// </summary>
    protected Sprite LoadObstacleSprite(string spriteName, float targetWorldSize = 1.25f)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return null;
        
        try
        {
            string spritePath = $"Assets/Art/Obstacles/{spriteName}.png";
            
            // Siempre cargar como Texture2D para tener control sobre el pixelsPerUnit
            Texture2D texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            if (texture2D != null)
            {
                // Calcular el pixelsPerUnit necesario para obtener el tamaño objetivo
                // targetWorldSize = textureWidth / pixelsPerUnit
                // pixelsPerUnit = textureWidth / targetWorldSize
                // Usar el ancho de la textura como referencia para normalizar el tamaño
                float targetPixelsPerUnit = texture2D.width / targetWorldSize;
                
                // Crear sprite desde texture con el tamaño normalizado
                return Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), targetPixelsPerUnit);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo cargar el sprite {spriteName}: {e.Message}");
        }
#else
        // En build, intentar cargar desde Resources
        Texture2D texture = Resources.Load<Texture2D>($"Art/Obstacles/{spriteName}");
        if (texture != null)
        {
            float targetPixelsPerUnit = texture.width / targetWorldSize;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), targetPixelsPerUnit);
        }
        
        // Fallback: intentar como Sprite
        Sprite loadedSprite = Resources.Load<Sprite>($"Art/Obstacles/{spriteName}");
        if (loadedSprite != null)
        {
            // Normalizar el tamaño también en build
            float currentWorldSize = loadedSprite.rect.width / loadedSprite.pixelsPerUnit;
            if (Mathf.Abs(currentWorldSize - targetWorldSize) > 0.2f && loadedSprite.texture != null)
            {
                Texture2D spriteTexture = loadedSprite.texture;
                float targetPixelsPerUnit2 = spriteTexture.width / targetWorldSize;
                return Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0.5f, 0.5f), targetPixelsPerUnit2);
            }
            return loadedSprite;
        }
#endif
        return null;
    }
    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return;
        
        // Verificar por tag o por nombre
        bool isPlayer = collision.CompareTag("Player") || 
                       collision.gameObject.name == "Player" ||
                       collision.gameObject.CompareTag("Player");
        
        if (isPlayer)
        {
            Debug.Log("Colisión detectada con Player! GameObject: " + collision.gameObject.name);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
            else
            {
                Debug.LogError("GameManager.Instance es null!");
            }
        }
    }

    // También detectar colisiones normales por si acaso
    public virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.gameObject == null) return;
        
        bool isPlayer = collision.gameObject.CompareTag("Player") || 
                       collision.gameObject.name == "Player";
        
        if (isPlayer)
        {
            Debug.Log("Colisión normal detectada con Player!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
    }
}

