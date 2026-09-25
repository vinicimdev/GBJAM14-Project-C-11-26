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

        [Header(Levels)]
        _BlackPoint ("Black Point", Range(0, 1))    = 0
        _WhitePoint ("White Point", Range(0, 1))    = 1
        _Gamma      ("Gamma",       Range(0.2, 5))  = 1

        [Header(Band Cutoffs)]
        _Cutoff0 ("Cutoff 0 to 1", Range(0, 1)) = 0.1667
        _Cutoff1 ("Cutoff 1 to 2", Range(0, 1)) = 0.5
        _Cutoff2 ("Cutoff 2 to 3", Range(0, 1)) = 0.8333

        [Header(Dither)]
        [Enum(Bayer 4x4, 0, Bayer 8x8, 1)] _Pattern ("Pattern", Float) = 0
        // 0 = hard bands, 1 = dither across the whole band.
        _TransitionWidth ("Transition Width", Range(0, 1)) = 0.5
        _DitherScale     ("Dither Pixel Size", Range(1, 8)) = 1

        [Header(Debug)]
        [Enum(Final, 0, Luma, 1, Bands, 2)] _DebugView ("Debug View", Float) = 0
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
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color0;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float  _BlackPoint;
                float  _WhitePoint;
                float  _Gamma;
                float  _Cutoff0;
                float  _Cutoff1;
                float  _Cutoff2;
                float  _Pattern;
                float  _TransitionWidth;
                float  _DitherScale;
                float  _DebugView;
                float  _Blend;
            CBUFFER_END

            // Set globally by the camera to keep the pattern locked to the world.
            float2 _DitherOffset;

            // Set globally by SettingsWindow while the game runs, so the material asset never changes.
            // While _SettingsPaletteOn is 0 (edit mode, a fresh launch) the material's own values show.
            float  _SettingsPaletteOn;
            float4 _SettingsColor0;
            float4 _SettingsColor1;
            float4 _SettingsColor2;
            float4 _SettingsColor3;
            float  _SettingsDitherOff;

            // Recursive Bayer matrix of size 2^n, in (0,1).
            float Bayer(uint2 p, uint n)
            {
                uint m = 0;
                for (uint i = 0; i < n; i++)
                {
                    uint x = (p.x >> i) & 1;
                    uint y = (p.y >> i) & 1;
                    m = (m << 2) | ((x ^ y) << 1) | y;
                }
                return (m + 0.5) / (1u << (2 * n));
            }

            float Pattern(int2 p)
            {
                return Bayer(p, _Pattern < 0.5 ? 2 : 3);
            }

            // Maps luma so band centers land on 0..3 and the cutoffs on 0.5, 1.5, 2.5.
            float BandSpace(float l)
            {
                float c0 = _Cutoff0;
                float c1 = max(_Cutoff1, c0 + 1e-4);
                float c2 = max(_Cutoff2, c1 + 1e-4);
                if (l < c0) return 0.5 * l / max(c0, 1e-4);
                if (l < c1) return 0.5 + (l - c0) / (c1 - c0);
                if (l < c2) return 1.5 + (l - c1) / (c2 - c1);
                return 2.5 + 0.5 * (l - c2) / max(1.0 - c2, 1e-4);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord).rgb;

                // Band in gamma space. Matching on linear luminance would drop everything
                // below sRGB 0.45 into the darkest band.
            #ifdef UNITY_COLORSPACE_GAMMA
                float3 c = src;
            #else
                float3 c = LinearToSRGB(saturate(src));
            #endif
                float l = dot(c, float3(0.2126, 0.7152, 0.0722));
                l = saturate((l - _BlackPoint) / max(_WhitePoint - _BlackPoint, 1e-4));
                l = pow(l, 1.0 / _Gamma);

                if (_DebugView > 0.5 && _DebugView < 1.5) return half4(l.xxx, 1);

                // t sits between bands floor(t) and floor(t)+1; the cutoff is at the midpoint.
                float t    = BandSpace(l);
                float width = _SettingsDitherOff > 0.5 ? 0.0 : _TransitionWidth;
                float up   = saturate((frac(t) - 0.5) / max(width, 1e-4) + 0.5);
                int2  p    = (int2)floor((input.positionCS.xy + _DitherOffset) / max(_DitherScale, 1.0));
                int   idx  = min((int)t + (up > Pattern(p) ? 1 : 0), 3);

                if (_DebugView > 1.5) idx = min((int)round(t), 3);

                float3 pal[4] = { _Color0.rgb, _Color1.rgb, _Color2.rgb, _Color3.rgb };
                if (_SettingsPaletteOn > 0.5)
                {
                    pal[0] = _SettingsColor0.rgb;
                    pal[1] = _SettingsColor1.rgb;
                    pal[2] = _SettingsColor2.rgb;
                    pal[3] = _SettingsColor3.rgb;
                }
                return half4(lerp(src, pal[idx], _Blend), 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
