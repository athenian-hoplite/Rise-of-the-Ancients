Shader "Custom/Road" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Specular ("Specular", Color) = (0.2, 0.2, 0.2)
	}
	SubShader {
		Tags { "RenderType"="Opaque" "Queue" = "Geometry+1"} // Draw after normal geometry so that roads are over terrain
		LOD 200
		Offset -1, -1 // Also add offset to make GPU think these objects are closer to the camera than in reality
		
		CGPROGRAM
		#pragma surface surf StandardSpecular fullforwardshadows decal:blend vertex:vert // Added decal:blend to allow blending
		#pragma target 3.0

		#pragma multi_compile _ HEX_MAP_EDIT_MODE

		#include "HexCellData.cginc"
		#include "HexMetrics.cginc"

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
			float2 visibility;
		};

		half _Glossiness;
		fixed3 _Specular;
		fixed4 _Color;

		void vert (inout appdata_full v, out Input data) {
			UNITY_INITIALIZE_OUTPUT(Input, data);

			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);

			data.visibility.x = cell0.x * v.color.x + cell1.x * v.color.y;
			data.visibility.x = lerp(0.25, 1, data.visibility.x);
			data.visibility.y = cell0.y * v.color.x + cell1.y * v.color.y;
		}

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) {

			// Sample noise from texture, scaling down coordinates to not tile the texture too much
			float4 noise = tex2D(_MainTex, IN.worldPos.xz * (3 * TILING_SCALE));

			float blend = IN.uv_MainTex.x; // Get U coordinate, 0 at road edge 1 at road center, we defined when creating roads
			// Perturn the blend (adding 0.5 since on average noise is 0.5 and that would destroy roads, giving them really low alpha)
			blend *= noise.x + 0.5; 

			// In [0,0.4] values will be 0, in [0.4,0.7] values will steadily increase, in [0.7,1] values will be 1
			blend = smoothstep(0.4, 0.7, blend); 

			// Perturb the color aswell to make the road look "dirty" and irregular
			// Scale down the noise by 75% and add 0.25 to make 1 a possible value (noise is always between 0 and 1)
			// Note that we sample from a differnt channel than when perturbing blend
			fixed4 c = _Color * ((noise.y * 0.75 + 0.25) * IN.visibility.x);

			float explored = IN.visibility.y;
			o.Albedo = c.rgb;
			o.Specular = _Specular * explored;
			o.Smoothness = _Glossiness;
			o.Occlusion = explored;

			// Define alpha with calculated blend and factor explored
			o.Alpha = blend * explored;
		}
		ENDCG
	}
	FallBack "Diffuse"
}