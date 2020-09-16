using UnityEngine;

/// <summary>
/// Describes the type of edge connection.
/// </summary>
public enum HexEdgeType {
	Flat, Slope, Cliff
}

/// <summary>
/// Static class with information about hexagon metrics.
/// </summary>
public static class HexMetrics {
	
	public const float OuterToInner = 0.866025404f; // sqrt(3) / 2
	public const float InnerToOuter = 1f / OuterToInner;

    public const float OuterRadius = 10f;

	public const float InnerRadius = OuterRadius * OuterToInner;  // inner = outer * sqrt(3) / 2
	
	public const float InnerDiameter = InnerRadius * 2f;

    /// <summary>
    /// Hexagon outer corners. Hexagon with corner on top orientation.
    /// </summary>
    static Vector3[] Corners = {
		new Vector3(0f, 0f, OuterRadius),
		new Vector3(InnerRadius, 0f, 0.5f * OuterRadius),
		new Vector3(InnerRadius, 0f, -0.5f * OuterRadius),
		new Vector3(0f, 0f, -OuterRadius),
		new Vector3(-InnerRadius, 0f, -0.5f * OuterRadius),
		new Vector3(-InnerRadius, 0f, 0.5f * OuterRadius),
        new Vector3(0f, 0f, OuterRadius) // This is a cheat wrapp to make triangulation easier and always allow Corners[direction+1]
	};
	
	public const float SolidFactor = 0.8f; // Percentage of hexagon that forms it's core
	
	public const float BlendFactor = 1f - SolidFactor; // Percentage of hexagon that forms its outer part

	// Percentage of water surface hexagon that is the core, analogous to SolidFactor for land hexes
	public const float WaterFactor = 0.6f;

	// Percentage of water surface hexagon that forms its outer part, analogous to BlendFactor for land hexes
	public const float WaterBlendFactor = 1f - WaterFactor;
	
	public const float ElevationStep = 3f;	// The increase in Y for each level of elevation

	public const int TerracesPerSlope = 2;	// Number of terraces connecting hexes with different heights

	public const int TerraceSteps = TerracesPerSlope * 2 + 1;

	public const float HorizontalTerraceStepSize = 1f / TerraceSteps;

