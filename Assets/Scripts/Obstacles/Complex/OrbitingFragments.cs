using UnityEngine;
using static LogHelper;

/// <summary>
/// Grupo de 3-5 fragmentos pequeños que orbitan alrededor de un punto central.
/// El conjunto se mueve como un solo obstáculo (con ObstacleMover), pero los fragmentos
/// giran continuamente alrededor de su centro, creando un patrón difícil de predecir.
/// 
/// El radio de órbita local y la velocidad de rotación son configurables.
/// Cada fragmento tiene su propio collider, haciendo que el jugador deba encontrar
/// el hueco entre los fragmentos.
/// 
/// Dificultad: VeryHard — requiere timing preciso para pasar entre los fragmentos.
/// </summary>
[ObstacleDifficulty(ObstacleDifficultyLevel.VeryHard)]
public class OrbitingFragments : ObstacleBase, IObstacleDifficulty
{
    [Header("Orbit Settings")]
    [Tooltip("Número de fragmentos orbitantes")]
    public int fragmentCount = 4;
    [Tooltip("Radio de órbita de los fragmentos alrededor del centro")]
    public float orbitRadius = 0.6f;
    [Tooltip("Velocidad de rotación de los fragmentos (grados/segundo)")]
    public float orbitSpeed = 200f;
    [Tooltip("¿Los fragmentos orbitan en sentido horario?")]
    public bool clockwise = true;
    
    [Header("Fragment Visual")]
    [Tooltip("Tamaño de cada fragmento individual")]
    public float fragmentSize = 0.35f;
    
    // Componentes
    private GameObject[] fragments;
    private float[] fragmentAngles;
    private SpriteRenderer[] fragmentRenderers;
    private float currentOrbitAngle = 0f;
    
    // Centro visual (núcleo brillante)
    private GameObject coreObject;
    private SpriteRenderer coreRenderer;

    public ObstacleDifficultyLevel GetDifficulty()
    {
        return ObstacleDifficultyLevel.VeryHard;
    }

    private void Start()
    {
        Log($"OrbitingFragments: Start() called for {gameObject.name} at {transform.position}");
        
        ConfigureRootRendererAsStar();

        // Randomizar dirección
        clockwise = Random.Range(0, 2) == 0;
        
        // Randomizar número de fragmentos (3-5)
        fragmentCount = Random.Range(3, 6);
        
        CreateCore();
        CreateFragments();
        
        Log($"OrbitingFragments: Created {fragmentCount} fragments orbiting at radius {orbitRadius}");
    }

    private void ConfigureRootRendererAsStar()
    {
        SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
        if (rootRenderer == null) return;

        Sprite starSprite = SpriteGenerator.CreateStarSprite(0.35f, CosmicTheme.EtherealLila);

        rootRenderer.sprite = starSprite;
        rootRenderer.color = new Color(CosmicTheme.EtherealLila.r, CosmicTheme.EtherealLila.g, CosmicTheme.EtherealLila.b, 0.5f);
        rootRenderer.sortingOrder = 4;
        rootRenderer.sortingLayerName = "Default";
    }

