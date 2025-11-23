using UnityEngine;
using System.Collections;

public class ObstacleManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float minSpawnInterval = 2f;
    public float maxSpawnInterval = 4f;
    
    [Header("Difficulty Progression")]
    public float minSpawnIntervalMin = 0.5f; // Intervalo mínimo al máximo de dificultad
    public float maxSpawnIntervalMin = 1.0f; // Intervalo máximo al máximo de dificultad
    public float difficultyIncreaseRate = 0.1f; // Reducción del intervalo por segundo de juego
    public float difficultyUpdateInterval = 1f; // Cada cuánto actualizar la dificultad

    [Header("Obstacle Prefabs")]
    public GameObject doorFixedPrefab;
    public GameObject doorRandomPrefab;
    public GameObject oscillatingBarrierPrefab;
    public GameObject rotatingArcPrefab;
    public GameObject staticArcPrefab;

    [Header("Spawn Settings")]
    public float spawnRadius = 2f; // Mismo radio que la órbita del jugador
    public Transform center;
    private PlayerOrbit playerOrbit;

    [Header("Movement Settings")]
    public float obstacleSpeed = 3f;
    public float spawnDistanceFromScreen = 12f; // Distancia fuera de la pantalla para spawnear

    private Camera mainCamera;
    private float timeSinceLastSpawn = 0f;
    private float nextSpawnTime;
    private float gameTime = 0f; // Tiempo transcurrido desde el inicio
    private float timeSinceDifficultyUpdate = 0f;
    private float currentMinSpawnInterval;
    private float currentMaxSpawnInterval;

    private void Start()
    {
        Debug.Log("ObstacleManager: Start() called");
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        if (mainCamera == null)
        {
            Debug.LogError("ObstacleManager: No camera found in Start()!");
        }
        else
        {
            Debug.Log($"ObstacleManager: Camera found - Orthographic: {mainCamera.orthographic}, Size: {mainCamera.orthographicSize}");
        }

        if (center == null)
        {
            GameObject centerObj = GameObject.Find("Center");
            if (centerObj != null)
            {
                center = centerObj.transform;
            }
            else
            {
                center = new GameObject("Center").transform;
                center.position = Vector3.zero;
            }
        }

        // Obtener el radio del jugador para usar la misma órbita
        playerOrbit = FindObjectOfType<PlayerOrbit>();
        if (playerOrbit != null)
        {
            spawnRadius = playerOrbit.radius;
        }

        // Auto-load prefabs if not assigned (only at runtime)
        if (Application.isPlaying)
        {
            LoadPrefabsIfNeeded();
            
            // Verificar cuántos prefabs están cargados
            int loadedPrefabs = 0;
            if (doorFixedPrefab != null) loadedPrefabs++;
            if (doorRandomPrefab != null) loadedPrefabs++;
            if (oscillatingBarrierPrefab != null) loadedPrefabs++;
            if (rotatingArcPrefab != null) loadedPrefabs++;
            if (staticArcPrefab != null) loadedPrefabs++;
            
            Debug.Log($"ObstacleManager: Loaded {loadedPrefabs}/5 prefabs");
        }

        // Inicializar intervalos actuales
        currentMinSpawnInterval = minSpawnInterval;
        currentMaxSpawnInterval = maxSpawnInterval;
        
        // Spawnear el primer obstáculo inmediatamente
        nextSpawnTime = 0f;
        timeSinceLastSpawn = 0f;
        Debug.Log($"ObstacleManager: First obstacle will spawn immediately");
    }

    private void LoadPrefabsIfNeeded()
    {
        #if UNITY_EDITOR
        if (!Application.isPlaying) return;
        
        if (doorFixedPrefab == null)
            doorFixedPrefab = LoadPrefabByName("DoorFixed");
        if (doorRandomPrefab == null)
            doorRandomPrefab = LoadPrefabByName("DoorRandom");
        if (oscillatingBarrierPrefab == null)
            oscillatingBarrierPrefab = LoadPrefabByName("OscillatingBarrier");
        if (rotatingArcPrefab == null)
            rotatingArcPrefab = LoadPrefabByName("RotatingArc");
        if (staticArcPrefab == null)
            staticArcPrefab = LoadPrefabByName("StaticArc");
        #endif
    }

    #if UNITY_EDITOR
    private GameObject LoadPrefabByName(string name)
    {
        if (!Application.isPlaying) return null;
        
        try
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets(name + " t:Prefab");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }
        catch (System.Exception)
        {
            // Silently fail if asset database is not available
        }
        return null;
    }
    #endif

    private void Update()
    {
        if (!Application.isPlaying) return;
        
        gameTime += Time.deltaTime;
        timeSinceLastSpawn += Time.deltaTime;
        timeSinceDifficultyUpdate += Time.deltaTime;

        // Actualizar dificultad progresivamente
        if (timeSinceDifficultyUpdate >= difficultyUpdateInterval)
        {
            UpdateDifficulty();
            timeSinceDifficultyUpdate = 0f;
        }

        if (timeSinceLastSpawn >= nextSpawnTime)
        {
            Debug.Log($"ObstacleManager: Attempting to spawn obstacle (time: {timeSinceLastSpawn}, threshold: {nextSpawnTime})");
            SpawnObstacle();
            timeSinceLastSpawn = 0f;
            nextSpawnTime = Random.Range(currentMinSpawnInterval, currentMaxSpawnInterval);
            Debug.Log($"ObstacleManager: Next spawn in {nextSpawnTime} seconds (min: {currentMinSpawnInterval:F2}, max: {currentMaxSpawnInterval:F2})");
        }
    }

    private void UpdateDifficulty()
    {
        // Reducir progresivamente los intervalos de spawn
        // Cuanto más tiempo pase, más rápido aparecerán los obstáculos
        float reduction = gameTime * difficultyIncreaseRate;
        
        // Calcular nuevos intervalos (reducir progresivamente)
        float newMinInterval = Mathf.Max(
            minSpawnInterval - reduction,
            minSpawnIntervalMin
        );
        float newMaxInterval = Mathf.Max(
            maxSpawnInterval - reduction,
            maxSpawnIntervalMin
        );
        
        // Asegurar que el mínimo no sea mayor que el máximo
        if (newMinInterval > newMaxInterval)
        {
            newMinInterval = newMaxInterval;
        }
        
        currentMinSpawnInterval = newMinInterval;
        currentMaxSpawnInterval = newMaxInterval;
        
        Debug.Log($"ObstacleManager: Difficulty updated - Spawn intervals: {currentMinSpawnInterval:F2}s - {currentMaxSpawnInterval:F2}s (Game time: {gameTime:F1}s)");
    }

    private void SpawnObstacle()
    {
        GameObject[] prefabs = { doorFixedPrefab, doorRandomPrefab, oscillatingBarrierPrefab, rotatingArcPrefab, staticArcPrefab };
        
        // Filter out null prefabs
        System.Collections.Generic.List<GameObject> validPrefabs = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject prefab in prefabs)
        {
            if (prefab != null)
            {
                validPrefabs.Add(prefab);
            }
        }

        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning("ObstacleManager: No valid obstacle prefabs assigned!");
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("ObstacleManager: No camera found!");
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
            if (mainCamera == null)
            {
                Debug.LogError("ObstacleManager: Still no camera found! Cannot spawn obstacles.");
                return;
            }
        }

        // Verificar que la cámara sea ortográfica
        if (!mainCamera.orthographic)
        {
            Debug.LogWarning("ObstacleManager: Camera is not orthographic! Setting to orthographic.");
            mainCamera.orthographic = true;
        }

        // Select random prefab
        GameObject selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];

        // Obtener los límites de la pantalla en coordenadas del mundo
        float screenHeight = mainCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCamera.aspect;
        Vector3 cameraPos = mainCamera.transform.position;
        
        // Obtener el radio de la órbita del jugador
        float orbitRadius = spawnRadius; // Ya tenemos esto del Start()
        if (playerOrbit != null)
        {
            orbitRadius = playerOrbit.radius;
        }
        
        // Elegir un punto aleatorio en la órbita del jugador por donde debe pasar el obstáculo
        float targetAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 targetPointOnOrbit = center.position + new Vector3(
            Mathf.Cos(targetAngle) * orbitRadius,
            Mathf.Sin(targetAngle) * orbitRadius,
            0f
        );
        
        // Calcular una posición de spawn fuera de la pantalla
        // Elegir un ángulo aleatorio para spawnear (puede ser cualquier dirección)
        float spawnAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        // Calcular la distancia desde el centro de la pantalla para spawnear fuera
        float maxScreenDistance = Mathf.Max(screenWidth, screenHeight) / 2f + spawnDistanceFromScreen;
        
        // Spawnear en una posición fuera de la pantalla en la dirección del ángulo
        // Forzar Z = 0 para que esté en el mismo plano que el jugador
        Vector3 spawnPosition = new Vector3(
            cameraPos.x + Mathf.Cos(spawnAngle) * maxScreenDistance,
            cameraPos.y + Mathf.Sin(spawnAngle) * maxScreenDistance,
            0f // Forzar Z = 0 explícitamente
        );
        
        // Calcular la dirección de movimiento desde el spawn hacia el punto en la órbita
        Vector2 movementDirection = (targetPointOnOrbit - spawnPosition).normalized;

        // Determinar orientación según el tipo de obstáculo y dirección de movimiento
        float rotationAngle = 0f;
        string prefabName = selectedPrefab.name.ToLower();
        
        // Calcular el ángulo de rotación basado en la dirección de movimiento
        float directionAngle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;
        
        if (prefabName.Contains("door"))
        {
            // Las puertas se orientan perpendicularmente a la dirección de movimiento
            rotationAngle = directionAngle + 90f;
        }
        else if (prefabName.Contains("barrier"))
        {
            // Las barreras se orientan en la dirección de movimiento
            rotationAngle = directionAngle;
        }
        else if (prefabName.Contains("arc"))
        {
            // Los arcos pueden rotar, pero por ahora los orientamos según la dirección
            rotationAngle = directionAngle;
        }
        else
        {
            // Por defecto, orientar según la dirección de movimiento
            rotationAngle = directionAngle;
        }
        
        Quaternion rotation = Quaternion.Euler(0, 0, rotationAngle);
        GameObject obstacle = Instantiate(selectedPrefab, spawnPosition, rotation);
        
        if (obstacle == null)
        {
            Debug.LogError("ObstacleManager: Failed to instantiate obstacle!");
            return;
        }
        
        // Forzar la posición Z del obstáculo a 0 después de instanciarlo
        Vector3 pos = obstacle.transform.position;
        obstacle.transform.position = new Vector3(pos.x, pos.y, 0f);
        
        // Agregar componente de movimiento al obstáculo
        ObstacleMover mover = obstacle.GetComponent<ObstacleMover>();
        if (mover == null)
        {
            mover = obstacle.AddComponent<ObstacleMover>();
        }
        
        if (mover == null)
        {
            Debug.LogError("ObstacleManager: Failed to add ObstacleMover component!");
            return;
        }
        
        mover.SetDirection(movementDirection);
        mover.SetSpeed(obstacleSpeed);
        
        // Agregar efecto de brillo aleatorio al obstáculo
        ObstacleGlow glow = obstacle.GetComponent<ObstacleGlow>();
        if (glow == null)
        {
            glow = obstacle.AddComponent<ObstacleGlow>();
        }
        
        Debug.Log($"ObstacleManager: Spawned {selectedPrefab.name} at {obstacle.transform.position} moving {movementDirection}");
    }
}

