Shader "Custom/CardUniversal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // Подсветка (Highlight)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0.0, 10.0)) = 2.0
        _OutlineGlow ("Outline Glow", Range(0.0, 10.0)) = 1.0
        
        // Деформация атаки (Attack)
        _AttackSkewAmount ("Attack Skew Amount", Range(0.0, 1.0)) = 0.0
        
        // Деформация hurt (Hurt)
        _HurtSkewAmount ("Hurt Skew Amount", Range(0.0, 1.0)) = 0.0
        _HurtShakeAmount ("Hurt Shake Amount", Range(0.0, 1.0)) = 0.0
        _HurtShakeSpeed ("Hurt Shake Speed", Range(0.0, 20.0)) = 10.0
        
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _AlphaTex;
            fixed4 _Color;
            fixed4 _RendererColor;
            float4 _Flip;
            
            // Подсветка
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineGlow;
            
            // Деформация атаки
            float _AttackSkewAmount;
            
            // Деформация hurt
            float _HurtSkewAmount;
            float _HurtShakeAmount;
            float _HurtShakeSpeed;

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Unity's sprite flip function
            float4 UnityFlipSprite(float4 pos, float4 flip)
            {
                float2 halfSize = float2(0.5, 0.5);
                pos.xy = (pos.xy - halfSize) * flip.xy + halfSize;
                return pos;
            }

            v2f vert(appdata IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                // Apply sprite flip
                float4 vertex = UnityFlipSprite(IN.vertex, _Flip);
                
                // ========== ДЕФОРМАЦИЯ АТАКИ (Attack Skew) ==========
                // Деформация карточки в параллелограмм - сдвиг верхней грани вправо
                float normalizedY = saturate((vertex.y + 0.5)); // Преобразуем [-0.5, 0.5] в [0, 1]
                float attackSkewOffset = normalizedY * _AttackSkewAmount;
                vertex.x += attackSkewOffset;
                
                // ========== ДЕФОРМАЦИЯ HURT (Hurt Skew) ==========
                // Деформация карточки в параллелограмм - сдвиг верхней грани ВЛЕВО (противоположно атаке)
                float hurtSkewOffset = normalizedY * _HurtSkewAmount;
                vertex.x -= hurtSkewOffset; // Минус для сдвига влево
                
                // ========== ДЕФОРМАЦИЯ HURT (Shake) ==========
                // Тряска карточки при получении урона
                if (_HurtShakeAmount > 0.001)
                {
                    float shakeX = sin(_Time.y * _HurtShakeSpeed + vertex.y * 10.0) * _HurtShakeAmount * 0.1;
                    float shakeY = cos(_Time.y * _HurtShakeSpeed + vertex.x * 10.0) * _HurtShakeAmount * 0.1;
                    vertex.x += shakeX;
                    vertex.y += shakeY;
                }
                
                OUT.vertex = UnityObjectToClipPos(vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);
                
                #ifdef ETC1_EXTERNAL_ALPHA
                color.a = tex2D(_AlphaTex, uv).r;
                #endif
                
                return color;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                float centerAlpha = c.a;
                
                // ========== ПОДСВЕТКА (Outline) ==========
                // Sample neighboring pixels to detect edges (outline)
                float2 uv = IN.texcoord;
                float2 pixelSize = _MainTex_TexelSize.xy * _OutlineWidth;

                // Check surrounding pixels in 8 directions
                float maxAlpha = centerAlpha;
                maxAlpha = max(maxAlpha, SampleSpriteTexture(uv + float2(-pixelSize.x, -pixelSize.y)).a);
                maxAlpha = max(maxAlpha, SampleSpriteTexture(uv + float2(0, -pixelSize.y)).a);
                maxAlpha = max(maxAlpha, SampleSpriteTexture(uv + float2(pixelSize.x, -pixelSize.y)).a);
                maxAlpha = max(maxAlpha, SampleSpriteTexture(uv + float2(-pixelSize.x, 0)).a);
                maxAlpha = max(maxAlpha, SampleSpriteTexture(uv + float2(pixelSize.x, 0)).a);
                maxAlpha = max(maxAlpha, SampleSpriteTexture(uv + float2(-pixelSize.x, pixelSize.y)).a);
                maxAlpha = max(maxAlpha, SampleSpriteTexture(uv + float2(0, pixelSize.y)).a);
                maxAlpha = max(maxAlpha, SampleSpriteTexture(uv + float2(pixelSize.x, pixelSize.y)).a);

                // Calculate outline: draw outline where neighbors have alpha but center doesn't
                float outlineAlpha = (maxAlpha - centerAlpha) * _OutlineGlow;
                
                // Mix outline color with sprite color
                fixed4 outlineColor = _OutlineColor;
                outlineColor.a *= outlineAlpha;
                
                // Combine sprite and outline
                c.rgb = lerp(c.rgb, outlineColor.rgb, outlineColor.a);
                c.a = max(c.a, outlineAlpha * outlineColor.a);
                
                // ========== ФИНАЛЬНЫЙ ЦВЕТ ==========
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}

