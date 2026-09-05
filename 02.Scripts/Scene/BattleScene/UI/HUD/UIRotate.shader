Shader "Custom/UI_RotateSimple_UI"
{
    Properties
    {
        // ★ UI Image가 스프라이트 텍스처를 per-renderer로 주입할 수 있도록
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Speed ("Rotation Speed (rad/sec)", Float) = 1.0

        // ★ Unity UI 마스킹/클립을 위한 표준 프로퍼티 (그냥 그대로 두세요)
        [HideInInspector]_StencilComp ("Stencil Comp", Float) = 8
        [HideInInspector]_Stencil ("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp ("Stencil Op", Float) = 0
        [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        // ★ UI 마스킹 설정
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Speed;

            float4 _ClipRect;            // UI 클립(RectMask2D 등)
            float4 _TextureSampleAdd;    // 색공간 보정용

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color   : COLOR;
            };

            struct v2f
            {
                float4 pos  : SV_POSITION;
                float2 uv   : TEXCOORD0;
                fixed4 color: COLOR;
                float4 worldPos : TEXCOORD1;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                o.worldPos = v.vertex;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 중심(0.5,0.5) 기준 UV 회전
                float2 center = float2(0.5, 0.5);
                float a = _Time.y * _Speed;
                float c = cos(a), s = sin(a);
                float2 uv = i.uv - center;
                uv = float2(c*uv.x - s*uv.y, s*uv.x + c*uv.y) + center;

                // UI 친화 샘플 (감마/선형 보정)
                fixed4 col = (tex2D(_MainTex, uv) + _TextureSampleAdd) * i.color;

                // RectMask2D 등 클리핑
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPos, _ClipRect);
                #endif

                // Alpha Clip 옵션
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
