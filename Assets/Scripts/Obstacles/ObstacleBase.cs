using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObstacleBase : MonoBehaviour
{
    /// <summary>
    /// Carga un sprite de obstáculo desde Assets/Art/Obstacles/
    /// Normaliza el tamaño para que sea consistente con los sprites generados (~1.25 unidades del mundo)
    /// Funciona tanto en editor como en builds
    /// </summary>
    protected Sprite LoadObstacleSprite(string spriteName, float targetWorldSize = 1.25f)
    {
        if (!Application.isPlaying) return null;
        
        // Primero intentar cargar desde Resources (funciona en editor y builds si están en carpeta Resources)
        Texture2D texture = Resources.Load<Texture2D>($"Art/Obstacles/{spriteName}");
        if (texture == null)
        {
            // Intentar como Sprite desde Resources
            Sprite loadedSprite = Resources.Load<Sprite>($"Art/Obstacles/{spriteName}");
            if (loadedSprite != null)
            {
                // Normalizar el tamaño si es necesario
                float currentWorldSize = loadedSprite.rect.width / loadedSprite.pixelsPerUnit;
                if (Mathf.Abs(currentWorldSize - targetWorldSize) > 0.2f && loadedSprite.texture != null)
                {
                    texture = loadedSprite.texture;
                }
                else
                {
                    return loadedSprite;
                }
            }
        }
        
        #if UNITY_EDITOR
        // En el editor, intentar usar AssetDatabase como fallback
        if (texture == null)
        {
            try
            {
                string spritePath = $"Assets/Art/Obstacles/{spriteName}.png";
                texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"No se pudo cargar el sprite {spriteName}: {e.Message}");
            }
        }
        #endif
        
        // Si tenemos una textura, crear el sprite con el tamaño normalizado
        if (texture != null)
        {
            float targetPixelsPerUnit = texture.width / targetWorldSize;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), targetPixelsPerUnit);
        }
        
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

