using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using ROTA.Memory;

public class HexGrid : MonoBehaviour {

	public static HexGrid INSTANCE = null;

	public int Seed;

	public int CellCountX = 20, CellCountZ = 15;
	int ChunkCountX, ChunkCountZ;

	public HexCell CellPrefab;
    public Text CellLabelPrefab;
	public HexGridChunk ChunkPrefab;

	HexGridChunk[] Chunks;
    HexCell[] Cells;

	public bool Wrapping;
	Transform[] Columns;
	int CurrentCenterColumnIndex = -1;

// ! MOVEMENT ---------------------------------

	HexCellPriorityQueue searchFrontier;
	int searchFrontierPhase;
	HexCell currentPathFrom, currentPathTo;
	bool currentPathExists;

	public bool HasPath { get { return currentPathExists; } }

// ! UNITS ------------------------------------

	public HexUnit unitPrefab;

	List<HexUnit> units = new List<HexUnit>();

	void ClearUnits () {
		for (int i = 0; i < units.Count; i++) {
			units[i].Die();
		}
		units.Clear();
	}

	public void AddUnit (HexUnit unit, HexCell location, float orientation) {
		units.Add(unit);
		unit.Grid = this;
		unit.Location = location;
		unit.Orientation = orientation;
	}

	public void RemoveUnit (HexUnit unit) {
		units.Remove(unit);
		unit.Die();
	}

// ! FOG OF WAR ------------------------------------

	HexCellShaderData cellShaderData;

	// Used for the different visibility with different heights recalculation
	public void ResetVisibility () {
		for (int i = 0; i < Cells.Length; i++) {
			Cells[i].ResetVisibility();
		}

		for (int i = 0; i < units.Count; i++) {
			HexUnit unit = units[i];
			IncreaseVisibility(unit.Location, unit.VisionRange);
		}
	}

// ! ------------------------------------------	


	public Texture2D NoiseSource; // ! This should not be here, load it directly in HexMetrics later

	/// <summary>
	/// Here to make sure this survives recompiles.
	/// </summary>
	void OnEnable () {
		INSTANCE = this;

		if ( ! HexMetrics.NoiseSource) {
			HexMetrics.NoiseSource = NoiseSource;
			HexMetrics.InitializeHashGrid(Seed);
			HexUnit.unitPrefab = unitPrefab;
			HexMetrics.WrapSize = Wrapping ? CellCountX : 0;
			ResetVisibility();
		}
	}

	void Awake () {
		HexMetrics.NoiseSource = NoiseSource;
		HexMetrics.InitializeHashGrid(Seed);
		HexUnit.unitPrefab = unitPrefab;
		cellShaderData = gameObject.AddComponent<HexCellShaderData>();
		cellShaderData.Grid = this;

		CreateMap(CellCountX, CellCountZ, Wrapping);
	}

	/// <summary>
	/// Returns true if map was created with specifield dimensions, false if otherwise.
	/// </summary>
	public bool CreateMap (int x, int z, bool wrapping) {

		ClearPath(); // Clear any pathfinding highlighting
		ClearUnits(); // Clear Units

		// Sanity Check
		if (x <= 0 || x % HexMetrics.ChunkSizeX != 0 || z <= 0 || z % HexMetrics.ChunkSizeZ != 0) {
			Debug.LogError("Unsupported map size " + x + " x " + z + " .");
			return false;
		}

		// If defined clear old data, columns and their chunks
		if (Columns != null) {
			for (int i = 0; i < Columns.Length; i++) {
				Destroy(Columns[i].gameObject);
			}
		}

		CellCountX = x;
		CellCountZ = z;

		Wrapping = wrapping;
		HexMetrics.WrapSize = wrapping ? CellCountX : 0;
		CurrentCenterColumnIndex = -1;

		ChunkCountX = CellCountX / HexMetrics.ChunkSizeX;
		ChunkCountZ = CellCountZ / HexMetrics.ChunkSizeZ;

		cellShaderData.Initialize(CellCountX, CellCountZ);
		CreateChunks();
		CreateCells();

		HexMapCamera.ValidatePosition();

		return true;
	}

	void CreateChunks () {

		Columns = new Transform[ChunkCountX];
		for (int x = 0; x < ChunkCountX; x++) {
			Columns[x] = new GameObject("Column").transform;
			Columns[x].SetParent(transform, false);
		}

		Chunks = new HexGridChunk[ChunkCountX * ChunkCountZ];

		for (int z = 0, i = 0; z < ChunkCountZ; z++) {
			for (int x = 0; x < ChunkCountX; x++) {
				HexGridChunk chunk = Chunks[i++] = Instantiate(ChunkPrefab);
				chunk.transform.SetParent(Columns[x], false);
			}
		}
	}

	void CreateCells () {
		Cells = new HexCell[CellCountZ * CellCountX];

		for (int z = 0, i = 0; z < CellCountZ; z++) {
			for (int x = 0; x < CellCountX; x++) {
				CreateCell(x, z, i++);
			}
		}
	}

