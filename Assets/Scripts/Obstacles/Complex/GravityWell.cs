using UnityEngine;
using static LogHelper;

/// <summary>
/// Pozo gravitatorio que no mata directamente al jugador, sino que desvía su órbita
/// temporalmente, haciendo más difícil esquivar otros obstáculos.
/// 
/// Funcionamiento:
/// - Cuando el jugador entra en el campo de influencia, su velocidad angular se modifica.
/// - El efecto es una "atracción" o "repulsión" gravitatoria que altera la trayectoria.
/// - El pozo en sí no mata, pero la desviación puede hacer que el jugador choque
///   con otros obstáculos.
/// - Visual: vórtice oscuro con anillos de acreción.
/// 
/// Dificultad: VeryHard — no es letal directamente pero complica mucho la situación.
/// </summary>
[ObstacleDifficulty(ObstacleDifficultyLevel.VeryHard)]
public class GravityWell : ObstacleBase, IObstacleDifficulty
{
    [Header("Gravity Settings")]
    [Tooltip("Radio del campo de influencia gravitatorio")]
    public float influenceRadius = 2.5f;
    [Tooltip("Fuerza del efecto gravitatorio sobre la velocidad angular del jugador")]
    public float gravityStrength = 1.5f;
    [Tooltip("¿Atrae (true) o repele (false)?")]
    public bool isAttracting = true;
    [Tooltip("Duración del efecto sobre el jugador después de salir del campo (segundos)")]
    public float lingeringDuration = 0.5f;
    
    [Header("Visual")]
    [Tooltip("Velocidad de rotación del vórtice (grados/segundo)")]
    public float vortexRotationSpeed = 120f;
    [Tooltip("Velocidad de pulsación del campo de influencia")]
    public float fieldPulseSpeed = 2f;
    
    // Componentes visuales
    private GameObject vortexCore;
    private SpriteRenderer coreRenderer;
    private GameObject influenceField;
    private SpriteRenderer fieldRenderer;
    private GameObject accretionRing;
    private SpriteRenderer ringRenderer;
    
    // Estado
    private PlayerOrbit playerOrbit;
    private bool playerInField = false;
    private float originalAngularSpeed;
    private float lingeringTimer = 0f;
    private bool hasAppliedEffect = false;

    public ObstacleDifficultyLevel GetDifficulty()
    {
        return ObstacleDifficultyLevel.VeryHard;
    }

    private void Start()
    {
        Log($"GravityWell: Start() called for {gameObject.name} at {transform.position}");
        
        // Randomizar tipo
        isAttracting = Random.Range(0, 2) == 0;

        ConfigureRootRendererAsStar();
        
        CreateVisuals();
        FindPlayer();
        
        Log($"GravityWell: Created ({(isAttracting ? "attracting" : "repelling")}) with influence radius {influenceRadius}");
    }

    private void ConfigureRootRendererAsStar()
    {
        SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
        if (rootRenderer == null) return;

        Color starColor = isAttracting
            ? new Color(0.8f, 0.35f, 1f, 0.85f)
            : new Color(0.35f, 0.85f, 1f, 0.85f);

        Sprite starSprite = SpriteGenerator.CreateStarSprite(0.4f, starColor);

        rootRenderer.sprite = starSprite;
        rootRenderer.color = starColor;
        rootRenderer.sortingOrder = 2;
        rootRenderer.sortingLayerName = "Default";
    }

    private void FindPlayer()
    {
        playerOrbit = FindFirstObjectByType<PlayerOrbit>();
        if (playerOrbit != null)
        {
            originalAngularSpeed = playerOrbit.angularSpeed;
        }
    }

    private void Update()
    {
        // Re-buscar player si se perdió la referencia
        if (playerOrbit == null)
        {
            FindPlayer();
            if (playerOrbit == null) return;
        }
        
        // Rotar el vórtice
        if (vortexCore != null)
        {
            float direction = isAttracting ? -1f : 1f;
            vortexCore.transform.Rotate(0f, 0f, vortexRotationSpeed * direction * Time.deltaTime);
        }
        
        // Rotar el anillo de acreción (más lento, dirección opuesta)
        if (accretionRing != null)
        {
            float direction = isAttracting ? 1f : -1f;
            accretionRing.transform.Rotate(0f, 0f, vortexRotationSpeed * 0.3f * direction * Time.deltaTime);
        }
        
        // Pulsar el campo de influencia
        if (influenceField != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * fieldPulseSpeed) * 0.1f;
            influenceField.transform.localScale = Vector3.one * influenceRadius * 2f * pulse;
            
            // Alpha pulsante
            if (fieldRenderer != null)
            {
                Color c = fieldRenderer.color;
                c.a = 0.08f + Mathf.Sin(Time.time * fieldPulseSpeed) * 0.03f;
                fieldRenderer.color = c;
            }
        }
        
        // Comprobar si el jugador está dentro del campo de influencia
        float distToPlayer = Vector3.Distance(transform.position, playerOrbit.transform.position);
        
