Shader "BeeKingdom/Experiments/SoftShadow"
{
    Properties
    {
        _Color ("Shadow Color", Color) = (0, 0, 0, 1)
        _Intensity ("Intensity", Float) = 0.45
        _Radius ("Radius", Float) = 0.5
        _Aspect ("Ellipse Aspect (x/y)", Float) = 1
        _Falloff ("Falloff Exponent", Float) = 2
        _Offset ("UV Center Offset", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _Radius;
                float _Aspect;
                float _Falloff;
                float4 _Offset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                o.color = input.color;
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 off = input.uv * 2.0 - 1.0;
                off -= _Offset.xy;
                off.y *= _Aspect;
                float d = length(off);
                float edge = 1.0 - smoothstep(0.0, _Radius * 2.0, d);
                float alpha = _Intensity * pow(edge, _Falloff) * input.color.a;
                return half4(_Color.rgb, _Color.a * alpha);
            }
            ENDHLSL
        }
    }
}
