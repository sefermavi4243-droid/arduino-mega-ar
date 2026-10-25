// Dijital ikizin hologram görünümü.
// Kenar parlaması (fresnel), kayan tarama çizgileri, aşağıdan yukarı "tarama" ile ortaya çıkma
// ve seçili parça için nabız efekti. Additive karışım: gerçek nesnenin üstüne ışık gibi biner.
Shader "G6/Hologram"
{
    Properties
    {
        _Color ("Renk", Color) = (0.25, 0.9, 1, 1)
        _Alpha ("Saydamlık", Range(0, 1)) = 0.3
        _RimPower ("Kenar Gücü", Range(0.5, 8)) = 2.5
        _ScanDensity ("Tarama Çizgisi Sıklığı", Float) = 140
        _Reveal ("Ortaya Çıkma", Range(0, 1)) = 1
        _RevealMinY ("Tarama Alt Y", Float) = 0
        _RevealMaxY ("Tarama Üst Y", Float) = 1
        _Highlight ("Vurgu", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Hologram"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            Offset -1, -1 // gerçek yüzeyle aynı düzlemde titremesin (z-fighting)

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Alpha;
                half _RimPower;
                float _ScanDensity;
                half _Reveal;
                float _RevealMinY;
                float _RevealMaxY;
                half _Highlight;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half rim = pow(1.0h - saturate(dot(n, v)), _RimPower);

                // Tarama cephesinin üstündeki pikseller henüz "taranmadı": çizilmez.
                float front = lerp(_RevealMinY, _RevealMaxY, _Reveal);
                clip(front - i.positionWS.y);
                half edge = saturate(1.0h - (front - i.positionWS.y) * 150.0h) * (1.0h - step(0.999h, _Reveal));

                half scan = 0.5h + 0.5h * sin(i.positionWS.y * _ScanDensity - _Time.y * 6.0h);
                half pulse = _Highlight * (0.65h + 0.35h * sin(_Time.y * 9.0h));

                half alpha = _Alpha * (0.3h + 0.7h * rim) + scan * 0.07h * _Alpha + edge + pulse * 0.45h;
                half3 col = _Color.rgb * (1.0h + rim * 1.6h + pulse * 1.8h) + edge * half3(1, 1, 1);
                return half4(col, saturate(alpha));
            }
            ENDHLSL
        }
    }
}
