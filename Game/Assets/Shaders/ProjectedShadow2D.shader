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
    // MaterialPropertyBlock.
    //
    // BATCHING STRATEGY — GPU Instancing (NOT SRP Batcher)
    // -------------------------------------------------------
    // URP's SRP Batcher stores per-material data in a CBUFFER_START(UnityPerMaterial)
    // block.  MaterialPropertyBlock bypasses that block and writes directly to a
    // per-renderer slot — which is INCOMPATIBLE with the SRP Batcher path.  When
    // 60+ shadow renderers are spawned at different times (e.g. on OnDayChanged),
    // renderers whose CBUFFER hasn't been synced yet read stale/zero values and
    // appear invisible.
    //
    // Solution: place all per-instance properties in UNITY_INSTANCING_BUFFER_START
    // instead of CBUFFER_START(UnityPerMaterial).  This makes the shader use the
    // GPU Instancing path, which is fully compatible with MaterialPropertyBlock —
    // Unity uploads the property-block data as per-instance buffer data every draw
    // call, so newly spawned renderers always read correct values immediately.
    //
    // Required on the Material asset: tick "Enable GPU Instancing".
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
            // GPU Instancing — required for MaterialPropertyBlock to work correctly
            // when many renderers sharing this material are spawned at different times.
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
                // Instance ID must be forwarded to the fragment shader so
                // UNITY_ACCESS_INSTANCED_PROP works there too.
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // ── Per-instance properties (GPU Instancing path) ─────────────────
            // All shadow parameters vary per renderer — MaterialPropertyBlock
            // writes them as per-instance data, which this buffer reads correctly.
            // DO NOT move these back into CBUFFER_START(UnityPerMaterial): that
            // would re-enable the SRP Batcher path and break MaterialPropertyBlock.
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ShadowColor)
                UNITY_DEFINE_INSTANCED_PROP(float,  _ShadowLeanX)
                UNITY_DEFINE_INSTANCED_PROP(float,  _ShadowScaleX)
                UNITY_DEFINE_INSTANCED_PROP(float,  _ShadowFlattenY)
                UNITY_DEFINE_INSTANCED_PROP(float,  _ShadowAlpha)
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);   // forward instance ID to frag
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float4 pos = IN.positionOS;

                // ── Sun-direction shear ───────────────────────────────────────
                // Higher vertices (pos.y > 0 with bottom-pivot sprites) lean
                // further in the shadow direction.  _ShadowLeanX is negative
                // when the sun is on the right (shadow goes left) and positive
                // when the sun is on the left (shadow goes right).
                float origY    = pos.y;
                float scaleX   = UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowScaleX);
                float leanX    = UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowLeanX);
                float flattenY = UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowFlattenY);

                pos.x = pos.x * scaleX + origY * leanX;

                // ── Flatten Y so the shadow lies on the ground below the feet ─
                // Negate Y so the shadow extends downward from the pivot:
                //   origY = 0 (feet) → pos.y = 0  (stays at foot level)
                //   origY = h (head) → pos.y = -h * flattenY (below feet)
                // _ShadowFlattenY = 0 → fully flat  |  = 1 → full sprite height
                pos.y = -origY * flattenY;

                OUT.positionHCS = TransformObjectToHClip(pos);
                // Sprite UVs are already atlas-correct; no tiling/offset needed.
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);   // required to access instanced props

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Discard fully transparent pixels (keeps silhouette clean)
                clip(tex.a - 0.01);

                float4 col   = UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowColor);
                float  alpha = UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowAlpha);

                // Output flat shadow colour with sprite alpha * overall opacity
                return half4(col.rgb, tex.a * alpha * IN.color.a);
            }
            ENDHLSL
        }
    }
}
