using UnityEngine;
using UnityEngine.UI;
using System.IO;
using ROTA.Models;

public class HexCell : MonoBehaviour {

    public Vector3 Position {
		get {
			return this.transform.localPosition;
		}
	}

	public int ColumnIndex { get; set; }

	/// <summary>
	/// Elevation level of this cell.
	/// </summary>
    public int Elevation { // Elevation level 
		get {
			return elevation;
		}
		set {

            if (elevation == value) {
				return;
			}

			int originalViewElevation = ViewElevation;

			elevation = value;

			if (ViewElevation != originalViewElevation) {
				ShaderData.ViewElevationChanged();
			}

			RefreshPosition();
			// Enforce rules for rivers.
			ValidateRivers();

			// Enforce elevation rules for roads.
			for (int i = 0; i < Roads.Length; i++) {
				if (Roads[i] && GetElevationDifference((HexDirection)i) > 1) {
					SetRoad(i, false);
				}
			}

            Refresh(); // Update chunk (to update mesh)
		}
	}

	/// <summary>
	/// The water level at this cell.
	/// </summary>
	public int WaterLevel {
		get {
			return waterLevel;
		}
		set {
			if (waterLevel == value) {
				return;
			}
			int originalViewElevation = ViewElevation;

			waterLevel = value;
			
			if (ViewElevation != originalViewElevation) {
				ShaderData.ViewElevationChanged();
			}
			ValidateRivers();
			Refresh();
		}
	}

