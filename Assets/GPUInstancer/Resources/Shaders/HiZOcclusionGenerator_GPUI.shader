Shader "Hidden/GPUInstancer/HiZOcclusionGenerator" 
{
    Properties
    { 
        _MainTex ("Base (RGB)", 2D) = "black" {}
    }

    SubShader
    {
		Cull Off ZWrite Off ZTest Always

		CGINCLUDE
		#include "UnityCG.cginc"

		struct Input
		{
			float4 pos : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct Varyings
		{
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		Texture2D _MainTex;
		SamplerState sampler_MainTex;

		float4 _MainTex_TexelSize;

		Texture2D _CameraDepthTexture;
		SamplerState sampler_CameraDepthTexture;

		Varyings vertex(in Input input)
		{
			Varyings output;

			output.pos = UnityObjectToClipPos(input.pos.xyz);
			output.uv = input.uv;

#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0.0)
				output.uv.y = 1.0 - input.uv.y;
#endif
			return output;
		}

		float4 sampleDepth(in Varyings input) : SV_Target
		{
#if UNITY_REVERSED_Z
			return _CameraDepthTexture.Sample(sampler_CameraDepthTexture, input.uv).r;
#else
			return 1.0 - _CameraDepthTexture.Sample(sampler_CameraDepthTexture, input.uv).r;
#endif
		}

		float4 reduce(in Varyings input) : SV_Target
		{
#if SHADER_API_METAL
			int2 xy = (int2) (input.uv * (_MainTex_TexelSize.zw - 1.));
			float4 texels[2] = {
				float4(_MainTex.mips[0][xy].rg, _MainTex.mips[0][xy + int2(1, 0)].rg),
				float4(_MainTex.mips[0][xy + int2(0, 1)].rg, _MainTex.mips[0][xy + 1].rg)
			};
    
			float4 r = float4(texels[0].rb, texels[1].rb);
			float4 g = float4(texels[0].ga, texels[1].ga);
#else
        
			float4 r = _MainTex.GatherRed(sampler_MainTex, input.uv, 0);
			float4 g = _MainTex.GatherGreen(sampler_MainTex, input.uv, 0);
        
#endif
			float minimum = min(min(min(r.x, r.y), r.z), r.w);
			float maximum = max(max(max(g.x, g.y), g.z), g.w);
			return float4(minimum, maximum, 1.0, 1.0);
		}
		ENDCG

        Pass
        {
			CGPROGRAM
			#pragma target 4.5
            #pragma vertex vertex
            #pragma fragment sampleDepth
			ENDCG
        }

        Pass
        {
			CGPROGRAM
            #pragma target 4.5
            #pragma vertex vertex
            #pragma fragment reduce
			ENDCG
        }
    }
}
