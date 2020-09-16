using UnityEngine;

public class HexGridChunk : MonoBehaviour {

	// Splat map weights
	static Color weights1 = new Color(1f, 0f, 0f);
	static Color weights2 = new Color(0f, 1f, 0f);
	static Color weights3 = new Color(0f, 0f, 1f);

    public HexMesh Terrain, Rivers, Roads, Water, WaterShore, Estuaries;
	public HexFeatureManager Features;
    HexCell[] Cells;
	Canvas GridCanvas; // Used to display cell coordinates

	void Awake () {
		GridCanvas = GetComponentInChildren<Canvas>();
		Cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
	}

    /// <summary>
    /// Called after all the regular Update functions and only when object is enabled.
    /// By default object is enabled so mesh will be initialized.
    /// </summary>
    void LateUpdate () {
		Triangulate();
		this.enabled = false; // Unregister with Unity's update loop.
	}
    
    /// <summary>
    /// Toggle hex labels.
    /// </summary>
    public void ShowUI (bool visible) {
		GridCanvas.gameObject.SetActive(visible);
	}

    /// <summary>
    /// Adds a cell to the chunk at the given index.
    /// </summary>
    public void AddCell (int index, HexCell cell) {
		Cells[index] = cell;
        cell.Chunk = this;
		cell.transform.SetParent(transform, false);
		cell.UiRect.SetParent(GridCanvas.transform, false);
	}

    /// <summary>
    /// Sets the chunk to refresh on the next frame. If called more than once on the same frame
    /// chunk will still only update once, in the next frame.
    /// </summary>
    public void Refresh () {
        // Enabling the object registers it with Unity to be updated.
		this.enabled = true;
	}

    /// <summary>
    /// Builds the mesh of the hex grid out of the provided array of cells.
    /// </summary>
    public void Triangulate () {
		Terrain.Clear();
		Rivers.Clear();
		Roads.Clear();
		Water.Clear();
		WaterShore.Clear();
		Estuaries.Clear();
		Features.Clear();
		for (int i = 0; i < Cells.Length; i++) {
			Triangulate(Cells[i]);
		}
        Terrain.Apply();
		Rivers.Apply();
		Roads.Apply();
		Water.Apply();
		WaterShore.Apply();
		Estuaries.Apply();
		Features.Apply();
	}
	
	/// <summary>
	/// Creates all the geometry needed by cell.
	/// </summary>
	void Triangulate (HexCell cell) {
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			Triangulate(d, cell);
		}

