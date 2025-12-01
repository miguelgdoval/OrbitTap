using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema que asegura que siempre haya un espacio libre en la órbita para que el jugador pueda sobrevivir
/// </summary>
public class OrbitSafetySystem : MonoBehaviour
{
    [Header("Safety Settings")]
    [Tooltip("Ángulo mínimo libre garantizado en la órbita (aumentado para más justicia)")]
    public float minFreeArcAngle = 120f; // Ángulo mínimo libre en la órbita (en grados) - AUMENTADO de 90° a 120°
    [Tooltip("Ángulo que bloquea cada obstáculo (considera el tamaño del obstáculo)")]
    public float obstacleBlockAngle = 40f; // Ángulo que bloquea cada obstáculo (en grados) - AUMENTADO de 30° a 40°
    [Tooltip("Radio para verificar obstáculos cerca de la órbita")]
    public float checkRadius = 3f; // Radio para verificar obstáculos cerca de la órbita - AUMENTADO de 2.5 a 3

    private Transform center;
    private float orbitRadius;
    private List<GameObject> activeObstacles;

    public static OrbitSafetySystem Instance { get; private set; }

    private void Awake()
    {
        if (!Application.isPlaying) return;

        if (Instance == null)
        {
            Instance = this;
            activeObstacles = new List<GameObject>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (!Application.isPlaying) return;

        // Buscar el centro
        GameObject centerObj = GameObject.Find("Center");
        if (centerObj != null)
        {
            center = centerObj.transform;
        }

        // Obtener el radio de la órbita
        PlayerOrbit player = FindObjectOfType<PlayerOrbit>();
        if (player != null)
        {
            orbitRadius = player.radius;
        }
        else
        {
            orbitRadius = 2f; // Valor por defecto
        }
    }

    /// <summary>
    /// Registra un obstáculo activo
    /// </summary>
    public void RegisterObstacle(GameObject obstacle)
    {
        if (activeObstacles == null)
        {
            activeObstacles = new List<GameObject>();
        }

        if (!activeObstacles.Contains(obstacle))
        {
            activeObstacles.Add(obstacle);
        }
    }

    /// <summary>
    /// Desregistra un obstáculo cuando se destruye
    /// </summary>
    public void UnregisterObstacle(GameObject obstacle)
    {
        if (activeObstacles != null)
        {
            activeObstacles.Remove(obstacle);
        }
    }

    /// <summary>
    /// Verifica si un ángulo objetivo en la órbita es seguro (no bloquea completamente el camino)
    /// </summary>
    public bool IsAngleSafe(float targetAngleDegrees)
    {
        if (center == null) return true; // Si no hay centro, permitir spawn

        // Obtener los ángulos bloqueados por obstáculos activos
        List<float> blockedAngles = GetBlockedAngles();

        // Convertir el ángulo objetivo a 0-360
        float normalizedAngle = NormalizeAngle(targetAngleDegrees);

        // Verificar si el ángulo objetivo está demasiado cerca de obstáculos existentes
        foreach (float blockedAngle in blockedAngles)
        {
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(normalizedAngle, blockedAngle));
            if (angleDiff < obstacleBlockAngle)
            {
                return false; // Demasiado cerca de un obstáculo existente
            }
        }

        // Verificar que aún haya suficiente espacio libre
        return HasMinimumFreeSpace(blockedAngles, normalizedAngle);
    }

    /// <summary>
    /// Encuentra un ángulo seguro alternativo si el propuesto no es seguro
    /// </summary>
    public float FindSafeAngle(float preferredAngle)
    {
        if (center == null) return preferredAngle;

        List<float> blockedAngles = GetBlockedAngles();
        float normalizedPreferred = NormalizeAngle(preferredAngle);

        // Si el ángulo preferido es seguro, usarlo
        if (IsAngleSafe(normalizedPreferred))
        {
            return normalizedPreferred;
        }

        // Buscar un ángulo seguro probando diferentes direcciones (más exhaustivo)
        float[] testAngles = new float[]
        {
            normalizedPreferred + 30f,
            normalizedPreferred - 30f,
            normalizedPreferred + 60f,
            normalizedPreferred - 60f,
            normalizedPreferred + 90f,
            normalizedPreferred - 90f,
            normalizedPreferred + 120f,
            normalizedPreferred - 120f,
            normalizedPreferred + 150f,
            normalizedPreferred - 150f,
            normalizedPreferred + 180f
        };

        foreach (float testAngle in testAngles)
        {
            float normalizedTest = NormalizeAngle(testAngle);
            if (IsAngleSafe(normalizedTest))
            {
                return normalizedTest;
            }
        }

        // Si no se encuentra un ángulo seguro, buscar el área con más espacio libre
        float largestGapAngle = FindLargestFreeGap(blockedAngles);
        
        // Verificar que el ángulo encontrado realmente sea seguro
        if (IsAngleSafe(largestGapAngle))
        {
            return largestGapAngle;
        }
        
        // Último recurso: buscar en incrementos pequeños alrededor del ángulo del gap más grande
        for (int i = -20; i <= 20; i += 5)
        {
            float testAngle = NormalizeAngle(largestGapAngle + i);
            if (IsAngleSafe(testAngle))
            {
                return testAngle;
            }
        }
        
        // Si todo falla, devolver el ángulo preferido (mejor que nada)
        return normalizedPreferred;
    }

