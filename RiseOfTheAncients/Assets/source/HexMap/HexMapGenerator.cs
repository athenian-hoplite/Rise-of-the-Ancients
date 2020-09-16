using System.Collections.Generic;
using ROTA.Memory;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour {

// * Map Generation Parameters ------------------------------

	[Range(0f, 0.5f)]
	public float JitterProbability = 0.25f; // Used to randomize terrain chunks in RaiseTerrain

    [Range(20, 200)]
	public int ChunkSizeMin = 30; // Minimum randomly generated land chunk

	[Range(20, 200)]
	public int ChunkSizeMax = 100; // Maximum randomly generated land chunk

    [Range(5, 95)]
	public int LandPercentage = 50;

    [Range(1, 5)]
	public int WaterLevel = 3; // Sea level of the map

    [Range(0f, 1f)]
	public float HighRiseProbability = 0.25f; // Cliff chunks probability

    [Range(0f, 0.4f)]
	public float SinkProbability = 0.2f; // Sink chunks probability

    [Range(-4, 0)]
	public int ElevationMinimum = -2;

	[Range(6, 10)]
	public int ElevationMaximum = 8;

    [Range(0, 10)]
	public int MapBorderX = 5; // Margins

	[Range(0, 10)]
	public int MapBorderZ = 5; // Margins

    [Range(0, 10)]
	public int RegionBorder = 5;

    [Range(1, 4)]
	public int RegionCount = 1; // Number of regions

    [Range(0, 100)]
	public int ErosionPercentage = 50;

	[Range(0f, 1f)]
	public float EvaporationFactor = 0.5f; // Percentage of clouds lost

	[Range(0f, 1f)]
	public float PrecipitationFactor = 0.25f; // Percentage of clouds lost inland

	[Range(0f, 1f)]
	public float RunoffFactor = 0.25f; // Vertical spread of moisture

	[Range(0f, 1f)]
	public float SeepageFactor = 0.125f; // Horizontal spread of moisture

	public HexDirection WindDirection = HexDirection.NW; // Global prevailing wind, doesnt make sense to be only 1

	[Range(1f, 10f)]
	public float WindStrength = 4f;

	[Range(0f, 1f)]
	public float StartingMoisture = 0.1f;

	[Range(0, 20)]
	public int RiverPercentage = 10;

	[Range(0f, 1f)]
	public float ExtraLakeProbability = 0.25f;

	[Range(0f, 1f)]
	public float LowTemperature = 0f;

	[Range(0f, 1f)]
	public float HighTemperature = 1f;

	public enum HemisphereMode {
		Both, North, South
	}

	public HemisphereMode Hemisphere = HemisphereMode.Both;

	[Range(0f, 1f)]
	public float TemperatureJitter = 0.1f;

	int TemperatureJitterChannel; // Which noise channel should be used for temperature noise

    public int Seed;

    public bool UseFixedSeed; // If the seed field should be used or else one will be generated randomly

// * BIOMES --------------------------------------------------------

	static float[] TemperatureBands = { 0.1f, 0.3f, 0.6f };
					
	static float[] MoistureBands = { 0.12f, 0.28f, 0.85f };

	struct Biome {
		public int terrain, plant;

		public Biome (int terrain, int plant) {
			this.terrain = terrain;
			this.plant = plant;
		}
	}

	static Biome[] biomes = {
		new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0),
		new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2),
		new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2),
		new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3)
	};

