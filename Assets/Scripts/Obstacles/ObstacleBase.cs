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
            
            // CRÍTICO: OnTriggerEnter2D se ejecuta en el frame de la colisión
            // Pero PlayerOrbit.Update() puede ejecutarse después en el mismo frame
            // Por eso NO pasamos la posición aquí, dejamos que DestroyPlanet() capture la posición
            // después de desactivar PlayerOrbit
            GameObject playerObj = collision.gameObject;
            
            // Activar animación de destrucción del planeta
            // NO pasar posición - DestroyPlanet() la capturará después de desactivar PlayerOrbit
            PlanetDestructionController destructionController = playerObj.GetComponent<PlanetDestructionController>();
            if (destructionController != null)
            {
                // Llamar sin parámetros para que capture la posición exacta después de desactivar PlayerOrbit
                destructionController.DestroyPlanet();
            }
            
            // Llamar a GameOver después de un pequeño delay para que la animación se inicie
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
            
            // CRÍTICO: Usar la posición del planeta en el momento de la colisión
            // La posición del GameObject es más precisa que el punto de contacto
            Vector3 collisionPoint = collision.gameObject.transform.position;
            
            // Activar animación de destrucción del planeta con la posición exacta del planeta
            PlanetDestructionController destructionController = collision.gameObject.GetComponent<PlanetDestructionController>();
            if (destructionController != null)
            {
                destructionController.DestroyPlanet(collisionPoint);
            }
            
            // Llamar a GameOver después de un pequeño delay para que la animación se inicie
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
    }
}

