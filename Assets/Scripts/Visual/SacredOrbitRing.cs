using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dibuja el anillo sagrado de la órbita con símbolos sutiles
/// </summary>
public class SacredOrbitRing : MonoBehaviour
{
    [Header("Ring Settings")]
    public float radius = 2f;
    public float lineWidth = 0.08f; // Más grueso
    public int segments = 64;
    public Color ringColor = Color.white;
    public float symbolOpacity = 0.08f; // Más sutil
    public float ringOpacity = 0.15f; // Opacidad muy baja para efecto difuminado

    private LineRenderer lineRenderer;
    private List<GameObject> symbols;
    private Transform center;

    private void Start()
    {
        if (!Application.isPlaying) return;

        // Buscar el centro
        GameObject centerObj = GameObject.Find("Center");
        if (centerObj != null)
        {
            center = centerObj.transform;
        }
        else
        {
            center = transform;
        }

        // Obtener el radio del jugador si existe
        PlayerOrbit player = FindObjectOfType<PlayerOrbit>();
        if (player != null)
        {
            radius = player.radius;
        }

        CreateRing();
        CreateSymbols();
    }

    private void CreateRing()
    {
        // Crear LineRenderer para el anillo
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        
        // Color más sutil y difuminado
        Color subtleGold = new Color(
            CosmicTheme.SoftGold.r,
            CosmicTheme.SoftGold.g,
            CosmicTheme.SoftGold.b,
            ringOpacity
        );
        lineRenderer.startColor = subtleGold;
        lineRenderer.endColor = subtleGold;
        
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.sortingOrder = 1;
        
        // Hacer el material más suave/difuminado
        if (lineRenderer.material != null)
        {
            lineRenderer.material.SetFloat("_Mode", 2); // Fade mode para transparencia
            lineRenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineRenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineRenderer.material.SetInt("_ZWrite", 0);
            lineRenderer.material.DisableKeyword("_ALPHATEST_ON");
            lineRenderer.material.EnableKeyword("_ALPHABLEND_ON");
            lineRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            lineRenderer.material.renderQueue = 3000;
        }

        // Crear puntos del círculo
        Vector3[] points = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            points[i] = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
        }

        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
    }

    private void CreateSymbols()
    {
        symbols = new List<GameObject>();
        int symbolCount = 8; // Número de símbolos alrededor del anillo

        for (int i = 0; i < symbolCount; i++)
        {
            float angle = (float)i / symbolCount * Mathf.PI * 2f;
            Vector3 symbolPos = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );

            GameObject symbol = new GameObject($"Symbol_{i}");
            symbol.transform.SetParent(transform);
            symbol.transform.localPosition = symbolPos;
            symbol.transform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);

            SpriteRenderer sr = symbol.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRuneSprite();
            // Color aún más sutil para los símbolos
            sr.color = new Color(CosmicTheme.SoftGold.r, CosmicTheme.SoftGold.g, CosmicTheme.SoftGold.b, symbolOpacity);
            sr.sortingOrder = 0;
            
            // Hacer el material más suave/difuminado
            if (sr.material != null)
            {
                sr.material.SetFloat("_Mode", 2); // Fade mode
                sr.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                sr.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                sr.material.SetInt("_ZWrite", 0);
                sr.material.DisableKeyword("_ALPHATEST_ON");
                sr.material.EnableKeyword("_ALPHABLEND_ON");
                sr.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                sr.material.renderQueue = 3000;
            }

            symbols.Add(symbol);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        // Rotar símbolos lentamente
        if (symbols != null)
        {
            foreach (GameObject symbol in symbols)
            {
                if (symbol != null)
                {
                    symbol.transform.Rotate(0, 0, 5f * Time.deltaTime);
                }
            }
        }
    }

    private Sprite CreateRuneSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);

        // Crear un símbolo simple tipo runa (cruz con líneas)
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);

                // Crear patrón de runa simple
                bool isRune = false;
                
                // Línea vertical central
                if (Mathf.Abs(x - center.x) < 1.5f && dist < size / 2f - 2f)
                {
                    isRune = true;
                }
                // Línea horizontal
                if (Mathf.Abs(y - center.y) < 1.5f && Mathf.Abs(x - center.x) < size / 3f)
                {
                    isRune = true;
                }
                // Líneas diagonales sutiles
                if (Mathf.Abs((x - center.x) + (y - center.y)) < 2f && dist < size / 3f)
                {
                    isRune = true;
                }

                colors[y * size + x] = isRune ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}

