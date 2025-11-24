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
    
    [Header("Complex Obstacle Prefabs")]
    public GameObject pulsatingRingPrefab;
    public GameObject spiralFragmentPrefab;
    public GameObject zigzagBarrierPrefab;
    
    [Header("Difficulty Settings")]
    public ObstacleDifficultyLevel maxDifficultyLevel = ObstacleDifficultyLevel.VeryHard; // Dificultad máxima permitida
    public bool useDifficultyProgression = true; // Si true, aumenta la dificultad con el tiempo
    public float timeToUnlockMedium = 10f; // Segundos para desbloquear obstáculos Medium
    public float timeToUnlockHard = 30f; // Segundos para desbloquear obstáculos Hard
    public float timeToUnlockVeryHard = 60f; // Segundos para desbloquear obstáculos VeryHard

    [Header("Spawn Settings")]
    public float spawnRadius = 2f; // Mismo radio que la órbita del jugador
    public Transform center;
    private PlayerOrbit playerOrbit;

    [Header("Movement Settings")]
    public float obstacleSpeed = 3f;
    public float spawnDistanceFromScreen = 12f; // Distancia fuera de la pantalla para spawnear
    public float speedVariation = 1.5f; // Multiplicador máximo para variación de velocidad (1.0 a 1.5x)
    public float sizeVariation = 3.0f; // Multiplicador máximo para variación de tamaño (1.0 a 3.0x)
    public int maxObstaclesOnScreen = 5; // Máximo número de obstáculos en pantalla simultáneamente

    private Camera mainCamera;
    private float timeSinceLastSpawn = 0f;
    private float nextSpawnTime;
    private float gameTime = 0f; // Tiempo transcurrido desde el inicio
    private float timeSinceDifficultyUpdate = 0f;
    private float currentMinSpawnInterval;
    private float currentMaxSpawnInterval;
    private OrbitSafetySystem safetySystem;

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

        // Obtener o crear el sistema de seguridad
        safetySystem = FindObjectOfType<OrbitSafetySystem>();
        if (safetySystem == null && Application.isPlaying)
        {
            GameObject safetyObj = new GameObject("OrbitSafetySystem");
            safetySystem = safetyObj.AddComponent<OrbitSafetySystem>();
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
            if (pulsatingRingPrefab != null) loadedPrefabs++;
            if (spiralFragmentPrefab != null) loadedPrefabs++;
            if (zigzagBarrierPrefab != null) loadedPrefabs++;
            
            Debug.Log($"ObstacleManager: Loaded {loadedPrefabs}/8 prefabs (Max difficulty: {maxDifficultyLevel})");
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
        if (pulsatingRingPrefab == null)
            pulsatingRingPrefab = LoadPrefabByName("PulsatingRing");
        if (spiralFragmentPrefab == null)
            spiralFragmentPrefab = LoadPrefabByName("SpiralFragment");
        if (zigzagBarrierPrefab == null)
            zigzagBarrierPrefab = LoadPrefabByName("ZigzagBarrier");
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
            // Verificar cuántos obstáculos hay en pantalla antes de spawnear
            int obstaclesOnScreen = CountObstaclesOnScreen();
            
            if (obstaclesOnScreen < maxObstaclesOnScreen)
            {
                Debug.Log($"ObstacleManager: Attempting to spawn obstacle (time: {timeSinceLastSpawn}, threshold: {nextSpawnTime}, obstacles on screen: {obstaclesOnScreen}/{maxObstaclesOnScreen})");
                SpawnObstacle();
                timeSinceLastSpawn = 0f;
                nextSpawnTime = Random.Range(currentMinSpawnInterval, currentMaxSpawnInterval);
                Debug.Log($"ObstacleManager: Next spawn in {nextSpawnTime} seconds (min: {currentMinSpawnInterval:F2}, max: {currentMaxSpawnInterval:F2})");
            }
            else
            {
                // Esperar un poco más antes de intentar spawnear de nuevo
                timeSinceLastSpawn = nextSpawnTime - 0.5f; // Reducir el tiempo para intentar de nuevo pronto
                Debug.Log($"ObstacleManager: Max obstacles reached ({obstaclesOnScreen}/{maxObstaclesOnScreen}), waiting...");
            }
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

    /// <summary>
    /// Obtiene el nivel de dificultad actual basado en el tiempo de juego
    /// </summary>
    private ObstacleDifficultyLevel GetCurrentDifficultyLevel()
    {
        if (!useDifficultyProgression)
        {
            return maxDifficultyLevel;
        }
        
        if (gameTime >= timeToUnlockVeryHard)
        {
            return ObstacleDifficultyLevel.VeryHard;
        }
        else if (gameTime >= timeToUnlockHard)
        {
            return ObstacleDifficultyLevel.Hard;
        }
        else if (gameTime >= timeToUnlockMedium)
        {
            return ObstacleDifficultyLevel.Medium;
        }
        else
        {
            return ObstacleDifficultyLevel.Easy;
        }
    }
    
    /// <summary>
    /// Obtiene la dificultad de un prefab de obstáculo
    /// </summary>
    private ObstacleDifficultyLevel GetObstacleDifficulty(GameObject prefab)
    {
        if (prefab == null) return ObstacleDifficultyLevel.Easy;
        
        // Primero intentar obtener la dificultad del atributo en el tipo del componente
        // Buscar todos los componentes MonoBehaviour en el prefab
        MonoBehaviour[] components = prefab.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour comp in components)
        {
            if (comp == null) continue;
            
            System.Type compType = comp.GetType();
            
            // Verificar si el componente implementa IObstacleDifficulty
            if (comp is IObstacleDifficulty)
            {
                return ((IObstacleDifficulty)comp).GetDifficulty();
            }
            
            // Verificar si tiene el atributo ObstacleDifficultyAttribute
            var attributes = System.Attribute.GetCustomAttributes(compType, typeof(ObstacleDifficultyAttribute));
            if (attributes.Length > 0)
            {
                return ((ObstacleDifficultyAttribute)attributes[0]).Difficulty;
            }
        }
        
        // Si no se encontró, intentar por nombre del prefab (fallback)
        string prefabName = prefab.name.ToLower();
        if (prefabName.Contains("pulsating") || prefabName.Contains("spiral") || prefabName.Contains("zigzag"))
        {
            return ObstacleDifficultyLevel.Hard;
        }
        else if (prefabName.Contains("random") || prefabName.Contains("oscillating") || prefabName.Contains("rotating"))
        {
            return ObstacleDifficultyLevel.Medium;
        }
        
        // Por defecto, asumir Easy
        return ObstacleDifficultyLevel.Easy;
    }
    
    /// <summary>
    /// TEMPORAL: Crea obstáculos complejos dinámicamente si los prefabs no están disponibles
    /// </summary>
    private void CreateComplexObstacleDynamically()
    {
        // Seleccionar un tipo aleatorio de obstáculo complejo
        int obstacleType = Random.Range(0, 3);
        
        GameObject obstacleObj = new GameObject();
        obstacleObj.name = obstacleType == 0 ? "PulsatingRing" : (obstacleType == 1 ? "SpiralFragment" : "ZigzagBarrier");
        
        // Agregar el componente correspondiente
        if (obstacleType == 0)
        {
            obstacleObj.AddComponent<PulsatingRing>();
        }
        else if (obstacleType == 1)
        {
            obstacleObj.AddComponent<SpiralFragment>();
        }
        else
        {
            obstacleObj.AddComponent<ZigzagBarrier>();
        }
        
        // Continuar con el spawn normal desde aquí
        // Necesitamos calcular la posición de spawn y dirección
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("ObstacleManager: No camera found for dynamic obstacle creation!");
            Destroy(obstacleObj);
            return;
        }
        
        // Obtener los límites de la pantalla
        float screenHeight = mainCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * mainCamera.aspect;
        Vector3 cameraPos = mainCamera.transform.position;
        
        // Obtener el radio de la órbita del jugador
        float orbitRadius = spawnRadius;
        if (playerOrbit != null)
        {
            orbitRadius = playerOrbit.radius;
        }
        
        // Elegir un punto en la órbita del jugador
        float targetAngleDegrees = Random.Range(0f, 360f);
        if (safetySystem != null)
        {
            if (!safetySystem.IsAngleSafe(targetAngleDegrees))
            {
                targetAngleDegrees = safetySystem.FindSafeAngle(targetAngleDegrees);
            }
        }
        
        float targetAngle = targetAngleDegrees * Mathf.Deg2Rad;
        Vector3 targetPointOnOrbit = center.position + new Vector3(
            Mathf.Cos(targetAngle) * orbitRadius,
            Mathf.Sin(targetAngle) * orbitRadius,
            0f
        );
        
        // Calcular posición de spawn
        float spawnAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float maxScreenDistance = Mathf.Max(screenWidth, screenHeight) / 2f + spawnDistanceFromScreen;
        
        Vector3 spawnPosition = new Vector3(
            cameraPos.x + Mathf.Cos(spawnAngle) * maxScreenDistance,
            cameraPos.y + Mathf.Sin(spawnAngle) * maxScreenDistance,
            0f
        );
        
        obstacleObj.transform.position = spawnPosition;
        
        // Calcular dirección de movimiento
        Vector2 movementDirection = (targetPointOnOrbit - spawnPosition).normalized;
        
        // Agregar ObstacleMover
        ObstacleMover mover = obstacleObj.AddComponent<ObstacleMover>();
        float randomSpeedMultiplier = Random.Range(1.0f, speedVariation);
        float randomSpeed = obstacleSpeed * randomSpeedMultiplier;
        mover.SetDirection(movementDirection);
        mover.SetSpeed(randomSpeed);
        
        // Agregar efectos
        ObstacleGlow glow = obstacleObj.AddComponent<ObstacleGlow>();
        ObstacleSafetyTracker tracker = obstacleObj.AddComponent<ObstacleSafetyTracker>();
        
        // Registrar en el sistema de seguridad
        if (safetySystem != null)
        {
            safetySystem.RegisterObstacle(obstacleObj);
        }
        
        // Aplicar tamaño aleatorio
        float randomValue = Random.Range(0f, 1f);
        float randomSizeMultiplier = 1.0f + (sizeVariation - 1.0f) * (randomValue * randomValue);
        StartCoroutine(ApplyObstacleScale(obstacleObj, randomSizeMultiplier));
        
        Debug.Log($"ObstacleManager: Created dynamic obstacle {obstacleObj.name} at {spawnPosition}");
    }

    private void SpawnObstacle()
    {
        // Obtener todos los prefabs disponibles (antiguos y nuevos)
        GameObject[] allPrefabs = { 
            doorFixedPrefab, doorRandomPrefab, oscillatingBarrierPrefab, 
            rotatingArcPrefab, staticArcPrefab,
            pulsatingRingPrefab, spiralFragmentPrefab, zigzagBarrierPrefab
        };
        
        // Obtener el nivel de dificultad actual
        ObstacleDifficultyLevel currentDifficulty = GetCurrentDifficultyLevel();
        
        // Filtrar prefabs válidos y que estén dentro del nivel de dificultad permitido
        System.Collections.Generic.List<GameObject> validPrefabs = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject prefab in allPrefabs)
        {
            if (prefab != null)
            {
                ObstacleDifficultyLevel prefabDifficulty = GetObstacleDifficulty(prefab);
                
                // Solo incluir si la dificultad del prefab es menor o igual a la máxima permitida
                // y menor o igual a la dificultad actual
                if (prefabDifficulty <= maxDifficultyLevel && prefabDifficulty <= currentDifficulty)
                {
                    validPrefabs.Add(prefab);
                }
            }
            else
            {
                Debug.LogWarning($"ObstacleManager: Prefab is null in array");
            }
        }

        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning($"ObstacleManager: No valid obstacle prefabs assigned! (Current difficulty: {currentDifficulty}, Max: {maxDifficultyLevel})");
            Debug.LogWarning($"ObstacleManager: PulsatingRing={pulsatingRingPrefab != null}, SpiralFragment={spiralFragmentPrefab != null}, ZigzagBarrier={zigzagBarrierPrefab != null}");
            
            // Si no hay prefabs disponibles, intentar crear obstáculos dinámicamente (solo para los complejos)
            if (currentDifficulty >= ObstacleDifficultyLevel.Hard)
            {
                CreateComplexObstacleDynamically();
            }
            return;
        }
        
        Debug.Log($"ObstacleManager: Spawning obstacle (Current difficulty: {currentDifficulty}, Available prefabs: {validPrefabs.Count})");

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
        
        // Elegir un punto en la órbita del jugador por donde debe pasar el obstáculo
        // Usar el sistema de seguridad para asegurar que siempre haya un camino libre
        float targetAngleDegrees = Random.Range(0f, 360f);
        
        // Verificar si el ángulo es seguro, si no, encontrar uno seguro
        if (safetySystem != null)
        {
            if (!safetySystem.IsAngleSafe(targetAngleDegrees))
            {
                targetAngleDegrees = safetySystem.FindSafeAngle(targetAngleDegrees);
            }
        }
        
        float targetAngle = targetAngleDegrees * Mathf.Deg2Rad;
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
        
        // Asignar velocidad aleatoria (entre 1.0x y speedVariation)
        float randomSpeedMultiplier = Random.Range(1.0f, speedVariation);
        float randomSpeed = obstacleSpeed * randomSpeedMultiplier;
        
        mover.SetDirection(movementDirection);
        mover.SetSpeed(randomSpeed);
        
        // Aplicar tamaño aleatorio al obstáculo (entre 1.0x y sizeVariation)
        // Usar distribución sesgada: más probable que sea pequeño, menos probable que sea grande
        // Usar una función cuadrática para sesgar hacia valores más pequeños
        float randomValue = Random.Range(0f, 1f);
        float randomSizeMultiplier = 1.0f + (sizeVariation - 1.0f) * (randomValue * randomValue);
        
        // Aplicar escalado después de que se ejecute Start() para que los sprites ya estén creados
        StartCoroutine(ApplyObstacleScale(obstacle, randomSizeMultiplier));
        
        // Agregar efecto de brillo aleatorio al obstáculo
        ObstacleGlow glow = obstacle.GetComponent<ObstacleGlow>();
        if (glow == null)
        {
            glow = obstacle.AddComponent<ObstacleGlow>();
        }
        
        // Registrar el obstáculo en el sistema de seguridad
        if (safetySystem != null)
        {
            safetySystem.RegisterObstacle(obstacle);
        }
        
        // Agregar un componente para desregistrarse cuando se destruya
        ObstacleSafetyTracker tracker = obstacle.GetComponent<ObstacleSafetyTracker>();
        if (tracker == null)
        {
            tracker = obstacle.AddComponent<ObstacleSafetyTracker>();
        }
        
        Debug.Log($"ObstacleManager: Spawned {selectedPrefab.name} at {obstacle.transform.position} moving {movementDirection} (speed: {randomSpeed:F2}x, size: {randomSizeMultiplier:F2}x)");
    }

    /// <summary>
    /// Aplica el escalado al obstáculo después de que se ejecute Start()
    /// </summary>
    private System.Collections.IEnumerator ApplyObstacleScale(GameObject obstacle, float scaleMultiplier)
    {
        // Esperar dos frames para asegurar que Start() de todos los componentes se haya ejecutado
        yield return null;
        yield return null;
        
        if (obstacle == null) yield break;
        
        // Aplicar el escalado al transform principal (esto escalará automáticamente todos los hijos)
        obstacle.transform.localScale = Vector3.one * scaleMultiplier;
        
        // Los colliders se escalan automáticamente con el transform en Unity
        AdjustCollidersToScale(obstacle, scaleMultiplier);
    }

    /// <summary>
    /// Cuenta cuántos obstáculos hay actualmente en pantalla
    /// </summary>
    private int CountObstaclesOnScreen()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }

        if (mainCamera == null) return 0;

        int count = 0;
        
        // Buscar todos los objetos con ObstacleMover (todos los obstáculos activos tienen este componente)
        ObstacleMover[] allObstacles = FindObjectsOfType<ObstacleMover>();
        
        foreach (ObstacleMover mover in allObstacles)
        {
            if (mover == null || mover.gameObject == null) continue;
            
            // Verificar si el obstáculo está en pantalla o cerca de ella
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(mover.transform.position);
            
            // Considerar que está "en pantalla" si está dentro o cerca de los límites de la pantalla
            if (viewportPos.x > -0.5f && viewportPos.x < 1.5f &&
                viewportPos.y > -0.5f && viewportPos.y < 1.5f &&
                viewportPos.z >= mainCamera.nearClipPlane && viewportPos.z <= mainCamera.farClipPlane)
            {
                count++;
            }
        }
        
        return count;
    }

    /// <summary>
    /// Ajusta los colliders al nuevo tamaño del obstáculo
    /// </summary>
    private void AdjustCollidersToScale(GameObject obstacle, float scaleMultiplier)
    {
        if (obstacle == null) return;
        
        // Ajustar todos los colliders en el obstáculo y sus hijos
        Collider2D[] colliders = obstacle.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D collider in colliders)
        {
            if (collider == null) continue;
            
            // Los colliders se escalan automáticamente con el transform,
            // pero podemos ajustar el tamaño base si es necesario
            // Nota: Unity escala automáticamente los colliders con el transform,
            // así que esto puede no ser necesario, pero lo dejamos por si acaso
        }
    }
}

