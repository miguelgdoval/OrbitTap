using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema que asegura que siempre haya un espacio libre en la órbita para que el jugador pueda sobrevivir
/// </summary>
public class OrbitSafetySystem : MonoBehaviour
{
    [Header("Safety Settings")]
    public float minFreeArcAngle = 90f; // Ángulo mínimo libre en la órbita (en grados)
    public float obstacleBlockAngle = 30f; // Ángulo que bloquea cada obstáculo (en grados)
    public float checkRadius = 2.5f; // Radio para verificar obstáculos cerca de la órbita

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

        // Buscar un ángulo seguro probando diferentes direcciones
        float[] testAngles = new float[]
        {
            normalizedPreferred + 45f,
            normalizedPreferred - 45f,
            normalizedPreferred + 90f,
            normalizedPreferred - 90f,
            normalizedPreferred + 135f,
            normalizedPreferred - 135f,
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
        return FindLargestFreeGap(blockedAngles);
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
                blockedAngles.Add(NormalizeAngle(angle));
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
        for (int i = 0; i < allBlocked.Count; i++)
        {
            float currentAngle = allBlocked[i];
            float nextAngle = (i < allBlocked.Count - 1) ? allBlocked[i + 1] : allBlocked[0] + 360f;
            
            float gap = nextAngle - currentAngle;
            if (gap < 0) gap += 360f; // Manejar el wrap-around

            if (gap > largestGap)
            {
                largestGap = gap;
            }
        }

        // Verificar que el espacio libre más grande sea suficiente
        return largestGap >= minFreeArcAngle;
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

