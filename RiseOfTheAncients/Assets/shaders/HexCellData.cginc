#if !defined(HEXCELLDATA)
#define HEXCELLDATA

sampler2D _HexCellData; 		// The texture
float4 _HexCellData_TexelSize; 	// The Vec4 defined in initialize

// When edit mode keyword is defined set visibility and exploration to 1 (everything visible)
float4 FilterCellData (float4 data) {
	#if defined(HEX_MAP_EDIT_MODE)
		data.xy = 1; // Set both visibility and exploration (red and green channels aka X Y)
	#endif
	return data;
}

float4 GetCellData (appdata_full v, int index) {
	// We store cell indices at the texcoord2 property of the terrain mesh (v in this case)
	// To sample our cell data texture we actually need to use UV coordinates so we must build them
	// from the cell index
	// _HexCellData_TexelSize.x = 1 / textureWidth (see HexCellShaderData::Initialize)
	// _HexCellData_TexelSize.y = 1 / textureHeight (see HexCellShaderData::Initialize)
	// Adding 0.5 to align with the center of pixels
	float2 uv;
	uv.x = (v.texcoord2[index] + 0.5) * _HexCellData_TexelSize.x; // The result is CellZCoord.U
	float row = floor(uv.x); // row = CellZCoord
	uv.x -= row; // To get the correct U coord then we just subtract the integer part
	uv.y = (row + 0.5) * _HexCellData_TexelSize.y; // V coord is CellZCoord / textureHeight 

	// When sampling the texture in the vertex shared we need to indicate which mipmap to use
	// We use none so pass the texture coordinates and then 0 for the rest
	float4 data = tex2Dlod(_HexCellData, float4(uv, 0, 0)); // Get our cell data
	data.w *= 255; // We stored terrain type as a byte but GPU convert to [0,1] so put it back to [0,255]

	return FilterCellData(data);
}

float4 GetCellData (float2 cellDataCoordinates) {
	float2 uv = cellDataCoordinates + 0.5;
	uv.x *= _HexCellData_TexelSize.x;
	uv.y *= _HexCellData_TexelSize.y;

	return FilterCellData(tex2Dlod(_HexCellData, float4(uv, 0, 0)));
}

#endif