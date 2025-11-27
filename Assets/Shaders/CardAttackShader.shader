Shader "Custom/CardAttack"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SkewAmount ("Skew Amount", Range(0.0, 1.0)) = 0.0
        _AnimationTime ("Animation Time", Range(0.0, 1.0)) = 0.0
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
            float _SkewAmount;
            float _AnimationTime;

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
                
                // Деформация карточки в параллелограмм
                // Для спрайтов в Unity координаты обычно в локальном пространстве
                // где центр спрайта находится в (0, 0), а границы примерно от -0.5 до 0.5
                // Преобразуем Y координату в диапазон 0-1, где 0 = низ, 1 = верх
                // Для стандартного спрайта: vertex.y от -0.5 (низ) до 0.5 (верх)
                float normalizedY = saturate((vertex.y + 0.5)); // Преобразуем [-0.5, 0.5] в [0, 1]
                
                // Применяем сдвиг: чем выше вершина, тем больше сдвиг вправо
                // _SkewAmount - это максимальный сдвиг в единицах локального пространства
                float skewOffset = normalizedY * _SkewAmount;
                vertex.x += skewOffset;
                
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
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}

