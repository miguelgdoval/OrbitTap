using UnityEngine;

/// <summary>
/// Helper script para configurar correctamente los fondos al inicio
/// Asegura que los fondos tengan el tamaño y posición correctos
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundSetupHelper : MonoBehaviour
{
    [Header("Setup Settings")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private bool scaleToFitCamera = true;
    [SerializeField] private float zPosition = 10f; // Posición Z (más lejos de la cámara)
    
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    
    private void Start()
    {
        if (setupOnStart)
        {
            SetupBackground();
        }
    }
    
    /// <summary>
    /// Configura el fondo para que cubra toda la pantalla
    /// </summary>
    public void SetupBackground()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning($"BackgroundSetupHelper: {gameObject.name} no tiene SpriteRenderer o sprite asignado");
            return;
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        if (mainCamera == null)
        {
            Debug.LogWarning("BackgroundSetupHelper: No se encontró la cámara");
            return;
        }
        
        // Establecer posición Z
        Vector3 pos = transform.position;
        pos.z = zPosition;
        transform.position = pos;
        
        // Escalar para cubrir toda la pantalla si está habilitado
        if (scaleToFitCamera && mainCamera.orthographic)
        {
            float cameraHeight = mainCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            
            Sprite sprite = spriteRenderer.sprite;
            float spriteWidth = sprite.bounds.size.x;
            float spriteHeight = sprite.bounds.size.y;
            
            // Calcular escala necesaria
            float scaleX = cameraWidth / spriteWidth;
            float scaleY = cameraHeight / spriteHeight;
            
            // Usar el mayor para asegurar que cubra toda la pantalla
            float scale = Mathf.Max(scaleX, scaleY) * 1.1f; // 1.1f para margen extra
            
            transform.localScale = new Vector3(scale, scale, 1f);
        }
        
        Debug.Log($"BackgroundSetupHelper: {gameObject.name} configurado correctamente (Scale: {transform.localScale}, Z: {zPosition})");
    }
    
    /// <summary>
    /// Fuerza la actualización del setup
    /// </summary>
    [ContextMenu("Setup Background Now")]
    public void SetupNow()
    {
        SetupBackground();
    }
}

