using UnityEngine;

/// <summary>
/// Componente que detecta colisiones en los hijos de los obstáculos
/// y notifica al ObstacleBase padre
/// </summary>
public class ObstacleCollisionDetector : MonoBehaviour
{
    private bool hasTriggeredCollision = false; // Flag para evitar múltiples triggers
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollision(collision);
    }
    
    private void OnTriggerStay2D(Collider2D collision)
    {
        // También detectar en OnTriggerStay2D para capturar la colisión más temprano
        HandleCollision(collision);
    }
    
    private void HandleCollision(Collider2D collision)
    {
        if (collision == null || hasTriggeredCollision) return;
        
        // Verificar si es el jugador
        bool isPlayer = collision.CompareTag("Player") || 
                       collision.gameObject.name == "Player";
        
        if (isPlayer)
        {
            hasTriggeredCollision = true; // Marcar para evitar múltiples triggers
            
            Debug.Log("Colisión detectada en hijo del obstáculo! GameObject: " + gameObject.name);
            
            // CRÍTICO: Detener PlayerOrbit INMEDIATAMENTE antes de cualquier otra cosa
            GameObject playerObj = collision.gameObject;
            PlayerOrbit playerOrbit = playerObj.GetComponent<PlayerOrbit>();
            if (playerOrbit != null)
            {
                playerOrbit.enabled = false; // Detener movimiento INMEDIATAMENTE
            }
            
            // CRÍTICO: NO detener el movimiento del padre - solo desconectar este segmento
            // Esto permite que los otros segmentos continúen moviéndose
            Transform parent = transform.parent;
            Vector3 collisionPoint; // Declarar variable
            
            // Copiar movimiento del padre ANTES de desconectarlo (si existe)
            ObstacleMover parentMover = null;
            if (parent != null)
            {
                parentMover = parent.GetComponent<ObstacleMover>();
                
                // CRÍTICO: Capturar la posición del PADRE antes de desconectar
                // La explosión debe ocurrir donde está el obstáculo
                Vector3 obstacleExactPosition = parent.position;
                collisionPoint = obstacleExactPosition;
                
                // DEBUG: Verificar posición
                Debug.Log($"ObstacleCollisionDetector: Posición del PADRE (obstáculo) capturada - {obstacleExactPosition}");
            }
            else
            {
                // Si no hay padre, usar la posición de este GameObject
                Vector3 obstacleExactPosition = transform.position;
                collisionPoint = obstacleExactPosition;
                Debug.Log($"ObstacleCollisionDetector: Posición del obstáculo (sin padre) capturada - {obstacleExactPosition}");
            }
            
            // CRÍTICO: Destruir solo este segmento hijo, NO el padre completo
            // Primero intentar destruir este GameObject específico
            ObstacleDestructionController thisSegmentDestruction = GetComponent<ObstacleDestructionController>();
            if (thisSegmentDestruction == null)
            {
                // Si no tiene su propio destructor, agregarlo
                thisSegmentDestruction = gameObject.AddComponent<ObstacleDestructionController>();
            }
            
            // Copiar movimiento del padre si existe (para que el segmento continúe moviéndose)
            // IMPORTANTE: Hacer esto ANTES de desconectar del padre
            if (parentMover != null)
            {
                // Agregar ObstacleMover a este segmento con la misma velocidad y dirección
                ObstacleMover segmentMover = gameObject.GetComponent<ObstacleMover>();
                if (segmentMover == null)
                {
                    segmentMover = gameObject.AddComponent<ObstacleMover>();
                }
                segmentMover.SetDirection(parentMover.direction);
                segmentMover.SetSpeed(parentMover.speed);
            }
            
            // CRÍTICO: Desconectar este segmento del padre DESPUÉS de copiar el movimiento
            // Esto permite que el padre y los otros segmentos continúen moviéndose normalmente
            if (parent != null)
            {
                transform.SetParent(null);
            }
            
            if (thisSegmentDestruction != null)
            {
                // Destruir solo este segmento, pasando la posición del PLANETA (punto de contacto real)
                thisSegmentDestruction.DestroyObstacle(collisionPoint);
            }
            else
            {
                // Fallback: destruir directamente este GameObject
                Debug.LogWarning($"ObstacleCollisionDetector: No se pudo crear ObstacleDestructionController para {gameObject.name}, destruyendo directamente");
                if (parent != null)
                {
                    transform.SetParent(null);
                }
                Destroy(gameObject);
            }
            
            // Activar animación de destrucción del planeta INMEDIATAMENTE
            // PlayerOrbit ya está desactivado, así que podemos capturar la posición exacta
            PlanetDestructionController destructionController = playerObj.GetComponent<PlanetDestructionController>();
            if (destructionController != null)
            {
                // Llamar sin parámetros - DestroyPlanet() capturará la posición exacta ahora
                destructionController.DestroyPlanet();
            }
            
            // CRÍTICO: NO llamar a ObstacleBase del padre porque destruiría todo el obstáculo
            // Solo llamar directamente a GameOver y DestroyPlanet (ya se hizo arriba)
            // Esto permite que los otros segmentos continúen
            if (GameManager.Instance != null)
            {
                    GameManager.Instance.GameOver();
                }
                else
                {
                    Debug.LogError("ObstacleCollisionDetector: GameManager.Instance es null!");
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.gameObject == null || hasTriggeredCollision) return;
        
        bool isPlayer = collision.gameObject.CompareTag("Player") || 
                       collision.gameObject.name == "Player";
        
        if (isPlayer)
        {
            hasTriggeredCollision = true; // Marcar para evitar múltiples triggers
            
            Debug.Log("Colisión normal detectada en hijo del obstáculo! GameObject: " + gameObject.name);
            
            // CRÍTICO: Detener PlayerOrbit INMEDIATAMENTE antes de cualquier otra cosa
            GameObject playerObj = collision.gameObject;
            PlayerOrbit playerOrbit = playerObj.GetComponent<PlayerOrbit>();
            if (playerOrbit != null)
            {
                playerOrbit.enabled = false; // Detener movimiento INMEDIATAMENTE
            }
            
            // CRÍTICO: NO detener el movimiento del padre - solo desconectar este segmento
            // Esto permite que los otros segmentos continúen moviéndose
            Transform parent = transform.parent;
            Vector3 collisionPoint; // Declarar variable
            
            // Copiar movimiento del padre ANTES de desconectarlo (si existe)
            ObstacleMover parentMover = null;
            if (parent != null)
            {
                parentMover = parent.GetComponent<ObstacleMover>();
                
                // CRÍTICO: Capturar la posición del PADRE antes de desconectar
                // La explosión debe ocurrir donde está el obstáculo
                Vector3 obstacleExactPosition = parent.position;
                collisionPoint = obstacleExactPosition;
                
                // DEBUG: Verificar posición
                Debug.Log($"ObstacleCollisionDetector: Posición del PADRE (obstáculo) capturada (OnCollisionEnter2D) - {obstacleExactPosition}");
            }
            else
            {
                // Si no hay padre, usar la posición de este GameObject
                Vector3 obstacleExactPosition = transform.position;
                collisionPoint = obstacleExactPosition;
                Debug.Log($"ObstacleCollisionDetector: Posición del obstáculo (sin padre) capturada (OnCollisionEnter2D) - {obstacleExactPosition}");
            }
            
            // CRÍTICO: Destruir solo este segmento hijo, NO el padre completo
            ObstacleDestructionController thisSegmentDestruction = GetComponent<ObstacleDestructionController>();
            if (thisSegmentDestruction == null)
            {
                // Si no tiene su propio destructor, agregarlo
                thisSegmentDestruction = gameObject.AddComponent<ObstacleDestructionController>();
            }
            
            // Copiar movimiento del padre si existe (para que el segmento continúe moviéndose)
            // IMPORTANTE: Hacer esto ANTES de desconectar del padre
            if (parentMover != null)
            {
                // Agregar ObstacleMover a este segmento con la misma velocidad y dirección
                ObstacleMover segmentMover = gameObject.GetComponent<ObstacleMover>();
                if (segmentMover == null)
                {
                    segmentMover = gameObject.AddComponent<ObstacleMover>();
                }
                segmentMover.SetDirection(parentMover.direction);
                segmentMover.SetSpeed(parentMover.speed);
            }
            
            // CRÍTICO: Desconectar este segmento del padre DESPUÉS de copiar el movimiento
            // Esto permite que el padre y los otros segmentos continúen moviéndose normalmente
            if (parent != null)
            {
                transform.SetParent(null);
            }
            
            if (thisSegmentDestruction != null)
            {
                // Destruir solo este segmento, pasando la posición del PLANETA (punto de contacto real)
                thisSegmentDestruction.DestroyObstacle(collisionPoint);
            }
            else
            {
                // Fallback: destruir directamente este GameObject
                Debug.LogWarning($"ObstacleCollisionDetector: No se pudo crear ObstacleDestructionController para {gameObject.name}, destruyendo directamente");
                if (parent != null)
                {
                    transform.SetParent(null);
                }
                Destroy(gameObject);
            }
            
            // Activar animación de destrucción del planeta INMEDIATAMENTE
            // PlayerOrbit ya está desactivado, así que podemos capturar la posición exacta
            PlanetDestructionController destructionController = playerObj.GetComponent<PlanetDestructionController>();
            if (destructionController != null)
            {
                // Llamar sin parámetros - DestroyPlanet() capturará la posición exacta ahora
                destructionController.DestroyPlanet();
            }
            
            // CRÍTICO: NO llamar a ObstacleBase del padre porque destruiría todo el obstáculo
            // Solo llamar directamente a GameOver (DestroyPlanet ya se llamó arriba)
            // Esto permite que los otros segmentos continúen
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
    }
}

