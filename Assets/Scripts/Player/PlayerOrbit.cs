using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    public float radius = 2f;
    public float angle = 0f;
    public float angularSpeed = 2f;
    public Transform center;

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;
    public FlashEffect flashEffect;

    private void Start()
    {
        if (!Application.isPlaying) return;
        
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

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Create sprite if none exists - Asteroide Errante
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = LoadPlayerSprite();
            if (spriteRenderer.sprite == null)
            {
                // Fallback a estrella si no se encuentra el sprite
                spriteRenderer.sprite = SpriteGenerator.CreateStarSprite(0.3f, CosmicTheme.EtherealLila);
                spriteRenderer.color = CosmicTheme.EtherealLila;
            }
            else
            {
                spriteRenderer.color = Color.white; // Color blanco para mantener los colores originales
            }
        }
        
        // Agregar cola de partículas
        if (GetComponent<StarParticleTrail>() == null)
        {
            gameObject.AddComponent<StarParticleTrail>();
        }

        if (flashEffect == null)
        {
            flashEffect = GetComponent<FlashEffect>();
        }
    }

    private void Update()
    {
        if (center == null) return;

        // Update angle based on angular speed
        angle += angularSpeed * Time.deltaTime;

        // Calculate position using circular motion
        float x = center.position.x + Mathf.Cos(angle) * radius;
        float y = center.position.y + Mathf.Sin(angle) * radius;

        transform.position = new Vector3(x, y, 0f);
    }

    public void ToggleDirection()
    {
        angularSpeed = -angularSpeed;
        
        // Trigger flash effect
        if (flashEffect != null)
        {
            flashEffect.TriggerFlash();
        }
    }
    
    #if UNITY_EDITOR
    private Sprite LoadPlayerSprite()
    {
        if (!Application.isPlaying) return null;
        
        try
        {
            // Buscar el sprite del asteroide errante
            string[] guids = AssetDatabase.FindAssets("AsteroideErrante t:Sprite");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
                
                // Si no se encuentra como Sprite, intentar como Texture2D
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null)
                {
                    // Crear sprite desde texture
                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo cargar el sprite del asteroide: {e.Message}");
        }
        return null;
    }
    #else
    private Sprite LoadPlayerSprite()
    {
        // En build, intentar cargar desde Resources
        return Resources.Load<Sprite>("Art/Protagonist/AsteroideErrante");
    }
    #endif
}

