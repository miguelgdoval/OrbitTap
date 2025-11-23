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

