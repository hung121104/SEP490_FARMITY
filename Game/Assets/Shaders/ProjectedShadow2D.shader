Shader "Custom/ProjectedShadow2D"
{
    // ─────────────────────────────────────────────────────────────────────────
    // Projected-shadow shader for 2D URP sprites.
    //
    // Each vertex is sheared horizontally by its Y position in object space,
    // projecting the sprite silhouette in the sun direction.  The Y axis is
    // optionally flattened so the shadow appears to lie on the ground.
    // The fragment discards transparent pixels and outputs a flat tinted colour.
    //
    // Runtime parameters (_ShadowLeanX, _ShadowScaleX, _ShadowFlattenY,
    // _ShadowAlpha) are pushed every frame by SpriteShadowShader.cs via
    // MaterialPropertyBlock — no material instancing is required.
    // ─────────────────────────────────────────────────────────────────────────

    Properties
    {
        _MainTex        ("Sprite Texture",   2D)          = "white" {}
        _ShadowColor    ("Shadow Color",     Color)       = (0.02, 0.02, 0.08, 1.0)
        _ShadowLeanX    ("Shadow Lean X",    Float)       = 0.5
        _ShadowScaleX   ("Shadow Scale X",   Float)       = 1.0
        _ShadowFlattenY ("Shadow Flatten Y", Range(0, 1)) = 0.5
        _ShadowAlpha    ("Shadow Alpha",     Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent-1"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend  SrcAlpha OneMinusSrcAlpha
        Cull   Off
        ZWrite Off

        Pass
        {
            Name "ProjectedShadow2D"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _ShadowColor;
                float  _ShadowLeanX;
                float  _ShadowScaleX;
                float  _ShadowFlattenY;
                float  _ShadowAlpha;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float4 pos = IN.positionOS;

                // ── Sun-direction shear ───────────────────────────────────────
                // Higher vertices (pos.y > 0 with bottom-pivot sprites) lean
                // further in the shadow direction.  _ShadowLeanX is negative
                // when the sun is on the right (shadow goes left) and positive
                // when the sun is on the left (shadow goes right).
                float origY = pos.y;
                pos.x = pos.x * _ShadowScaleX + origY * _ShadowLeanX;

                // ── Flatten Y so the shadow lies on the ground below the feet ─
                // Negate Y so the shadow extends downward from the pivot:
                //   origY = 0 (feet) → pos.y = 0  (stays at foot level)
                //   origY = h (head) → pos.y = -h * flattenY (below feet)
                // _ShadowFlattenY = 0 → fully flat  |  = 1 → full sprite height
                pos.y = -origY * _ShadowFlattenY;

                OUT.positionHCS = TransformObjectToHClip(pos);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Discard fully transparent pixels (keeps silhouette clean)
                clip(tex.a - 0.01);

                // Output flat shadow colour with sprite alpha * overall opacity
                return half4(_ShadowColor.rgb, tex.a * _ShadowAlpha * IN.color.a);
            }
            ENDHLSL
        }
    }
}
