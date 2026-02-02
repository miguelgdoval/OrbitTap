using UnityEngine;
using static LogHelper;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObstacleBase : MonoBehaviour
{
    private void Awake()
    {
        // Asegurar que el obstáculo tenga ObstacleDestructionController
        if (GetComponent<ObstacleDestructionController>() == null)
        {
            ObstacleDestructionController destructionController = gameObject.AddComponent<ObstacleDestructionController>();
            Log($"ObstacleBase: Agregado ObstacleDestructionController a {gameObject.name}");
        }
    }
    
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
                LogWarning($"No se pudo cargar el sprite {spriteName}: {e.Message}");
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
    private bool hasTriggeredCollision = false; // Flag para evitar múltiples triggers
    
    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollision(collision);
    }
    
    public virtual void OnTriggerStay2D(Collider2D collision)
    {
        // También detectar en OnTriggerStay2D para capturar la colisión más temprano
        HandleCollision(collision);
    }
    
    private void HandleCollision(Collider2D collision)
    {
        if (collision == null || hasTriggeredCollision) return;
        
        // Verificar por tag o por nombre
        bool isPlayer = collision.CompareTag("Player") || 
                       collision.gameObject.name == "Player" ||
                       collision.gameObject.CompareTag("Player");
        
        if (isPlayer)
        {
            hasTriggeredCollision = true; // Marcar para evitar múltiples triggers
            
            Log("Colisión detectada con Player! GameObject: " + collision.gameObject.name);
            
            // CRÍTICO: Detener PlayerOrbit INMEDIATAMENTE antes de cualquier otra cosa
            GameObject playerObj = collision.gameObject;
            PlayerOrbit playerOrbit = playerObj.GetComponent<PlayerOrbit>();
            if (playerOrbit != null)
            {
                playerOrbit.enabled = false; // Detener movimiento INMEDIATAMENTE
            }
            
            // CRÍTICO: Detener ObstacleMover INMEDIATAMENTE para evitar que mueva el obstáculo
            // Esto debe hacerse ANTES de capturar la posición, igual que el planeta
            ObstacleMover obstacleMover = GetComponent<ObstacleMover>();
            if (obstacleMover != null)
            {
                obstacleMover.enabled = false;
            }
            
            // CRÍTICO: Detener Rigidbody2D si existe
            Rigidbody2D obstacleRb = GetComponent<Rigidbody2D>();
            if (obstacleRb != null)
            {
                obstacleRb.linearVelocity = Vector2.zero;
                obstacleRb.angularVelocity = 0f;
                obstacleRb.isKinematic = true;
            }
            
            // CRÍTICO: Capturar la posición ACTUAL del obstáculo DESPUÉS de detener el movimiento
            // La explosión debe ocurrir donde está el obstáculo, igual que el planeta usa su propia posición
            Vector3 obstacleExactPosition = transform.position;
            transform.position = obstacleExactPosition; // Forzar posición fija
            
            // DEBUG: Verificar posición
            Log($"ObstacleBase: Posición del OBSTÁCULO capturada - {obstacleExactPosition}, transform.position: {transform.position}");
            
            // Destruir el obstáculo primero, pasando la posición del OBSTÁCULO (donde está)
            ObstacleDestructionController obstacleDestruction = GetComponent<ObstacleDestructionController>();
            if (obstacleDestruction == null)
            {
                // Si no existe, agregarlo ahora
                obstacleDestruction = gameObject.AddComponent<ObstacleDestructionController>();
                Log($"ObstacleBase: ObstacleDestructionController agregado dinámicamente a {gameObject.name} (OnTriggerEnter2D)");
            }
            if (obstacleDestruction != null)
            {
                obstacleDestruction.DestroyObstacle(obstacleExactPosition);
            }
            else
            {
                LogError($"ObstacleBase: No se pudo obtener o crear ObstacleDestructionController para {gameObject.name} (OnTriggerEnter2D)");
            }
            
            // Activar animación de destrucción del planeta INMEDIATAMENTE
            // PlayerOrbit ya está desactivado, así que podemos capturar la posición exacta
            PlanetDestructionController destructionController = playerObj.GetComponent<PlanetDestructionController>();
            if (destructionController != null)
            {
                // Llamar sin parámetros - DestroyPlanet() capturará la posición exacta ahora
                destructionController.DestroyPlanet();
            }
            
            // Llamar a GameOver después de un pequeño delay para que la animación se inicie
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
            else
            {
                LogError("GameManager.Instance es null!");
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
            Log("Colisión normal detectada con Player!");
            
            // CRÍTICO: Capturar la posición del PLANETA (punto de colisión real)
            // La explosión debe ocurrir donde está el planeta, que es el punto de contacto
            Vector3 collisionPoint = collision.gameObject.transform.position;
            
            // CRÍTICO: Detener ObstacleMover INMEDIATAMENTE para evitar que mueva el obstáculo
            ObstacleMover obstacleMover = GetComponent<ObstacleMover>();
            if (obstacleMover != null)
            {
                obstacleMover.enabled = false;
            }
            
            // CRÍTICO: Detener Rigidbody2D si existe
            Rigidbody2D obstacleRb = GetComponent<Rigidbody2D>();
            if (obstacleRb != null)
            {
                obstacleRb.linearVelocity = Vector2.zero;
                obstacleRb.angularVelocity = 0f;
                obstacleRb.isKinematic = true;
            }
            
            // DEBUG: Verificar posición
            Log($"ObstacleBase: Usando posición del PLANETA como punto de colisión (OnCollisionEnter2D) - {collisionPoint}");
            
            // Destruir el obstáculo primero, pasando la posición del PLANETA (punto de contacto real)
            ObstacleDestructionController obstacleDestruction = GetComponent<ObstacleDestructionController>();
            if (obstacleDestruction == null)
            {
                // Si no existe, agregarlo ahora
                obstacleDestruction = gameObject.AddComponent<ObstacleDestructionController>();
                Log($"ObstacleBase: ObstacleDestructionController agregado dinámicamente a {gameObject.name} (OnCollisionEnter2D)");
            }
            if (obstacleDestruction != null)
            {
                obstacleDestruction.DestroyObstacle(collisionPoint);
            }
            else
            {
                LogError($"ObstacleBase: No se pudo obtener o crear ObstacleDestructionController para {gameObject.name} (OnCollisionEnter2D)");
            }
            
            // Activar animación de destrucción del planeta con la posición exacta del planeta
            // collisionPoint ya está definido arriba
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

