using UnityEngine;

public class FlashEffect : MonoBehaviour
{
    [Header("Flash Settings")]
    public float flashDuration = 0.1f;
    public Color flashColor = new Color(1f, 1f, 1f, 1f);

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void TriggerFlash()
    {
        if (spriteRenderer == null || isFlashing) return;

        StartCoroutine(FlashCoroutine());
    }

    private System.Collections.IEnumerator FlashCoroutine()
    {
        isFlashing = true;
        spriteRenderer.color = flashColor;

        yield return new WaitForSeconds(flashDuration);

        spriteRenderer.color = originalColor;
        isFlashing = false;
    }
}

