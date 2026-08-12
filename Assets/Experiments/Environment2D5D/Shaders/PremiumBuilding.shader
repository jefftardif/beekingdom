Shader "BeeKingdom/Experiments/PremiumBuilding"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionStrength ("Emission Strength", Float) = 1
        _LightDir ("Fake Light Direction", Vector) = (-0.35, 0.55, -0.75, 0)
        _LightColor ("Fake Light Color", Color) = (1.05, 1.0, 0.9, 1)
        _AmbientColor ("Ambient Color", Color) = (0.52, 0.55, 0.6, 1)
        _SpecColor ("Specular Color", Color) = (1, 0.92, 0.75, 1)
        _Shininess ("Shininess", Float) = 24
        _RimColor ("Rim Color", Color) = (0.55, 0.42, 0.2, 1)
        _RimStrength ("Rim Strength", Float) = 0.25
        _NoiseScale ("Detail Noise Scale", Float) = 3
        _NoiseStrength ("Detail Noise Strength", Float) = 0.06
        _GrainScale ("Wood Grain Scale", Float) = 0
        _GrainStrength ("Wood Grain Strength", Float) = 0
        _Cull ("Cull", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        Cull [_Cull]
        ZWrite On

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 color : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EmissionColor;
                float _EmissionStrength;
                float4 _LightDir;
                float4 _LightColor;
                float4 _AmbientColor;
                float4 _SpecColor;
                float _Shininess;
                float4 _RimColor;
                float _RimStrength;
                float _NoiseScale;
                float _NoiseStrength;
                float _GrainScale;
                float _GrainStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = input.uv;
                o.color = input.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 V = normalize(_WorldSpaceCameraPos - input.positionWS);
                float3 L = normalize(_LightDir.xyz);
                float ndl = saturate(dot(N, L));

                float3 diffuse = lerp(_AmbientColor.rgb, _LightColor.rgb, ndl);

                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(N, H)), _Shininess);
                float fresnel = 1.0 - saturate(dot(N, V));
                float rim = pow(fresnel, 3.0) * _RimStrength;

                // Procedural surface detail (no textures): organic mottle from UVs +
                // world position, plus optional vertical wood-grain planks.
                float2 duv = input.uv * _NoiseScale;
                float n1 = sin(duv.x * 12.9898 + duv.y * 78.233 + input.positionWS.y * 1.7);
                float n2 = sin(duv.y * 39.71 - duv.x * 14.13 + input.positionWS.x * 2.1);
                float mottle = (n1 * n2) * 0.5 + 0.5;
                float detail = 1.0 - _NoiseStrength + mottle * _NoiseStrength * 2.0;

                float grain = 1.0;
                if (_GrainScale > 0.001)
                {
                    float g = sin(input.uv.x * _GrainScale * 31.4159);
                    float plankEdge = pow(abs(sin(input.uv.x * _GrainScale * 6.28318)), 24.0);
                    grain = 1.0 - _GrainStrength + g * _GrainStrength * 0.7;
                    grain *= 1.0 - _GrainStrength * 1.2 * plankEdge;
                }

                float3 color = _BaseColor.rgb * input.color.rgb * diffuse * detail * grain;
                color += _SpecColor.rgb * spec;
                color += _RimColor.rgb * rim;
                color += _EmissionColor.rgb * _EmissionStrength;

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
