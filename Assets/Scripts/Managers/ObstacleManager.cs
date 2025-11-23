using UnityEngine;
using System.Collections;

public class ObstacleManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float minSpawnInterval = 2f;
    public float maxSpawnInterval = 4f;

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

    private float timeSinceLastSpawn = 0f;
    private float nextSpawnTime;

    private void Start()
    {
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
        }

        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
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
        timeSinceLastSpawn += Time.deltaTime;

        if (timeSinceLastSpawn >= nextSpawnTime)
        {
            SpawnObstacle();
            timeSinceLastSpawn = 0f;
            nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
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

        // Select random prefab
        GameObject selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];

        // Spawn at random angle around center, en la misma órbita que el jugador
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 spawnPosition = center.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spawnRadius;

        // Determinar orientación según el tipo de obstáculo
        float rotationAngle = 0f;
        string prefabName = selectedPrefab.name.ToLower();
        
        if (prefabName.Contains("door"))
        {
            // Las puertas deben estar orientadas radialmente (hacia/desde el centro)
            // El eje Y local apunta hacia el centro
            rotationAngle = (angle * Mathf.Rad2Deg) + 90f; // +90 para que Y apunte hacia el centro
        }
        else if (prefabName.Contains("barrier"))
        {
            // Las barreras oscilantes deben estar orientadas tangencialmente
            rotationAngle = (angle * Mathf.Rad2Deg);
        }
        else if (prefabName.Contains("arc"))
        {
            // Los arcos rotan alrededor del centro, orientación inicial no importa mucho
            rotationAngle = 0f;
        }
        else
        {
            // Por defecto, orientar tangencialmente
            rotationAngle = (angle * Mathf.Rad2Deg);
        }
        
        Quaternion rotation = Quaternion.Euler(0, 0, rotationAngle);
        GameObject obstacle = Instantiate(selectedPrefab, spawnPosition, rotation);
    }
}

