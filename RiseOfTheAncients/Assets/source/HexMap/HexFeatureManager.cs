using UnityEngine;

public class HexFeatureManager : MonoBehaviour {

    Transform container;
	public Transform WallTower, Bridge;
    public HexFeatureCollection[] UrbanCollections, FarmCollections, PlantCollections;
	public Transform[] Special;
	public HexMesh Walls;

	public void Clear () {
        if (container) {
			Destroy(container.gameObject);
		}
		container = new GameObject("Features Container").transform;
		container.SetParent(transform, false);

		Walls.Clear();
    }

	public void Apply () {
		Walls.Apply();
	}

	public void AddSpecialFeature (HexCell cell, Vector3 position) {
		Transform instance = Instantiate(Special[cell.SpecialIndex - 1]);
		instance.localPosition = HexMetrics.Perturb(position);
		HexHash hash = HexMetrics.SampleHashGrid(position);
		instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
		instance.SetParent(container, false);
	}

	/// <summary>
	/// If eligible creates a wall segment between 2 cells.
	/// </summary>
	public void AddWall (EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell, bool hasRiver, bool hasRoad) {
		
		bool walledUnwalled = nearCell.Walled != farCell.Walled; // Walls only between walled and unwalled cells
		bool noUnderwater = ! nearCell.IsUnderwater && ! farCell.IsUnderwater; // No walls underwater
		bool noSlope = nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff; // No walls on slopes

		if (walledUnwalled && noUnderwater && noSlope) {
			AddWallSegment(near.v1, far.v1, near.v2, far.v2);
			if (hasRiver || hasRoad) {
				// Leave a gap.
				AddWallCap(near.v2, far.v2);
				AddWallCap(far.v4, near.v4);
			}
			else {
				AddWallSegment(near.v2, far.v2, near.v3, far.v3);
				AddWallSegment(near.v3, far.v3, near.v4, far.v4);
			}
			AddWallSegment(near.v4, far.v4, near.v5, far.v5);
		}
	}

