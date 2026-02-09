using UnityEngine;
using System.Collections;
using static LogHelper;

/// <summary>
/// Gestiona el spawning de Stellar Shards durante la partida y el tracking de lo recogido.
/// 
/// Balance económico:
/// - Spawn cada 8-12s → ~3-4 shards por partida media (30s)
/// - Normal (80%): 2⭐, Raro (15%): 5⭐, Ultra-raro (5%): 15⭐
/// - Esperado por partida media: ~7-10⭐
/// - Comparación: Misiones diarias dan ~225⭐/día, IAP 500⭐ = paquete más barato
/// - 10 partidas/día × ~8⭐ = ~80⭐ → suplementa misiones sin reemplazar IAP
/// </summary>
public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance { get; private set; }
    
    [Header("Spawn Settings")]
    [Tooltip("Intervalo mínimo entre spawns de shards (segundos)")]
    public float minSpawnInterval = 8f;
    [Tooltip("Intervalo máximo entre spawns de shards (segundos)")]
    public float maxSpawnInterval = 12f;
    [Tooltip("Delay inicial antes de empezar a spawnear (segundos)")]
    public float initialDelay = 5f;
    [Tooltip("Máximo de shards en pantalla simultáneamente")]
    public int maxShardsOnScreen = 3;
    
    [Header("Rarity Weights")]
    [Tooltip("Probabilidad de shard normal (0-1)")]
    public float normalChance = 0.80f;
    [Tooltip("Probabilidad de shard raro (0-1)")]
    public float rareChance = 0.15f;
    // Ultra-raro = 1 - normal - raro = 0.05f
    
    [Header("Spawn Safety")]
    [Tooltip("Distancia mínima del player para spawnear (en ángulos)")]
    public float minAngleFromPlayer = 150f; // Mínimo ~media órbita de distancia
    [Tooltip("Distancia mínima entre shards (en ángulos)")]
    public float minAngleBetweenShards = 60f;
    
    // Session tracking
    private int totalShardsCollected = 0;
    private int totalValueCollected = 0;
    private int normalCollected = 0;
    private int rareCollected = 0;
    private int ultraRareCollected = 0;
    
    // State
    private bool isSpawning = false;
    private float spawnTimer = 0f;
    private float nextSpawnTime;
    private PlayerOrbit playerOrbit;
    private Transform center;
    
    // UI
    private UnityEngine.UI.Text shardsUIText;
    
    // Propiedades públicas para lectura
    public int TotalShardsCollected => totalShardsCollected;
    public int TotalValueCollected => totalValueCollected;
    public int NormalCollected => normalCollected;
    public int RareCollected => rareCollected;
    public int UltraRareCollected => ultraRareCollected;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Inicia el spawning de coleccionables. Llamar al iniciar una partida.
    /// </summary>
    public void StartSpawning()
    {
        if (isSpawning) return;
        
        // Buscar referencias necesarias
        playerOrbit = FindFirstObjectByType<PlayerOrbit>();
        
        GameObject centerObj = GameObject.Find("Center");
        if (centerObj != null)
        {
            center = centerObj.transform;
        }
        
        if (playerOrbit == null || center == null)
        {
            LogWarning("[CollectibleManager] No se encontró PlayerOrbit o Center, reintentando...");
            StartCoroutine(RetryStartSpawning());
            return;
        }
        
        isSpawning = true;
        spawnTimer = -initialDelay; // Delay negativo = esperar antes de empezar
        nextSpawnTime = 0f;
        
        Log("[CollectibleManager] Spawning de coleccionables iniciado");
    }
    
    private IEnumerator RetryStartSpawning()
    {
        yield return new WaitForSeconds(0.5f);
        
        playerOrbit = FindFirstObjectByType<PlayerOrbit>();
        GameObject centerObj = GameObject.Find("Center");
        if (centerObj != null) center = centerObj.transform;
        
        if (playerOrbit != null && center != null)
        {
            StartSpawning();
        }
        else
        {
            LogWarning("[CollectibleManager] No se pudo iniciar spawning - referencias no encontradas");
        }
    }
    
    /// <summary>
    /// Detiene el spawning de coleccionables.
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
    }
    
    /// <summary>
    /// Resetea el tracking de la sesión para una nueva partida.
    /// </summary>
    public void ResetSession()
    {
        totalShardsCollected = 0;
        totalValueCollected = 0;
        normalCollected = 0;
        rareCollected = 0;
        ultraRareCollected = 0;
        isSpawning = false;
        spawnTimer = 0f;
        
        // Resetear UI
        UpdateShardsUI();
        
        // Destruir shards activos
        CollectibleShard[] activeShards = FindObjectsByType<CollectibleShard>(FindObjectsSortMode.None);
        foreach (CollectibleShard shard in activeShards)
        {
            if (shard != null)
            {
                Destroy(shard.gameObject);
            }
        }
        
        Log("[CollectibleManager] Sesión reseteada");
    }
    
    private void Update()
    {
        if (!isSpawning) return;
        
        // Re-buscar player si la referencia se perdió (ej: después de un revive)
        if (playerOrbit == null)
        {
            playerOrbit = FindFirstObjectByType<PlayerOrbit>();
            if (playerOrbit == null) return;
        }
        
        spawnTimer += Time.deltaTime;
        
        if (spawnTimer >= nextSpawnTime)
        {
            // Verificar cuántos shards hay en pantalla
            int currentShards = CountActiveShardsOnScreen();
            
            if (currentShards < maxShardsOnScreen)
            {
                SpawnShard();
            }
            
            spawnTimer = 0f;
            nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
        }
    }
    
    /// <summary>
    /// Cuenta los shards activos en pantalla.
    /// </summary>
    private int CountActiveShardsOnScreen()
    {
        CollectibleShard[] shards = FindObjectsByType<CollectibleShard>(FindObjectsSortMode.None);
        return shards.Length;
    }
    
    /// <summary>
    /// Spawnea un shard en la órbita del jugador.
    /// </summary>
    private void SpawnShard()
    {
        if (center == null || playerOrbit == null) return;
        
        float orbitRadius = playerOrbit.radius;
        
        // Determinar rareza
        CollectibleShard.ShardRarity rarity = DetermineRarity();
        
        // Encontrar un ángulo seguro (lejos del jugador y otros shards)
        float spawnAngle = FindSafeSpawnAngle(orbitRadius);
        if (spawnAngle < 0f) return; // No se encontró ángulo seguro
        
        // Crear el shard
        GameObject shardObj = new GameObject($"Shard_{rarity}");
        shardObj.transform.position = center.position + new Vector3(
            Mathf.Cos(spawnAngle) * orbitRadius,
            Mathf.Sin(spawnAngle) * orbitRadius,
            0f
        );
        
        // SpriteRenderer
        SpriteRenderer sr = shardObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 11; // Por encima del jugador (10)
        
        // Collider (trigger) - radio pequeño para que haya que pasar cerca
        CircleCollider2D collider = shardObj.AddComponent<CircleCollider2D>();
        collider.radius = 0.35f;
        collider.isTrigger = true;
        
        // Rigidbody2D (necesario para triggers)
        Rigidbody2D rb = shardObj.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        
        // Componente CollectibleShard
        CollectibleShard shard = shardObj.AddComponent<CollectibleShard>();
        shard.Setup(rarity);
        
        Log($"[CollectibleManager] Shard {rarity} spawneado en ángulo {spawnAngle * Mathf.Rad2Deg:F0}°");
    }
    
    /// <summary>
    /// Determina la rareza del shard basándose en las probabilidades configuradas.
    /// </summary>
    private CollectibleShard.ShardRarity DetermineRarity()
    {
        float roll = Random.value; // 0.0 - 1.0
        
        if (roll < normalChance)
        {
            return CollectibleShard.ShardRarity.Normal;
        }
        else if (roll < normalChance + rareChance)
        {
            return CollectibleShard.ShardRarity.Rare;
        }
        else
        {
            return CollectibleShard.ShardRarity.UltraRare;
        }
    }
    
    /// <summary>
    /// Encuentra un ángulo seguro en la órbita para spawnear, lejos del jugador y de otros shards.
    /// </summary>
    private float FindSafeSpawnAngle(float orbitRadius)
    {
        float playerAngle = playerOrbit.angle; // En radianes
        
        // Intentar hasta 10 veces encontrar un ángulo válido
        for (int attempt = 0; attempt < 10; attempt++)
        {
            float candidateAngle = Random.Range(0f, Mathf.PI * 2f);
            
            // Verificar distancia del jugador (en ángulos)
            float angleDiffFromPlayer = Mathf.Abs(Mathf.DeltaAngle(
                candidateAngle * Mathf.Rad2Deg, 
                playerAngle * Mathf.Rad2Deg
            ));
            
            if (angleDiffFromPlayer < minAngleFromPlayer)
            {
                continue; // Muy cerca del jugador
            }
            
            // Verificar distancia de otros shards
            bool tooCloseToOtherShard = false;
            CollectibleShard[] activeShards = FindObjectsByType<CollectibleShard>(FindObjectsSortMode.None);
            
            foreach (CollectibleShard existingShard in activeShards)
            {
                if (existingShard == null) continue;
                
                float existingAngle = Mathf.Atan2(
                    existingShard.transform.position.y - center.position.y,
                    existingShard.transform.position.x - center.position.x
                );
                
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(
                    candidateAngle * Mathf.Rad2Deg, 
                    existingAngle * Mathf.Rad2Deg
                ));
                
                if (angleDiff < minAngleBetweenShards)
                {
                    tooCloseToOtherShard = true;
                    break;
                }
            }
            
            if (!tooCloseToOtherShard)
            {
                return candidateAngle;
            }
        }
        
        // Si no encontramos ángulo seguro, no spawnear
        return -1f;
    }
    
    /// <summary>
    /// Asigna la referencia al texto de UI para mostrar las shards recogidas.
    /// </summary>
    public void SetShardsUI(UnityEngine.UI.Text uiText)
    {
        shardsUIText = uiText;
        UpdateShardsUI();
    }
    
    /// <summary>
    /// Actualiza el texto de UI con el valor actual.
    /// </summary>
    private void UpdateShardsUI()
    {
        if (shardsUIText != null)
        {
            shardsUIText.text = $"{totalValueCollected} ⭐";
        }
    }
    
    /// <summary>
    /// Llamado por CollectibleShard cuando el jugador recoge un shard.
    /// </summary>
    public void OnShardCollected(CollectibleShard shard)
    {
        totalShardsCollected++;
        totalValueCollected += shard.value;
        
        switch (shard.rarity)
        {
            case CollectibleShard.ShardRarity.Normal:
                normalCollected++;
                break;
            case CollectibleShard.ShardRarity.Rare:
                rareCollected++;
                break;
            case CollectibleShard.ShardRarity.UltraRare:
                ultraRareCollected++;
                break;
        }
        
        // Reportar progreso de misión CollectCurrency
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.ReportProgress(MissionObjectiveType.CollectCurrency, shard.value);
        }
        
        // Actualizar UI del HUD
        UpdateShardsUI();
        
        // Animación de pop en el contador
        if (shardsUIText != null)
        {
            StartCoroutine(PopShardsUI());
        }
        
        Log($"[CollectibleManager] Shard recogido: {shard.rarity} (+{shard.value}⭐). Total sesión: {totalValueCollected}⭐");
    }
    
    /// <summary>
    /// Animación de "pop" cuando se recoge un shard — el texto crece y vuelve a su tamaño.
    /// </summary>
    private IEnumerator PopShardsUI()
    {
        if (shardsUIText == null) yield break;
        
        Transform t = shardsUIText.transform;
        float duration = 0.25f;
        float maxScale = 1.4f;
        float elapsed = 0f;
        
        // Crecer
        while (elapsed < duration * 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / (duration * 0.4f);
            float scale = Mathf.Lerp(1f, maxScale, progress);
            t.localScale = Vector3.one * scale;
            yield return null;
        }
        
        // Encoger de vuelta
        elapsed = 0f;
        while (elapsed < duration * 0.6f)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / (duration * 0.6f);
            float scale = Mathf.Lerp(maxScale, 1f, progress);
            t.localScale = Vector3.one * scale;
            yield return null;
        }
        
        t.localScale = Vector3.one;
    }
    
    /// <summary>
    /// Otorga las Stellar Shards recogidas durante la partida al CurrencyManager.
    /// Llamar al finalizar la partida (game over).
    /// </summary>
    public void AwardSessionEarnings()
    {
        if (totalValueCollected <= 0) return;
        
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddStellarShards(totalValueCollected);
            Log($"[CollectibleManager] {totalValueCollected}⭐ otorgadas al jugador ({totalShardsCollected} shards recogidos)");
        }
        else
        {
            LogWarning("[CollectibleManager] CurrencyManager no disponible para otorgar recompensas");
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
