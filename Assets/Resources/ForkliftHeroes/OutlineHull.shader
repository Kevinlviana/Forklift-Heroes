// Contorno por "casco invertido": desenha o objeto de novo, um pouco maior e com as faces de tras.
// Funciona bem em formas convexas centradas no pivo (caixas, cilindros, esferas).
Shader "ForkliftHeroes/OutlineHull"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.04, 0.04, 0.07, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.2)) = 0.045
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 p = IN.positionOS.xyz * (1.0 + _OutlineWidth);
                OUT.positionCS = TransformObjectToHClip(p);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