	/// <summary>
	/// If eligible create a walled connection in the tripple connection triangle area.
	/// </summary>
	public void AddWall (Vector3 c1, HexCell cell1, Vector3 c2, HexCell cell2, Vector3 c3, HexCell cell3) {
		if (cell1.Walled) {
			if (cell2.Walled) {
				if (!cell3.Walled) {
					AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
				}
			}
			else if (cell3.Walled) {
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
			else {
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
		}
		else if (cell2.Walled) {
			if (cell3.Walled) {
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
			else {
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
		}
		else if (cell3.Walled) {
			AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
		}
	}

	/// <summary>
	/// Creates a quad perpendicular to the wall to close off the side. Usefull in wall openings
	/// for road passage for example.
	/// </summary>
	void AddWallCap (Vector3 near, Vector3 far) {
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb(far);

		Vector3 center = HexMetrics.WallLerp(near, far);
		Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

		Vector3 v1, v2, v3, v4;

		v1 = v3 = center - thickness;
		v2 = v4 = center + thickness;
		v3.y = v4.y = center.y + HexMetrics.WallHeight;
		Walls.AddQuadUnperturbed(v1, v2, v3, v4);
	}
	
	/// <summary>
	/// Create a wall segment that converges on a point. Usefull for connecting walls
	/// to cliff sides.
	/// </summary>
	void AddWallWedge (Vector3 near, Vector3 far, Vector3 point) {
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb(far);
		point = HexMetrics.Perturb(point);

		Vector3 center = HexMetrics.WallLerp(near, far);
		Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

		Vector3 v1, v2, v3, v4;
		Vector3 pointTop = point;
		point.y = center.y;

		v1 = v3 = center - thickness;
		v2 = v4 = center + thickness;
		v3.y = v4.y = pointTop.y = center.y + HexMetrics.WallHeight;

		Walls.AddQuadUnperturbed(v1, point, v3, pointTop);
		Walls.AddQuadUnperturbed(point, v2, pointTop, v4);
		Walls.AddTriangleUnperturbed(pointTop, v3, v4);
	}

	/// <summary>
	/// Creates a wall segment on the tripple connection triangle.
	/// </summary>
	void AddWallSegment (Vector3 pivot, HexCell pivotCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell) {
		
		if (pivotCell.IsUnderwater) {
			return; // If pivot is underwater then there is no wall
		}

		// For a connection to exist there have to be walls on each side, for that to happen
		// these conditions must be true
		bool hasLeftWall = ! leftCell.IsUnderwater && pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
		bool hasRighWall = ! rightCell.IsUnderwater && pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

		// If there are walls on either side then make a segment
		// If there is a wall on only one side and the wall meets a cliff then connect to the cliff
		// Else if wall only on one side and no cliff just close the wall sides
		if (hasLeftWall) {
			if (hasRighWall) {
				AddWallSegment(pivot, left, pivot, right, true);
			}
			else if (leftCell.Elevation < rightCell.Elevation) {
				AddWallWedge(pivot, left, right); 
			}
			else {
				AddWallCap(pivot, left); 
			}
		}
		else if (hasRighWall) {
			if (rightCell.Elevation < leftCell.Elevation) {
				AddWallWedge(right, pivot, left); 
			}
			else {
				AddWallCap(right, pivot); 
			}
		}
	}

	/// <summary>
	/// Creates a wall segment.
	/// </summary>
	void AddWallSegment (Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight, bool addTower = false) {
		// Perturb before calculations and then add unperturbed quads. This makes wall shapes more consistent
		nearLeft = HexMetrics.Perturb(nearLeft);
		farLeft = HexMetrics.Perturb(farLeft);
		nearRight = HexMetrics.Perturb(nearRight);
		farRight = HexMetrics.Perturb(farRight);

		Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
		Vector3 right = HexMetrics.WallLerp(nearRight, farRight);
		
		Vector3 leftThicknessOffset = HexMetrics.WallThicknessOffset(nearLeft, farLeft);
		Vector3 rightThicknessOffset = HexMetrics.WallThicknessOffset(nearRight, farRight);

		float leftTop = left.y + HexMetrics.WallHeight;
		float rightTop = right.y + HexMetrics.WallHeight;

		Vector3 v1, v2, v3, v4;
		v1 = v3 = left - leftThicknessOffset;
		v2 = v4 = right - rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		Walls.AddQuadUnperturbed(v1, v2, v3, v4);

		Vector3 t1 = v3, t2 = v4;

		v1 = v3 = left + leftThicknessOffset;
		v2 = v4 = right + rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		Walls.AddQuadUnperturbed(v2, v1, v4, v3);

		Walls.AddQuadUnperturbed(t1, t2, v3, v4); // Top of Wall

		if (addTower) {
			Transform towerInstance = Instantiate(WallTower);
			towerInstance.transform.localPosition = (left + right) * 0.5f;
			// Tell Unity what is right, rotation is then computed automatically
			Vector3 rightDirection = right - left;
			rightDirection.y = 0f;
			towerInstance.transform.right = rightDirection;
			towerInstance.SetParent(container, false);
		}
	}

	public void AddBridge (Vector3 roadCenter1, Vector3 roadCenter2) {
		roadCenter1 = HexMetrics.Perturb(roadCenter1);
		roadCenter2 = HexMetrics.Perturb(roadCenter2);
		Transform instance = Instantiate(Bridge);
		instance.forward = roadCenter2 - roadCenter1; // Tell Unity how to rotate the object, if we set the forward vector Unity figures out the rotation
		instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;

		float length = Vector3.Distance(roadCenter1, roadCenter2);

		// Scale bridge length according to needed distance
		instance.localScale = new Vector3(1f,	1f, length * (1f / HexMetrics.BridgeDesignLength));
		instance.SetParent(container, false);
	}

	public void AddFeature (HexCell cell, Vector3 position) {

		if (cell.IsSpecial) {
			return;
		}

		HexHash hash = HexMetrics.SampleHashGrid(position);
		Transform prefab = PickPrefab(UrbanCollections, cell.UrbanLevel, hash.a, hash.d);
		Transform otherPrefab = PickPrefab(FarmCollections, cell.FarmLevel, hash.b, hash.d);
		float usedHash = hash.a;
		if (prefab) {
			if (otherPrefab && hash.b < hash.a) {
				prefab = otherPrefab;
				usedHash = hash.b;
			}
		}
		else if (otherPrefab) {
			prefab = otherPrefab;
			usedHash = hash.b;
		}
		otherPrefab = PickPrefab(PlantCollections, cell.PlantLevel, hash.c, hash.d);
		if (prefab) {
			if (otherPrefab && hash.c < usedHash) {
				prefab = otherPrefab;
			}
		}
		else if (otherPrefab) {
			prefab = otherPrefab;
		}
		else {
			return;
		}
		Transform instance = Instantiate(prefab);
		position.y += instance.localScale.y * 0.5f;
		instance.localPosition = HexMetrics.Perturb(position);
		instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);
		instance.SetParent(container, false);
	}

    Transform PickPrefab (HexFeatureCollection[] collection, int level, float hash, float choice) {
		if (level > 0) {
			float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
			for (int i = 0; i < thresholds.Length; i++) {
				if (hash < thresholds[i]) {
					return collection[i].Pick(choice);
				}
			}
		}
		return null;
	}

}