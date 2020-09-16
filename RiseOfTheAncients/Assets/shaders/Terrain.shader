Shader "Custom/Terrain" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("TerrainTextureArray", 2DArray) = "white" {} // Terrain textures array
		_GridTex ("GridTexture", 2D) = "white" {}			   // Hex grid texture
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Specular ("Specular", Color) = (0.2, 0.2, 0.2)
		_BackgroundColor ("Background Color", Color) = (0,0,0)
		[Toggle(SHOW_MAP_DATA)] _ShowMapData ("Show Map Data", Float) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf StandardSpecular fullforwardshadows vertex:vert
		#pragma target 3.5

		#pragma multi_compile _ GRID_ON
		#pragma multi_compile _ HEX_MAP_EDIT_MODE

		#pragma shader_feature SHOW_MAP_DATA

		#include "HexCellData.cginc"
		#include "HexMetrics.cginc"

		UNITY_DECLARE_TEX2DARRAY(_MainTex);
		sampler2D _GridTex;

		struct Input {
			float4 color : COLOR;
			float3 worldPos;
			float3 terrain;
			float4 visibility;

			#if defined(SHOW_MAP_DATA)
				float mapData;
			#endif
		};

		// Pass terrain types to fragment shader
		void vert (inout appdata_full v, out Input data) {
			UNITY_INITIALIZE_OUTPUT(Input, data);
			float4 cell0 = GetCellData(v, 0);
			float4 cell1 = GetCellData(v, 1);
			float4 cell2 = GetCellData(v, 2);

			data.terrain.x = cell0.w;
			data.terrain.y = cell1.w;
			data.terrain.z = cell2.w;

			data.visibility.x = cell0.x;
			data.visibility.y = cell1.x;
			data.visibility.z = cell2.x;

			data.visibility.xyz = lerp(0.25, 1, data.visibility.xyz);

			// Compute exploration and store in w, exploration the green channel (Y) of hex cell shader data.
			data.visibility.w = cell0.y * v.color.x + cell1.y * v.color.y + cell2.y * v.color.z;

			#if defined(SHOW_MAP_DATA) // The Z channel has the humidity information DEBUGGING
				data.mapData = cell0.z * v.color.x + cell1.z * v.color.y + cell2.z * v.color.z;
			#endif
		}

		half _Glossiness;
		fixed3 _Specular;
		fixed4 _Color;
		half3 _BackgroundColor;

		float4 GetTerrainColor (Input IN, int index) {
			float3 uvw = float3(IN.worldPos.xz * (2 * TILING_SCALE), IN.terrain[index]); // Make UVs
			float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvw); // Sample
			return c * (IN.color[index] * IN.visibility[index]); // Modulate texture sample with splat map color and visibility
		}

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
			
			fixed4 c = GetTerrainColor(IN, 0) + GetTerrainColor(IN, 1) + GetTerrainColor(IN, 2);

			fixed4 grid = 1; // By default no grid

			#if defined(GRID_ON)

				float2 gridUV = IN.worldPos.xz; // Start gridUV as world position of fragment
				// Our grid texture is 2 x 2 cells
				// The forward distance between adjacent cell centers is 15, twice that to move two cells straight up. 
				// So we have to divide our grid's V coordinates by 30
				gridUV.y *= 1 / (2 * 15.0);
				//  And the inner radius of our cells is 5√3, so four times that is needed to move two cells to the right. 
				gridUV.x *= 1 / (4 * 8.66025404);

				grid = tex2D(_GridTex, gridUV);

			#endif

			float explored = IN.visibility.w;
			o.Albedo = c.rgb * grid * _Color * explored;

			#if defined(SHOW_MAP_DATA) // If showing humidity override normal colors
				o.Albedo = IN.mapData * grid;
			#endif

			o.Specular = _Specular * explored;
			o.Smoothness = _Glossiness;
			o.Occlusion = explored;
			o.Emission = _BackgroundColor * (1 -  explored);
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}