using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Sistema de partículas sutiles para el fondo del menú de opciones
/// Crea un efecto visual espacial minimalista con partículas flotantes
/// </summary>
public class SettingsParticleBackground : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private int particleCount = 15;
    [SerializeField] private float particleSize = 0.5f;
    [SerializeField] private float floatSpeed = 0.3f;
    [SerializeField] private float floatRange = 50f;
    [SerializeField] private float fadeSpeed = 1.5f;
    
    [Header("Colors")]
    [SerializeField] private Color primaryColor = Color.white;
    [SerializeField] private Color secondaryColor = Color.white;
    
    private List<GameObject> particles;
    private List<Vector3> startPositions;
    private List<Vector3> floatDirections;
    private List<float> floatOffsets;
    private List<float> particleAlphas;
    
    private RectTransform rectTransform;
    private bool isActive = false;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }
    }
    
    private void Start()
    {
        if (!Application.isPlaying) return;
        
        // Usar colores neon del tema
        primaryColor = CosmicTheme.NeonCyan;
        secondaryColor = CosmicTheme.NeonMagenta;
        
        CreateParticles();
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        
        if (particles != null)
        {
            foreach (var particle in particles)
            {
                if (particle != null)
                {
                    particle.SetActive(active);
                }
            }
        }
    }
    
    private void CreateParticles()
    {
        particles = new List<GameObject>();
        startPositions = new List<Vector3>();
        floatDirections = new List<Vector3>();
        floatOffsets = new List<float>();
        particleAlphas = new List<float>();
        
        if (rectTransform == null) return;
        
        // Obtener el tamaño del panel (usar rect.width y rect.height si sizeDelta es 0)
        Vector2 panelSize = rectTransform.sizeDelta;
        if (panelSize.x == 0 || panelSize.y == 0)
        {
            // Si sizeDelta es 0, el rect está usando anchors, usar rect.width/height
            panelSize = new Vector2(rectTransform.rect.width, rectTransform.rect.height);
            if (panelSize.x == 0 || panelSize.y == 0)
            {
                panelSize = new Vector2(900, 700); // Tamaño por defecto
            }
        }
        
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject($"MenuParticle_{i}");
            particle.transform.SetParent(transform, false);
            
            // RectTransform para UI
            RectTransform particleRect = particle.AddComponent<RectTransform>();
            
            // Posición aleatoria dentro del panel
            float x = Random.Range(-panelSize.x * 0.4f, panelSize.x * 0.4f);
            float y = Random.Range(-panelSize.y * 0.4f, panelSize.y * 0.4f);
            Vector3 startPos = new Vector3(x, y, 0);
            
            particleRect.anchoredPosition = startPos;
            particleRect.sizeDelta = new Vector2(particleSize * 32f, particleSize * 32f);
            particleRect.anchorMin = new Vector2(0.5f, 0.5f);
            particleRect.anchorMax = new Vector2(0.5f, 0.5f);
            particleRect.pivot = new Vector2(0.5f, 0.5f);
            
            // UI Image (para funcionar dentro de Canvas)
            Image particleImage = particle.AddComponent<Image>();
            particleImage.sprite = CreateParticleSprite();
            
            // Color aleatorio entre primary y secondary
            Color particleColor = Color.Lerp(primaryColor, secondaryColor, Random.Range(0f, 1f));
            particleColor.a = 0f; // Empezar invisible
            particleImage.color = particleColor;
            
            // Dirección de flotación aleatoria
            Vector3 direction = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0f
            ).normalized;
            
            // Offset temporal para variación
            float offset = Random.Range(0f, Mathf.PI * 2f);
            
            particles.Add(particle);
            startPositions.Add(startPos);
            floatDirections.Add(direction);
            floatOffsets.Add(offset);
            particleAlphas.Add(0f);
        }
    }
    
    private void Update()
    {
        if (!Application.isPlaying || !isActive) return;
        if (particles == null || particles.Count == 0) return;
        
        if (rectTransform == null) return;
        
        Vector2 panelSize = rectTransform.sizeDelta;
        if (panelSize.x == 0 || panelSize.y == 0)
        {
            panelSize = new Vector2(rectTransform.rect.width, rectTransform.rect.height);
            if (panelSize.x == 0 || panelSize.y == 0)
            {
                panelSize = new Vector2(900, 700);
            }
        }
        
        for (int i = 0; i < particles.Count; i++)
        {
            if (particles[i] == null) continue;
            
            // Movimiento flotante suave
            float time = Time.time * floatSpeed + floatOffsets[i];
            Vector3 floatMovement = floatDirections[i] * Mathf.Sin(time) * floatRange * 0.5f;
            
            // Movimiento circular adicional
            float angle = time * 0.5f;
            Vector3 circularMovement = new Vector3(
                Mathf.Cos(angle) * floatRange * 0.3f,
                Mathf.Sin(angle) * floatRange * 0.3f,
                0f
            );
            
            Vector3 newPosition = startPositions[i] + floatMovement + circularMovement;
            
            // Mantener dentro del panel (con margen)
            newPosition.x = Mathf.Clamp(newPosition.x, -panelSize.x * 0.45f, panelSize.x * 0.45f);
            newPosition.y = Mathf.Clamp(newPosition.y, -panelSize.y * 0.45f, panelSize.y * 0.45f);
            
            RectTransform particleRect = particles[i].GetComponent<RectTransform>();
            if (particleRect != null)
            {
                particleRect.anchoredPosition = newPosition;
            }
            
            // Fade in/out sutil
            float targetAlpha = 0.15f + Mathf.Sin(time * 0.7f) * 0.1f; // Entre 0.05 y 0.25
            particleAlphas[i] = Mathf.Lerp(particleAlphas[i], targetAlpha, Time.deltaTime * fadeSpeed);
            
            Image particleImage = particles[i].GetComponent<Image>();
            if (particleImage != null)
            {
                Color c = particleImage.color;
                c.a = particleAlphas[i];
                particleImage.color = c;
            }
        }
    }
    
    private Sprite CreateParticleSprite()
    {
        int size = Mathf.RoundToInt(particleSize * 32f);
        size = Mathf.Clamp(size, 8, 32);
        
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    // Gradiente radial suave con borde más suave
                    float normalizedDist = dist / radius;
                    float alpha = 1f - normalizedDist;
                    alpha = Mathf.Pow(alpha, 2f); // Hacer el fade más suave
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
    
    private void OnDestroy()
    {
        if (particles != null)
        {
            foreach (var particle in particles)
            {
                if (particle != null)
                {
                    Destroy(particle);
                }
            }
        }
    }
}

