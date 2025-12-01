using UnityEngine;

/// <summary>
/// Componente que detecta colisiones en los hijos de los obstáculos
/// y notifica al ObstacleBase padre
/// </summary>
public class ObstacleCollisionDetector : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return;
        
        // Verificar si es el jugador
        bool isPlayer = collision.CompareTag("Player") || 
                       collision.gameObject.name == "Player";
        
        if (isPlayer)
        {
            Debug.Log("Colisión detectada en hijo del obstáculo! GameObject: " + gameObject.name);
            
            // CRÍTICO: Destruir solo este segmento hijo, NO el padre completo
            // Primero intentar destruir este GameObject específico
            ObstacleDestructionController thisSegmentDestruction = GetComponent<ObstacleDestructionController>();
            if (thisSegmentDestruction == null)
            {
                // Si no tiene su propio destructor, agregarlo
                thisSegmentDestruction = gameObject.AddComponent<ObstacleDestructionController>();
            }
            
            // Guardar referencia al padre antes de desconectarlo
            Transform parent = transform.parent;
            
            // Copiar movimiento del padre si existe (para que el segmento continúe moviéndose)
            if (parent != null)
            {
                ObstacleMover parentMover = parent.GetComponent<ObstacleMover>();
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
                
                // Desconectar este segmento del padre antes de destruirlo
                // Esto permite que los otros segmentos continúen
                transform.SetParent(null);
            }
            
            if (thisSegmentDestruction != null)
            {
                // Destruir solo este segmento
                thisSegmentDestruction.DestroyObstacle();
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
        if (collision == null || collision.gameObject == null) return;
        
        bool isPlayer = collision.gameObject.CompareTag("Player") || 
                       collision.gameObject.name == "Player";
        
        if (isPlayer)
        {
            Debug.Log("Colisión normal detectada en hijo del obstáculo! GameObject: " + gameObject.name);
            
            // CRÍTICO: Destruir solo este segmento hijo, NO el padre completo
            ObstacleDestructionController thisSegmentDestruction = GetComponent<ObstacleDestructionController>();
            if (thisSegmentDestruction == null)
            {
                // Si no tiene su propio destructor, agregarlo
                thisSegmentDestruction = gameObject.AddComponent<ObstacleDestructionController>();
            }
            
            // Guardar referencia al padre antes de desconectarlo
            Transform parent = transform.parent;
            
            // Copiar movimiento del padre si existe (para que el segmento continúe moviéndose)
            if (parent != null)
            {
                ObstacleMover parentMover = parent.GetComponent<ObstacleMover>();
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
                
                // Desconectar este segmento del padre antes de destruirlo
                transform.SetParent(null);
            }
            
            if (thisSegmentDestruction != null)
            {
                // Destruir solo este segmento
                thisSegmentDestruction.DestroyObstacle();
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
            
            // CRÍTICO: Usar la posición del planeta en el momento de la colisión
            Vector3 collisionPoint = collision.gameObject.transform.position;
            
            // Activar animación de destrucción del planeta con la posición exacta del planeta
            PlanetDestructionController destructionController = collision.gameObject.GetComponent<PlanetDestructionController>();
            if (destructionController != null)
            {
                destructionController.DestroyPlanet(collisionPoint);
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

