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
        
        // Forzar orientación horizontal (landscape) en móviles
        ConfigureScreenOrientation();
        
        InitializeGame();
    }
    
    private void ConfigureScreenOrientation()
    {
        // Configurar orientación solo para móviles
        #if UNITY_ANDROID || UNITY_IOS
        // Permitir solo rotaciones horizontales
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        
        // Forzar orientación horizontal
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        #endif
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
            
            // Escalar el personaje al 64% del tamaño original (más pequeño para el asteroide)
            player.transform.localScale = Vector3.one * 0.64f;

            // SpriteRenderer - Asteroide Errante
            SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
            sr.sprite = LoadPlayerSprite();
            if (sr.sprite == null)
            {
                // Fallback a estrella si no se encuentra el sprite
                sr.sprite = SpriteGenerator.CreateStarSprite(0.3f, CosmicTheme.EtherealLila);
                sr.color = CosmicTheme.EtherealLila;
            }
            else
            {
                sr.color = Color.white; // Color blanco para mantener los colores originales del sprite
            }
            sr.sortingOrder = 10;

            // Rigidbody2D - necesario para que las colisiones con triggers funcionen
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.isKinematic = true; // No afectado por física, solo para colisiones
            rb.gravityScale = 0f;

            // CircleCollider2D - debe ser trigger para detectar colisiones con obstáculos
            // El collider se escalará automáticamente con el transform, pero ajustamos el radio base
            CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f; // Este se escalará automáticamente a 0.4f (0.5 * 0.8)
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
            
            // PlanetSurface - Rotación de superficie del planeta
            PlanetSurface planetSurface = player.AddComponent<PlanetSurface>();
            planetSurface.rotationSpeed = 20f; // Grados por segundo
            
            // PlanetIdleAnimator - Animación idle del planeta (rotación, breathing, glow)
            PlanetIdleAnimator idleAnimator = player.AddComponent<PlanetIdleAnimator>();
            idleAnimator.rotationSpeed = 15f; // Rotación suave del planeta
            idleAnimator.scaleAmplitude = 0.03f; // Breathing effect sutil
            idleAnimator.scaleFrequency = 0.4f; // Más lento: ciclo completo cada ~2.5 segundos
            idleAnimator.glowAmplitude = 0.15f; // Glow animado
            idleAnimator.glowFrequency = 1.2f;
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

        // Crear AudioManager
        if (AudioManager.Instance == null)
        {
            GameObject audioManager = new GameObject("AudioManager");
            audioManager.AddComponent<AudioManager>();
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
        if (om.pulsatingRingPrefab == null)
            om.pulsatingRingPrefab = LoadPrefabByName("PulsatingRing");
        if (om.spiralFragmentPrefab == null)
            om.spiralFragmentPrefab = LoadPrefabByName("SpiralFragment");
        if (om.zigzagBarrierPrefab == null)
            om.zigzagBarrierPrefab = LoadPrefabByName("ZigzagBarrier");
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
    
    /// <summary>
    /// Función helper para cargar sprites que funciona tanto en editor como en builds
    /// </summary>
    private Sprite LoadPlayerSprite()
    {
        if (!Application.isPlaying) return null;
        
        // Primero intentar cargar desde Resources (funciona en editor y builds si están en carpeta Resources)
        Sprite sprite = Resources.Load<Sprite>("Art/Protagonist/AsteroideErrante");
        if (sprite != null) return sprite;
        
        // Intentar cargar como Texture2D desde Resources
        Texture2D texture = Resources.Load<Texture2D>("Art/Protagonist/AsteroideErrante");
        if (texture != null)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
        
        #if UNITY_EDITOR
        // En el editor, intentar usar AssetDatabase como fallback
        try
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("AsteroideErrante t:Sprite");
            if (guids.Length == 0)
            {
                guids = UnityEditor.AssetDatabase.FindAssets("AsteroideErrante t:Texture2D");
            }
            
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) return sprite;
                
                texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo cargar el sprite del asteroide: {e.Message}");
        }
        #endif
        
        return null;
    }

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