        if (distToPlayer < influenceRadius)
        {
            if (!playerInField)
            {
                // Jugador acaba de entrar en el campo
                playerInField = true;
                hasAppliedEffect = true;
                ApplyGravityEffect(distToPlayer);
                Log($"[GravityWell] Player entered gravity field (dist: {distToPlayer:F2})");
            }
            else
            {
                // Jugador sigue en el campo: actualizar efecto según distancia
                ApplyGravityEffect(distToPlayer);
            }
        }
        else
        {
            if (playerInField)
            {
                // Jugador acaba de salir del campo
                playerInField = false;
                lingeringTimer = lingeringDuration;
                Log($"[GravityWell] Player left gravity field");
            }
            
            // Efecto residual
            if (lingeringTimer > 0f)
            {
                lingeringTimer -= Time.deltaTime;
                if (lingeringTimer <= 0f && hasAppliedEffect)
                {
                    RemoveGravityEffect();
                    hasAppliedEffect = false;
                }
            }
        }
        
        // Visual feedback: campo brilla más cuando el jugador está cerca
        if (fieldRenderer != null)
        {
            float proximityFactor = Mathf.Clamp01(1f - distToPlayer / (influenceRadius * 1.5f));
            Color baseColor = isAttracting 
                ? new Color(0.3f, 0.1f, 0.5f, 0.08f + proximityFactor * 0.12f)  // Púrpura oscuro
                : new Color(0.1f, 0.4f, 0.5f, 0.08f + proximityFactor * 0.12f); // Cyan oscuro
            fieldRenderer.color = baseColor;
        }
    }

    private void ApplyGravityEffect(float distance)
    {
        if (playerOrbit == null) return;
        
        // Calcular la fuerza basada en la distancia (más fuerte cuanto más cerca)
        float normalizedDist = Mathf.Clamp01(distance / influenceRadius);
        float effectStrength = (1f - normalizedDist) * gravityStrength;
        
        // Aplicar modificación a la velocidad angular
        if (isAttracting)
        {
            // Atrae: reduce la velocidad angular (el jugador va más lento, "frenado")
            playerOrbit.angularSpeed = originalAngularSpeed * (1f - effectStrength * 0.4f);
        }
        else
        {
            // Repele: aumenta la velocidad angular (el jugador va más rápido, "empujado")
            playerOrbit.angularSpeed = originalAngularSpeed * (1f + effectStrength * 0.5f);
        }
    }

    private void RemoveGravityEffect()
    {
        if (playerOrbit == null) return;
        
        // Restaurar velocidad angular original gradualmente
        // (el efecto se disipa con el lingering timer)
        playerOrbit.angularSpeed = originalAngularSpeed;
        Log("[GravityWell] Gravity effect removed, angular speed restored.");
    }

    private void OnDestroy()
    {
        // Asegurar que el efecto se elimina cuando el obstáculo se destruye
        if (hasAppliedEffect)
        {
            RemoveGravityEffect();
        }
    }

    private void CreateVisuals()
    {
        // 1. Campo de influencia (círculo grande semitransparente)
        influenceField = new GameObject("InfluenceField");
        influenceField.transform.SetParent(transform);
        influenceField.transform.localPosition = Vector3.zero;
        influenceField.transform.localScale = Vector3.one * influenceRadius * 2f;
        
        fieldRenderer = influenceField.AddComponent<SpriteRenderer>();
        fieldRenderer.sprite = CreateFieldSprite();
        fieldRenderer.color = isAttracting 
            ? new Color(0.3f, 0.1f, 0.5f, 0.1f)  // Púrpura para atracción
            : new Color(0.1f, 0.4f, 0.5f, 0.1f);  // Cyan para repulsión
        fieldRenderer.sortingOrder = 3;
        fieldRenderer.sortingLayerName = "Default";
        
        // 2. Anillo de acreción (anillo intermedio)
        accretionRing = new GameObject("AccretionRing");
        accretionRing.transform.SetParent(transform);
        accretionRing.transform.localPosition = Vector3.zero;
        accretionRing.transform.localScale = Vector3.one * 1.2f;
        
        ringRenderer = accretionRing.AddComponent<SpriteRenderer>();
        ringRenderer.sprite = CreateAccretionRingSprite();
        ringRenderer.color = isAttracting 
            ? new Color(0.6f, 0.2f, 0.8f, 0.5f)
            : new Color(0.2f, 0.7f, 0.8f, 0.5f);
        ringRenderer.sortingOrder = 4;
        ringRenderer.sortingLayerName = "Default";
        
        // 3. Núcleo del vórtice (centro oscuro con detalles brillantes)
        vortexCore = new GameObject("VortexCore");
        vortexCore.transform.SetParent(transform);
        vortexCore.transform.localPosition = Vector3.zero;
        vortexCore.transform.localScale = Vector3.one * 0.7f;
        
        coreRenderer = vortexCore.AddComponent<SpriteRenderer>();
        coreRenderer.sprite = CreateVortexCoreSprite();
        coreRenderer.color = Color.white;
        coreRenderer.sortingOrder = 5;
        coreRenderer.sortingLayerName = "Default";
        
        // El GravityWell NO tiene collider letal propio.
        // Solo afecta la velocidad angular del jugador cuando está en el campo.
        // PERO necesitamos que el ObstacleMover pueda detectar cuando sale de pantalla.
        // Usamos un trigger muy grande para el campo, que NO llama a GameOver.
    }

    private Sprite CreateFieldSprite()
    {
        // Círculo con gradiente radial suave
        int textureSize = 128;
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
                    // Gradiente: más opaco hacia el centro, transparente en los bordes
                    float alpha = Mathf.Pow(1f - normalizedDist, 1.5f);
                    
                    // Patrón de ondas concéntricas sutiles
                    float wave = Mathf.Abs(Mathf.Sin(normalizedDist * Mathf.PI * 6f)) * 0.3f;
                    alpha = alpha * (0.7f + wave);
                    
                    colors[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
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

    private Sprite CreateAccretionRingSprite()
    {
        // Anillo con segmentos discontinuos (como materia orbitando)
        int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] colors = new Color[textureSize * textureSize];
        
        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float outerRadius = textureSize / 2f - 4f;
        float innerRadius = outerRadius * 0.65f;
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                
                if (dist >= innerRadius && dist <= outerRadius)
                {
                    float normalizedDist = (dist - innerRadius) / (outerRadius - innerRadius);
                    float angle = Mathf.Atan2(pos.y - center.y, pos.x - center.x);
                    
                    // Patrón de segmentos discontinuos
                    float segmentPattern = Mathf.Abs(Mathf.Sin(angle * 5f));
                    
                    // Espiral sutil
                    float spiral = Mathf.Abs(Mathf.Sin(angle * 3f + normalizedDist * 4f));
                    
                    float pattern = segmentPattern * 0.6f + spiral * 0.4f;
                    
                    // Alpha: más opaco en el centro del anillo
                    float alpha = (1f - Mathf.Abs(normalizedDist - 0.5f) * 2f);
                    alpha = Mathf.Pow(alpha, 0.7f) * pattern;
                    
                    // Bordes suaves
                    if (dist < innerRadius + 3f)
                    {
                        alpha *= (dist - innerRadius) / 3f;
                    }
                    else if (dist > outerRadius - 3f)
                    {
                        alpha *= (outerRadius - dist) / 3f;
                    }
                    
                    colors[y * textureSize + x] = new Color(1f, 1f, 1f, alpha * 0.7f);
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

    private Sprite CreateVortexCoreSprite()
    {
        // Centro oscuro con detalles brillantes tipo singularidad
        int textureSize = 96;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] colors = new Color[textureSize * textureSize];
        
        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float maxRadius = textureSize / 2f - 2f;
        
        Color darkCore = new Color(0.05f, 0.02f, 0.1f, 1f);  // Casi negro con tinte púrpura
        Color brightEdge = isAttracting 
            ? new Color(0.8f, 0.3f, 1f, 1f)   // Púrpura brillante
            : new Color(0.3f, 0.9f, 1f, 1f);  // Cyan brillante
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= maxRadius)
                {
                    float normalizedDist = dist / maxRadius;
                    float alpha;
                    Color pixelColor;
                    
                    if (normalizedDist < 0.4f)
                    {
                        // Centro oscuro
                        pixelColor = darkCore;
                        alpha = 0.9f;
                    }
                    else if (normalizedDist < 0.7f)
                    {
                        // Zona de transición
                        float t = (normalizedDist - 0.4f) / 0.3f;
                        pixelColor = Color.Lerp(darkCore, brightEdge, t);
                        alpha = 0.7f;
                    }
                    else
                    {
                        // Borde brillante (horizonte de eventos)
                        float t = (normalizedDist - 0.7f) / 0.3f;
                        pixelColor = Color.Lerp(brightEdge, Color.white, t * 0.3f);
                        alpha = Mathf.Lerp(0.7f, 0f, t);
                    }
                    
                    // Patrón espiral en la zona de transición
                    float angle = Mathf.Atan2(y - center.y, x - center.x);
                    float spiralPattern = Mathf.Sin(angle * 4f + normalizedDist * 10f);
                    if (normalizedDist > 0.3f && normalizedDist < 0.8f && spiralPattern > 0.7f)
                    {
                        pixelColor = Color.Lerp(pixelColor, brightEdge, 0.4f);
                    }
                    
                    colors[y * textureSize + x] = new Color(pixelColor.r, pixelColor.g, pixelColor.b, alpha);
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

    // Sobreescribir el manejo de colisiones de ObstacleBase.
    // El GravityWell NO mata al jugador en colisión — solo afecta la velocidad.
    public override void OnTriggerEnter2D(Collider2D collision) 
    {
        // No llamar a la base — NO queremos matar al jugador.
        // La lógica gravitatoria se maneja en Update().
    }
    
    public override void OnTriggerStay2D(Collider2D collision)
    {
        // No llamar a la base.
    }
    
    public override void OnCollisionEnter2D(Collision2D collision)
    {
        // No llamar a la base.
    }
}
