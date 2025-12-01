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
            Debug.Log("Colisión detectada en hijo del obstáculo! GameObject: " + collision.gameObject.name);
            
            // Destruir el obstáculo primero (buscar en el padre)
            ObstacleDestructionController obstacleDestruction = GetComponentInParent<ObstacleDestructionController>();
            if (obstacleDestruction != null)
            {
                obstacleDestruction.DestroyObstacle();
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
            
            // Buscar ObstacleBase en el padre
            ObstacleBase obstacleBase = GetComponentInParent<ObstacleBase>();
            if (obstacleBase != null)
            {
                // Llamar al método de colisión del padre
                obstacleBase.OnTriggerEnter2D(collision);
            }
            else
            {
                // Si no hay ObstacleBase, llamar directamente a GameManager
                Debug.Log("ObstacleCollisionDetector: No se encontró ObstacleBase en el padre, llamando directamente a GameManager");
                if (GameManager.Instance != null)
                {
                    Debug.Log("ObstacleCollisionDetector: Llamando directamente a GameManager.GameOver()");
                    GameManager.Instance.GameOver();
                }
                else
                {
                    Debug.LogError("ObstacleCollisionDetector: GameManager.Instance es null!");
                }
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
            Debug.Log("Colisión normal detectada en hijo del obstáculo!");
            
            // Destruir el obstáculo primero (buscar en el padre)
            ObstacleDestructionController obstacleDestruction = GetComponentInParent<ObstacleDestructionController>();
            if (obstacleDestruction != null)
            {
                obstacleDestruction.DestroyObstacle();
            }
            
            // CRÍTICO: Usar la posición del planeta en el momento de la colisión
            Vector3 collisionPoint = collision.gameObject.transform.position;
            
            // Activar animación de destrucción del planeta con la posición exacta del planeta
            PlanetDestructionController destructionController = collision.gameObject.GetComponent<PlanetDestructionController>();
            if (destructionController != null)
            {
                destructionController.DestroyPlanet(collisionPoint);
            }
            
            ObstacleBase obstacleBase = GetComponentInParent<ObstacleBase>();
            if (obstacleBase != null)
            {
                obstacleBase.OnCollisionEnter2D(collision);
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
    }
}