    private List<float> GetBlockedAngles()
    {
        List<float> blockedAngles = new List<float>();

        if (center == null || activeObstacles == null) return blockedAngles;

        // Limpiar obstáculos nulos
        activeObstacles.RemoveAll(obj => obj == null);

        foreach (GameObject obstacle in activeObstacles)
        {
            if (obstacle == null) continue;

            Vector3 obstaclePos = obstacle.transform.position;
            Vector3 toObstacle = obstaclePos - center.position;
            float distance = toObstacle.magnitude;

            // Solo considerar obstáculos cerca de la órbita
            if (Mathf.Abs(distance - orbitRadius) < checkRadius)
            {
                float angle = Mathf.Atan2(toObstacle.y, toObstacle.x) * Mathf.Rad2Deg;
                
                // Considerar el tamaño del obstáculo para calcular el ángulo bloqueado
                float actualBlockAngle = obstacleBlockAngle;
                SpriteRenderer sr = obstacle.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    // Calcular el tamaño angular del obstáculo basado en su tamaño
                    float obstacleSize = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
                    float angularSize = (obstacleSize / orbitRadius) * Mathf.Rad2Deg;
                    actualBlockAngle = Mathf.Max(obstacleBlockAngle, angularSize * 1.2f); // 20% de margen
                }
                
                // Agregar múltiples ángulos alrededor del obstáculo para mejor cobertura
                int samples = Mathf.CeilToInt(actualBlockAngle / 10f); // Muestrear cada 10 grados
                for (int i = 0; i < samples; i++)
                {
                    float offset = (i - samples / 2f) * (actualBlockAngle / samples);
                    blockedAngles.Add(NormalizeAngle(angle + offset));
                }
            }
        }

        return blockedAngles;
    }

    private bool HasMinimumFreeSpace(List<float> blockedAngles, float newBlockAngle)
    {
        if (blockedAngles.Count == 0) return true;

        // Agregar el nuevo ángulo a la lista para verificar
        List<float> allBlocked = new List<float>(blockedAngles);
        allBlocked.Add(NormalizeAngle(newBlockAngle));
        allBlocked.Sort();

        // Verificar los espacios entre obstáculos
        float largestGap = 0f;
        float secondLargestGap = 0f; // También verificar el segundo gap más grande
        
        for (int i = 0; i < allBlocked.Count; i++)
        {
            float currentAngle = allBlocked[i];
            float nextAngle = (i < allBlocked.Count - 1) ? allBlocked[i + 1] : allBlocked[0] + 360f;
            
            float gap = nextAngle - currentAngle;
            if (gap < 0) gap += 360f; // Manejar el wrap-around

            if (gap > largestGap)
            {
                secondLargestGap = largestGap;
                largestGap = gap;
            }
            else if (gap > secondLargestGap)
            {
                secondLargestGap = gap;
            }
        }

        // Verificar que el espacio libre más grande sea suficiente
        // También verificar que haya al menos dos espacios razonables (más justo)
        bool hasMainGap = largestGap >= minFreeArcAngle;
        bool hasSecondaryGap = secondLargestGap >= minFreeArcAngle * 0.6f; // Al menos 60% del mínimo
        
        return hasMainGap && hasSecondaryGap;
    }

    private float FindLargestFreeGap(List<float> blockedAngles)
    {
        if (blockedAngles.Count == 0)
        {
            return Random.Range(0f, 360f);
        }

        blockedAngles.Sort();
        float largestGap = 0f;
        float bestAngle = 0f;

        for (int i = 0; i < blockedAngles.Count; i++)
        {
            float currentAngle = blockedAngles[i];
            float nextAngle = (i < blockedAngles.Count - 1) ? blockedAngles[i + 1] : blockedAngles[0] + 360f;
            
            float gap = nextAngle - currentAngle;
            if (gap < 0) gap += 360f;

            if (gap > largestGap)
            {
                largestGap = gap;
                // El ángulo en el medio del gap
                bestAngle = NormalizeAngle(currentAngle + gap / 2f);
            }
        }

        return bestAngle;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle < 0f) angle += 360f;
        while (angle >= 360f) angle -= 360f;
        return angle;
    }
}

