Shader "CustomSRP/ScreenSpaceUV"
{
    Properties {}
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        ENDHLSL

        Pass
        {
            Tags
            {
                "LightMode" = "CustomUnlit"
            }


            HLSLPROGRAM
            #pragma vertex ScreenSpaceUVVertex
            #pragma fragment ScreenSpaceUVFragment

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 positionNDC : TAR_PositionNDC;
            };


            Varyings ScreenSpaceUVVertex(Attributes VIN)
            {
                const VertexPositionInputs vertexPositionInput = GetVertexPositionInputs(VIN.positionOS);
                Varyings VOUT;
                VOUT.positionCS = vertexPositionInput.positionCS;
                VOUT.positionNDC = vertexPositionInput.positionNDC;
                return VOUT;
            }


            float4 ScreenSpaceUVFragment(Varyings IN) : SV_TARGET
            {
				return float4(IN.positionNDC.xy / IN.positionNDC.w, 0, 1);
                return float4(GetNormalizedScreenSpaceUV(IN.positionCS), 0, 1);
            }
            ENDHLSL
        }
    }
}