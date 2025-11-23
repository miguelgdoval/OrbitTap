using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Crea una cola de partículas sutil para la estrella naciente
/// </summary>
public class StarParticleTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    public int trailLength = 5;
    public float trailSpacing = 0.1f;
    public float fadeSpeed = 2f;

    private List<GameObject> trailParticles;
    private List<float> trailAlphas;
    private Vector3 lastPosition;

    private void Start()
    {
        if (!Application.isPlaying) return;

        trailParticles = new List<GameObject>();
        trailAlphas = new List<float>();

        // Crear partículas de la cola
        for (int i = 0; i < trailLength; i++)
        {
            GameObject particle = new GameObject($"TrailParticle_{i}");
            particle.transform.SetParent(transform);
            particle.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
            sr.sprite = CreateParticleSprite();
            sr.color = new Color(CosmicTheme.EtherealLila.r, CosmicTheme.EtherealLila.g, CosmicTheme.EtherealLila.b, 0f);
            sr.sortingOrder = 4 - i; // Partículas más atrás tienen menor orden

            trailParticles.Add(particle);
            trailAlphas.Add(0f);
        }

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        if (trailParticles == null || trailParticles.Count == 0) return;

        // Actualizar posición de las partículas
        for (int i = trailParticles.Count - 1; i > 0; i--)
        {
            if (trailParticles[i] != null && trailParticles[i - 1] != null)
            {
                Vector3 targetPos = trailParticles[i - 1].transform.position;
                trailParticles[i].transform.position = Vector3.Lerp(
                    trailParticles[i].transform.position,
                    targetPos,
                    Time.deltaTime * 10f
                );
            }
        }

        // La primera partícula sigue al jugador con un pequeño retraso
        if (trailParticles.Count > 0 && trailParticles[0] != null)
        {
            Vector3 offset = (lastPosition - transform.position).normalized * trailSpacing;
            trailParticles[0].transform.position = transform.position + offset;
        }

        // Actualizar alphas (fade out)
        for (int i = 0; i < trailParticles.Count; i++)
        {
            if (trailParticles[i] != null)
            {
                float targetAlpha = 1f - (float)i / trailLength;
                trailAlphas[i] = Mathf.Lerp(trailAlphas[i], targetAlpha, Time.deltaTime * fadeSpeed);

                SpriteRenderer sr = trailParticles[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = trailAlphas[i] * 0.3f; // Opacidad baja para sutileza
                    sr.color = c;
                }
            }
        }

        lastPosition = transform.position;
    }

    private Sprite CreateParticleSprite()
    {
        int size = 16;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    // Gradiente radial para suavidad
                    float alpha = 1f - (dist / radius);
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
}

