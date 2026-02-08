using UnityEngine;
using static LogHelper;

/// <summary>
/// Componente que mueve los obstáculos en una dirección y los destruye cuando salen de la pantalla
/// </summary>
public class ObstacleMover : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f;
    public Vector2 direction = Vector2.zero; // Se establecerá al spawnear

    private Camera mainCamera;
    private bool hasEnteredScreen = false; // Para saber si ya entró a la pantalla
    private float spawnTime;
    private ObstacleDestructionController destructionController; // Cacheado para evitar GetComponent en Update

    private void Start()
    {
        spawnTime = Time.time;
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        
        // Cachear referencia a ObstacleDestructionController para evitar GetComponent en Update
        destructionController = GetComponent<ObstacleDestructionController>();
        
        Log($"ObstacleMover: {gameObject.name} started at {transform.position}, direction: {direction}, speed: {speed}");
    }

    private void Update()
    {
        // Verificar si el obstáculo está siendo destruido (usar referencia cacheada)
        if (destructionController == null)
        {
            destructionController = GetComponent<ObstacleDestructionController>();
        }
        if (destructionController != null && destructionController.IsDestroying())
        {
            // El obstáculo está en proceso de destrucción, no mover
            return;
        }
        
        // Verificar que la dirección sea válida
        if (direction == Vector2.zero)
        {
            LogWarning($"ObstacleMover: Direction is zero for {gameObject.name}");
            return;
        }

        // Mover el obstáculo en la dirección especificada
        Vector3 newPos = transform.position + (Vector3)(direction.normalized * speed * Time.deltaTime);
        // Mantener Z = 0 siempre
        transform.position = new Vector3(newPos.x, newPos.y, 0f);

        // Verificar si está dentro de la pantalla (para marcar que ya entró)
        if (!hasEnteredScreen && IsOnScreen())
        {
            hasEnteredScreen = true;
            Log($"ObstacleMover: {gameObject.name} entered screen at {transform.position}");
        }

        // Solo devolver al pool si ya entró a la pantalla y ahora está fuera
        // O si han pasado más de 10 segundos (por si acaso)
        // NOTA: Usar la variable destructionController ya declarada arriba
        if (hasEnteredScreen && IsOutOfScreen())
        {
            // Verificar si hay un ObstacleDestructionController en proceso de destrucción
            if (destructionController == null || !destructionController.IsDestroying())
            {
                Log($"ObstacleMover: Returning {gameObject.name} to pool - out of screen after entering");
                ReturnToPool();
            }
        }
        else if (Time.time - spawnTime > 10f)
        {
            // Verificar si hay un ObstacleDestructionController en proceso de destrucción
            if (destructionController == null || !destructionController.IsDestroying())
            {
                // Devolver al pool después de 10 segundos por seguridad
                Log($"ObstacleMover: Returning {gameObject.name} to pool - timeout");
                ReturnToPool();
            }
        }
    }

    private bool IsOnScreen()
    {
        if (mainCamera == null) return false;

        Vector3 screenPos = mainCamera.WorldToViewportPoint(transform.position);
        
        // Verificar si está dentro de la pantalla (con un pequeño margen)
        return screenPos.x >= -0.1f && screenPos.x <= 1.1f && 
               screenPos.y >= -0.1f && screenPos.y <= 1.1f &&
               screenPos.z >= mainCamera.nearClipPlane && screenPos.z <= mainCamera.farClipPlane;
    }

    private bool IsOutOfScreen()
    {
        if (mainCamera == null) return false;

        Vector3 screenPos = mainCamera.WorldToViewportPoint(transform.position);
        
        // Si está completamente fuera de los límites de la pantalla (con margen)
        return screenPos.x < -0.3f || screenPos.x > 1.3f || 
               screenPos.y < -0.3f || screenPos.y > 1.3f ||
               screenPos.z < mainCamera.nearClipPlane || screenPos.z > mainCamera.farClipPlane;
    }

    /// <summary>
    /// Establece la dirección de movimiento del obstáculo
    /// </summary>
    public void SetDirection(Vector2 dir)
    {
        direction = dir;
    }

    /// <summary>
    /// Establece la velocidad del obstáculo
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }
    
    /// <summary>
    /// Devuelve el obstáculo al pool en lugar de destruirlo
    /// </summary>
    private void ReturnToPool()
    {
        // Registrar que se evitó un obstáculo (solo si entró a la pantalla y no está siendo destruido)
        if (hasEnteredScreen)
        {
            // Usar referencia cacheada
            if (destructionController == null)
            {
                destructionController = GetComponent<ObstacleDestructionController>();
            }
            if (destructionController == null || !destructionController.IsDestroying())
            {
                // El obstáculo salió de pantalla sin colisionar = fue evitado
                if (StatisticsManager.Instance != null)
                {
                    StatisticsManager.Instance.RecordObstacleAvoided();
                }
            }
        }
        
        if (ObstacleManager.Instance != null)
        {
            ObstacleManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            // Fallback: destruir normalmente si no hay pool
            Destroy(gameObject);
        }
    }
}