	/// <summary>
	/// Gets the cell at the given position.
	/// </summary>
	public HexCell GetCell (Vector3 position) {
		position = transform.InverseTransformPoint(position);
		HexCoordinates coordinates = HexCoordinates.FromPosition(position);

		return GetCell(coordinates);
	}

	/// <summary>
	/// Gets the cell at the given hex grid coordinates.
	/// </summary>
	public HexCell GetCell (HexCoordinates coordinates) {
		int z = coordinates.Z;
		if (z < 0 || z >= CellCountZ) {
			return null;
		}
		int x = coordinates.X + z / 2;
		if (x < 0 || x >= CellCountX) {
			return null;
		}
		return Cells[x + z * CellCountX];
	}

	/// <summary>
	/// Get the cell hit by the given ray.
	/// </summary>
	public HexCell GetCell (Ray ray) {
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit)) {
			return GetCell(hit.point);
		}
		return null;
	}

	/// <summary>
	/// Get a cell by its classic tilemap X and Z coordinates.
	/// </summary>
	public HexCell GetCell (int xOffset, int zOffset) {
		return Cells[xOffset + zOffset * CellCountX];
	}
	
	/// <summary>
	/// Get a cell by its direct index in the Hex Grid cells array.
	/// </summary>
	public HexCell GetCell (int cellIndex) {
		return Cells[cellIndex];
	}
	
	void CreateCell (int x, int z, int i) {

		Vector3 position;
		position.x = (x + z * 0.5f - z / 2) * HexMetrics.InnerDiameter;
		position.y = 0f;
		position.z = z * (HexMetrics.OuterRadius * 1.5f);

        // * Create HexCells (prefabs)
		HexCell cell = Cells[i] = Instantiate<HexCell>(CellPrefab);
		cell.transform.localPosition = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.Index = i;
		cell.ColumnIndex = x / HexMetrics.ChunkSizeX;
		cell.ShaderData = cellShaderData;

		// Cells at map edge cannot be explored
		if (Wrapping) {
			// Hide N and S edges, wrapping is always E-W
			cell.Explorable = z > 0 && z < CellCountZ - 1;
		}
		else {
			// When not wrapping hide E and W edges also
			cell.Explorable = x > 0 && z > 0 && x < CellCountX - 1 && z < CellCountZ - 1;
		}
		
		// * Create neighborhood relationships
		if (x > 0) { 
			// Make the East-West connection
			cell.SetNeighbor(HexDirection.W, Cells[i - 1]);

			if (Wrapping && x == CellCountX - 1) {
				cell.SetNeighbor(HexDirection.E, Cells[i - x]);
			}
		}
		if (z > 0) {
			if ((z & 1) == 0) { // On EVEN rows
				// Make the SE-NW connection
				cell.SetNeighbor(HexDirection.SE, Cells[i - CellCountX]);
				if (x > 0) {
					// Make the SW-NE connection
					cell.SetNeighbor(HexDirection.SW, Cells[i - CellCountX - 1]);
				}
				else if (Wrapping) {
					cell.SetNeighbor(HexDirection.SW, Cells[i - 1]);
				}
			}
			else { // On ODD rows
				// Make the SW-NE connection
				cell.SetNeighbor(HexDirection.SW, Cells[i - CellCountX]);
				if (x < CellCountX - 1) {
					// Make the SE-NW connection
					cell.SetNeighbor(HexDirection.SE, Cells[i - CellCountX + 1]);
				}
				else if (Wrapping) {
					cell.SetNeighbor(HexDirection.SE, Cells[i - CellCountX * 2 + 1]);
				}
			}
		}

        // * Create HexCell coordinates text
        Text label = Instantiate<Text>(CellLabelPrefab);
		label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
		cell.UiRect = label.rectTransform;

		cell.Elevation = 0; // Guarantees that noise is applied to elevation (see Elevation set method)

		AddCellToChunk(x, z, cell);
	}

	void AddCellToChunk (int x, int z, HexCell cell) {
		int chunkX = x / HexMetrics.ChunkSizeX;
		int chunkZ = z / HexMetrics.ChunkSizeZ;
		HexGridChunk chunk = Chunks[chunkX + chunkZ * ChunkCountX];

		int localX = x - chunkX * HexMetrics.ChunkSizeX;
		int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
		chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);
	}

	/// <summary>
	/// Toggle cell labels.
	/// </summary>
	public void ShowUI (bool visible) {
		for (int i = 0; i < Chunks.Length; i++) {
			Chunks[i].ShowUI(visible);
		}
	}

	public void ClearPath () {
		if (currentPathExists) {
			HexCell current = currentPathTo;
			while (current != currentPathFrom) {
				current.SetLabel(null);
				current.DisableHighlight();
				current = current.PathFrom;
			}
			current.DisableHighlight();
			currentPathExists = false;
		}
		else if (currentPathFrom) {
			currentPathFrom.DisableHighlight();
			currentPathTo.DisableHighlight();
		}
		currentPathFrom = currentPathTo = null;
	}

	List<HexCell> GetVisibleCells (HexCell fromCell, int range) {
		List<HexCell> visibleCells = ListPool<HexCell>.GLGet();

		searchFrontierPhase += 2;
		if (searchFrontier == null) {
			searchFrontier = new HexCellPriorityQueue();
		}
		else {
			searchFrontier.Clear();
		}

		// Units view range (Line of Sight) increase by altitude of cell they are in
		range += fromCell.ViewElevation;

		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;
		searchFrontier.Enqueue(fromCell);

		HexCoordinates fromCoordinates = fromCell.Coordinates;
		while (searchFrontier.Count > 0) {
			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;
			visibleCells.Add(current);

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = current.GetNeighbor(d);
				if (
					neighbor == null ||
					neighbor.SearchPhase > searchFrontierPhase
				) {
					continue;
				}

				int distance = current.Distance + 1;
				if (distance + neighbor.ViewElevation > range ||
					distance > fromCoordinates.DistanceTo(neighbor.Coordinates)
					|| ! neighbor.Explorable) {
					// Take neighbor view elevation into acount when comparing with range
					// Tall cells should block vision this way
					// Also ignore cells farther away than the shortest path to them
					// If we didnt do this then when high cells blocked vision we would sometimes
					// Be able to see around them, as if our vision could bend corners, because although
					// Higher cells blocked vision the searching algorithm still found a path to them
					// Also cells that cannot be explored should block vision
					continue;
				}

				if (neighbor.SearchPhase < searchFrontierPhase) {
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = distance;
					neighbor.SearchHeuristic = 0;
					searchFrontier.Enqueue(neighbor);
				}
				else if (distance < neighbor.Distance) {
					int oldPriority = neighbor.SearchPriority;
					neighbor.Distance = distance;
					searchFrontier.Change(neighbor, oldPriority);
				}
			}
		}
		return visibleCells;
	}

	public void IncreaseVisibility (HexCell fromCell, int range) {
		List<HexCell> cells = GetVisibleCells(fromCell, range);
		for (int i = 0; i < cells.Count; i++) {
			cells[i].IncreaseVisibility();
		}
		ListPool<HexCell>.GLRestore(cells);
	}

	public void DecreaseVisibility (HexCell fromCell, int range) {
		List<HexCell> cells = GetVisibleCells(fromCell, range);
		for (int i = 0; i < cells.Count; i++) {
			cells[i].DecreaseVisibility();
		}
		ListPool<HexCell>.GLRestore(cells);
	}

	public void Save (BinaryWriter writer) {
		// Save Map Size
		writer.Write(CellCountX);
		writer.Write(CellCountZ);

		writer.Write(Wrapping);

		for (int i = 0; i < Cells.Length; i++) {
			Cells[i].Save(writer);
		}

		writer.Write(units.Count);
		for (int i = 0; i < units.Count; i++) {
			units[i].Save(writer);
		}
	}

	public void Load (BinaryReader reader) {

		ClearPath(); // Clear any pathfinding highlighting
		ClearUnits(); // Clear Units

		// Read map size
		int x = reader.ReadInt32(); 
		int z = reader.ReadInt32();

		Wrapping = reader.ReadBoolean();

		// If map different size then create new map else just overwrite old cell data
		if (x != CellCountX || z != CellCountZ) {
			if ( ! CreateMap(x, z, Wrapping)) {
				return;
			}
		}

		bool originalImmediateMode = cellShaderData.ImmediateMode;
		// Avoid lag of transitioning the whole map to visible
		// If cells were already visible before then no transition needed now that we load the map
		cellShaderData.ImmediateMode = true; 

		for (int i = 0; i < Cells.Length; i++) {
			Cells[i].Load(reader);
		}

		for (int i = 0; i < Chunks.Length; i++) {
			Chunks[i].Refresh();
		}

		int unitCount = reader.ReadInt32();
		for (int i = 0; i < unitCount; i++) {
			HexUnit.Load(reader, this);
		}

		cellShaderData.ImmediateMode = originalImmediateMode; // Restore the option
	}

	public void CenterMap (float xPosition) {
		int centerColumnIndex = (int) (xPosition / (HexMetrics.InnerDiameter * HexMetrics.ChunkSizeX));
		
		if (centerColumnIndex == CurrentCenterColumnIndex) {
			return;
		}

		CurrentCenterColumnIndex = centerColumnIndex;

		// When there is an even number of columns there is a 1 columns bias
		int minColumnIndex = centerColumnIndex - ChunkCountX / 2;
		int maxColumnIndex = centerColumnIndex + ChunkCountX / 2;

		Vector3 position;
		position.y = position.z = 0f;
		for (int i = 0; i < Columns.Length; i++) {
			if (i < minColumnIndex) {
				position.x = ChunkCountX * (HexMetrics.InnerDiameter * HexMetrics.ChunkSizeX);
			}
			else if (i > maxColumnIndex) {
				position.x = ChunkCountX * -(HexMetrics.InnerDiameter * HexMetrics.ChunkSizeX);
			}
			else {
				position.x = 0f;
			}

			Columns[i].localPosition = position;
		}
	}

	// ! Used to make units change column so they correctly follow map warpping
	public void MakeChildOfColumn (Transform child, int columnIndex) {
		child.SetParent(Columns[columnIndex], false);
	}

}
