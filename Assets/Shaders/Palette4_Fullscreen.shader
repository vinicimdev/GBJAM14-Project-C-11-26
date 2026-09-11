// Palette4_Fullscreen.shader
// URP full-screen post effect: crushes the image down to exactly 4 user-chosen colors.
// Works with URP's built-in "Full Screen Pass Renderer Feature" (Unity 2022.2+ / URP 14+).
//
// Readability aids that stay inside the 4-color budget:
//   * Edge pass      - depth/normal discontinuities are forced to palette index 0, so
//                      silhouettes and cube creases survive quantization.
//   * Aerial depth   - distant geometry is pulled toward a mid tone, keeping the value
//                      extremes available for near/foreground objects.
//   * Band thresholds- the three ramp breakpoints are authored directly instead of being
//                      implied by a blunt contrast stretch.
//
// NOTE: the edge pass reads _CameraDepthTexture and _CameraNormalsTexture. Set the
// Full Screen Pass Renderer Feature's "Requirements" to Depth + Normal (requirements: 3).
Shader "Hidden/Custom/Palette4_Fullscreen"
{
    Properties
    {
        [Header(Palette)]
        _Color0 ("Color 0 (darkest)",  Color) = (0.058, 0.219, 0.058, 1)
        _Color1 ("Color 1",            Color) = (0.188, 0.384, 0.188, 1)
        _Color2 ("Color 2",            Color) = (0.545, 0.674, 0.058, 1)
        _Color3 ("Color 3 (lightest)", Color) = (0.608, 0.737, 0.058, 1)

        [Header(Matching)]
        [KeywordEnum(Luminance, Nearest)] _Match ("Match Mode", Float) = 0
        [Toggle(_GAMMA_MATCH)] _GammaMatch ("Match In Gamma Space", Float) = 1
        _Contrast   ("Contrast",   Range(0.1, 4)) = 1
        _Brightness ("Brightness", Range(-1, 1))  = 0

        [Header(Band Thresholds)]
        // Breakpoints between palette entries, in match-space luminance.
        // Defaults reproduce the old uniform round(lum * 3) behaviour.
        _Threshold0 ("Threshold 0 to 1", Range(0, 1)) = 0.1667
        _Threshold1 ("Threshold 1 to 2", Range(0, 1)) = 0.5
        _Threshold2 ("Threshold 2 to 3", Range(0, 1)) = 0.8333

        [Header(Edges)]
        _EdgeStrength    ("Edge Strength",     Range(0, 1))  = 1
        _EdgeThickness   ("Edge Thickness px", Range(1, 3))  = 1
        _DepthThreshold  ("Depth Sensitivity",  Range(0.0005, 0.2)) = 0.02
        _NormalThreshold ("Normal Sensitivity", Range(0.001, 2))    = 0.25
        // Surfaces seen edge-on (ground planes near the horizon) have huge per-pixel
        // depth gradients. Relax the depth test there or they smear into solid outline.
        _GrazingRelax    ("Grazing Angle Relax", Range(1, 32)) = 12

        [Header(Aerial Perspective)]
        _AerialStrength ("Aerial Strength", Range(0, 1)) = 0
        _AerialStart    ("Aerial Start",  Float) = 20
        _AerialEnd      ("Aerial End",    Float) = 200
        _AerialColor    ("Aerial Target Color", Color) = (0.5, 0.5, 0.5, 1)

        [Header(Dither)]
        _DitherStrength ("Dither Strength",  Range(0, 1)) = 0.5
        _DitherScale    ("Dither Pixel Size", Range(1, 8)) = 1

        [Header(Blend)]
        _Blend ("Effect Amount", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "Palette4"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _MATCH_LUMINANCE _MATCH_NEAREST
            #pragma shader_feature_local_fragment _GAMMA_MATCH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            // URP 14+/Unity 6: Blit.hlsl ships in the core package, not universal.
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color0;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float  _Contrast;
                float  _Brightness;
                float  _Threshold0;
                float  _Threshold1;
                float  _Threshold2;
                float  _EdgeStrength;
                float  _EdgeThickness;
                float  _DepthThreshold;
                float  _NormalThreshold;
                float  _GrazingRelax;
                float  _AerialStrength;
                float  _AerialStart;
                float  _AerialEnd;
                float4 _AerialColor;
                float  _DitherStrength;
                float  _DitherScale;
                float  _Blend;
            CBUFFER_END

            // 4x4 ordered Bayer matrix, normalized to [0,1)
            float Bayer4x4(int2 p)
            {
                const float m[16] =
                {
                     0.0 / 16,  8.0 / 16,  2.0 / 16, 10.0 / 16,
                    12.0 / 16,  4.0 / 16, 14.0 / 16,  6.0 / 16,
                     3.0 / 16, 11.0 / 16,  1.0 / 16,  9.0 / 16,
                    15.0 / 16,  7.0 / 16, 13.0 / 16,  5.0 / 16
                };
                int i = (p.y & 3) * 4 + (p.x & 3);
                return m[i];
            }

            // Matching happens here. Palette colors and scene color must be in the same space.
            float3 ToMatchSpace(float3 c)
            {
            #if defined(_GAMMA_MATCH) && !defined(UNITY_COLORSPACE_GAMMA)
                return LinearToSRGB(saturate(c));
            #else
                return c;
            #endif
            }

            void GetPalette(out float3 pal[4])
            {
                pal[0] = _Color0.rgb;
                pal[1] = _Color1.rgb;
                pal[2] = _Color2.rgb;
                pal[3] = _Color3.rgb;
            }

            // True where nothing was rendered (cleared depth), i.e. skybox.
            bool IsSky(float rawDepth)
            {
            #if UNITY_REVERSED_Z
                return rawDepth <= 1e-6;
            #else
                return rawDepth >= 1.0 - 1e-6;
            #endif
            }

            float EyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            // Roberts cross over depth and normals. Returns 1 on a discontinuity.
            // depthScale already folds in distance and grazing angle, so the depth test
            // stays a pure silhouette detector instead of tripping on receding ground.
            float DetectEdge(float2 uv, float2 texel, float depthScale)
            {
                float2 dA = texel;                  // diagonal (-1,-1) .. (+1,+1)
                float2 dB = float2(texel.x, -texel.y);

                float d0 = EyeDepth(uv - dA);
                float d1 = EyeDepth(uv + dA);
                float d2 = EyeDepth(uv - dB);
                float d3 = EyeDepth(uv + dB);

                float depthGrad = abs(d1 - d0) + abs(d3 - d2);
                float depthEdge = step(_DepthThreshold * depthScale, depthGrad);

                float3 n0 = SampleSceneNormals(uv - dA);
                float3 n1 = SampleSceneNormals(uv + dA);
                float3 n2 = SampleSceneNormals(uv - dB);
                float3 n3 = SampleSceneNormals(uv + dB);

                float3 na = n1 - n0;
                float3 nb = n3 - n2;
                float  normalGrad = dot(na, na) + dot(nb, nb);
                float  normalEdge = step(_NormalThreshold, normalGrad);

                return max(depthEdge, normalEdge);
            }

            // Piecewise-linear remap of luminance into continuous palette-index space,
            // where the authored thresholds land exactly on the 0.5 / 1.5 / 2.5 midpoints.
            // With the default thresholds this is identical to lum * 3.
            float LumToIndex(float lum, float t0, float t1, float t2)
            {
                // Keep the thresholds monotonic even if the user drags them past each other.
                t1 = max(t1, t0);
                t2 = max(t2, t1);

                if (lum < t0) return lerp(0.0, 0.5, saturate(lum / max(t0, 1e-5)));
                if (lum < t1) return lerp(0.5, 1.5, saturate((lum - t0) / max(t1 - t0, 1e-5)));
                if (lum < t2) return lerp(1.5, 2.5, saturate((lum - t1) / max(t2 - t1, 1e-5)));
                return lerp(2.5, 3.0, saturate((lum - t2) / max(1.0 - t2, 1e-5)));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv  = input.texcoord;
                float3 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

                // Ordered dither value, optionally chunked into bigger pixels
                float2 pixelPos = uv * _ScreenParams.xy / max(_DitherScale, 1.0);
                float  bayer    = Bayer4x4((int2)floor(pixelPos));

                float3 pal[4];
                GetPalette(pal);

                // --- Scene depth: shared by the edge pass and aerial perspective ---
                float rawDepth  = SampleSceneDepth(uv);
                bool  sky       = IsSky(rawDepth);
                float centerEye = LinearEyeDepth(rawDepth, _ZBufferParams);

                // A surface seen edge-on changes depth fast per pixel without being a
                // silhouette, so scale the depth tolerance by distance AND grazing angle.
                float3 worldPos = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 viewDir  = normalize(_WorldSpaceCameraPos - worldPos);
                float3 centerN  = SampleSceneNormals(uv);
                float  grazing  = 1.0 - saturate(dot(centerN, viewDir));
                float  depthScale = max(centerEye, 1.0) * lerp(1.0, _GrazingRelax, grazing * grazing);

                // Sky has no shape to outline and no distance worth compressing.
                float2 texel = (1.0 / _ScreenParams.xy) * _EdgeThickness;
                float  edge  = (sky || _EdgeStrength <= 0.0)
                                 ? 0.0
                                 : DetectEdge(uv, texel, depthScale) * _EdgeStrength;

                float aerial = sky
                                 ? 0.0
                                 : saturate((centerEye - _AerialStart)
                                            / max(_AerialEnd - _AerialStart, 1e-4)) * _AerialStrength;

                // Bring everything into the same working space and apply tone tweaks
                float3 c = ToMatchSpace(src);
                c = saturate((c - 0.5) * _Contrast + 0.5 + _Brightness);

                // Aerial perspective: fade distant surfaces toward a single mid tone so the
                // full value range stays reserved for what's close to the camera.
                c = lerp(c, ToMatchSpace(_AerialColor.rgb), aerial);

                // Edges suppress dither, otherwise the outline breaks up into speckle.
                float dither = (bayer - 0.5) * _DitherStrength * (1.0 - edge);

                float3 outColor;

            #if defined(_MATCH_NEAREST)
                // --- Nearest color in RGB, with dither between the two closest entries ---
                int   best = 0, second = 0;
                float bestD = 1e9, secondD = 1e9;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    float3 d = c - ToMatchSpace(pal[i]);
                    float  dist = dot(d, d);
                    if (dist < bestD)      { secondD = bestD; second = best; bestD = dist; best = i; }
                    else if (dist < secondD) { secondD = dist; second = i; }
                }

                float b = sqrt(bestD);
                float s = sqrt(secondD);
                float mixAmount = (b + s) > 1e-5 ? b / (b + s) : 0.0; // 0 = clearly best, 0.5 = tie
                int   idx = ((dither + 0.5 * _DitherStrength) < mixAmount) ? second : best;
                outColor = lerp(pal[idx], pal[0], edge);
            #else
                // --- Luminance ramp: darkest -> lightest, palette order matters ---
                float lum = dot(c, float3(0.2126, 0.7152, 0.0722));

                // Continuous index space: dither amplitude is now half a band by
                // construction, so uneven thresholds stay correctly dithered.
                float cont = LumToIndex(saturate(lum), _Threshold0, _Threshold1, _Threshold2);

                // Pull edge pixels down to the darkest entry.
                cont = lerp(cont, 0.0, edge);

                int idx = (int)clamp(round(cont + dither), 0.0, 3.0);
                outColor = pal[idx];
            #endif

                return half4(lerp(src, outColor, _Blend), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
