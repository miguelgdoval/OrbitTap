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

        // Obtener multiplicador de velocidad (slowmo power-up)
        float speedMultiplier = 1f;
        if (PowerUpManager.Instance != null)
        {
            speedMultiplier = PowerUpManager.Instance.GetObstacleSpeedMultiplier();
        }
        
        // Aplicar multiplicador de Danger Zone (obstáculos más rápidos durante oleadas)
        if (DangerZoneManager.Instance != null)
        {
            speedMultiplier *= DangerZoneManager.Instance.GetDangerSpeedMultiplier();
        }
        
        // Mover el obstáculo en la dirección especificada
        Vector3 newPos = transform.position + (Vector3)(direction.normalized * speed * speedMultiplier * Time.deltaTime);
        // Mantener Z = 0 siempre
        transform.position = new Vector3(newPos.x, newPos.y, 0f);

        // Verificar si está dentro de la pantalla (para marcar que ya entró)
        if (!hasEnteredScreen && IsOnScreen())
        {
            hasEnteredScreen = true;
            Log($"ObstacleMover: {gameObject.name} entered screen at {transform.position}");
        }

        // Solo devolver al pool si ya entró a la pantalla y ahora está fuera
        // O si han pasado más de 15 segundos Y no está visible (por si acaso)
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
        else if (Time.time - spawnTime > 15f && !IsOnScreen())
        {
            // Timeout de seguridad: solo si NO está visible en pantalla
            // (evita eliminar obstáculos ralentizados por Slowmo que siguen en pantalla)
            if (destructionController == null || !destructionController.IsDestroying())
            {
                Log($"ObstacleMover: Returning {gameObject.name} to pool - timeout (not on screen)");
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
                
                // Notificar al ComboManager (incrementar racha)
                if (ComboManager.Instance != null)
                {
                    ComboManager.Instance.OnObstacleDodged();
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