	public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);
	
	public static Texture2D NoiseSource; // Texture with noise pattern for sampling, faster than sampling Perlin noise
	
	public const float CellPerturbStrength = 4f; // Multiplier used for perturbing mesh vertices with perlin noise in the X and Z axis

	public const float ElevationPerturbStrength = 1.5f; // Used for perturb in the Y axis
	
	public const float NoiseScale = 0.003f; // Scale down noise to avoid tiling the noise texture too much and loosing locality
	
	public const int ChunkSizeX = 5, ChunkSizeZ = 5;

	public const float StreamBedElevationOffset = -1.75f; // Offset in elevation for river beds
	
	public const float WaterElevationOffset = -0.5f; // Offset in elevation for water surface (river and water generally)
	
	public static int WrapSize;

	public static bool Wrapping {
		get {
			return WrapSize > 0;
		}
	}
	
	/// <summary>
	/// Get the first outer corner of the hexagon for the given direction (clockwise).
	/// </summary>
	public static Vector3 GetFirstCorner (HexDirection direction) {
		return Corners[(int)direction];
	}

	/// <summary>
	/// Get the second outer corner of the hexagon for the given direction (clockwise).
	/// </summary>
	public static Vector3 GetSecondCorner (HexDirection direction) {
		return Corners[(int)direction + 1];
	}

	/// <summary>
	/// Get the first corner of the hexagon's core (clockwise).
	/// </summary>
	public static Vector3 GetFirstSolidCorner (HexDirection direction) {
		return Corners[(int)direction] * SolidFactor;
	}

	/// <summary>
	/// Get the second corner of the hexagon's core (clockwise).
	/// </summary>
	public static Vector3 GetSecondSolidCorner (HexDirection direction) {
		return Corners[(int)direction + 1] * SolidFactor;
	}
	
	/// <summary>
	/// Get the first corner of the water surface hexagon's core (clockwise).
	/// </summary>
	public static Vector3 GetFirstWaterCorner (HexDirection direction) {
		return Corners[(int)direction] * WaterFactor;
	}

	/// <summary>
	/// Get the second corner of the water surface hexagon's core (clockwise).
	/// </summary>
	public static Vector3 GetSecondWaterCorner (HexDirection direction) {
		return Corners[(int)direction + 1] * WaterFactor;
	}

	/// <summary>
	/// Gets the bridge size. The bridge is the quad that connects two hexagon cores.
	/// </summary>
	public static Vector3 GetBridge (HexDirection direction) {
		return (Corners[(int)direction] + Corners[(int)direction + 1]) * BlendFactor;
	}
	
	/// <summary>
	/// Gets the water bridge size. The bridge is the quad that connects two hexagon cores.
	/// </summary>
	public static Vector3 GetWaterBridge (HexDirection direction) {
		return (Corners[(int)direction] + Corners[(int)direction + 1]) * WaterBlendFactor;
	}
	
	/// <summary>
	/// Get the middle point of the edge dividing the solid and blend regions of the hex.
	/// </summary>
	public static Vector3 GetSolidEdgeMiddle (HexDirection direction) {
		return (Corners[(int)direction] + Corners[(int)direction + 1]) * (0.5f * SolidFactor);
	}

	/// <summary>
	/// Given two oposite vertices of a bridge returns the vertex position at the given terrace step.
	/// </summary>
	public static Vector3 TerraceLerp (Vector3 a, Vector3 b, int step) {
		// * Horizontal interpolation
		float h = step * HexMetrics.HorizontalTerraceStepSize;
		a.x += (b.x - a.x) * h;
		a.z += (b.z - a.z) * h;

		// * Vertical interpolation. (step+1)/2 guarantees only increase in y in odd steps
		// * this is necessary to correctly form the terraces
		float v = ((step + 1) / 2) * HexMetrics.VerticalTerraceStepSize;
		a.y += (b.y - a.y) * v;

		return a;
	}

	/// <summary>
	/// Given the two colors of a bridge, returns color at given step.
	/// </summary>
	public static Color TerraceLerp (Color a, Color b, int step) {
		float h = step * HexMetrics.HorizontalTerraceStepSize;
		return Color.Lerp(a, b, h);
	}

	/// <summary>
	/// Given two elevations returns the edge type.
	/// </summary>
	public static HexEdgeType GetEdgeType (int elevation1, int elevation2) {
		if (elevation1 == elevation2) {
			return HexEdgeType.Flat;
		}
		int delta = elevation2 - elevation1;
		if (delta == 1 || delta == -1) {
			return HexEdgeType.Slope;
		}
		return HexEdgeType.Cliff;
	}
	
	/// <summary>
	/// Returns the given vector modified by sampling perlin noise. Y axis is not afected.
	/// </summary>
	public static Vector3 Perturb (Vector3 position) {
		Vector4 sample = SampleNoise(position);
		// Y is not perturbed to allow for plane centers
		position.x += (sample.x * 2f - 1f) * CellPerturbStrength;
		position.z += (sample.z * 2f - 1f) * CellPerturbStrength;
		return position;
	}

	/// <summary>
	/// Returns 4 channels of Perlin noise.
	/// </summary>
	public static Vector4 SampleNoise (Vector3 position) {
		Vector4 sample = NoiseSource.GetPixelBilinear(
			position.x * NoiseScale,
			position.z * NoiseScale
		);

		if (Wrapping && position.x < InnerDiameter * 1.5f) {
			Vector4 sample2 = NoiseSource.GetPixelBilinear(
				(position.x + WrapSize * InnerDiameter) * NoiseScale,
				position.z * NoiseScale
			);

			sample = Vector4.Lerp(sample2, sample, position.x * (1f / InnerDiameter) - 0.5f);
		}

		return sample;
		// * NoiseScale is used because bilinear sampling is made with UV coordinates [0,1]
		// * If a scaling was not applied the sampling position would be restricted to that range
		// * and would tile the texture with any value above 1 loosing perlin noise localization. 
		// * By scaling the position down we effectivly scale up our sampling space.
	}

	// FEATURES ------------------------------------------------------------------------------

	public const float WallHeight = 4f;
	public const float WallThickness = 0.75f;
	public const float WallElevationOffset = VerticalTerraceStepSize;
	public const float WallYOffset = -1f; // How burried on the ground walls should be
	public const float BridgeDesignLength = 7f; // Standard bridge length

	public static Vector3 WallThicknessOffset (Vector3 near, Vector3 far) {
		Vector3 offset;
		offset.x = far.x - near.x;
		offset.y = 0f;
		offset.z = far.z - near.z;
		return offset.normalized * (WallThickness * 0.5f);
	}

	/// <summary>
	/// Gets the positioning of the wall segment. Used primarily to offset walls downwards
	/// and avoid gaps in terraces.
	/// </summary>
	public static Vector3 WallLerp (Vector3 near, Vector3 far) {
		near.x += (far.x - near.x) * 0.5f;
		near.z += (far.z - near.z) * 0.5f;
		float v = near.y < far.y ? WallElevationOffset : (1f - WallElevationOffset);
		near.y += (far.y - near.y) * v + WallYOffset;
		return near;
	}

	public const int HashGridSize = 256;
	static HexHash[] HashGrid;
	public const float HashGridScale = 0.25f;

	static float[][] featureThresholds = {
		new float[] {0.0f, 0.0f, 0.4f},
		new float[] {0.0f, 0.4f, 0.6f},
		new float[] {0.4f, 0.6f, 0.8f}
	};

	public static void InitializeHashGrid (int seed) {
		HashGrid = new HexHash[HashGridSize * HashGridSize];
		Random.State currentState = Random.state;
		Random.InitState(seed);
		for (int i = 0; i < HashGrid.Length; i++) {
			HashGrid[i] = HexHash.Create();
		}
		Random.state = currentState;
	}

	public static HexHash SampleHashGrid (Vector3 position) {
		int x = (int)(position.x * HashGridScale) % HashGridSize;
		if (x < 0) { x += HashGridSize; }
		int z = (int)(position.z * HashGridScale) % HashGridSize;
		if (z < 0) { z += HashGridSize; }
		return HashGrid[x + z * HashGridSize];
	}

	public static float[] GetFeatureThresholds (int level) {
		return featureThresholds[level];
	}

	// ------------------------------------------------------------------------------------

}