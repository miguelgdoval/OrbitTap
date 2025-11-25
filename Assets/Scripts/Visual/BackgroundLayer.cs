using UnityEngine;

/// <summary>
/// Componente que gestiona el desplazamiento de una capa individual del fondo.
/// Soporta tanto SpriteRenderer como Image UI, optimizado para móvil.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundLayer : MonoBehaviour
{
    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 1f;
    [SerializeField] private bool isStaticLayer = false;
    [SerializeField] private bool useUVScrolling = true; // Si false, usa transform.position
    
    [Header("Material Settings")]
    [SerializeField] private string texturePropertyName = "_MainTex";
    
    private SpriteRenderer spriteRenderer;
    private Material materialInstance;
    private Vector2 uvOffset = Vector2.zero;
    private Vector3 initialPosition;
    
    // Para modo transform (alternativa a UV)
    private float scrollDistance = 0f;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Crear instancia del material para no afectar el material compartido
        if (spriteRenderer.material != null)
        {
            materialInstance = new Material(spriteRenderer.material);
            spriteRenderer.material = materialInstance;
        }
        
        initialPosition = transform.localPosition;
    }
    
    private void Update()
    {
        if (isStaticLayer) return;
        
        if (useUVScrolling && materialInstance != null)
        {
            // Método UV: más eficiente para móvil, no mueve el transform
            uvOffset.y += scrollSpeed * Time.deltaTime;
            
            // Normalizar para evitar overflow
            if (uvOffset.y > 1f)
            {
                uvOffset.y -= 1f;
            }
            
            materialInstance.SetTextureOffset(texturePropertyName, uvOffset);
        }
        else
        {
            // Método Transform: alternativa si UV no funciona bien
            scrollDistance += scrollSpeed * Time.deltaTime;
            transform.localPosition = initialPosition + Vector3.down * scrollDistance;
            
            // Resetear posición cuando se sale de pantalla (asumiendo altura de sprite)
            if (spriteRenderer.bounds.size.y > 0)
            {
                float spriteHeight = spriteRenderer.bounds.size.y;
                if (scrollDistance > spriteHeight)
                {
                    scrollDistance -= spriteHeight;
                }
            }
        }
    }
    
    /// <summary>
    /// Establece la velocidad de scroll de esta capa
    /// </summary>
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = speed;
    }
    
    /// <summary>
    /// Obtiene la velocidad de scroll actual
    /// </summary>
    public float GetScrollSpeed()
    {
        return scrollSpeed;
    }
    
    /// <summary>
    /// Activa o desactiva el movimiento de esta capa
    /// </summary>
    public void SetStatic(bool isStatic)
    {
        isStaticLayer = isStatic;
    }
    
    /// <summary>
    /// Resetea la posición/UV de la capa
    /// </summary>
    public void ResetLayer()
    {
        uvOffset = Vector2.zero;
        scrollDistance = 0f;
        transform.localPosition = initialPosition;
        
        if (materialInstance != null)
        {
            materialInstance.SetTextureOffset(texturePropertyName, Vector2.zero);
        }
    }
    
    private void OnDestroy()
    {
        // Limpiar material instanciado
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}

