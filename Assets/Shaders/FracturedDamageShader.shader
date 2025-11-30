Shader "Custom/FracturedDamageShader"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Damage Settings)]
        _DamageAmount ("Damage Amount", Range(0, 1)) = 0
        _DamageGlow ("Damage Glow Intensity", Range(0, 2)) = 1
        _ShakeIntensity ("Shake Intensity", Range(0, 0.1)) = 0.02
        _CellDensity ("Cell Density", Range(5, 50)) = 20
        _EdgeThickness ("Edge Thickness", Range(0.001, 0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _DamageAmount;
            float _DamageGlow;
            float _ShakeIntensity;
            float _CellDensity;
            float _EdgeThickness;

            // Simple noise function for procedural generation
            float hash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            // 2D Noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Voronoi pattern generator
            float voronoi(float2 uv, float density)
            {
                float2 cellId = floor(uv * density);
                float2 cellUV = frac(uv * density);
                
                float minDist = 8.0;
                
                // Check neighboring cells
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(float(x), float(y));
                        float2 cellPoint = neighbor + hash(cellId + neighbor);
                        float2 diff = neighbor + cellPoint - cellUV;
                        float dist = length(diff);
                        minDist = min(minDist, dist);
                    }
                }
                
                return minDist;
            }

            // Get distance to nearest edge in Voronoi pattern
            float voronoiEdge(float2 uv, float density)
            {
                float2 cellId = floor(uv * density);
                float2 cellUV = frac(uv * density);
                
                float minDist = 8.0;
                float secondMinDist = 8.0;
                
                // Find two closest points
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(float(x), float(y));
                        float2 cellPoint = neighbor + hash(cellId + neighbor);
                        float2 diff = neighbor + cellPoint - cellUV;
                        float dist = length(diff);
                        
                        if (dist < minDist)
                        {
                            secondMinDist = minDist;
                            minDist = dist;
                        }
                        else if (dist < secondMinDist)
                        {
                            secondMinDist = dist;
                        }
                    }
                }
                
                // Edge is where distances are similar
                return abs(minDist - secondMinDist);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                
                // Apply shake based on damage amount
                float shakeAmount = _DamageAmount * _ShakeIntensity;
                float2 shakeOffset = float2(
                    noise(IN.texcoord * 10.0 + _Time.y * 5.0) - 0.5,
                    noise(IN.texcoord * 10.0 + _Time.y * 5.0 + 100.0) - 0.5
                ) * shakeAmount;
                
                // Apply shake to vertex position
                float4 worldPos = mul(unity_ObjectToWorld, IN.vertex);
                worldPos.xy += shakeOffset;
                OUT.vertex = mul(UNITY_MATRIX_VP, worldPos);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            // Get the closest Voronoi cell center for a given UV
            float2 getClosestCellCenter(float2 uv, float density, out float2 cellId)
            {
                cellId = floor(uv * density);
                float2 cellUV = frac(uv * density);
                
                float minDist = 8.0;
                float2 closestCenter = float2(0.5, 0.5);
                float2 closestCellId = cellId;
                
                // Check neighboring cells to find the closest center
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(float(x), float(y));
                        float2 testCellId = cellId + neighbor;
                        float2 cellPoint = neighbor + hash(testCellId);
                        float2 diff = neighbor + cellPoint - cellUV;
                        float dist = length(diff);
                        
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestCenter = cellPoint;
                            closestCellId = testCellId;
                        }
                    }
                }
                
                cellId = closestCellId;
                return closestCenter;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Early exit if no damage
                if (_DamageAmount <= 0.001)
                {
                    return tex2D(_MainTex, IN.texcoord) * IN.color;
                }
                
                // Calculate center-relative position
                float2 center = float2(0.5, 0.5);
                float2 toCenter = IN.texcoord - center;
                float distFromCenter = length(toCenter);
                
                // Get the Voronoi cell this pixel belongs to
                float2 cellId;
                float2 cellCenter = getClosestCellCenter(IN.texcoord, _CellDensity, cellId);
                
                // Calculate the offset for this fragment based on its cell ID
                // Each fragment gets a unique offset that makes it look separated
                float2 cellHash = hash(cellId);
                
                // Calculate direction from center of sprite to this fragment's cell center
                float2 cellCenterInUV = (cellId + 0.5) / _CellDensity;
                float2 directionFromCenter = normalize(cellCenterInUV - center);
                
                // Each fragment moves outward from center based on damage
                float2 fragmentOffset = directionFromCenter * _DamageAmount * 0.08;
                
                // Add random offset per fragment for more organic separation
                float2 randomOffset = (cellHash - 0.5) * _DamageAmount * 0.04;
                fragmentOffset += randomOffset;
                
                // Add rotation offset for each fragment (slight rotation per cell)
                float fragmentRotation = (cellHash.x - 0.5) * _DamageAmount * 0.15; // Rotation per fragment
                float2 rotatedUV = IN.texcoord - center;
                float cosRot = cos(fragmentRotation);
                float sinRot = sin(fragmentRotation);
                rotatedUV = float2(
                    rotatedUV.x * cosRot - rotatedUV.y * sinRot,
                    rotatedUV.x * sinRot + rotatedUV.y * cosRot
                ) + center;
                
                // Apply fragment offset to UV - each fragment shows its portion but displaced
                // This makes each fragment look like a separate piece
                float2 fragmentUV = rotatedUV - fragmentOffset;
                
                // Add shake/vibration within each fragment
                float2 shakeOffset = float2(
                    noise(IN.texcoord * 15.0 + _Time.y * 8.0) - 0.5,
                    noise(IN.texcoord * 15.0 + _Time.y * 8.0 + 200.0) - 0.5
                ) * _DamageAmount * _ShakeIntensity * 0.3;
                
                fragmentUV += shakeOffset;
                
                // Sample the sprite at the fragment's UV position
                // This makes each fragment show its portion of the original sprite
                fixed4 c = tex2D(_MainTex, fragmentUV) * IN.color;
                
                // Generate Voronoi edge pattern for cracks
                float edgeDist = voronoiEdge(IN.texcoord, _CellDensity);
                
                // Create crack pattern - edges of Voronoi cells
                float crackMask = smoothstep(_EdgeThickness * 0.5, _EdgeThickness, edgeDist);
                crackMask = 1.0 - crackMask; // Invert so edges are 1
                
                // Apply damage amount to crack visibility
                crackMask *= _DamageAmount;
                
                // Glowing crack edges - red/orange/white
                float glowIntensity = crackMask * _DamageGlow;
                
                // Color gradient: white -> orange -> red based on damage
                fixed3 glowColor;
                if (_DamageAmount < 0.5)
                {
                    // White to orange
                    glowColor = lerp(fixed3(1.0, 1.0, 1.0), fixed3(1.0, 0.6, 0.2), _DamageAmount * 2.0);
                }
                else
                {
                    // Orange to red
                    glowColor = lerp(fixed3(1.0, 0.6, 0.2), fixed3(1.0, 0.2, 0.0), (_DamageAmount - 0.5) * 2.0);
                }
                
                // Add glow to cracks - make cracks visible as gaps between fragments
                c.rgb = lerp(c.rgb, glowColor, glowIntensity);
                
                // Add rim glow effect on edges
                float rimFactor = smoothstep(0.4, 0.5, distFromCenter) * _DamageAmount;
                c.rgb = lerp(c.rgb, c.rgb + glowColor * rimFactor * _DamageGlow * 0.3, _DamageAmount);
                
                // Slight desaturation at high damage
                float desaturation = _DamageAmount * 0.3;
                fixed gray = dot(c.rgb, fixed3(0.299, 0.587, 0.114));
                c.rgb = lerp(c.rgb, fixed3(gray, gray, gray), desaturation);
                
                // Create gap effect between fragments - make edges darker/transparent
                float edgeDarkness = crackMask * _DamageAmount * 0.7;
                c.rgb *= (1.0 - edgeDarkness);
                
                // Make cracks more visible as actual gaps
                float gapAlpha = crackMask * _DamageAmount;
                c.a = lerp(c.a, c.a * (1.0 - gapAlpha * 0.8), _DamageAmount);
                
                return c;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}
