Shader "Hovl/Particles/Distortion"
{
    Properties
    {
        _NormalMap("Normal Map", 2D) = "bump" {}
        _Distortionpower("Distortion power", Float) = 0.05
        _InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _NormalMap_ST;
                float _Distortionpower;
                float _InvFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float4 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 screenUV : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = positionInputs.positionCS;
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv.xy, _NormalMap);
                o.screenUV = GetNormalizedScreenSpaceUV(positionInputs.positionCS);
                o.worldPos = positionInputs.positionWS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 软粒子：用场景深度让靠近几何体的粒子淡出。
                float sceneZ = LinearEyeDepth(SampleSceneDepth(i.screenUV), _ZBufferParams);
                float partZ = -TransformWorldToView(i.worldPos).z;
                float fade = saturate(_InvFade * (sceneZ - partZ));
                i.color.a *= fade;

                // 用 URP 的 _CameraOpaqueTexture 替代 Built-in 的 GrabPass。
                half3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));
                float distanceAtten = 1.0 / max(0.0001, distance(_WorldSpaceCameraPos, i.worldPos));
                float2 offset = normal.xy * _CameraOpaqueTexture_TexelSize.xy
                              * _Distortionpower * i.color.a * distanceAtten;
                float3 grabColor = SampleSceneColor(i.screenUV + offset);

                half clampResult = saturate((abs(normal.r) + abs(normal.g) * 30) - 0.03);
                return half4(grabColor, clampResult);
            }
            ENDHLSL
        }
    }
}
