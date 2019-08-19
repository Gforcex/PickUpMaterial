//

Shader "Hidden/PickUpMaterial"
{
	Properties
	{
		_MaterialID("MaterialID", Int) = 0
		_BaseMap("Base Color", 2D) = "white" {}
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0
	}
	
	SubShader
	{
		Pass
		{
			ZWrite On
			Blend Off
			//Cull Off

			Name "Unlit"
			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag

            struct Attributes
            {
                float4 positionOS       : POSITION;
				float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
            };

			int _MaterialID;
            sampler2D _BaseMap;
            float4 _BaseMap_ST;
			float _Cutoff;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

				output.positionCS = UnityObjectToClipPos(input.positionOS);
				output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
				//clip(tex2D(_BaseMap, input.uv).a - _Cutoff);

                return (float)_MaterialID/255;
            }
            ENDHLSL
        }
    }
}
