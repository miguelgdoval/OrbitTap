using UnityEngine;

/// <summary>
/// Genera sprites para fragmentos de constelaciones rotos
/// </summary>
public static class ConstellationFragmentGenerator
{
    /// <summary>
    /// Crea un sprite de fragmento de constelación con forma geométrica inspirada en estrellas rotas
    /// </summary>
    public static Sprite CreateFragmentSprite(float size, Color baseColor, bool includeSymbol = false)
    {
        int textureSize = 128;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] colors = new Color[textureSize * textureSize];

        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float maxRadius = textureSize / 2f - 4f;

        // Crear forma geométrica tipo fragmento (triángulo, hexágono, o forma irregular)
        // Usar un valor aleatorio para que cada fragmento tenga una forma diferente
        int shapeType = Random.Range(0, 3);
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);
                bool isFragment = false;
                float alpha = 0f;

                switch (shapeType)
                {
                    case 0: // Triángulo
                        isFragment = IsInTriangle(pos, center, maxRadius);
                        break;
                    case 1: // Hexágono
                        isFragment = IsInHexagon(pos, center, maxRadius);
                        break;
                    case 2: // Forma irregular (estrella rota)
                        isFragment = IsInBrokenStar(pos, center, maxRadius);
                        break;
                }

                if (isFragment)
                {
                    // Gradiente radial para suavidad
                    alpha = 1f - (dist / maxRadius);
                    alpha = Mathf.Clamp01(alpha);
                    
                    // Trazo dorado en los bordes
                    float edgeDist = maxRadius - dist;
                    if (edgeDist < 3f)
                    {
                        // Mezclar con dorado en los bordes
                        Color edgeColor = Color.Lerp(baseColor, CosmicTheme.SoftGold, 0.7f);
                        colors[y * textureSize + x] = new Color(edgeColor.r, edgeColor.g, edgeColor.b, alpha * 0.8f);
                    }
                    else
                    {
                        // Relleno tenue azul-lila
                        colors[y * textureSize + x] = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * 0.4f);
                    }

                    // Símbolo grabado en el centro (muy sutil)
                    if (includeSymbol && dist < maxRadius * 0.3f)
                    {
                        float symbolAlpha = 0.2f;
                        if (IsSymbolPattern(pos, center, maxRadius * 0.3f))
                        {
                            Color symbolColor = new Color(CosmicTheme.SoftGold.r, CosmicTheme.SoftGold.g, CosmicTheme.SoftGold.b, symbolAlpha);
                            colors[y * textureSize + x] = Color.Lerp(colors[y * textureSize + x], symbolColor, 0.5f);
                        }
                    }
                }
                else
                {
                    colors[y * textureSize + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), 200f);
    }

    private static bool IsInTriangle(Vector2 pos, Vector2 center, float radius)
    {
        // Triángulo equilátero
        float angle = Mathf.Atan2(pos.y - center.y, pos.x - center.x);
        float dist = Vector2.Distance(pos, center);
        
        // Crear triángulo rotado
        Vector2[] vertices = new Vector2[3];
        for (int i = 0; i < 3; i++)
        {
            float vAngle = (i * 120f - 90f) * Mathf.Deg2Rad;
            vertices[i] = center + new Vector2(Mathf.Cos(vAngle), Mathf.Sin(vAngle)) * radius;
        }
        
        return PointInTriangle(pos, vertices[0], vertices[1], vertices[2]);
    }

    private static bool IsInHexagon(Vector2 pos, Vector2 center, float radius)
    {
        float angle = Mathf.Atan2(pos.y - center.y, pos.x - center.x);
        float dist = Vector2.Distance(pos, center);
        
        // Hexágono
        float hexRadius = radius * 0.9f;
        float angleStep = 60f * Mathf.Deg2Rad;
        float closestDist = float.MaxValue;
        
        for (int i = 0; i < 6; i++)
        {
            float vAngle = i * angleStep;
            Vector2 vertex = center + new Vector2(Mathf.Cos(vAngle), Mathf.Sin(vAngle)) * hexRadius;
            float d = Vector2.Distance(pos, vertex);
            if (d < closestDist) closestDist = d;
        }
        
        return dist < hexRadius * 0.8f;
    }

    private static bool IsInBrokenStar(Vector2 pos, Vector2 center, float radius)
    {
        // Forma de estrella rota (irregular)
        float angle = Mathf.Atan2(pos.y - center.y, pos.x - center.x);
        float dist = Vector2.Distance(pos, center);
        
        // Crear forma de estrella de 5 puntas pero "rota"
        float starRadius = radius * 0.85f;
        float innerRadius = starRadius * 0.5f;
        
        // Calcular si está dentro de la estrella
        float normalizedAngle = (angle + Mathf.PI) / (2f * Mathf.PI);
        float pointAngle = normalizedAngle * 360f;
        
        // Estrella de 5 puntas
        float starAngle = pointAngle % 72f;
        float currentRadius = starAngle < 36f ? 
            Mathf.Lerp(innerRadius, starRadius, starAngle / 36f) :
            Mathf.Lerp(starRadius, innerRadius, (starAngle - 36f) / 36f);
        
        return dist < currentRadius * 1.1f; // Margen para suavidad
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private static bool IsSymbolPattern(Vector2 pos, Vector2 center, float radius)
    {
        // Patrón de símbolo simple (cruz o runa)
        float dist = Vector2.Distance(pos, center);
        if (dist > radius) return false;

        // Línea vertical
        if (Mathf.Abs(pos.x - center.x) < 1.5f && dist < radius * 0.8f)
            return true;
        
        // Línea horizontal
        if (Mathf.Abs(pos.y - center.y) < 1.5f && dist < radius * 0.6f)
            return true;

        return false;
    }
}

