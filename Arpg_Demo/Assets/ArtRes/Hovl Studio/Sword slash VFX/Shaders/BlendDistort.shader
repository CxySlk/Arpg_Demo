Shader "Hovl/Particles/BlendDistort"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _Noise("Noise", 2D) = "white" {}
        _Flow("Flow", 2D) = "white" {}
        _Mask("Mask", 2D) = "white" {}
        _NormalMap("NormalMap", 2D) = "bump" {}
        _Color("Color", Color) = (0.5,0.5,0.5,1)
        _Distortionpower("Distortion power", Float) = 0
        _SpeedMainTexUVNoiseZW("Speed MainTex U/V + Noise Z/W", Vector) = (0,0,0,0)
        _DistortionSpeedXYPowerZ("Distortion Speed XY Power Z", Vector) = (0,0,0,0)
        _Emission("Emission", Float) = 2
        _Opacity("Opacity", Range( 0 , 3)) = 1
        [Toggle] _Usedepth("Use depth?", Float) = 1
        [Toggle] _Softedges("Soft edges", Float) = 0
        _Depthpower("Depth power", Float) = 1
        [HideInInspector] _texcoord( "", 2D ) = "white" {}
        [HideInInspector] _tex4coord( "", 2D ) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent+0"
            "IgnoreProjector"="True"
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_Noise);
            SAMPLER(sampler_Noise);
            TEXTURE2D(_Flow);
            SAMPLER(sampler_Flow);
            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Noise_ST;
                float4 _Flow_ST;
                float4 _Mask_ST;
                float4 _NormalMap_ST;
                float4 _Color;
                float4 _SpeedMainTexUVNoiseZW;
                float4 _DistortionSpeedXYPowerZ;
                float _Distortionpower;
                float _Emission;
                float _Opacity;
                float _Usedepth;
                float _Softedges;
                float _Depthpower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 uv0 : TEXCOORD0;
                float4 uv4 : TEXCOORD3;
                float4 color : COLOR;
                float3 normal : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 uv4 : TEXCOORD1;
                float4 color : COLOR;
                float3 worldNormal : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
                float2 screenUV : TEXCOORD4;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = positionInputs.positionCS;
                o.uv0 = v.uv0.xy;
                o.uv4 = v.uv4;
                o.color = v.color;
                o.worldNormal = TransformObjectToWorldNormal(v.normal);
                o.worldPos = positionInputs.positionWS;
                o.screenUV = GetNormalizedScreenSpaceUV(positionInputs.positionCS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // ---- 屏幕扭曲：用 _CameraOpaqueTexture 替代 GrabPass ----
                float2 speedNoise = float2(_SpeedMainTexUVNoiseZW.z, _SpeedMainTexUVNoiseZW.w);
                float2 normalUV = i.uv0 * _NormalMap_ST.xy + _NormalMap_ST.zw;
                float2 normalPanner = _Time.y * speedNoise + normalUV;
                half3 normal = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalPanner),
                    _Distortionpower);
                float3 grabColor = SampleSceneColor(i.screenUV + normal.xy);

                // ---- 主纹理 / 流动 / 遮罩 ----
                float2 speedMain = float2(_SpeedMainTexUVNoiseZW.x, _SpeedMainTexUVNoiseZW.y);
                float2 mainUV = i.uv0 * _MainTex_ST.xy + _MainTex_ST.zw;
                float2 mainPanner = _Time.y * speedMain + mainUV;

                float2 distortionSpeed = float2(_DistortionSpeedXYPowerZ.x, _DistortionSpeedXYPowerZ.y);
                float2 flowUV = i.uv4.xy * _Flow_ST.xy + _Flow_ST.zw;
                float2 flowPanner = _Time.y * distortionSpeed + flowUV;

                float2 maskUV = i.uv0 * _Mask_ST.xy + _Mask_ST.zw;
                float flowPower = _DistortionSpeedXYPowerZ.z;
                float2 flowMask = SAMPLE_TEXTURE2D(_Flow, sampler_Flow, flowPanner).rg
                                * SAMPLE_TEXTURE2D(_Mask, sampler_Mask, maskUV).rg
                                * flowPower;
                float4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainPanner - flowMask);

                // ---- 噪声 ----
                float2 noiseUV = i.uv0 * _Noise_ST.xy + _Noise_ST.zw;
                float2 noisePanner = _Time.y * speedNoise + noiseUV;
                float2 noiseOffset = float2(i.uv4.w, 0.0);
                float4 noiseColor = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, noisePanner + noiseOffset);

                // ---- 颜色 / 透明度 / 自发光 ----
                float alpha = mainColor.a * noiseColor.a * _Color.a * i.color.a * _Opacity;
                float3 emission = (mainColor.rgb * noiseColor.rgb * _Color.rgb * i.color.rgb)
                                * _Emission * alpha;
                float3 rgb = lerp(grabColor + emission, grabColor * emission, i.uv4.z);

                // ---- 深度衰减 ----
                float sceneZ = LinearEyeDepth(SampleSceneDepth(i.screenUV), _ZBufferParams);
                float partZ = -TransformWorldToView(i.worldPos).z;
                float distanceDepth = abs((sceneZ - partZ) / max(0.0001, _Depthpower));
                float depthAlpha = lerp(alpha, alpha * saturate(distanceDepth), _Usedepth);

                // ---- 软边缘 ----
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float dotResult = dot(i.worldNormal, viewDir);
                float powResult = dotResult * dotResult * dotResult * 5.0;
                float signResult = sign(dot(i.worldNormal, viewDir));
                float remap = 1.0 + (signResult - -1.0) * (0.0 - 1.0) / (1.0 - -1.0);
                float lerpEdge = lerp(powResult, (0.0 + (powResult - 0.0) * (1.0 - 0.0) / (-1.0 - 0.0)), remap);
                float edge = clamp(lerpEdge, 0.0, 1.0);
                float finalAlpha = lerp(depthAlpha, depthAlpha * edge, _Softedges);

                return float4(rgb, finalAlpha);
            }
            ENDHLSL
        }
    }
}