		// Features
		if (!cell.IsUnderwater) {
			if (!cell.HasRiver && !cell.HasRoads) {
				Features.AddFeature(cell, cell.Position);
			}
			if (cell.IsSpecial) {
				Features.AddSpecialFeature(cell, cell.Position);
			}
		}
	}

	/// <summary>
	/// Creates the geometry for the given cell in the given direction.
	/// </summary>
	void Triangulate (HexDirection direction, HexCell cell) {
		Vector3 center = cell.Position;
		EdgeVertices e = new EdgeVertices(
			center + HexMetrics.GetFirstSolidCorner(direction),
			center + HexMetrics.GetSecondSolidCorner(direction)
		);

		// If the cell has a river triangulation is different
		if (cell.HasRiver) {
			if (cell.HasRiverThroughEdge(direction)) {
				e.v3.y = cell.StreamBedY;
				if (cell.HasRiverBeginOrEnd) {
					TriangulateWithRiverBeginOrEnd(direction, cell, center, e); // Cell is beggining or end of river
				}
				else {
					TriangulateWithRiver(direction, cell, center, e); // Cell is transversed by river
				}
			}
			else { // River does not flow in this direction
				TriangulateAdjacentToRiver(direction, cell, center, e);
			}
		}
		else {
			TriangulateWithoutRiver(direction, cell, center, e); // Else normal triangulation

			if ( ! cell.IsUnderwater && ! cell.HasRoadThroughEdge(direction)) {
				Features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
			}
		}

		if (direction <= HexDirection.SE) {
			TriangulateConnection(direction, cell, e);
		}

		// If cell is underwater then we need to draw water geometry on top
		if (cell.IsUnderwater) {
			TriangulateWater(direction, cell, center);
		}
	}
	

	/// <summary>
	/// Creates the triangles for a side of the hexagon.
	/// </summary>
	void TriangulateEdgeFan (Vector3 center, EdgeVertices edge, float index) {
		// Add geometry
		Terrain.AddTriangle(center, edge.v1, edge.v2);
		Terrain.AddTriangle(center, edge.v2, edge.v3);
		Terrain.AddTriangle(center, edge.v3, edge.v4);
		Terrain.AddTriangle(center, edge.v4, edge.v5);

		// Add splat map colors and terrain type into cell data
		Vector3 indices;
		indices.x = indices.y = indices.z = index;
		Terrain.AddTriangleCellData(indices, weights1);
		Terrain.AddTriangleCellData(indices, weights1);
		Terrain.AddTriangleCellData(indices, weights1);
		Terrain.AddTriangleCellData(indices, weights1);
	}

	/// <summary>
	/// Creates the triangles for a side of the hexagon, with river passing through (incoming and outgoing river) at this side.
	/// </summary>
	void TriangulateWithRiver (HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e) {
		Vector3 centerL, centerR;
		if (cell.HasRiverThroughEdge(direction.Opposite())) { // If river flows forward through the cell
			// Extend center into line
			centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
			centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
		}
		else if (cell.HasRiverThroughEdge(direction.Next())) { // If river bends sharply in next dir
			centerL = center;
			centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
		}
		else if (cell.HasRiverThroughEdge(direction.Previous())) { // If river bends sharply in prev dir
			centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
			centerR = center;
		}
		else if (cell.HasRiverThroughEdge(direction.Next2())) { //  River bends smoothly to next.next dir
			centerL = center;
			centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.InnerToOuter);
		}
		else { //  River bends smoothly to previous.previous dir
			centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.InnerToOuter);
			centerR = center;
		}

		center = Vector3.Lerp(centerL, centerR, 0.5f); // Average center to serve all cases
		
		// Edge 1/2 the way from edge and cell center
		EdgeVertices m = new EdgeVertices(
			Vector3.Lerp(centerL, e.v1, 0.5f),
			Vector3.Lerp(centerR, e.v5, 0.5f),
			1f / 6f
		);
		m.v3.y = center.y = e.v3.y; // Lower middle edge and center to river bed

		TriangulateEdgeStrip(m, weights1, cell.Index, e, weights1, cell.Index);

		// Final gap between the middle edge and the center
		// Geo
		Terrain.AddTriangle(centerL, m.v1, m.v2);
		Terrain.AddQuad(centerL, center, m.v2, m.v3);
		Terrain.AddQuad(center, centerR, m.v3, m.v4);
		Terrain.AddTriangle(centerR, m.v4, m.v5);

		// Splat + Terrain Types 
		Vector3 indices;
		indices.x = indices.y = indices.z = cell.Index;
		Terrain.AddTriangleCellData(indices, weights1);
		Terrain.AddQuadCellData(indices, weights1);
		Terrain.AddQuadCellData(indices, weights1);
		Terrain.AddTriangleCellData(indices, weights1);

		if ( ! cell.IsUnderwater) { // No river water surface when underwater, river bed allowed
			// River water surface
			bool reversed = cell.IncomingRiver == direction;
			TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed, indices);
			TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed, indices);
		}
	}
	
	/// <summary>
	/// Creates the triangles for a side of the hexagon, with river starting or ending this side.
	/// </summary>
	void TriangulateWithRiverBeginOrEnd (HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e) {
		// Edge 1/2 the way from edge and cell center
		EdgeVertices m = new EdgeVertices(
			Vector3.Lerp(center, e.v1, 0.5f),
			Vector3.Lerp(center, e.v5, 0.5f)
		);
		m.v3.y = e.v3.y; // middle at river bed height, but not the center of the cell

		TriangulateEdgeStrip(m, weights1, cell.Index, e, weights1, cell.Index);
		TriangulateEdgeFan(center, m, cell.Index);

		if ( ! cell.IsUnderwater) { // No river water surface underwater, riverbed itself is allowed
			// River water surface
			bool reversed = cell.HasIncomingRiver;

			Vector3 indices;
			indices.x = indices.y = indices.z = cell.Index;
			TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed, indices);

			// Add first/last water surface triangle for river begin/end
			center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
			Rivers.AddTriangle(center, m.v2, m.v4);
			if (reversed) {
				Rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
			}
			else {
				Rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
			}

			Rivers.AddTriangleCellData(indices, weights1);
		}
	}
	
	/// <summary>
	/// Creates the triangles for a side of the hexagon, when there is a river in the hex.
	/// </summary>
	void TriangulateAdjacentToRiver (HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e) {
		
		if (cell.HasRoads) {
			TriangulateRoadAdjacentToRiver(direction, cell, center, e);
		}

		// Determine center by finding river channel center edge
		if (cell.HasRiverThroughEdge(direction.Next())) {
			if (cell.HasRiverThroughEdge(direction.Previous())) {
				center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.InnerToOuter * 0.5f);
			}
			else if (cell.HasRiverThroughEdge(direction.Previous2())) {
				center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
			}
		}
		else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2())) {
			center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
		}

		EdgeVertices m = new EdgeVertices(
			Vector3.Lerp(center, e.v1, 0.5f),
			Vector3.Lerp(center, e.v5, 0.5f)
		);
		
		TriangulateEdgeStrip(m, weights1, cell.Index, e, weights1, cell.Index);
		TriangulateEdgeFan(center, m, cell.Index);

		// If possible add any features
		if ( ! cell.IsUnderwater && ! cell.HasRoadThroughEdge(direction)) {
			Features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
		}
	}
	
	void TriangulateWithoutRiver (HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e) {
		// If no river draw normal edge geometry
		TriangulateEdgeFan(center, e, cell.Index);
		
		if (cell.HasRoads) { // If roads present draw road geometry on top
			Vector2 interpolators = GetRoadInterpolators(direction, cell);
			TriangulateRoad(
				center, Vector3.Lerp(center, e.v1, interpolators.x), 
				Vector3.Lerp(center, e.v5, interpolators.y), 
				e, cell.HasRoadThroughEdge(direction), cell.Index
			);
		}
	}
	
	/// <summary>
	/// Creates geometry for river water surface with different start and end Y.
	/// </summary>
	void TriangulateRiverQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, bool reversed, Vector3 indices) {
		v1.y = v2.y = y1;
		v3.y = v4.y = y2;
		Rivers.AddQuad(v1, v2, v3, v4);

		// Add UVs according to river orientation (used in water flow effect in river shader)
		if (reversed) {
			Rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
		}
		else {
			Rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
		}

		Rivers.AddQuadCellData(indices, weights1, weights2);
	}
	
	/// <summary>
	/// Creates geometry for river water surface.
	/// </summary>
	void TriangulateRiverQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool reversed, Vector3 indices) {
		TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed, indices);
	}
	
	/// <summary>
	/// Creates road geometry for cell center.
	/// </summary>
	void TriangulateRoad (Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e, bool hasRoadThroughCellEdge, float index) {

		if (hasRoadThroughCellEdge) { // Road in this direction ?
			Vector3 indices;
			indices.x = indices.y = indices.z = index;
			Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
			TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4, weights1, weights1, indices);

			// Center connection triangles
			Roads.AddTriangle(center, mL, mC);
			Roads.AddTriangle(center, mC, mR);
			Roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
			Roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));

			Roads.AddTriangleCellData(indices, weights1);
			Roads.AddTriangleCellData(indices, weights1);
		}
		else {
			// Then road exists in cell but not at this edge so draw auxiliary road geometry
			TriangulateRoadEdge(center, mL, mR, index); 
		}

	}
	
	/// <summary>
	/// Creates the geometry for connecting different roads that converge on the cell center.
	/// </summary>
	void TriangulateRoadEdge (Vector3 center, Vector3 mL, Vector3 mR, float index) {
		Roads.AddTriangle(center, mL, mR);
		Roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
		Vector3 indices;
		indices.x = indices.y = indices.z = index;
		Roads.AddTriangleCellData(indices, weights1);
	}
	
	/// <summary>
	/// Create the two quads of a road segment.
	/// </summary>
	void TriangulateRoadSegment (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6, Color w1, Color w2, Vector3 indices) {
		Roads.AddQuad(v1, v2, v4, v5);
		Roads.AddQuad(v2, v3, v5, v6);

		// This results in U for the 2 road quads -> 0 ||||| 1 1 ||||| 0
		// This way we can use U in the shader as a measure of how close we are to the center of the road
		Roads.AddQuadUV(0f, 1f, 0f, 0f); 
		Roads.AddQuadUV(1f, 0f, 0f, 0f); 

		Roads.AddQuadCellData(indices, w1, w2);
		Roads.AddQuadCellData(indices, w1, w2);
	}
	
	/// <summary>
	/// Creates road geometry when there is a river in the cell.
	/// </summary>
	void TriangulateRoadAdjacentToRiver (HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e) {
		bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
		bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
		bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
		Vector2 interpolators = GetRoadInterpolators(direction, cell);
		Vector3 roadCenter = center;

		// When there is a river start/end or a river zig-zag then we only need to offset road center
		// If there is a straight section of river cutting through or a smooth turn then those divide the road network

		if (cell.HasRiverBeginOrEnd) { // If this is river start or river end
			roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
		}
		else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite()) { // If this is a river section cutting cell in the middle
			Vector3 corner;
			if (previousHasRiver) {
				if ( ! hasRoadThroughEdge && ! cell.HasRoadThroughEdge(direction.Next())) {
					return; // Dont draw on the other side of the river
				}
				corner = HexMetrics.GetSecondSolidCorner(direction);
			}
			else {
				if ( ! hasRoadThroughEdge && ! cell.HasRoadThroughEdge(direction.Previous())) {
					return; // Dont draw on the other side of the river
				}
				corner = HexMetrics.GetFirstSolidCorner(direction);
			}

			roadCenter += corner * 0.5f;

			// Prevent duplicate bridges and add a bridge if road on both sides
			if (cell.IncomingRiver == direction.Next() && (
				cell.HasRoadThroughEdge(direction.Next2()) ||
				cell.HasRoadThroughEdge(direction.Opposite())
			)) {
				Features.AddBridge(roadCenter, center - corner * 0.5f);
			}

			center += corner * 0.25f;
		}
		else if (cell.IncomingRiver == cell.OutgoingRiver.Previous()) { // Check if zig-zag and offset
			roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f; 
		}
		else if (cell.IncomingRiver == cell.OutgoingRiver.Next()) { // Check if zig-zag and offset
			roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
		}
		else if (previousHasRiver && nextHasRiver) { // Check smooth curve (inside of the curve)
			if ( ! hasRoadThroughEdge) {
				return; // Dont draw on the other side of the river
			}
			Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.InnerToOuter;
			roadCenter += offset * 0.7f;
			center += offset * 0.5f;
		}
		else { // Smooth curve (outside of the curve)
			HexDirection middle;
			if (previousHasRiver) {
				middle = direction.Next();
			}
			else if (nextHasRiver) {
				middle = direction.Previous();
			}
			else {
				middle = direction;
			}

			if ( ! cell.HasRoadThroughEdge(middle) && ! cell.HasRoadThroughEdge(middle.Previous()) &&
				 ! cell.HasRoadThroughEdge(middle.Next())) 
			{
				return; // Dont draw on the other side of the river
			}

			// Set center and add bridge
			Vector3 offset = HexMetrics.GetSolidEdgeMiddle(middle);
			roadCenter += offset * 0.25f;

			// Prevent duplicate bridges and road on both sides
			if (direction == middle && cell.HasRoadThroughEdge(direction.Opposite())) {
				Features.AddBridge(roadCenter, center - offset * (HexMetrics.InnerToOuter * 0.7f));
			}
		}

		Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
		Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
		TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge, cell.Index);

		// Create auxiliary geometry
		if (previousHasRiver) {
			TriangulateRoadEdge(roadCenter, center, mL, cell.Index);
		}
		if (nextHasRiver) {
			TriangulateRoadEdge(roadCenter, mR, center, cell.Index);
		}
	}
	
	/// <summary>
	/// Given a cell and a direction returns the interpolation value that should be used
	/// to place the road connecting triangle's vertices for the given direction. 
	/// </summary>
	Vector2 GetRoadInterpolators (HexDirection direction, HexCell cell) {
		Vector2 interpolators;
		if (cell.HasRoadThroughEdge(direction)) {
			interpolators.x = interpolators.y = 0.5f;
		}
		else {
			interpolators.x = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
			interpolators.y = cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
		}
		return interpolators;
	}
	
	/// <summary>
	/// Creates the triangles for the bridge between two cells.
	/// </summary>
	void TriangulateEdgeStrip (EdgeVertices e1, Color w1, float index1, EdgeVertices e2, Color w2, float index2, bool hasRoad = false) {
		// Geometry
		Terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
		Terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
		Terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
		Terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

		// Add splat map and terrain types to cell data
		Vector3 indices;
		indices.x = indices.z = index1;
		indices.y = index2;
		Terrain.AddQuadCellData(indices, w1, w2);
		Terrain.AddQuadCellData(indices, w1, w2);
		Terrain.AddQuadCellData(indices, w1, w2);
		Terrain.AddQuadCellData(indices, w1, w2);

		// If road present then build it in the middle of the cell bridge.
		if (hasRoad) {
			TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4, w1, w2, indices);
		}
	}

	/// <summary>
	/// Creates the geometry for the hexagon connections, meaning the bridge quad and the
	/// tripple connection triangle.
	/// </summary>
	void TriangulateConnection (HexDirection direction, HexCell cell, EdgeVertices e1) {

		HexCell neighbor = cell.GetNeighbor(direction);
		if (neighbor == null) { 
			return; // No connection to make
		}
		
		Vector3 bridge = HexMetrics.GetBridge(direction);
		bridge.y = neighbor.Position.y - cell.Position.y;
		EdgeVertices e2 = new EdgeVertices(
			e1.v1 + bridge,
			e1.v5 + bridge
		);

		bool hasRiver = cell.HasRiverThroughEdge(direction);
		bool hasRoad = cell.HasRoadThroughEdge(direction);

		// If this cell has river also displace neighbor and create water surface
		if (hasRiver) {
			e2.v3.y = neighbor.StreamBedY;
			Vector3 indices;
			indices.x = indices.z = cell.Index;
			indices.y = neighbor.Index;


			if ( ! cell.IsUnderwater) { // No rivers underwater
				if ( ! neighbor.IsUnderwater) { // If neighbor not underwater normal river
					TriangulateRiverQuad(
						e1.v2, e1.v4, e2.v2, e2.v4,
						cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
						cell.HasIncomingRiver && cell.IncomingRiver == direction,
						indices
					);
				}
				else if (cell.Elevation > neighbor.WaterLevel) { // If neighbor underwater and waterfall to neighbor
					TriangulateWaterfallInWater(
						e1.v2, e1.v4, e2.v2, e2.v4,
						cell.RiverSurfaceY, neighbor.RiverSurfaceY,
						neighbor.WaterSurfaceY,
						indices
					);
				}
			}
			else if ( ! neighbor.IsUnderwater && neighbor.Elevation > cell.WaterLevel) { // If we underwater and waterfall from neighbor
				TriangulateWaterfallInWater(
					e2.v4, e2.v2, e1.v4, e1.v2,
					neighbor.RiverSurfaceY, cell.RiverSurfaceY,
					cell.WaterSurfaceY,
					indices
				);
			}
		}

		// Create the bridge
		if (cell.GetEdgeType(direction) == HexEdgeType.Slope) { // If slope make terraces
			TriangulateEdgeTerraces(e1, cell, e2, neighbor, hasRoad);
		}
		else { // Else normal bridge
			TriangulateEdgeStrip(e1, weights1, cell.Index, e2, weights2, neighbor.Index, hasRoad);
		}

		// * Let feature manager decide if walls need to be created.
		Features.AddWall(e1, cell, e2, neighbor, hasRiver, hasRoad);

		// Create the tripple connection triangle
		HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
		if (direction <= HexDirection.E && nextNeighbor != null) {
			Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
			v5.y = nextNeighbor.Position.y;
			
			// Find triangle orientation based on elevation of the tripple junction point cells
			// by finding the lowest cell of the three
			if (cell.Elevation <= neighbor.Elevation) {
				if (cell.Elevation <= nextNeighbor.Elevation) { // If this cell is lowest
					TriangulateCorner(e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor);
				}
				else { // If next neighbor is lowest cell
					// Rotate the triangle counter-clockwise
					TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor); 
				}
			}
			else if (neighbor.Elevation <= nextNeighbor.Elevation) { // If neighbor is lowest cell
				// Rotate the triangle clockwise
				TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell);
			}
			else { // If next neighbor is lowest cell
				// Rotate the triangle counter-clockwise
				TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
			}
		}
	}

	/// <summary>
	/// Given an origin and a destination cell, and their bridge edges, creates the terrace
	/// geometry for their slopped bridge.
	/// </summary>
	void TriangulateEdgeTerraces (EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell, bool hasRoad) 
	{
		EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
		Color w2 = HexMetrics.TerraceLerp(weights1, weights2, 1);

		float i1 = beginCell.Index;
		float i2 = endCell.Index;

		// First incline
		TriangulateEdgeStrip(begin, weights1, i1, e2, w2, i2, hasRoad);

		// Create the intermediate steps
		for (int i = 2; i < HexMetrics.TerraceSteps; i++) {
			EdgeVertices e1 = e2;
			Color w1 = w2;
			e2 = EdgeVertices.TerraceLerp(begin, end, i);
			w2 = HexMetrics.TerraceLerp(weights1, weights2, i);
			TriangulateEdgeStrip(e1, w1, i1, e2, w2, i2, hasRoad);
		}

		// Last incline
		TriangulateEdgeStrip(e2, w2, i1, end, weights2, i2, hasRoad);
	}

	/// <summary>
	/// Given the bottom, left and right cell of the tripple connection triangle identify the edge case and build the
	/// geometry.
	/// </summary>
	void TriangulateCorner (Vector3 bottom, HexCell bottomCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell) {
		HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
		HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

		if (leftEdgeType == HexEdgeType.Slope) {
			if (rightEdgeType == HexEdgeType.Slope) { // SSF
				TriangulateCornerTerraces(
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}
			else if (rightEdgeType == HexEdgeType.Flat) { // SFS
				TriangulateCornerTerraces(
					left, leftCell, right, rightCell, bottom, bottomCell
				);
			}
			else {
				TriangulateCornerTerracesCliff( // SCS and SCC
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}
		}
		else if (rightEdgeType == HexEdgeType.Slope) {
			if (leftEdgeType == HexEdgeType.Flat) { // FSS
				TriangulateCornerTerraces(
					right, rightCell, bottom, bottomCell, left, leftCell
				);
			}
			else { // CSS & CSC
				TriangulateCornerCliffTerraces(
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}
		}
		else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) { // CCSR & CCSL
			if (leftCell.Elevation < rightCell.Elevation) { 
				TriangulateCornerCliffTerraces(
					right, rightCell, bottom, bottomCell, left, leftCell
				);
			}
			else {
				TriangulateCornerTerracesCliff(
					left, leftCell, right, rightCell, bottom, bottomCell
				);
			}
		}
		else { // Simple triangle takes care of FFF, CCF, CCCR, and CCCL
			Terrain.AddTriangle(bottom, left, right); // Geometry
			// Cell data
			Vector3 indices;
			indices.x = bottomCell.Index;
			indices.y = leftCell.Index;
			indices.z = rightCell.Index;
			Terrain.AddTriangleCellData(indices, weights1, weights2, weights3);
		}

		// * If necessary feature manager creates a wall connection in this corner
		Features.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);
	}

	/// <summary>
	/// Handles connection triangle geometry when terrace meets cliff.
	/// </summary>
	void TriangulateCornerTerracesCliff (Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell) {
		// Find point where terraces will meet on cliff side
		float b = 1f / (rightCell.Elevation - beginCell.Elevation);
		if (b < 0) { b = -b; }

		Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);

		Color boundaryWeights = Color.Lerp(weights1, weights3, b);
		Vector3 indices;
		indices.x = beginCell.Index;
		indices.y = leftCell.Index;
		indices.z = rightCell.Index;

		// Merge terraces into point on cliff side
		TriangulateBoundaryTriangle(begin, weights1, left, weights2, boundary, boundaryWeights, indices);

		// If other edge also has terraces then do the same thing rotated
		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			TriangulateBoundaryTriangle(left, weights2, right, weights3, boundary, boundaryWeights, indices);
		}
		else { // Else if it is a slope a simple sloped triangle is enough
			Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary); // Never perturb boundary
			Terrain.AddTriangleCellData(indices, weights2, weights3, boundaryWeights);
		}
	}

	/// <summary>
	/// Handles connection triangle geometry when cliff meets terrace.
	/// </summary>
	void TriangulateCornerCliffTerraces (Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell) {
		// Find point where terraces will meet on cliff side
		float b = 1f / (leftCell.Elevation - beginCell.Elevation);
		if (b < 0) { b = -b; }

		Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);

		Color boundaryWeights = Color.Lerp(weights1, weights2, b);
		Vector3 indices;
		indices.x = beginCell.Index;
		indices.y = leftCell.Index;
		indices.z = rightCell.Index;

		// Merge terraces into point on cliff side
		TriangulateBoundaryTriangle(right, weights3, begin, weights1, boundary, boundaryWeights, indices);

		// If other edge also has terraces then do the same thing rotated
		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
			TriangulateBoundaryTriangle(left, weights2, right, weights3, boundary, boundaryWeights, indices);
		}
		else { // Else if it is a slope a simple sloped triangle is enough
			Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary); // Never perturb boundary
			Terrain.AddTriangleCellData(indices, weights2, weights3, boundaryWeights);
		}
	}

	/// <summary>
	/// Creates geometry for connecting terraces with a cliff.
	/// </summary>
	void TriangulateBoundaryTriangle (Vector3 begin, Color beginWeights, Vector3 left, Color leftWeights, Vector3 boundary, Color boundaryWeights, Vector3 indices) {

		Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
		Color w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, 1);

		// Boundary must not suffer from noise or terraces and cliffs are impossible to merge
		// Use AddTriangleUnperturbed and perturb vertices other than boundary, v2 is perturbed at creation
		Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
		Terrain.AddTriangleCellData(indices, beginWeights, w2, boundaryWeights);

		for (int i = 2; i < HexMetrics.TerraceSteps; i++) {
			Vector3 v1 = v2;
			Color w1 = w2;
			v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
			w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, i);
			Terrain.AddTriangleUnperturbed(v1, v2, boundary);
			Terrain.AddTriangleCellData(indices, w1, w2, boundaryWeights);
		}

		Terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
		Terrain.AddTriangleCellData(indices, w2, leftWeights, boundaryWeights);
	}

	/// <summary>
	/// Case when tripple connection triangle must be terraced on 2 sides.
	/// </summary>
	void TriangulateCornerTerraces (Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell) {
		Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
		Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
		Color w3 = HexMetrics.TerraceLerp(weights1, weights2, 1);
		Color w4 = HexMetrics.TerraceLerp(weights1, weights3, 1);

		Vector3 indices;
		indices.x = beginCell.Index;
		indices.y = leftCell.Index;
		indices.z = rightCell.Index;

		// First incline, since this is a corner a triangle is used.
		Terrain.AddTriangle(begin, v3, v4); // Geo
		Terrain.AddTriangleCellData(indices, weights1, w3, w4); // Splat + Terrain type

		// After the first incline quads for the terraces and other inclines.
		for (int i = 2; i < HexMetrics.TerraceSteps; i++) {
			Vector3 v1 = v3;
			Vector3 v2 = v4;
			Color w1 = w3;
			Color w2 = w4;
			v3 = HexMetrics.TerraceLerp(begin, left, i);
			v4 = HexMetrics.TerraceLerp(begin, right, i);
			w3 = HexMetrics.TerraceLerp(weights1, weights2, i);
			w4 = HexMetrics.TerraceLerp(weights1, weights3, i);
			Terrain.AddQuad(v1, v2, v3, v4);
			Terrain.AddQuadCellData(indices, w1, w2, w3, w4);
		}

		// Last Incline
		Terrain.AddQuad(v3, v4, left, right);
		Terrain.AddQuadCellData(indices, w3, w4, weights2, weights3);
	}

	/// <summary>
	/// Creates water geometry over cell for the given direction.
	/// </summary>
	void TriangulateWater (HexDirection direction, HexCell cell, Vector3 center) {
		center.y = cell.WaterSurfaceY;

		HexCell neighbor = cell.GetNeighbor(direction);
		if (neighbor != null && !neighbor.IsUnderwater) {
			TriangulateWaterShore(direction, cell, neighbor, center);
		}
		else {
			TriangulateOpenWater(direction, cell, neighbor, center);
		}
	}

	/// <summary>
	/// Creates geometry for open water (no land neighbors).
	/// </summary>
	void TriangulateOpenWater (HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center) {
		// Careful to find the WATER corner not the solid corner
		// Careful to get the WATER bridge
		Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
		Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

		Water.AddTriangle(center, c1, c2);

		Vector3 indices;
		indices.x = indices.y = indices.z = cell.Index;
		Water.AddTriangleCellData(indices, weights1);

		if (direction <= HexDirection.SE && neighbor != null) {
			Vector3 bridge = HexMetrics.GetWaterBridge(direction);
			Vector3 e1 = c1 + bridge;
			Vector3 e2 = c2 + bridge;

			Water.AddQuad(c1, c2, e1, e2);

			indices.y = neighbor.Index;
			Water.AddQuadCellData(indices, weights1, weights2);

			// The tripple connection triangle for water surface
			if (direction <= HexDirection.E) {
				HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
				if (nextNeighbor == null || ! nextNeighbor.IsUnderwater) {
					return;
				}
				Water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));

				indices.z = nextNeighbor.Index;
				Water.AddTriangleCellData(indices, weights1, weights2, weights3);
			}
		}
	}

	/// <summary>
	/// Creates geometry for water connection non water cell.
	/// </summary>
	void TriangulateWaterShore (HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center) {
		// Careful to find the WATER corner not the solid corner
		// Careful to get the WATER bridge
		EdgeVertices e1 = new EdgeVertices(
			center + HexMetrics.GetFirstWaterCorner(direction),
			center + HexMetrics.GetSecondWaterCorner(direction)
		);

		// Triangle fan still part of open water
		Water.AddTriangle(center, e1.v1, e1.v2);
		Water.AddTriangle(center, e1.v2, e1.v3);
		Water.AddTriangle(center, e1.v3, e1.v4);
		Water.AddTriangle(center, e1.v4, e1.v5);
		Vector3 indices;
		indices.x = indices.z = cell.Index;
		indices.y = neighbor.Index;
		Water.AddTriangleCellData(indices, weights1);
		Water.AddTriangleCellData(indices, weights1);
		Water.AddTriangleCellData(indices, weights1);
		Water.AddTriangleCellData(indices, weights1);

		// Bridge - We get the bridge by finding the land hex center and working from there
		Vector3 center2 = neighbor.Position;
		if (neighbor.ColumnIndex < cell.ColumnIndex - 1) {
			center2.x += HexMetrics.WrapSize * HexMetrics.InnerDiameter;
		}
		else if (neighbor.ColumnIndex > cell.ColumnIndex + 1) {
			center2.x -= HexMetrics.WrapSize * HexMetrics.InnerDiameter;
		}
		center2.y = center.y;
		EdgeVertices e2 = new EdgeVertices(
			center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
			center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
		);

		if (cell.HasRiverThroughEdge(direction)) {
			TriangulateEstuary(e1, e2, cell.HasIncomingRiver && cell.IncomingRiver == direction, indices); // River and Shore meet -> Estuary -> different geometry
		}
		else { // Else normal shore geometry
			WaterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
			WaterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
			WaterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
			WaterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
			WaterShore.AddQuadUV(0f, 0f, 0f, 1f); // U null, V -> 0 water side, 1 shore side
			WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
			WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
			WaterShore.AddQuadUV(0f, 0f, 0f, 1f);

			WaterShore.AddQuadCellData(indices, weights1, weights2);
			WaterShore.AddQuadCellData(indices, weights1, weights2);
			WaterShore.AddQuadCellData(indices, weights1, weights2);
			WaterShore.AddQuadCellData(indices, weights1, weights2);
		}

		// Corner tripple connection triangle - shore water
		HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
		if (nextNeighbor != null) {
			Vector3 center3 = nextNeighbor.Position;
			if (nextNeighbor.ColumnIndex < cell.ColumnIndex - 1) {
				center3.x += HexMetrics.WrapSize * HexMetrics.InnerDiameter;
			}
			else if (nextNeighbor.ColumnIndex > cell.ColumnIndex + 1) {
				center3.x -= HexMetrics.WrapSize * HexMetrics.InnerDiameter;
			}
			// Since this is a tripple connection we need to check if neighbor is underwater or not
			// If he is underwater connect with WaterCorner if not with SolidCorner
			Vector3 v3 = center3 + (nextNeighbor.IsUnderwater ?
				HexMetrics.GetFirstWaterCorner(direction.Previous()) :
				HexMetrics.GetFirstSolidCorner(direction.Previous()));
			v3.y = center.y;
			WaterShore.AddTriangle(e1.v5, e2.v5, v3);
			WaterShore.AddTriangleUV(
				new Vector2(0f, 0f),
				new Vector2(0f, 1f),
				new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f) // U null, V -> 0 water side, 1 shore side
			);

			indices.z = nextNeighbor.Index;
			WaterShore.AddTriangleCellData(indices, weights1, weights2, weights3);
		}
	}

	/// <summary>
	/// Creates the estuary (when river meets shore) geometry.
	/// </summary>
	void TriangulateEstuary (EdgeVertices e1, EdgeVertices e2, bool incomingRiver, Vector3 indices) {
		// Some of the estuary is still water shore, namely one triangle on each side
		// that leaves the estuary itself with a trapezoid shape connecting the shore/river to water
		WaterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
		WaterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
		WaterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
		WaterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

		WaterShore.AddTriangleCellData(indices, weights2, weights1, weights1);
		WaterShore.AddTriangleCellData(indices, weights2, weights1, weights1);

		// Make the estuary trapezoid
		Estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3); // Adding it rotated for symetric geometry
		Estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
		Estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

		// For shore effect
		Estuaries.AddQuadUV(
			new Vector2(0f, 1f), new Vector2(0f, 0f),
			new Vector2(1f, 1f), new Vector2(0f, 0f)
		);
		Estuaries.AddTriangleUV(
			new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f)
		);
		Estuaries.AddQuadUV(
			new Vector2(0f, 0f), new Vector2(0f, 0f),
			new Vector2(1f, 1f), new Vector2(0f, 1f)
		);

		Estuaries.AddQuadCellData(indices, weights2, weights1, weights2, weights1);
		Estuaries.AddTriangleCellData(indices, weights1, weights2, weights2);
		Estuaries.AddQuadCellData(indices, weights1, weights2);

		// For river effect, depending on river flow direction UVs are different
		// That is if river flows into the water or out of the water (e.g. when a river starts from a lake)
		if (incomingRiver) {
			Estuaries.AddQuadUV2(new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f), new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
			Estuaries.AddTriangleUV2(new Vector2(0.5f, 1.1f), new Vector2(1f, 0.8f), new Vector2(0f, 0.8f));
			Estuaries.AddQuadUV2(new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f), new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f));
		}
		else {
			Estuaries.AddQuadUV2(new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f), new Vector2(0f, 0f), new Vector2(0.5f, -0.3f));
			Estuaries.AddTriangleUV2(new Vector2(0.5f, -0.3f), new Vector2(0f, 0f), new Vector2(1f, 0f));
			Estuaries.AddQuadUV2(new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f), new Vector2(1f, 0f), new Vector2(1.5f, -0.2f));
		}
	}

	/// <summary>
	/// Create geometry for a river than flows in a waterfall unto the water surface, 
	/// parameter orientation is aligned with river, looking downstream, left & right start vertex, 
	/// left and right end vertex, cell start height, cell end height and water (where waterfall ends) height.
	/// </summary>
	void TriangulateWaterfallInWater (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float waterY, Vector3 indices) {
		v1.y = v2.y = y1;
		v3.y = v4.y = y2;

		// Perturn vertices before interpolating
		// If we didnt do this and did it afterwards when creating the quad then we would loose
		// the results we will calculate with our interpolation bellow
		v1 = HexMetrics.Perturb(v1);
		v2 = HexMetrics.Perturb(v2);
		v3 = HexMetrics.Perturb(v3);
		v4 = HexMetrics.Perturb(v4);

		float t = (waterY - y2) / (y1 - y2); // calculate interpolator
		v3 = Vector3.Lerp(v3, v1, t);
		v4 = Vector3.Lerp(v4, v2, t);

		// Create unperturbed quad, we already perturbed the vertices
		Rivers.AddQuadUnperturbed(v1, v2, v3, v4);
		Rivers.AddQuadUV(0f, 1f, 0.8f, 1f);

		Rivers.AddQuadCellData(indices, weights1, weights2);
	}

}