    private void Update()
    {
        // Rotar todos los fragmentos alrededor del centro
        float direction = clockwise ? -1f : 1f;
        currentOrbitAngle += orbitSpeed * direction * Time.deltaTime;
        
        if (fragments == null) return;
        
        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i] == null) continue;
            
            float angle = (fragmentAngles[i] + currentOrbitAngle) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * orbitRadius;
            float y = Mathf.Sin(angle) * orbitRadius;
            
            fragments[i].transform.localPosition = new Vector3(x, y, 0f);
            
            // Rotar el fragmento individualmente para efecto visual
            fragments[i].transform.Rotate(0f, 0f, orbitSpeed * 0.5f * Time.deltaTime);
        }
        
        // Pulsar el núcleo sutilmente
        if (coreObject != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.1f;
            coreObject.transform.localScale = Vector3.one * 0.3f * pulse;
        }
        
        // Pulsar color de los fragmentos
        if (fragmentRenderers != null)
        {
            float colorPulse = Mathf.Sin(Time.time * 3f) * 0.15f;
            for (int i = 0; i < fragmentRenderers.Length; i++)
            {
                if (fragmentRenderers[i] != null)
                {
                    Color c = fragmentRenderers[i].color;
                    c.a = 0.8f + colorPulse;
                    fragmentRenderers[i].color = c;
                }
            }
        }
    }

    private void CreateCore()
    {
        coreObject = new GameObject("Core");
        coreObject.transform.SetParent(transform);
        coreObject.transform.localPosition = Vector3.zero;
        coreObject.transform.localScale = Vector3.one * 0.3f;
        
        coreRenderer = coreObject.AddComponent<SpriteRenderer>();
        Sprite coreSprite = SpriteGenerator.CreateStarSprite(0.3f, CosmicTheme.EtherealLila);

        coreRenderer.sprite = coreSprite;
        coreRenderer.color = new Color(CosmicTheme.EtherealLila.r, CosmicTheme.EtherealLila.g, CosmicTheme.EtherealLila.b, 0.8f);
        coreRenderer.sortingOrder = 5;
        coreRenderer.sortingLayerName = "Default";
        
        // El núcleo NO tiene collider — solo los fragmentos son peligrosos
    }

    private void CreateFragments()
    {
        fragments = new GameObject[fragmentCount];
        fragmentAngles = new float[fragmentCount];
        fragmentRenderers = new SpriteRenderer[fragmentCount];
        
        float angleStep = 360f / fragmentCount;
        
        for (int i = 0; i < fragmentCount; i++)
        {
            // Ángulo inicial con separación uniforme
            fragmentAngles[i] = i * angleStep;
            
            // Posición inicial
            float angle = fragmentAngles[i] * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * orbitRadius;
            float y = Mathf.Sin(angle) * orbitRadius;
            
            // Crear fragmento
            GameObject fragment = new GameObject($"Fragment_{i}");
            fragment.transform.SetParent(transform);
            fragment.transform.localPosition = new Vector3(x, y, 0f);
            
            // Visual
            SpriteRenderer sr = fragment.AddComponent<SpriteRenderer>();
            sr.sprite = CreateFragmentSprite(i);
            sr.color = GetFragmentColor(i);
            sr.sortingOrder = 6;
            sr.sortingLayerName = "Default";
            fragmentRenderers[i] = sr;
            
            // Collider
            float spriteWorldSize = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
            CircleCollider2D collider = fragment.AddComponent<CircleCollider2D>();
            collider.radius = spriteWorldSize * 0.35f;
            collider.isTrigger = true;
            
            // Detector de colisiones
            fragment.AddComponent<ObstacleCollisionDetector>();
            
            fragments[i] = fragment;
        }
    }

    private Color GetFragmentColor(int index)
    {
        // Colores variados en la paleta cósmica del juego (azules/lilas/dorados)
        Color[] palette = {
            Color.Lerp(CosmicTheme.ConstellationBlue, CosmicTheme.CelestialBlue, 0.5f),  // Azul constelación
            Color.Lerp(CosmicTheme.CelestialBlue, CosmicTheme.EtherealLila, 0.5f),      // Azul-lila
            CosmicTheme.EtherealLila,                                                    // Lila etéreo
            Color.Lerp(CosmicTheme.EtherealLila, CosmicTheme.SoftGold, 0.4f),          // Lila-dorado
            CosmicTheme.SoftGold,                                                         // Dorado suave
        };
        return palette[index % palette.Length];
    }

    private Sprite CreateCoreSprite()
    {
        // Sprite simple: círculo brillante con glow
        int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] colors = new Color[textureSize * textureSize];
        
        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float maxRadius = textureSize / 2f - 2f;
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= maxRadius)
                {
                    float normalizedDist = dist / maxRadius;
                    // Glow suave: muy brillante en el centro, fade rápido
                    float alpha = Mathf.Pow(1f - normalizedDist, 2f);
                    Color c = Color.Lerp(Color.white, new Color(1f, 0.6f, 0.2f), normalizedDist);
                    colors[y * textureSize + x] = new Color(c.r, c.g, c.b, alpha);
                }
                else
                {
                    colors[y * textureSize + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateFragmentSprite(int index)
    {
        // Forzar forma circular para evitar fragmentos cuadrados.
        // El color final se aplica en el SpriteRenderer de cada fragmento.
        return SpriteGenerator.CreateCircleSprite(0.2f, Color.white);
    }
}