// * --------------------------------------------------------

    struct MapRegion {
		public int xMin, xMax, zMin, zMax;
	}

	struct ClimateData {
		public float clouds, moisture;
	}

	List<MapRegion> regions;

	// The two lists that hold climate data so that it can be computed in parallel
	// Meaning calculation in one pass only take effect in the next pass, essentially functioning as a double buffer
	List<ClimateData> climate = new List<ClimateData>();
	List<ClimateData> nextClimate = new List<ClimateData>();

	// Used in create river
	List<HexDirection> FlowDirections = new List<HexDirection>();

	public HexGrid grid;
    int cellCount, landCells;

    HexCellPriorityQueue searchFrontier;
	int searchFrontierPhase;

	public void GenerateMap (int x, int z, bool wrapping) {

        Random.State originalRandomState = Random.state;

        if ( ! UseFixedSeed) {
            Seed = Random.Range(0, int.MaxValue);
            Seed ^= (int)System.DateTime.Now.Ticks;
            Seed ^= (int)Time.unscaledTime;
            Seed &= int.MaxValue; // Make positive
        }
		Random.InitState(Seed);

        cellCount = x * z;
		grid.CreateMap(x, z, wrapping);

        if (searchFrontier == null) {
			searchFrontier = new HexCellPriorityQueue();
		}

        for (int i = 0; i < cellCount; i++) {
			grid.GetCell(i).WaterLevel = WaterLevel;
		}

        CreateRegions();
        CreateLand();
        ErodeLand();
		CreateClimate();
		CreateRivers();
        SetTerrainType();

        // ! Pathfinding needs a rework ofc
        // Pathfinding uses cell search phase so reset at the end
        for (int i = 0; i < cellCount; i++) {
			grid.GetCell(i).SearchPhase = 0;
		}

        Random.state = originalRandomState;
	}

    void CreateRegions () {
		if (regions == null) {
			regions = new List<MapRegion>();
		}
		else {
			regions.Clear();
		}

        // Randomly Split the map into vertical or horizontal regions
		// * MapBorderX only makes sense when not wrapping
		int borderX = grid.Wrapping ? RegionBorder : MapBorderX;
		MapRegion region;
        switch (RegionCount) {

            default:
				// No reason to separate when there is only 1 region
				if (grid.Wrapping) {
					borderX = 0;
				}
                region.xMin = borderX;
                region.xMax = grid.CellCountX - borderX;
                region.zMin = MapBorderZ;
                region.zMax = grid.CellCountZ - MapBorderZ;
                regions.Add(region);
            break;

            case 2:
                if (Random.value < 0.5f) {
                    region.xMin = borderX;
                    region.xMax = grid.CellCountX / 2 - RegionBorder;
                    region.zMin = MapBorderZ;
                    region.zMax = grid.CellCountZ - MapBorderZ;
                    regions.Add(region);
                    region.xMin = grid.CellCountX / 2 + RegionBorder;
                    region.xMax = grid.CellCountX - borderX;
                    regions.Add(region);
                }
                else {
					// No reason to separate when there are 2 regions NORTH-SOUTH
					if (grid.Wrapping) {
						borderX = 0;
					}
                    region.xMin = borderX;
                    region.xMax = grid.CellCountX - borderX;
                    region.zMin = MapBorderZ;
                    region.zMax = grid.CellCountZ / 2 - RegionBorder;
                    regions.Add(region);
                    region.zMin = grid.CellCountZ / 2 + RegionBorder;
                    region.zMax = grid.CellCountZ - MapBorderZ;
                    regions.Add(region);
                }
            break;

            case 3:
                region.xMin = borderX;
                region.xMax = grid.CellCountX / 3 - RegionBorder;
                region.zMin = MapBorderZ;
                region.zMax = grid.CellCountZ - MapBorderZ;
                regions.Add(region);
                region.xMin = grid.CellCountX / 3 + RegionBorder;
                region.xMax = grid.CellCountX * 2 / 3 - RegionBorder;
                regions.Add(region);
                region.xMin = grid.CellCountX * 2 / 3 + RegionBorder;
                region.xMax = grid.CellCountX - borderX;
                regions.Add(region);
			break;

            case 4:
                region.xMin = borderX;
                region.xMax = grid.CellCountX / 2 - RegionBorder;
                region.zMin = MapBorderZ;
                region.zMax = grid.CellCountZ / 2 - RegionBorder;
                regions.Add(region);
                region.xMin = grid.CellCountX / 2 + RegionBorder;
                region.xMax = grid.CellCountX - borderX;
                regions.Add(region);
                region.zMin = grid.CellCountZ / 2 + RegionBorder;
                region.zMax = grid.CellCountZ - MapBorderZ;
                regions.Add(region);
                region.xMin = borderX;
                region.xMax = grid.CellCountX / 2 - RegionBorder;
                regions.Add(region);
			break;

		}
	}

    void CreateLand () {
		int landBudget = Mathf.RoundToInt(cellCount * LandPercentage * 0.01f);
		landCells = landBudget;

        // Guard against impossible setups. 10k iterations max or bail
        for (int guard = 0; guard < 10000; guard++) {

            // To distribute sinking equally apply to all regions
            bool sink = Random.value < SinkProbability;

            for (int i = 0; i < regions.Count; i++) {

                MapRegion region = regions[i];

                int chunkSize = Random.Range(ChunkSizeMin, ChunkSizeMax + 1);
                if (sink) {
					landBudget = SinkTerrain(chunkSize, landBudget, region);
				}
				else {
					landBudget = RaiseTerrain(chunkSize, landBudget, region);
                    if (landBudget == 0) {
						return;
					}
				}
            }
		}

        if (landBudget > 0) {
			Debug.LogWarning("Failed to use up " + landBudget + " land budget.");
			landCells -= landBudget;
		}
	}

    int RaiseTerrain (int chunkSize, int budget, MapRegion region) {
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell(region);
		firstCell.SearchPhase = searchFrontierPhase;
		firstCell.Distance = 0;
		firstCell.SearchHeuristic = 0;
		searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.Coordinates;

        int rise = Random.value < HighRiseProbability ? 2 : 1;
        int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0) {

			HexCell current = searchFrontier.Dequeue();
            int originalElevation = current.Elevation;

            int newElevation = originalElevation + rise;
			if (newElevation > ElevationMaximum) { // Avoid already too high areas
				continue;
			}

            current.Elevation = newElevation;

            // If this cell just became land
            if (originalElevation < WaterLevel && newElevation >= WaterLevel && --budget == 0) {
				break; // If land budget over then bail
			}

			size += 1;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor && neighbor.SearchPhase < searchFrontierPhase) {
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
                    // Randomly increase the cost estimation to jitter the terrain chunk
					neighbor.SearchHeuristic = Random.value < JitterProbability ? 1: 0;
					searchFrontier.Enqueue(neighbor);
				}
			}
		}
		searchFrontier.Clear();

        return budget;
	}

    int SinkTerrain (int chunkSize, int budget, MapRegion region) {
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell(region);
		firstCell.SearchPhase = searchFrontierPhase;
		firstCell.Distance = 0;
		firstCell.SearchHeuristic = 0;
		searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.Coordinates;

        int sink = Random.value < HighRiseProbability ? 2 : 1;
        int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0) {

			HexCell current = searchFrontier.Dequeue();
            int originalElevation = current.Elevation;
            int newElevation = current.Elevation - sink;
			if (newElevation < ElevationMinimum) { // Avoid already too low areas
				continue;
			}
            current.Elevation = newElevation;

            if (originalElevation >= WaterLevel && newElevation < WaterLevel) {
				budget += 1;
			}

			size += 1;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor && neighbor.SearchPhase < searchFrontierPhase) {
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
                    // Randomly increase the cost estimation to jitter the terrain chunk
					neighbor.SearchHeuristic = Random.value < JitterProbability ? 1: 0;
					searchFrontier.Enqueue(neighbor);
				}
			}
		}
		searchFrontier.Clear();

        return budget;
	}

    void ErodeLand () {
        List<HexCell> erodibleCells = ListPool<HexCell>.GLGet();
		for (int i = 0; i < cellCount; i++) {
			HexCell cell = grid.GetCell(i);
			if (IsErodible(cell)) {
				erodibleCells.Add(cell);
			}
		}

        int targetErodibleCount = (int)(erodibleCells.Count * (100 - ErosionPercentage) * 0.01f);

        while (erodibleCells.Count > targetErodibleCount) {
			int index = Random.Range(0, erodibleCells.Count);

			HexCell cell = erodibleCells[index];
            HexCell targetCell = GetErosionTarget(cell);

			cell.Elevation -= 1;
            targetCell.Elevation += 1;

            // Faster than removing in the middle of the list
			if ( ! IsErodible(cell)) {
				erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
				erodibleCells.RemoveAt(erodibleCells.Count - 1);
			}

            // Check if after erosion any neighbors are now erodable too
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = cell.GetNeighbor(d);
				if (
					neighbor && neighbor.Elevation == cell.Elevation + 2 &&
					! erodibleCells.Contains(neighbor)
				) {
					erodibleCells.Add(neighbor);
				}
			}

            if (IsErodible(targetCell) && ! erodibleCells.Contains(targetCell)) {
				erodibleCells.Add(targetCell);
			}

            // Check if erosion target neighbors stopped being erodable because of its increase in elevation
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = targetCell.GetNeighbor(d);
				if (
					neighbor && neighbor != cell && neighbor.Elevation == targetCell.Elevation + 1 && ! IsErodible(neighbor)
				) {
					erodibleCells.Remove(neighbor);
				}
			}

		}

		ListPool<HexCell>.GLRestore(erodibleCells);
    }

    bool IsErodible (HexCell cell) {
		int erodibleElevation = cell.Elevation - 2;
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			HexCell neighbor = cell.GetNeighbor(d);
			if (neighbor && neighbor.Elevation <= erodibleElevation) {
				return true;
			}
		}
		return false;
	}

    /// <summary>
    /// Finds a recipient for the given cells erosion deposits.
    /// </summary>
    HexCell GetErosionTarget (HexCell cell) {
		List<HexCell> candidates = ListPool<HexCell>.GLGet();
		int erodibleElevation = cell.Elevation - 2;
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			HexCell neighbor = cell.GetNeighbor(d);
			if (neighbor && neighbor.Elevation <= erodibleElevation) {
				candidates.Add(neighbor);
			}
		}
		HexCell target = candidates[Random.Range(0, candidates.Count)];
		ListPool<HexCell>.GLRestore(candidates);
		return target;
	}

    void SetTerrainType () {
		// Pick temperature noise channel at random
		TemperatureJitterChannel = Random.Range(0, 4);

		int rockDesertElevation = ElevationMaximum - (ElevationMaximum - WaterLevel) / 2;

		for (int i = 0; i < cellCount; i++) {
			HexCell cell = grid.GetCell(i);
			float temperature = DetermineTemperature(cell);
			float moisture = climate[i].moisture;

			if ( ! cell.IsUnderwater) { // On land
				int t = 0;
				for (; t < TemperatureBands.Length; t++) {
					if (temperature < TemperatureBands[t]) {
						break;
					}
				}
				int m = 0;
				for (; m < MoistureBands.Length; m++) {
					if (moisture < MoistureBands[m]) {
						break;
					}
				}
				Biome cellBiome = biomes[t * 4 + m];

				// When desert but high elevation turn into rock
				if (cellBiome.terrain == 0) {
					if (cell.Elevation >= rockDesertElevation) {
						cellBiome.terrain = 3;
					}
				}
				else if (cell.Elevation == ElevationMaximum) { // Snow
					cellBiome.terrain = 4;
				}

				if (cellBiome.terrain == 4) { // No plants in snow /doesnt make much sense
					cellBiome.plant = 0;
				}
				else if (cellBiome.plant < 3 && cell.HasRiver) { // More plants close to river
					cellBiome.plant += 1;
				}


				cell.TerrainTypeIndex = cellBiome.terrain;
				cell.PlantLevel = cellBiome.plant;
			}
			else { // Underwater
				int terrain;
				if (cell.Elevation == WaterLevel - 1) {
					int cliffs = 0, slopes = 0;
					// Detect lakes and inlets
					for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
						HexCell neighbor = cell.GetNeighbor(d);
						if (!neighbor) {
							continue;
						}
						int delta = neighbor.Elevation - cell.WaterLevel;
						if (delta == 0) {
							slopes += 1;
						}
						else if (delta > 0) {
							cliffs += 1;
						}
					}
					if (cliffs + slopes > 3) {
						terrain = 1;
					}
					else if (cliffs > 0) {
						terrain = 3;
					}
					else if (slopes > 0) {
						terrain = 0;
					}
					else {
						terrain = 1;
					}
				}
				else if (cell.Elevation >= WaterLevel) {
					terrain = 1;
				}
				else if (cell.Elevation < 0) {
					terrain = 3;
				}
				else {
					terrain = 2;
				}

				if (terrain == 1 && temperature < TemperatureBands[0]) {
					terrain = 2;
				}

				cell.TerrainTypeIndex = terrain;
			}
		}
	}

	HexCell GetRandomCell (MapRegion region) {
		return grid.GetCell(Random.Range(region.xMin, region.xMax), Random.Range(region.zMin, region.zMax));
	}

	void CreateClimate () {

		climate.Clear();
		nextClimate.Clear();

		ClimateData initialData = new ClimateData(); // Initial buffer
		initialData.moisture = StartingMoisture;
		ClimateData clearData = new ClimateData(); // Next buffer

		for (int i = 0; i < cellCount; i++) {
			climate.Add(initialData);
			nextClimate.Add(clearData);
		}

		// * Number of water cycles is 40, the more cycles the more refined the result
		for (int cycle = 0; cycle < 40; cycle++) {
			for (int i = 0; i < cellCount; i++) {
				EvolveClimate(i);
			}

			List<ClimateData> swap = climate;
			climate = nextClimate;
			nextClimate = swap;
		}
	}

	void EvolveClimate (int cellIndex) {
		HexCell cell = grid.GetCell(cellIndex);
		ClimateData cellClimate = climate[cellIndex];
		
		if (cell.IsUnderwater) {
			cellClimate.moisture = 1f;
			cellClimate.clouds += EvaporationFactor;
		}
		else {
			float evaporation = cellClimate.moisture * EvaporationFactor;
			cellClimate.moisture -= evaporation;
			cellClimate.clouds += evaporation;
		}

		float precipitation = cellClimate.clouds * PrecipitationFactor;
		cellClimate.clouds -= precipitation;
		cellClimate.moisture += precipitation;

		// Clouds cant hold in higher elevation terrain.
		// Force precipitation when over limit
		float cloudMaximum = 1f - cell.ViewElevation / (ElevationMaximum + 1f);
		if (cellClimate.clouds > cloudMaximum) {
			cellClimate.moisture += cellClimate.clouds - cloudMaximum;
			cellClimate.clouds = cloudMaximum;
		}

		// Disperse clouds to neighbors and runnoff moisture to them aswell
		HexDirection mainDispersalDirection = WindDirection.Opposite();

		// When prevailing wind strength is 1 (minimum) clouds are divided equally among the 6 neighbors
		float cloudDispersal = cellClimate.clouds * (1f / (5f + WindStrength)); 

		float runoff = cellClimate.moisture * RunoffFactor * (1f / 6f);
		float seepage = cellClimate.moisture * SeepageFactor * (1f / 6f);

		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			HexCell neighbor = cell.GetNeighbor(d);
			if ( ! neighbor) {
				continue;
			}
			ClimateData neighborClimate = nextClimate[neighbor.Index]; // Influence the next pass not the current

			// Cloud dispersal into neighbor, more into the prevailing wind direction
			if (d == mainDispersalDirection) {
				neighborClimate.clouds += cloudDispersal * WindStrength;
			}
			else {
				neighborClimate.clouds += cloudDispersal;
			}

			// Runoff of moisture into lower elevation neighbors
			int elevationDelta = neighbor.ViewElevation - cell.ViewElevation; // View elevation takes into account water level
			if (elevationDelta < 0) {
				cellClimate.moisture -= runoff;
				neighborClimate.moisture += runoff;
			}
			else if (elevationDelta == 0) {
				cellClimate.moisture -= seepage;
				neighborClimate.moisture += seepage;
			}

			nextClimate[neighbor.Index] = neighborClimate;
		}

		ClimateData nextCellClimate = nextClimate[cellIndex];
		nextCellClimate.moisture += cellClimate.moisture;
		if (nextCellClimate.moisture > 1f) { // Clamp
			nextCellClimate.moisture = 1f;
		}
		nextClimate[cellIndex] = nextCellClimate;
		climate[cellIndex] = new ClimateData();
	}

	void CreateRivers () {
		List<HexCell> riverOrigins = ListPool<HexCell>.GLGet();
		for (int i = 0; i < cellCount; i++) {
			HexCell cell = grid.GetCell(i);
			if (cell.IsUnderwater) {
				continue;
			}
			ClimateData data = climate[i];
			float weight = data.moisture * (cell.Elevation - WaterLevel) / (ElevationMaximum - WaterLevel);
			if (weight > 0.75f) {
				riverOrigins.Add(cell);
				riverOrigins.Add(cell);
			}
			if (weight > 0.5f) {
				riverOrigins.Add(cell);
			}
			if (weight > 0.25f) {
				riverOrigins.Add(cell);
			}
		}

		int riverBudget = Mathf.RoundToInt(landCells * RiverPercentage * 0.01f);

		while (riverBudget > 0 && riverOrigins.Count > 0) {
			int index = Random.Range(0, riverOrigins.Count);
			int lastIndex = riverOrigins.Count - 1;
			HexCell origin = riverOrigins[index];
			riverOrigins[index] = riverOrigins[lastIndex];
			riverOrigins.RemoveAt(lastIndex);

			// Dont place river origins next to water or other rivers
			if ( ! origin.HasRiver) {
				bool isValidOrigin = true;
				for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
					HexCell neighbor = origin.GetNeighbor(d);
					if (neighbor && (neighbor.HasRiver || neighbor.IsUnderwater)) {
						isValidOrigin = false;
						break;
					}
				}
				if (isValidOrigin) {
					riverBudget -= CreateRiver(origin);
				}
			}
		}
		
		if (riverBudget > 0) {
			Debug.LogWarning("Failed to use up river budget.");
		}

		ListPool<HexCell>.GLRestore(riverOrigins);
	}

	int CreateRiver (HexCell origin) {
		int length = 1;
		HexCell cell = origin;
		HexDirection direction = HexDirection.NE;
		while ( ! cell.IsUnderwater) {

			int minNeighborElevation = int.MaxValue;
			FlowDirections.Clear();

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {

				HexCell neighbor = cell.GetNeighbor(d);
				if ( ! neighbor) {
					continue;
				}
				if (neighbor.Elevation < minNeighborElevation) {
					minNeighborElevation = neighbor.Elevation;
				}

				if (neighbor == origin || neighbor.HasIncomingRiver) {
					continue;
				}


				int delta = neighbor.Elevation - cell.Elevation;
				if (delta > 0) { // Only flow downhill
					continue;
				}

				if (delta < 0) { // If downhill add more times to make more likely.
					FlowDirections.Add(d);
					FlowDirections.Add(d);
					FlowDirections.Add(d);
				}

				// If other river them "merge" and bail
				if (neighbor.HasOutgoingRiver) {
					cell.SetOutgoingRiver(d);
					return length;
				}

				// Give more weight to non sharp turns
				if (length == 1 || (d != direction.Next2() && d != direction.Previous2())) {
					FlowDirections.Add(d);
				}

				FlowDirections.Add(d);
			}

			// If no direction to flow
			if (FlowDirections.Count == 0) {
				if (length == 1) { // If at river origin simply abort
					return 0;
				}

				if (minNeighborElevation >= cell.Elevation) { // Try to place lake at river end
					cell.WaterLevel = minNeighborElevation;
					if (minNeighborElevation == cell.Elevation) {
						cell.Elevation = minNeighborElevation - 1;
					}
				}
				break;
			}

			direction = FlowDirections[Random.Range(0, FlowDirections.Count)];
			cell.SetOutgoingRiver(direction);
			length += 1;

			// Extra lakes at random points in river
			if (minNeighborElevation >= cell.Elevation && Random.value < ExtraLakeProbability) {
				cell.WaterLevel = cell.Elevation;
				cell.Elevation -= 1;
			}

			cell = cell.GetNeighbor(direction);
		}
		return length;
	}

	float DetermineTemperature (HexCell cell) {
		// Calculate South hemisphere by default
		float latitude = (float)cell.Coordinates.Z / grid.CellCountZ; // [0,1]

		if (Hemisphere == HemisphereMode.Both) {
			latitude *= 2f;
			if (latitude > 1f) {
				latitude = 2f - latitude;
			}
		}
		else if (Hemisphere == HemisphereMode.North) {
			latitude = 1f - latitude;
		}

		// Temperature based on latitude
		float temperature = Mathf.LerpUnclamped(LowTemperature, HighTemperature, latitude);

		// Account for elevation
		temperature *= 1f - (cell.ViewElevation - WaterLevel) / (ElevationMaximum - WaterLevel + 1f);

		// Sample noise and scale down position to avoid rapid tiling, also select random noise channel
		float jitter = HexMetrics.SampleNoise(cell.Position * 0.1f)[TemperatureJitterChannel];


		// Add variation with noise (noise * 2 - 1 to make interval [0,1] become [-1,1] 
		// and so affect temperature both negatively and positively)
		temperature += (jitter * 2f - 1f) * TemperatureJitter;

		return temperature;
	}

}