using UnityEngine;

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

        // Create sprite if none exists - Estrella naciente
        if (spriteRenderer != null && spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = SpriteGenerator.CreateStarSprite(0.3f, CosmicTheme.EtherealLila);
            spriteRenderer.color = CosmicTheme.EtherealLila;
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
}

