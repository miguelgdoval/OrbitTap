using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Este script inicializa todos los elementos del juego en tiempo de ejecución
/// para evitar problemas de serialización en las escenas.
/// Se ejecuta antes que otros scripts para asegurar que todo esté listo.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        if (!Application.isPlaying) return;
        
        InitializeGame();
    }

    private void InitializeGame()
    {
        // Crear Center
        GameObject center = GameObject.Find("Center");
        if (center == null)
        {
            center = new GameObject("Center");
            center.transform.position = Vector3.zero;
        }

        // Crear Player
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            
            // Asegurar que el tag "Player" existe y está asignado
            try
            {
                player.tag = "Player";
            }
            catch
            {
                // Si el tag no existe, usar el nombre del GameObject como alternativa
                Debug.LogWarning("Tag 'Player' no encontrado. Usando nombre del GameObject para detección.");
            }
            
            player.transform.position = new Vector3(2, 0, 0);

            // SpriteRenderer - Estrella naciente
            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteGenerator.CreateStarSprite(0.3f, CosmicTheme.EtherealLila);
            sr.color = CosmicTheme.EtherealLila;
            sr.sortingOrder = 10;

            // Rigidbody2D - necesario para que las colisiones con triggers funcionen
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.isKinematic = true; // No afectado por física, solo para colisiones
            rb.gravityScale = 0f;

            // CircleCollider2D - debe ser trigger para detectar colisiones con obstáculos
            CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            // PlayerOrbit
            PlayerOrbit orbit = player.AddComponent<PlayerOrbit>();
            orbit.radius = 2f;
            orbit.angle = 0f;
            orbit.angularSpeed = 2f;
            orbit.center = center.transform;

            // FlashEffect
            FlashEffect flash = player.AddComponent<FlashEffect>();
            orbit.flashEffect = flash;
            orbit.spriteRenderer = sr;
            
            // StarParticleTrail - Cola de partículas
            player.AddComponent<StarParticleTrail>();
        }

        // Crear fondo cósmico
        GameObject cosmicBg = GameObject.Find("CosmicBackground");
        if (cosmicBg == null)
        {
            cosmicBg = new GameObject("CosmicBackground");
            cosmicBg.AddComponent<CosmicBackground>();
        }

        // Crear anillo sagrado de órbita
        GameObject orbitRing = GameObject.Find("SacredOrbitRing");
        if (orbitRing == null)
        {
            orbitRing = new GameObject("SacredOrbitRing");
            orbitRing.transform.SetParent(center.transform);
            orbitRing.transform.localPosition = Vector3.zero;
            SacredOrbitRing ring = orbitRing.AddComponent<SacredOrbitRing>();
            ring.radius = 2f; // Mismo radio que la órbita del jugador
        }

        // Crear InputController
        GameObject inputController = GameObject.Find("InputController");
        if (inputController == null)
        {
            inputController = new GameObject("InputController");
            inputController.AddComponent<InputController>();
        }

        // Crear ObstacleManager
        GameObject obstacleManager = GameObject.Find("ObstacleManager");
        if (obstacleManager == null)
        {
            obstacleManager = new GameObject("ObstacleManager");
            ObstacleManager om = obstacleManager.AddComponent<ObstacleManager>();
            om.minSpawnInterval = 2f;
            om.maxSpawnInterval = 4f;
            om.spawnRadius = 2f; // Mismo radio que la órbita del jugador
            om.center = center.transform;
            
            // Cargar prefabs
            LoadObstaclePrefabs(om);
        }

        // Crear ScoreManager
        GameObject scoreManager = GameObject.Find("ScoreManager");
        if (scoreManager == null)
        {
            scoreManager = new GameObject("ScoreManager");
            ScoreManager sm = scoreManager.AddComponent<ScoreManager>();
            
            // Crear UI para Score
            CreateScoreUI(sm);
        }
    }

    private void LoadObstaclePrefabs(ObstacleManager om)
    {
        #if UNITY_EDITOR
        if (om.doorFixedPrefab == null)
            om.doorFixedPrefab = LoadPrefabByName("DoorFixed");
        if (om.doorRandomPrefab == null)
            om.doorRandomPrefab = LoadPrefabByName("DoorRandom");
        if (om.oscillatingBarrierPrefab == null)
            om.oscillatingBarrierPrefab = LoadPrefabByName("OscillatingBarrier");
        if (om.rotatingArcPrefab == null)
            om.rotatingArcPrefab = LoadPrefabByName("RotatingArc");
        if (om.staticArcPrefab == null)
            om.staticArcPrefab = LoadPrefabByName("StaticArc");
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

    private void CreateScoreUI(ScoreManager sm)
    {
        // Crear Canvas
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
            canvas.layer = 5; // UI layer

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.sizeDelta = Vector2.zero;
        }

        // Crear ScoreText
        GameObject scoreTextObj = GameObject.Find("ScoreText");
        if (scoreTextObj == null)
        {
            scoreTextObj = new GameObject("ScoreText");
            scoreTextObj.transform.SetParent(canvas.transform, false);
            Text scoreText = scoreTextObj.AddComponent<Text>();
            scoreText.text = "0";
            scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            scoreText.fontSize = 40;
            scoreText.color = CosmicTheme.SoftGold; // Dorado suave para la puntuación
            scoreText.alignment = TextAnchor.UpperCenter;

            RectTransform scoreRect = scoreTextObj.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 1f);
            scoreRect.anchorMax = new Vector2(0.5f, 1f);
            scoreRect.anchoredPosition = new Vector2(0, -50);
            scoreRect.sizeDelta = new Vector2(200, 50);

            sm.scoreText = scoreText;
        }
    }
}
