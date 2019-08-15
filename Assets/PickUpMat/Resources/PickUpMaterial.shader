//

Shader "Hidden/PickUpMaterial"
{
	Properties
	{
		_MaterialID("MaterialID", Int) = 0
	}
	
	SubShader
	{
		Pass
		{
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
            sampler2D _MainTex;
            float4 _MainTex_ST;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

				output.positionCS = UnityObjectToClipPos(input.positionOS);
				output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return (float)_MaterialID/255;
            }
            ENDHLSL
        }
    }
}
