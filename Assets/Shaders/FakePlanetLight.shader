Shader "Custom/FakePlanetLight"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LightDir ("Light Direction", Vector) = (0.7, 0.7, 0, 0)
        _LightIntensity ("Light Intensity", Range(0, 1)) = 0.3
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
            #pragma multi_compile_local _ PIXELSNAP_ON
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
                float2 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float2 _LightDir;
            float _LightIntensity;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex).xy;
                
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Sample the sprite texture
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // Calcular posición relativa al centro del sprite (en espacio UV)
                float2 uv = IN.texcoord;
                float2 center = float2(0.5, 0.5);
                float2 toCenter = uv - center;
                
                // Normalizar la dirección de la luz
                float2 lightDir = normalize(_LightDir);
                
                // Calcular dot product para simular iluminación difusa
                // Usar la dirección desde el centro hacia el pixel
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
                // Aumentar el brillo sin saturar demasiado
                c.rgb = lerp(c.rgb, c.rgb * 1.5, totalLight);
                
                return c;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}

