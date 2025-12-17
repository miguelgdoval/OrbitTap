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

    private void Start()
    {
        spawnTime = Time.time;
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        Log($"ObstacleMover: {gameObject.name} started at {transform.position}, direction: {direction}, speed: {speed}");
    }

    private void Update()
    {
        // Verificar si el obstáculo está siendo destruido
        ObstacleDestructionController destructionController = GetComponent<ObstacleDestructionController>();
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

        // Solo destruir si ya entró a la pantalla y ahora está fuera
        // O si han pasado más de 10 segundos (por si acaso)
        // NOTA: Usar la variable destructionController ya declarada arriba
        if (hasEnteredScreen && IsOutOfScreen())
        {
            // Verificar si hay un ObstacleDestructionController en proceso de destrucción
            if (destructionController == null || !destructionController.IsDestroying())
            {
                Log($"ObstacleMover: Destroying {gameObject.name} - out of screen after entering");
                Destroy(gameObject);
            }
        }
        else if (Time.time - spawnTime > 10f)
        {
            // Verificar si hay un ObstacleDestructionController en proceso de destrucción
            if (destructionController == null || !destructionController.IsDestroying())
            {
                // Destruir después de 10 segundos por seguridad
                Debug.Log($"ObstacleMover: Destroying {gameObject.name} - timeout");
                Destroy(gameObject);
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
}

