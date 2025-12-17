using UnityEngine;
using System.Collections;
using static LogHelper;

public class ObstacleManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float minSpawnInterval = 2f;
    public float maxSpawnInterval = 4f;
    
    [Header("Difficulty Progression")]
    public float minSpawnIntervalMin = 0.5f; // Intervalo mínimo al máximo de dificultad
    public float maxSpawnIntervalMin = 1.0f; // Intervalo máximo al máximo de dificultad
    public float difficultyIncreaseRate = 0.02f; // Reducción del intervalo por segundo de juego (reducido para progresión más gradual)
    public float difficultyUpdateInterval = 1f; // Cada cuánto actualizar la dificultad
    public float maxDifficultyTime = 120f; // Tiempo en segundos para alcanzar la dificultad máxima (más tiempo = progresión más gradual)

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
    public bool useScoreBasedDifficulty = true; // Si true, usa el score en lugar del tiempo para desbloquear dificultades
    public float scoreToUnlockMedium = 15f; // Puntos para desbloquear obstáculos Medium
    public float scoreToUnlockHard = 30f; // Puntos para desbloquear obstáculos Hard
    public float scoreToUnlockVeryHard = 60f; // Puntos para desbloquear obstáculos VeryHard
    public float timeToUnlockMedium = 20f; // Segundos para desbloquear obstáculos Medium (si useScoreBasedDifficulty es false)
    public float timeToUnlockHard = 50f; // Segundos para desbloquear obstáculos Hard (si useScoreBasedDifficulty es false)
    public float timeToUnlockVeryHard = 90f; // Segundos para desbloquear obstáculos VeryHard (si useScoreBasedDifficulty es false)

    [Header("Spawn Settings")]
    public float spawnRadius = 2f; // Mismo radio que la órbita del jugador
    public Transform center;
    private PlayerOrbit playerOrbit;

    [Header("Movement Settings")]
    public float obstacleSpeed = 3f;
    public float spawnDistanceFromScreen = 12f; // Distancia fuera de la pantalla para spawnear
    [Tooltip("Variación de velocidad reducida para más justicia (1.0x a 1.2x en lugar de 1.5x)")]
    public float speedVariation = 1.2f; // Multiplicador máximo para variación de velocidad (1.0 a 1.2x) - REDUCIDO de 1.5x
    [Tooltip("Variación de tamaño reducida para más justicia (1.0x a 2.0x en lugar de 3.0x)")]
    public float sizeVariation = 2.0f; // Multiplicador máximo para variación de tamaño (1.0 a 2.0x) - REDUCIDO de 3.0x
    [Tooltip("Máximo número de obstáculos en pantalla (reducido para más justicia)")]
    public int maxObstaclesOnScreen = 3; // Máximo número de obstáculos en pantalla simultáneamente - REDUCIDO de 5 a 3

    private Camera mainCamera;
    private float timeSinceLastSpawn = 0f;
    private float nextSpawnTime;
    private float gameTime = 0f; // Tiempo transcurrido desde el inicio
    private float timeSinceDifficultyUpdate = 0f;
    private float currentMinSpawnInterval;
    private float currentMaxSpawnInterval;
    private OrbitSafetySystem safetySystem;
    private ScoreManager scoreManager; // Referencia al ScoreManager para obtener el score actual
    private BackgroundManager backgroundManager; // Referencia al BackgroundManager para cambiar fondos
    private ObstacleDifficultyLevel lastDifficultyLevel = ObstacleDifficultyLevel.Easy; // Trackear cambios de dificultad
    
    [Header("Breathing Room System")]
    [Tooltip("Tiempo de pausa después de un choque cercano (en segundos)")]
    public float breathingRoomDuration = 1.5f; // Tiempo de pausa después de un choque cercano
    [Tooltip("Distancia mínima para considerar un 'choque cercano'")]
    public float nearMissDistance = 0.8f; // Distancia mínima para considerar un choque cercano
    [Tooltip("Multiplicador de intervalo de spawn cuando hay peligro")]
    public float dangerSpawnMultiplier = 1.5f; // Multiplicador cuando el jugador está en peligro
    
    private float breathingRoomTimer = 0f; // Timer para el sistema de breathing room
    private float lastNearMissTime = -10f; // Tiempo del último near miss
    private PlayerOrbit playerOrbitRef; // Referencia al jugador para detectar near misses

    private void Start()
    {
        Log("ObstacleManager: Start() called");
        
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
            Log($"ObstacleManager: Camera found - Orthographic: {mainCamera.orthographic}, Size: {mainCamera.orthographicSize}");
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
        playerOrbitRef = playerOrbit; // Guardar referencia para near miss detection
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
            
            Log($"ObstacleManager: Loaded {loadedPrefabs}/8 prefabs (Max difficulty: {maxDifficultyLevel})");
        }

        // Inicializar intervalos actuales
        currentMinSpawnInterval = minSpawnInterval;
        currentMaxSpawnInterval = maxSpawnInterval;
        
        // Buscar ScoreManager para obtener el score actual
        scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogWarning("ObstacleManager: ScoreManager no encontrado. La dificultad basada en score no funcionará.");
        }
        
        // Buscar BackgroundManager para cambiar fondos según dificultad
        backgroundManager = FindObjectOfType<BackgroundManager>();
        if (backgroundManager == null)
        {
            Debug.LogWarning("ObstacleManager: BackgroundManager no encontrado. Los fondos no cambiarán automáticamente.");
        }
        else
        {
            // Inicializar con el fondo de dificultad inicial
            ObstacleDifficultyLevel initialLevel = GetCurrentDifficultyLevel();
            string presetName = GetPresetNameFromDifficulty(initialLevel);
            BackgroundSystemAPI.SetPreset(presetName, 0f); // Sin transición al inicio
            lastDifficultyLevel = initialLevel;
        }
        
        // Spawnear el primer obstáculo inmediatamente
        nextSpawnTime = 0f;
        timeSinceLastSpawn = 0f;
        Log($"ObstacleManager: First obstacle will spawn immediately");
    }

    private void LoadPrefabsIfNeeded()
    {
        if (!Application.isPlaying) return;
        
        #if UNITY_EDITOR
        // En el editor, usar AssetDatabase
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
        #else
        // En builds, intentar cargar desde Resources (si están en una carpeta Resources)
        // Si no están, se crearán dinámicamente cuando sea necesario
        if (doorFixedPrefab == null)
            doorFixedPrefab = Resources.Load<GameObject>("Prefabs/Obstacles/DoorFixed");
        if (doorRandomPrefab == null)
            doorRandomPrefab = Resources.Load<GameObject>("Prefabs/Obstacles/DoorRandom");
        if (oscillatingBarrierPrefab == null)
            oscillatingBarrierPrefab = Resources.Load<GameObject>("Prefabs/Obstacles/OscillatingBarrier");
        if (rotatingArcPrefab == null)
            rotatingArcPrefab = Resources.Load<GameObject>("Prefabs/Obstacles/RotatingArc");
        if (staticArcPrefab == null)
            staticArcPrefab = Resources.Load<GameObject>("Prefabs/Obstacles/StaticArc");
        if (pulsatingRingPrefab == null)
            pulsatingRingPrefab = Resources.Load<GameObject>("Prefabs/Obstacles/PulsatingRing");
        if (spiralFragmentPrefab == null)
            spiralFragmentPrefab = Resources.Load<GameObject>("Prefabs/Obstacles/SpiralFragment");
        if (zigzagBarrierPrefab == null)
            zigzagBarrierPrefab = Resources.Load<GameObject>("Prefabs/Obstacles/ZigzagBarrier");
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
        
        // Sistema de Breathing Room: actualizar timer
        if (breathingRoomTimer > 0f)
        {
            breathingRoomTimer -= Time.deltaTime;
        }
        
        // Detectar near misses (choques cercanos)
        CheckForNearMisses();

        // Actualizar dificultad progresivamente
        if (timeSinceDifficultyUpdate >= difficultyUpdateInterval)
        {
            UpdateDifficulty();
            
            // Verificar si cambió el nivel de dificultad y actualizar fondo
            ObstacleDifficultyLevel currentLevel = GetCurrentDifficultyLevel();
            if (currentLevel != lastDifficultyLevel && backgroundManager != null)
            {
                string presetName = GetPresetNameFromDifficulty(currentLevel);
                BackgroundSystemAPI.SetPreset(presetName, 1f); // Transición de 1 segundo
                lastDifficultyLevel = currentLevel;
                Log($"ObstacleManager: Difficulty level changed to {currentLevel}, background updated to {presetName}");
            }
            
            timeSinceDifficultyUpdate = 0f;
        }

        // CRÍTICO: No spawnean si estamos en breathing room
        if (breathingRoomTimer > 0f)
        {
            return; // Pausar spawns durante breathing room
        }

        if (timeSinceLastSpawn >= nextSpawnTime)
        {
            // Verificar cuántos obstáculos hay en pantalla antes de spawnear
            int obstaclesOnScreen = CountObstaclesOnScreen();
            
            // Calcular multiplicador de spawn basado en peligro
            float spawnMultiplier = 1f;
            if (IsPlayerInDanger())
            {
                spawnMultiplier = dangerSpawnMultiplier;
            }
            
            float adjustedSpawnTime = nextSpawnTime * spawnMultiplier;
            
            if (timeSinceLastSpawn >= adjustedSpawnTime && obstaclesOnScreen < maxObstaclesOnScreen)
            {
                Log($"ObstacleManager: Attempting to spawn obstacle (time: {timeSinceLastSpawn}, threshold: {adjustedSpawnTime}, obstacles on screen: {obstaclesOnScreen}/{maxObstaclesOnScreen})");
                SpawnObstacle();
                timeSinceLastSpawn = 0f;
                nextSpawnTime = Random.Range(currentMinSpawnInterval, currentMaxSpawnInterval);
                Log($"ObstacleManager: Next spawn in {nextSpawnTime} seconds (min: {currentMinSpawnInterval:F2}, max: {currentMaxSpawnInterval:F2})");
            }
            else if (obstaclesOnScreen >= maxObstaclesOnScreen)
            {
                // Esperar un poco más antes de intentar spawnear de nuevo
                timeSinceLastSpawn = adjustedSpawnTime - 0.5f; // Reducir el tiempo para intentar de nuevo pronto
                Log($"ObstacleManager: Max obstacles reached ({obstaclesOnScreen}/{maxObstaclesOnScreen}), waiting...");
            }
        }
    }
    
    /// <summary>
    /// Detecta si el jugador pasa muy cerca de un obstáculo (near miss)
    /// </summary>
    private void CheckForNearMisses()
    {
        if (playerOrbitRef == null || center == null) return;
        
        GameObject player = playerOrbitRef.gameObject;
        if (player == null) return;
        
        Vector3 playerPos = player.transform.position;
        float orbitRadius = playerOrbitRef.radius;
        
        // Buscar todos los obstáculos activos
        ObstacleDestructionController[] allObstacles = FindObjectsOfType<ObstacleDestructionController>();
        
        foreach (ObstacleDestructionController obstacle in allObstacles)
        {
            if (obstacle == null || obstacle.IsDestroying()) continue;
            
            Vector3 obstaclePos = obstacle.transform.position;
            float distance = Vector3.Distance(playerPos, obstaclePos);
            
            // Si el jugador está muy cerca de un obstáculo
            if (distance < nearMissDistance)
            {
                // Verificar que el obstáculo esté cerca de la órbita
                float obstacleDistanceFromCenter = Vector3.Distance(obstaclePos, center.position);
                if (Mathf.Abs(obstacleDistanceFromCenter - orbitRadius) < 1f)
                {
                    // Near miss detectado!
                    float timeSinceLastMiss = Time.time - lastNearMissTime;
                    if (timeSinceLastMiss > 0.5f) // Evitar spam de near misses
                    {
                        lastNearMissTime = Time.time;
                        breathingRoomTimer = breathingRoomDuration;
                        Log($"ObstacleManager: Near miss detected! Breathing room activated for {breathingRoomDuration}s");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Verifica si el jugador está en peligro (muchos obstáculos cerca)
    /// </summary>
    private bool IsPlayerInDanger()
    {
        if (playerOrbitRef == null || center == null) return false;
        
        GameObject player = playerOrbitRef.gameObject;
        if (player == null) return false;
        
        Vector3 playerPos = player.transform.position;
        float dangerRadius = nearMissDistance * 2f; // Radio de peligro
        
        // Contar obstáculos cerca del jugador
        ObstacleDestructionController[] allObstacles = FindObjectsOfType<ObstacleDestructionController>();
        int nearbyObstacles = 0;
        
        foreach (ObstacleDestructionController obstacle in allObstacles)
        {
            if (obstacle == null || obstacle.IsDestroying()) continue;
            
            float distance = Vector3.Distance(playerPos, obstacle.transform.position);
            if (distance < dangerRadius)
            {
                nearbyObstacles++;
            }
        }
        
        // Si hay 2 o más obstáculos cerca, el jugador está en peligro
        return nearbyObstacles >= 2;
    }

    private void UpdateDifficulty()
    {
        // Usar una curva de progresión más suave (logarítmica/cuadrática) en lugar de lineal
        // Esto hace que la dificultad aumente más gradualmente al principio y más rápido después
        
        // Calcular el progreso normalizado (0 a 1) basado en el tiempo
        float progress = Mathf.Clamp01(gameTime / maxDifficultyTime);
        
        // Usar una curva cuadrática para suavizar la progresión
        // Al principio (progress cerca de 0) la reducción es pequeña
        // Al final (progress cerca de 1) la reducción es mayor
        float smoothProgress = progress * progress; // Curva cuadrática (más suave)
        // Alternativa: usar Mathf.SmoothStep para una curva aún más suave
        // float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
        
        // Calcular la reducción total basada en el progreso suavizado
        float totalReductionMin = (minSpawnInterval - minSpawnIntervalMin) * smoothProgress;
        float totalReductionMax = (maxSpawnInterval - maxSpawnIntervalMin) * smoothProgress;
        
        // Calcular nuevos intervalos (reducir progresivamente de forma suave)
        float newMinInterval = Mathf.Max(
            minSpawnInterval - totalReductionMin,
            minSpawnIntervalMin
        );
        float newMaxInterval = Mathf.Max(
            maxSpawnInterval - totalReductionMax,
            maxSpawnIntervalMin
        );
        
        // Asegurar que el mínimo no sea mayor que el máximo
        if (newMinInterval > newMaxInterval)
        {
            newMinInterval = newMaxInterval;
        }
        
        currentMinSpawnInterval = newMinInterval;
        currentMaxSpawnInterval = newMaxInterval;
        
        Debug.Log($"ObstacleManager: Difficulty updated - Spawn intervals: {currentMinSpawnInterval:F2}s - {currentMaxSpawnInterval:F2}s (Game time: {gameTime:F1}s, Progress: {progress:P0})");
    }

    /// <summary>
    /// Mapea el nivel de dificultad al nombre del preset de fondo
    /// </summary>
    private string GetPresetNameFromDifficulty(ObstacleDifficultyLevel level)
    {
        switch (level)
        {
            case ObstacleDifficultyLevel.Easy:
                return "Void Space";
            case ObstacleDifficultyLevel.Medium:
                return "Blue Drift";
            case ObstacleDifficultyLevel.Hard:
                return "Nebula Storm";
            case ObstacleDifficultyLevel.VeryHard:
                return "Cosmic Winds";
            default:
                return "Supernova Echo";
        }
    }
    
    /// <summary>
    /// Obtiene el nivel de dificultad actual basado en el tiempo de juego o el score
    /// </summary>
    private ObstacleDifficultyLevel GetCurrentDifficultyLevel()
    {
        if (!useDifficultyProgression)
        {
            return maxDifficultyLevel;
        }
        
        float progressValue;
        
        if (useScoreBasedDifficulty && scoreManager != null)
        {
            // Usar el score actual para determinar la dificultad
            progressValue = scoreManager.GetCurrentScore();
            
            // Log cada vez que cambia la dificultad (solo una vez por nivel)
            if (progressValue >= scoreToUnlockVeryHard)
            {
                return ObstacleDifficultyLevel.VeryHard;
            }
            else if (progressValue >= scoreToUnlockHard)
            {
                return ObstacleDifficultyLevel.Hard;
            }
            else if (progressValue >= scoreToUnlockMedium)
            {
                return ObstacleDifficultyLevel.Medium;
            }
            else
            {
                return ObstacleDifficultyLevel.Easy;
            }
        }
        else
        {
            // Usar el tiempo de juego para determinar la dificultad (comportamiento anterior)
            progressValue = gameTime;
            
            if (progressValue >= timeToUnlockVeryHard)
            {
                return ObstacleDifficultyLevel.VeryHard;
            }
            else if (progressValue >= timeToUnlockHard)
            {
                return ObstacleDifficultyLevel.Hard;
            }
            else if (progressValue >= timeToUnlockMedium)
            {
                return ObstacleDifficultyLevel.Medium;
            }
            else
            {
                return ObstacleDifficultyLevel.Easy;
            }
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
    /// Crea obstáculos simples dinámicamente si los prefabs no están disponibles
    /// </summary>
    private void CreateSimpleObstacleDynamically()
    {
        // Seleccionar un tipo aleatorio de obstáculo simple
        int obstacleType = Random.Range(0, 5);
        
        GameObject obstacleObj = new GameObject();
        string obstacleName = "";
        MonoBehaviour obstacleComponent = null;
        
        // Crear el componente correspondiente
        switch (obstacleType)
        {
            case 0:
                obstacleName = "DoorFixed";
                obstacleComponent = obstacleObj.AddComponent<DoorFixed>();
                break;
            case 1:
                obstacleName = "DoorRandom";
                obstacleComponent = obstacleObj.AddComponent<DoorRandom>();
                break;
            case 2:
                obstacleName = "StaticArc";
                obstacleComponent = obstacleObj.AddComponent<StaticArc>();
                break;
            case 3:
                obstacleName = "RotatingArc";
                obstacleComponent = obstacleObj.AddComponent<RotatingArc>();
                break;
            case 4:
                obstacleName = "OscillatingBarrier";
                obstacleComponent = obstacleObj.AddComponent<OscillatingBarrier>();
                break;
        }
        
        obstacleObj.name = obstacleName;
        
        // Continuar con el spawn normal desde aquí
        SetupDynamicObstacle(obstacleObj);
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
        SetupDynamicObstacle(obstacleObj);
    }
    
    /// <summary>
    /// Configura un obstáculo dinámico con posición, movimiento y efectos
    /// </summary>
    private void SetupDynamicObstacle(GameObject obstacleObj)
    {
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
        bool hasComplexPrefabs = false;
        
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
                    
                    // Verificar si es un obstáculo complejo (Hard)
                    if (prefabDifficulty == ObstacleDifficultyLevel.Hard)
                    {
                        hasComplexPrefabs = true;
                    }
                }
            }
        }

        // Si la dificultad es Hard o superior pero no hay prefabs complejos disponibles,
        // crear uno dinámicamente con cierta probabilidad
        // La probabilidad aumenta con la dificultad: Hard = 40%, VeryHard = 60%
        if (currentDifficulty >= ObstacleDifficultyLevel.Hard && !hasComplexPrefabs)
        {
            float complexSpawnChance = currentDifficulty == ObstacleDifficultyLevel.VeryHard ? 0.6f : 0.4f;
            
            if (Random.Range(0f, 1f) < complexSpawnChance)
            {
                Debug.Log($"ObstacleManager: Creating complex obstacle dynamically (difficulty: {currentDifficulty}, chance: {complexSpawnChance:P0})");
                CreateComplexObstacleDynamically();
                return;
            }
        }

        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning($"ObstacleManager: No valid obstacle prefabs assigned! (Current difficulty: {currentDifficulty}, Max: {maxDifficultyLevel})");
            Debug.LogWarning($"ObstacleManager: PulsatingRing={pulsatingRingPrefab != null}, SpiralFragment={spiralFragmentPrefab != null}, ZigzagBarrier={zigzagBarrierPrefab != null}");
            
            // Si no hay prefabs disponibles, crear obstáculos dinámicamente
            if (currentDifficulty >= ObstacleDifficultyLevel.Hard)
            {
                CreateComplexObstacleDynamically();
            }
            else
            {
                // Crear obstáculos simples dinámicamente si no hay prefabs
                CreateSimpleObstacleDynamically();
            }
            return;
        }
        
        // Log de debug con información del score si está disponible
        string scoreInfo = "";
        if (useScoreBasedDifficulty && scoreManager != null)
        {
            scoreInfo = $", Score: {scoreManager.GetCurrentScore()}";
        }
        Debug.Log($"ObstacleManager: Spawning obstacle (Current difficulty: {currentDifficulty}, Available prefabs: {validPrefabs.Count}, Has complex: {hasComplexPrefabs}{scoreInfo})");

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

