using UnityEngine;

public class ObstacleBase : MonoBehaviour
{
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