	/// <summary>
	/// Returns the vertical position of a river bed in this cell taking into account the cell's elevation.
	/// </summary>
	public float StreamBedY {
		get { return (elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep; }
	}

	/// <summary>
	/// Returns the vertical position of a river surface in this cell taking into account the cell's elevation.
	/// </summary>
	public float RiverSurfaceY {
		get { return (elevation + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep; }
	}

	public int TerrainTypeIndex {
		get {
			return terrainTypeIndex;
		}
		set {
			if (terrainTypeIndex != value) {
				terrainTypeIndex = value;
				ShaderData.RefreshTerrain(this);
			}
		}
	}

	public bool HasIncomingRiver { get { return hasIncomingRiver; } }

	public bool HasOutgoingRiver { get { return hasOutgoingRiver; } }

	public HexDirection IncomingRiver { get { return incomingRiver; } }

	public HexDirection OutgoingRiver { get { return outgoingRiver; } }

	public bool HasRiver { get { return hasIncomingRiver || hasOutgoingRiver; } }

	public bool HasRiverBeginOrEnd { get { return hasIncomingRiver != hasOutgoingRiver; } }

	public HexDirection RiverBeginOrEndDirection { get { return hasIncomingRiver ? incomingRiver : outgoingRiver; } }

	public bool HasRoads {
		get {
			for (int i = 0; i < Roads.Length; i++) {
				if (Roads[i]) {
					return true;
				}
			}
			return false;
		}
	}

	public bool IsUnderwater { get { return waterLevel > elevation; } }

	public float WaterSurfaceY { get { return (waterLevel + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep; } }

// ! FEATURES -----------------------------------------------------------------

	public int UrbanLevel {
		get {
			return urbanLevel;
		}
		set {
			if (urbanLevel != value) {
				urbanLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	public int FarmLevel {
		get {
			return farmLevel;
		}
		set {
			if (farmLevel != value) {
				farmLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	public int PlantLevel {
		get {
			return plantLevel;
		}
		set {
			if (plantLevel != value) {
				plantLevel = value;
				RefreshSelfOnly();
			}
		}
	}

	public bool Walled {
		get {
			return walled;
		}
		set {
			if (walled != value) {
				walled = value;
				Refresh();
			}
		}
	}

	public int SpecialIndex {
		get {
			return specialIndex;
		}
		set {
			if (specialIndex != value && ! HasRiver) {
				specialIndex = value;
				RemoveRoads();
				RefreshSelfOnly();
			}
		}
	}

	public bool IsSpecial {
		get {
			return specialIndex > 0;
		}
	}

	int terrainTypeIndex; // type of terrain
	int specialIndex;
	int urbanLevel, farmLevel, plantLevel;
	bool walled;

// ! MOVEMENT -----------------------------------------------------------------

	public HexCell PathFrom { get; set; }
	public int SearchHeuristic { get; set; }
	public int SearchPriority { get { return distance + SearchHeuristic; } }
	public HexCell NextWithSamePriority { get; set; }
	public int SearchPhase { get; set; }

	int distance;

	public int Distance {
		get {
			return distance;
		}
		set {
			distance = value;
		}
	}

	public void SetLabel (string text) {
		UnityEngine.UI.Text label = UiRect.GetComponent<Text>();
		label.text = text;
	}

	public void DisableHighlight () {
		Image highlight = UiRect.GetChild(0).GetComponent<Image>();
		highlight.enabled = false;
	}
	
	public void EnableHighlight (Color color) {
		Image highlight = UiRect.GetChild(0).GetComponent<Image>();
		highlight.color = color;
		highlight.enabled = true;
	}

// ! UNITS -----------------------------------------------------------------

	public HexUnit Unit { get; set; }

	public MapPawn pawn { get; set; }

// ! FOG OF WAR / TERRAIN TYPE -----------------------------------------------

	public HexCellShaderData ShaderData { get; set; }
	public int Index { get; set; }

	public bool IsVisible {
		get {
			return visibility > 0 && Explorable;
		}
	}

	public int ViewElevation {
		get {
			return elevation >= waterLevel ? elevation : waterLevel;
		}
	}

	public void IncreaseVisibility () {
		visibility += 1;
		if (visibility == 1) {
			IsExplored = true; // When viewed for the first time gets explored, just always set faster than check to set
			ShaderData.RefreshVisibility(this);
		}
	}

	public void DecreaseVisibility () {
		visibility -= 1;
		if (visibility == 0) {
			ShaderData.RefreshVisibility(this);
		}
	}

	public void ResetVisibility () {
		if (visibility > 0) {
			visibility = 0;
			ShaderData.RefreshVisibility(this);
		}
	}

	public bool IsExplored {
		get {
			return explored && Explorable;
		}
		private set {
			explored = value;
		}
	}

	public bool Explorable { get; set; } // ! Is this cell explorable

	int visibility;
	bool explored;

// ! --------------------------------------------------------------------------

	// On cell creation this value must be set to 0 by HexGrid to apply noise.
    // If it was initialized at 0 then Elevation.set would bail out.
	int elevation = int.MinValue;

	int waterLevel;

	public HexCoordinates Coordinates; // These are the coordinates in the hex "tile map"
    public RectTransform UiRect; // Coordinate Label
    public HexGridChunk Chunk; // Chunk in which this cell is inserted

    [SerializeField]
    HexCell[] Neighbors = new HexCell[6];
	
	bool hasIncomingRiver, hasOutgoingRiver;
	HexDirection incomingRiver, outgoingRiver;

	[SerializeField]
	bool[] Roads = new bool[6];
    
    /// <summary>
    /// Returns the cell's neighbour in the given direction.
    /// </summary>
    public HexCell GetNeighbor (HexDirection direction) {
        return Neighbors[(int)direction]; // This can never be out of bounds
    }

    /// <summary>
    /// Creates a bidirectional neighbour relationship between the two cells.
    /// </summary>
    public void SetNeighbor (HexDirection direction, HexCell cell) {
        Neighbors[(int)direction] = cell;
        cell.Neighbors[(int)direction.Opposite()] = this;
    }

    /// <summary>
    /// Returns the edge connection type with the neighboring cell at the given direction.
    /// </summary>
    public HexEdgeType GetEdgeType (HexDirection direction) {
        // ! If on the edge of the map this can throw NullReferenceException
		return HexMetrics.GetEdgeType(elevation, Neighbors[(int)direction].Elevation);
	}

    /// <summary>
    /// Returns the edge connection type with the given cell.
    /// </summary>
    public HexEdgeType GetEdgeType (HexCell otherCell) {
		return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
	}
	
	/// <summary>
	/// Get the absolute elevation difference between this cell 
	/// and it's neighbor in the given direction.
	/// </summary>
	public int GetElevationDifference (HexDirection direction) {
		int difference = elevation - GetNeighbor(direction).elevation;
		return difference >= 0 ? difference : -difference;
	}

    /// <summary>
    /// Refreshes the chunk containing this cell and any chunks with cells neighboring this cell.
    /// </summary>
    void Refresh () {
		if (Chunk) {
			Chunk.Refresh();
            for (int i = 0; i < Neighbors.Length; i++) {
				HexCell neighbor = Neighbors[i];
				if (neighbor != null && neighbor.Chunk != Chunk) {
					neighbor.Chunk.Refresh();
				}
			}

			if (Unit) {
				Unit.ValidateLocation();
			}
		}
	}
	
	/// <summary>
	/// Refreshes this cell's chunk exclusively. If called more than once on the same frame
    /// chunk will still only update once, in the next frame.
	/// </summary>
	void RefreshSelfOnly () {
		Chunk.Refresh();
		if (Unit) {
			Unit.ValidateLocation();
		}
	}
	
	void RefreshPosition () {
		Vector3 position = transform.localPosition;
		position.y = elevation * HexMetrics.ElevationStep;
		position.y +=
			(HexMetrics.SampleNoise(position).y * 2f - 1f) *
			HexMetrics.ElevationPerturbStrength;
		transform.localPosition = position;

		Vector3 uiPosition = UiRect.localPosition;
		uiPosition.z = -position.y;
		UiRect.localPosition = uiPosition;
	}

	/// <summary>
	/// Query if a river flows from this cell in the given direction.
	/// </summary>
	public bool HasRiverThroughEdge (HexDirection direction) {
		return hasIncomingRiver && incomingRiver == direction || hasOutgoingRiver && outgoingRiver == direction;
	}

	/// <summary>
	/// Removes both incoming and outgoing rivers (if any exist) from this cell.
	/// </summary>
	public void RemoveRiver () {
		RemoveOutgoingRiver();
		RemoveIncomingRiver();
	}

	/// <summary>
	/// Sets the outgoing river in the given direction. The neighbor in that direction gets an incoming river.
	/// </summary>
	public void SetOutgoingRiver (HexDirection direction) {
		// If already has a outgoing river bail out
		if (hasOutgoingRiver && outgoingRiver == direction) {
			return;
		}

		HexCell neighbor = GetNeighbor(direction);
		if ( ! IsValidRiverDestination(neighbor)) {
			return;
		}

		RemoveOutgoingRiver(); // Remove previous if any
		if (hasIncomingRiver && incomingRiver == direction) {
			RemoveIncomingRiver(); // Remove previous
		}

		hasOutgoingRiver = true;
		outgoingRiver = direction;
		specialIndex = 0; // Remove special feature

		neighbor.RemoveIncomingRiver(); // Remove previous
		neighbor.hasIncomingRiver = true;
		neighbor.incomingRiver = direction.Opposite();
		neighbor.specialIndex = 0; // Remove special feature

		SetRoad((int)direction, false); // This already calls refresh
	}

	/// <summary>
	/// Removes incoming river if defined and also removes outgoing river of relevant neighbor.
	/// </summary>
	public void RemoveIncomingRiver () {
		if ( ! hasIncomingRiver) {
			return;
		}
		hasIncomingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(incomingRiver);
		neighbor.hasOutgoingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	/// <summary>
	/// Removes outgoing river if defined and also removes incoming river of relevant neighbor.
	/// </summary>
	public void RemoveOutgoingRiver () {
		if ( ! hasOutgoingRiver) {
			return;
		}
		hasOutgoingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(outgoingRiver);
		neighbor.hasIncomingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	/// <summary>
	/// Checks wheter a neighbor is a valid destination for a river coming from this cell.
	/// </summary>
	bool IsValidRiverDestination (HexCell neighbor) {
		return neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
	}
	
	/// <summary>
	/// Checks if incoming and outgoing rivers are valid. Invalid ones are removed.
	/// </summary>
	void ValidateRivers () {
		if (hasOutgoingRiver && ! IsValidRiverDestination(GetNeighbor(outgoingRiver))) {
			RemoveOutgoingRiver();
		}
		if (hasIncomingRiver && ! GetNeighbor(incomingRiver).IsValidRiverDestination(this)) {
			RemoveIncomingRiver();
		}
	}

	/// <summary>
	/// Add a road connection in the given direction. There can be no river in that direction
	/// and there is a maximum elevation difference to allow road placement.
	/// </summary>
	public void AddRoad (HexDirection direction) {
		if ( ! Roads[(int)direction] && ! HasRiverThroughEdge(direction) && 
		! IsSpecial && ! GetNeighbor(direction).IsSpecial 
		&& GetElevationDifference(direction) <= 1) {
			SetRoad((int)direction, true);
		}
	}

	/// <summary>
	/// Removes all road connections.
	/// </summary>
	public void RemoveRoads () {
		for (int i = 0; i < Neighbors.Length; i++) {
			if (Roads[i]) {
				SetRoad(i, false); // This calls refresh
			}
		}
	}

	/// <summary>
	/// Sets the road state for the index road connection ([0,5] as with directions).
	/// True for connection, false for no road connection. Refreshes self and neighbor.
	/// </summary>
	void SetRoad (int index, bool state) {
		Roads[index] = state;
		Neighbors[index].Roads[(int)((HexDirection)index).Opposite()] = state;
		Neighbors[index].RefreshSelfOnly();
		RefreshSelfOnly();
	}

	/// <summary>
	/// Query if there is a road connection from this cell in the given direction.
	/// </summary>
	public bool HasRoadThroughEdge (HexDirection direction) {
		return Roads[(int)direction];
	}

	public void Save (BinaryWriter writer) {
		writer.Write((byte)terrainTypeIndex);
		writer.Write((byte)(elevation + 127)); // Support positive and negative
		writer.Write((byte)waterLevel);
		writer.Write((byte)urbanLevel);
		writer.Write((byte)farmLevel);
		writer.Write((byte)plantLevel);
		writer.Write((byte)specialIndex);
		writer.Write(walled);

		if (hasIncomingRiver) {
			// If river exists set 8th bit (128)
			writer.Write((byte)(incomingRiver + 128));
		}
		else {
			writer.Write((byte)0);
		}

		if (hasOutgoingRiver) {
			// If river exists set 8th bit (128)
			writer.Write((byte)(outgoingRiver + 128));
		}
		else {
			writer.Write((byte)0);
		}

		int roadFlags = 0;
		for (int i = 0; i < Roads.Length; i++) {
			if (Roads[i]) {
				roadFlags |= 1 << i; // Add flag
			}
		}
		writer.Write((byte)roadFlags);

		writer.Write(IsExplored);
	}

	public void Load (BinaryReader reader) {
		terrainTypeIndex = reader.ReadByte();
		ShaderData.RefreshTerrain(this);

		elevation = reader.ReadByte();
		elevation -= 127;

		RefreshPosition();

		waterLevel = reader.ReadByte();
		urbanLevel = reader.ReadByte();
		farmLevel = reader.ReadByte();
		plantLevel = reader.ReadByte();
		specialIndex = reader.ReadByte();
		walled = reader.ReadBoolean();

		// Check if 8th bit is 1 which signals existence of river
		byte riverData = reader.ReadByte();
		if (riverData >= 128) {
			hasIncomingRiver = true;
			incomingRiver = (HexDirection)(riverData - 128); // Read direction
		}
		else {
			hasIncomingRiver = false;
		}

		// Check if 8th bit is 1 which signals existence of river
		riverData = reader.ReadByte();
		if (riverData >= 128) {
			hasOutgoingRiver = true;
			outgoingRiver = (HexDirection)(riverData - 128);
		}
		else {
			hasOutgoingRiver = false;
		}

		int roadFlags = reader.ReadByte();
		for (int i = 0; i < Roads.Length; i++) {
			Roads[i] = (roadFlags & (1 << i)) != 0; // If flag set then roads exists
		}

		IsExplored = reader.ReadBoolean();
		ShaderData.RefreshVisibility(this);
	}

	// ! REMOVE
	public void SetMapData (float data) {
		ShaderData.SetMapData(this, data);
	}

}
