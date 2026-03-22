Shader "Custom/CloudShadow2D"
{
    // ─────────────────────────────────────────────────────────────────────────
    // Full-screen cloud shadow overlay for 2D URP.
    //
    // A camera-following quad samples a tileable noise texture at world-space
    // UVs, scrolling two noise layers in diverging directions (so the pattern
    // never obviously repeats).  The combined noise is contrast/threshold
    // adjusted and output as a darkening overlay via Multiply blending.
    //
    // Driven by CloudShadowController.cs which pushes all uniforms per frame.
    // ─────────────────────────────────────────────────────────────────────────

    Properties
    {
        _CloudNoise      ("Cloud Noise Texture", 2D)           = "white" {}
        _CloudScale      ("Cloud Scale",         Float)        = 30.0
        _CloudSpeed      ("Cloud Speed",         Float)        = 0.02
        _CloudContrast   ("Cloud Contrast",      Float)        = 3.0
        _CloudThreshold  ("Cloud Threshold",     Float)        = 0.1
        _CloudDirX       ("Cloud Direction X",   Float)        = 1.0
        _CloudDirY       ("Cloud Direction Y",   Float)        = 0.5
        _CloudDiverge    ("Diverge Angle (deg)", Float)        = 15.0
        _CloudShadowMin  ("Shadow Min Light",    Range(0, 1))  = 0.5
        _CloudOpacity    ("Overall Opacity",     Range(0, 1))  = 0.6
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent+100"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        // Multiply blending: darkens whatever is behind the quad.
        // rgb = src * dst  →  white noise = no change, dark noise = shadow
        Blend DstColor Zero
        Cull  Off
        ZWrite Off

        Pass
        {
            Name "CloudShadow2D"

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 worldUV     : TEXCOORD0;
            };

            TEXTURE2D(_CloudNoise);
            SAMPLER(sampler_CloudNoise);

            CBUFFER_START(UnityPerMaterial)
                float _CloudScale;
                float _CloudSpeed;
                float _CloudContrast;
                float _CloudThreshold;
                float _CloudDirX;
                float _CloudDirY;
                float _CloudDiverge;
                float _CloudShadowMin;
                float _CloudOpacity;
            CBUFFER_END

            // ── Helpers ──────────────────────────────────────────────────

            float2 RotateVec2(float2 v, float angleDeg)
            {
                float rad = angleDeg * (PI / 180.0);
                float c   = cos(rad);
                float s   = sin(rad);
                return float2(v.x * c - v.y * s,
                              v.x * s + v.y * c);
            }

            // ── Vertex ──────────────────────────────────────────────────

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Transform to clip space
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Compute world-space XY for stable noise UVs
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldUV = worldPos.xy;

                return OUT;
            }

            // ── Fragment ────────────────────────────────────────────────

            half4 frag(Varyings IN) : SV_Target
            {
                float invScale = 1.0 / _CloudScale;

                // Normalize cloud direction
                float2 cloudDir = normalize(float2(_CloudDirX, _CloudDirY));

                // Rotate direction for two diverging layers
                float2 dir1 = RotateVec2(cloudDir,  _CloudDiverge);
                float2 dir2 = RotateVec2(cloudDir, -_CloudDiverge);

                // Scroll offsets
                float2 offset1 = _Time.y * _CloudSpeed * dir1;
                float2 offset2 = _Time.y * _CloudSpeed * 0.89 * (PI / 3.0) * dir2;

                // Sample noise at two different scales/offsets (non-repeating)
                float sample1 = SAMPLE_TEXTURE2D(_CloudNoise, sampler_CloudNoise,
                                    IN.worldUV * invScale + offset1).r;
                float sample2 = SAMPLE_TEXTURE2D(_CloudNoise, sampler_CloudNoise,
                                    IN.worldUV * (invScale * 0.8) + offset2).r;

                // Combine: multiply gives organic blobby shapes
                float combined = sample1 * sample2;

                // Apply contrast to sharpen blob edges, then offset by threshold.
                // _CloudContrast: higher = harder cloud edges.
                // _CloudThreshold: higher = bigger cloud coverage (shifts mask outward).
                float shaped = (combined - 0.5) * _CloudContrast + 0.5;
                shaped = saturate(shaped + _CloudThreshold);

                // Binarize with a narrow soft edge so cloud borders are clean but
                // the interior is completely flat — no tonal gradient inside a shadow.
                // shaped near 0 → dense cloud (shadow), near 1 → clear sky.
                float shadowMask = 1.0 - smoothstep(0.4, 0.6, shaped);

                // All shadow pixels get exactly _CloudShadowMin brightness — unified.
                // _CloudOpacity fades the whole layer (time-of-day / rain).
                float finalLight = lerp(1.0, _CloudShadowMin, shadowMask * _CloudOpacity);

                // Multiply blending: white = no change, dark = shadow.
                return half4(finalLight, finalLight, finalLight, 1.0);
            }
            ENDHLSL
        }
    }
}
