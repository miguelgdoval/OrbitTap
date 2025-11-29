Shader "Custom/FakePlanetLightURP"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LightDir ("Light Direction", Vector) = (0.7, 0.7, 0, 0)
        _LightIntensity ("Light Intensity", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Sprite Unlit"
            Tags { "LightMode"="Universal2D" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                half4 color         : COLOR;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float2 _LightDir;
                float _LightIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample the sprite texture
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                
                // Calcular posición relativa al centro del sprite (en espacio UV)
                float2 uv = IN.uv;
                float2 center = float2(0.5, 0.5);
                float2 toCenter = uv - center;
                
                // Normalizar la dirección de la luz
                float2 lightDir = normalize(_LightDir);
                
                // Calcular dot product para simular iluminación difusa
                float2 normal = normalize(toCenter);
                float dotProduct = dot(normal, lightDir);
                
                // Convertir de [-1, 1] a [0, 1] y aplicar intensidad
                float lightFactor = (dotProduct * 0.5 + 0.5) * _LightIntensity;
                
                // Rim light effect (highlight en los bordes)
                float distFromCenter = length(toCenter);
                float rimFactor = smoothstep(0.3, 0.5, distFromCenter) * (1.0 - smoothstep(0.5, 0.7, distFromCenter));
                rimFactor *= _LightIntensity * 0.5;
                
                // Combinar iluminación difusa + rim light
                float totalLight = lightFactor + rimFactor;
                
                // Aplicar la iluminación al color final
                c.rgb = lerp(c.rgb, c.rgb * 1.5, totalLight);
                
                return c;
            }
            ENDHLSL
        }
    }
    
    Fallback "Sprites/Default"
}

