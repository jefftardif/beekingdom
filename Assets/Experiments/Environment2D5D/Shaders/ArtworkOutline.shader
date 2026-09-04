Shader "BeeKingdom/Experiments/ArtworkOutline"
{
    // Détourage lumineux de sélection épousant la silhouette alpha de l'artwork.
    //
    // Réutilise exactement la technique de MOVE (BuildingPlacementEditor / ArtworkUnlit) :
    //   - même texture d'artwork PNG (passée par le matériau du bâtiment matérialisé),
    //   - même quad géométrique (le MeshFilter de l'artwork est cloné), 
    //   - même seuil d'opacité (8/255) pour définir la silhouette.
    // Seuls les texels VOISINS d'un pixel transparent (bord de silhouette) sont émis :
    // l'écran n'affiche donc que le contour, jamais un rectangle ni un quad plein.
    // Compatible URP (LightMode = SRPDefaultUnlit), queue Transparent, double-sided,
    // sans écriture de Z (l'ordre de profondeur vient du décalage Z de l'overlay).
    Properties
    {
        _MainTex ("Artwork", 2D) = "white" {}
        _Color ("Outline Color", Color) = (1, 0.86, 0.3, 1)
        _OutlineWidth ("Outline Width (texels)", Float) = 2
        _Intensity ("Outline Intensity", Float) = 1
        _AlphaCutoff ("Alpha Cutoff", Float) = 0.0314
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
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _Color;
                float _OutlineWidth;
                float _Intensity;
                float _AlphaCutoff;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 center = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                if (center.a < _AlphaCutoff) discard;

                float2 step = float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y) * _OutlineWidth;

                half onEdge = 0.0h;
                if (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(step.x, 0.0)).a < _AlphaCutoff) onEdge = 1.0h;
                if (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(step.x, 0.0)).a < _AlphaCutoff) onEdge = 1.0h;
                if (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0.0, step.y)).a < _AlphaCutoff) onEdge = 1.0h;
                if (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(0.0, step.y)).a < _AlphaCutoff) onEdge = 1.0h;

                if (onEdge < 0.5) discard;
                return half4(_Color.rgb * _Intensity, _Color.a);
            }
            ENDHLSL
        }
    }
}
