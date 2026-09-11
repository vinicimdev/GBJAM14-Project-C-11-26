// URP full-screen post effect: crushes the image down to four chosen colors.
// Goes on a Full Screen Pass Renderer Feature. Requirements can stay at None.
Shader "Hidden/Custom/Palette4_Fullscreen"
{
    Properties
    {
        [Header(Palette)]
        // Darkest to lightest. Swap two entries and the picture comes out inverted.
        _Color0 ("Color 0 (darkest)",  Color) = (0.100, 0.100, 0.100, 1)
        _Color1 ("Color 1",            Color) = (0.250, 0.250, 0.250, 1)
        _Color2 ("Color 2",            Color) = (0.750, 0.750, 0.750, 1)
        _Color3 ("Color 3 (lightest)", Color) = (0.900, 0.900, 0.900, 1)

        [Header(Tone)]
        _Contrast   ("Contrast",   Range(0.1, 4)) = 1
        _Brightness ("Brightness", Range(-1, 1))  = 0

        [Header(Dither)]
        _DitherStrength ("Dither Strength",   Range(0, 1)) = 0.5
        _DitherScale    ("Dither Pixel Size", Range(1, 8)) = 1

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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            // Unity 6 / URP 14+ moved Blit.hlsl into the core package.
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color0;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float  _Contrast;
                float  _Brightness;
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
                return m[(p.y & 3) * 4 + (p.x & 3)];
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv  = input.texcoord;
                float3 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

                // Band in gamma space. Matching on linear luminance would drop everything
                // below sRGB 0.45 into the darkest band.
            #ifdef UNITY_COLORSPACE_GAMMA
                float3 c = src;
            #else
                float3 c = LinearToSRGB(saturate(src));
            #endif
                c = saturate((c - 0.5) * _Contrast + 0.5 + _Brightness);

                float lum = dot(c, float3(0.2126, 0.7152, 0.0722));

                // At full strength a pixel moves half a band at most, so the dither only
                // ever mixes adjacent palette entries.
                float2 pixelPos = uv * _ScreenParams.xy / max(_DitherScale, 1.0);
                float  dither   = (Bayer4x4((int2)pixelPos) - 0.5) * _DitherStrength;

                float3 pal[4] = { _Color0.rgb, _Color1.rgb, _Color2.rgb, _Color3.rgb };
                int    idx    = (int)clamp(round(lum * 3.0 + dither), 0.0, 3.0);

                return half4(lerp(src, pal[idx], _Blend), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
