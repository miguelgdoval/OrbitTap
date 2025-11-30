Shader "Custom/PlanetInnerEnergyShader"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Energy Settings)]
        _EnergyLevel ("Energy Level", Range(0, 1)) = 0
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 1
        _DistortionAmount ("Distortion Amount", Range(0, 0.1)) = 0.02
        _EnergyColor ("Energy Color", Color) = (0.2, 0.8, 1.0, 1.0)
        _NoiseScale ("Noise Scale", Range(1, 20)) = 5
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
            float _EnergyLevel;
            float _PulseSpeed;
            float _GlowIntensity;
            float _DistortionAmount;
            fixed4 _EnergyColor;
            float _NoiseScale;

            // Simple hash function for procedural generation
            float hash(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            // 2D Noise (Perlin-like)
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // Smoothstep
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Fractal noise (octaves for more detail)
            float fractalNoise(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Early exit if no energy
                if (_EnergyLevel <= 0.001)
                {
                    return tex2D(_MainTex, IN.texcoord) * IN.color;
                }
                
                // Calculate center-relative position
                float2 center = float2(0.5, 0.5);
                float2 toCenter = IN.texcoord - center;
                float distFromCenter = length(toCenter);
                
                // Normalized distance from center (0 = center, 1 = edge)
                float normalizedDist = distFromCenter * 2.0;
                
                // Pulse animation using sine wave
                float pulse = sin(_Time.y * _PulseSpeed * 2.0 * 3.14159) * 0.5 + 0.5; // 0 to 1
                float energyWithPulse = _EnergyLevel * (0.7 + pulse * 0.3); // Pulse between 70% and 100% of energy
                
                // Generate noise for energy texture
                float2 noiseUV = IN.texcoord * _NoiseScale;
                float energyNoise = fractalNoise(noiseUV + _Time.y * 0.5, 3);
                
                // Make energy brighter towards center
                float centerBrightness = 1.0 - smoothstep(0.0, 0.7, normalizedDist);
                energyNoise *= centerBrightness;
                
                // Apply energy level to noise
                float energyMask = energyNoise * energyWithPulse;
                
                // Thermal distortion - distort UV based on energy
                float2 distortionNoise = float2(
                    noise(noiseUV * 2.0 + _Time.y * 0.3),
                    noise(noiseUV * 2.0 + _Time.y * 0.3 + 100.0)
                );
                distortionNoise = (distortionNoise - 0.5) * 2.0; // -1 to 1
                
                float distortionStrength = _DistortionAmount * energyWithPulse;
                float2 distortedUV = IN.texcoord + distortionNoise * distortionStrength;
                
                // Sample original sprite with distortion
                fixed4 c = tex2D(_MainTex, distortedUV) * IN.color;
                
                // Calculate energy glow
                float glowMask = energyMask * _GlowIntensity;
                
                // Energy color contribution
                fixed3 energyGlow = _EnergyColor.rgb * glowMask;
                
                // Add energy glow to the sprite (multiplicative blend for internal feel)
                // Use a combination of additive and multiplicative for better internal glow
                float glowBlend = glowMask * 0.5; // Blend factor
                c.rgb = lerp(c.rgb, c.rgb * (1.0 + energyGlow), glowBlend);
                
                // Add pure energy color in brightest areas
                float brightEnergy = smoothstep(0.3, 0.7, energyMask);
                c.rgb = lerp(c.rgb, c.rgb + _EnergyColor.rgb * brightEnergy * _GlowIntensity * 0.3, energyWithPulse);
                
                // Enhance brightness in center area
                float centerGlow = centerBrightness * energyWithPulse * _GlowIntensity * 0.2;
                c.rgb += _EnergyColor.rgb * centerGlow;
                
                // Maintain original alpha but enhance it slightly with energy
                c.a = lerp(c.a, min(1.0, c.a * (1.0 + glowMask * 0.2)), energyWithPulse * 0.5);
                
                return c;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}

