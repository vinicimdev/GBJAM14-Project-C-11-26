// Simple two-sided sail for URP.
//
// Expects a flat 1x1 plane facing +Z with UVs 0-1 (v = 1 at the yard).
// Use SailMeshCreator.cs (Tools > Create Sail Mesh) to make one.
// Set each sail's size with the transform: X = width, Y = height, Z = 1.
// One material works for every sail size.

Shader "Custom/Sail"
{
    Properties
    {
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor]   _BaseColor ("Color", Color) = (0.85, 0.62, 0.22, 1)

        [Header(Billow)]
        _BillowDepth ("Depth (fraction of width)", Range(0, 0.4)) = 0.15
        _FootSlack   ("Foot Slack (0.5 loose, 1 pinned)", Range(0.5, 1)) = 0.7

        [Header(Ripples)]
        _RippleAmp   ("Amplitude (meters)", Range(0, 0.2)) = 0.03
        _RippleFreq  ("Frequency", Float) = 3
        _RippleSpeed ("Speed", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Off   // draw both sides

        // Shared by every pass so the sail bends the same way in color, shadows and depth
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            float  _BillowDepth;
            float  _FootSlack;
            float  _RippleAmp;
            float  _RippleFreq;
            float  _RippleSpeed;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv         : TEXCOORD0;
        };

        // Sail size in world units, read from the transform
        float3 GetObjectScale()
        {
            float4x4 m = GetObjectToWorldMatrix();
            return float3(length(m._m00_m10_m20), length(m._m01_m11_m21), length(m._m02_m12_m22));
        }

        // How far the cloth is pushed out along +Z at this UV (object units)
        float SailHeight(float2 uv, float3 scale)
        {
            // Belly: zero at the side edges and the yard, loose toward the foot
            float billow = sin(uv.x * PI) * sin((1.0 - uv.y) * PI * _FootSlack);

            // Ripples measured in meters, so they look the same size on every sail
            float2 m = uv * scale.xy;
            float  t = _Time.y * _RippleSpeed;
            float ripple = sin(m.x * _RippleFreq + t)
                         + 0.5 * sin((m.x + m.y) * _RippleFreq * 1.7 - t * 1.3);

            // Depth grows with width; ripples fade out where the cloth is tied
            float h = billow * (_BillowDepth * scale.x + ripple * _RippleAmp);
            return h / max(scale.z, 0.0001);   // meters -> object units
        }

        // Bends the flat plane and measures the slope to get a matching normal
        void DeformSail(float3 positionOS, float2 uv, out float3 positionWS, out float3 normalWS)
        {
            float3 scale = GetObjectScale();
            const float e = 0.01;

            float h0 = SailHeight(uv, scale);
            float hu = SailHeight(uv + float2(e, 0), scale);
            float hv = SailHeight(uv + float2(0, e), scale);

            float3 T = float3(e, 0, hu - h0);   // small step along the width
            float3 B = float3(0, e, hv - h0);   // small step along the height

            positionWS = TransformObjectToWorld(positionOS + float3(0, 0, h0));
            normalWS   = TransformObjectToWorldNormal(normalize(cross(T, B)));
        }
        ENDHLSL

        // ---------- Color ----------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                DeformSail(IN.positionOS.xyz, IN.uv, OUT.positionWS, OUT.normalWS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                // Flip the normal on the back side so both sides are lit correctly
                float3 N = normalize(IN.normalWS) * IS_FRONT_VFACE(face, 1.0, -1.0);

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb * _BaseColor.rgb;

                // Sun (with shadows) + ambient light
                Light sun = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half3 diffuse = sun.color * saturate(dot(N, sun.direction))
                              * sun.shadowAttenuation * sun.distanceAttenuation;
                half3 ambient = SampleSH(N);

                half3 color = albedo * (diffuse + ambient);
                return half4(MixFog(color, IN.fogFactor), 1);
            }
            ENDHLSL
        }

        // ---------- Shadows (uses the bent shape) ----------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;   // set by URP while rendering shadows

            float4 vert(Attributes IN) : SV_POSITION
            {
                float3 positionWS, normalWS;
                DeformSail(IN.positionOS.xyz, IN.uv, positionWS, normalWS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------- Depth (for the depth texture / depth prepass) ----------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert(Attributes IN) : SV_POSITION
            {
                float3 positionWS, normalWS;
                DeformSail(IN.positionOS.xyz, IN.uv, positionWS, normalWS);
                return TransformWorldToHClip(positionWS);
            }

            half4 frag(float4 positionCS : SV_POSITION) : SV_Target { return positionCS.z; }
            ENDHLSL
        }
    }
}
