using ROTA.Models;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {

	enum OptionalToggle {
		Ignore, Yes, No
	}

	public HexGrid HexGrid;

	public Material terrainMaterial;

	int activeElevation;
	int activeWaterLevel;
	int activeTerrainTypeIndex;

	// ! FEATURES
		int activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;
		bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;
	// ! -------------------

	bool applyElevation = false;
	bool applyWaterLevel = false;
	int brushSize;
	
	OptionalToggle riverMode, roadMode, walledMode;

	bool isDrag;
	HexDirection dragDirection;
	HexCell previousCell;

	void Awake ()
    {
		// Since material keywords are persistent always set grid off as default.
		terrainMaterial.DisableKeyword("GRID_ON");

		// By default edit mode ON
		Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
		SetEditMode(true);
	}

	void Update ()
    {
		if ( ! EventSystem.current.IsPointerOverGameObject()) {
			if (Input.GetMouseButton(0)) {
				HandleInput();
				return;
			}
			if (Input.GetKeyDown(KeyCode.U)) {
				if (Input.GetKey(KeyCode.LeftShift)) {
					DestroyUnit();
				}
				else {
					// CreateUnit();
					CreatePawn();
				}
			}
		}
		previousCell = null;
	}

	public void SetEditMode (bool toggle)
    {
		enabled = toggle;
	}

	HexCell GetCellUnderCursor () {
		// ! Camera.main !!!!!!!!!!!!!!!
		return HexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
	}

	void HandleInput ()
    {

		HexCell currentCell = GetCellUnderCursor();
		if (currentCell) {

			if (this.previousCell && this.previousCell != currentCell) {
				ValidateDrag(currentCell);
			}
			else {
				this.isDrag = false;
			}
				
			EditCells(currentCell);

			this.previousCell = currentCell;
		}
		else {
			this.previousCell = null;
		}
	}

	/// <summary>
	/// Checks if the current cell is a neighbor of the previous. If so IsDrag is set to true
	/// and drag direction stores the direction linking to two cells.
	/// ! This can cause jiterry drags. To avoid this prevent consecutive drags in opposite directions with a timer.
	/// </summary>
	void ValidateDrag (HexCell currentCell)
    {
		for (this.dragDirection = HexDirection.NE; this.dragDirection <= HexDirection.NW; this.dragDirection++) {
			if (this.previousCell.GetNeighbor(this.dragDirection) == currentCell) {
				this.isDrag = true;
				return;
			}
		}
		this.isDrag = false;
	}

	void EditCells (HexCell center)
    {
		int centerX = center.Coordinates.X;
		int centerZ = center.Coordinates.Z;

		// Bottom half of brush
		for (int r = 0, z = centerZ - this.brushSize; z <= centerZ; z++, r++) {
			for (int x = centerX - r; x <= centerX + this.brushSize; x++) {
				EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}

		// Upper half of brush
		for (int r = 0, z = centerZ + this.brushSize; z > centerZ; z--, r++) {
			for (int x = centerX - this.brushSize; x <= centerX + r; x++) {
				EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}
	}

	void EditCell (HexCell cell)
    {
		if (cell) {
			if (activeTerrainTypeIndex >= 0) {
				cell.TerrainTypeIndex = activeTerrainTypeIndex;
			}
			if (this.applyElevation) {
				cell.Elevation = this.activeElevation;
			}
			if (applyWaterLevel) {
				cell.WaterLevel = activeWaterLevel;
			}
			if (applySpecialIndex) {
				cell.SpecialIndex = activeSpecialIndex;
			}
			if (applyUrbanLevel) {
				cell.UrbanLevel = activeUrbanLevel;
			}
			if (applyFarmLevel) {
				cell.FarmLevel = activeFarmLevel;
			}
			if (applyPlantLevel) {
				cell.PlantLevel = activePlantLevel;
			}
			if (this.riverMode == OptionalToggle.No) {
				cell.RemoveRiver();
			}
			if (this.roadMode == OptionalToggle.No) {
				cell.RemoveRoads();
			}
			if (walledMode != OptionalToggle.Ignore) {
				cell.Walled = walledMode == OptionalToggle.Yes;
			}
			if (this.isDrag) {
				HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
				if (otherCell) {
					if (this.riverMode == OptionalToggle.Yes) {
						otherCell.SetOutgoingRiver(dragDirection);
					}
					if (this.roadMode == OptionalToggle.Yes) {
						otherCell.AddRoad(dragDirection);
					}
				}
			}
		}
	}

	public void SetTerrainTypeIndex (int index)
    {
		activeTerrainTypeIndex = index;
	}

	public void SetBrushSize (float size)
    {
		this.brushSize = (int)size;
	}

	public void SetApplyElevation (bool toggle)
    {
		this.applyElevation = toggle;
	}

	public void SetElevation (float elevation)
    {
		this.activeElevation = (int)elevation;
	}

	public void SetRiverMode (int mode)
    {
		this.riverMode = (OptionalToggle) mode;
	}

	public void SetRoadMode (int mode)
    {
		this.roadMode = (OptionalToggle)mode;
	}

	public void SetApplyWaterLevel (bool toggle)
    {
		applyWaterLevel = toggle;
	}
	
	public void SetWaterLevel (float level)
    {
		activeWaterLevel = (int)level;
	}

	public void ShowGrid (bool visible)
    {
		if (visible) {
			terrainMaterial.EnableKeyword("GRID_ON");
		}
		else {
			terrainMaterial.DisableKeyword("GRID_ON");
		}
	}

	// ! FEATURES -------------------------------------------------------------

	public void SetApplyUrbanLevel (bool toggle)
    {
		applyUrbanLevel = toggle;
	}
	
	public void SetUrbanLevel (float level)
    {
		activeUrbanLevel = (int)level;
	}

	public void SetApplyFarmLevel (bool toggle)
    {
		applyFarmLevel = toggle;
	}

	public void SetFarmLevel (float level)
    {
		activeFarmLevel = (int)level;
	}

	public void SetApplyPlantLevel (bool toggle)
    {
		applyPlantLevel = toggle;
	}

	public void SetPlantLevel (float level) {
		activePlantLevel = (int)level;
	}

	public void SetWalledMode (int mode)
    {
		walledMode = (OptionalToggle)mode;
	}

	public void SetApplySpecialIndex (bool toggle)
    {
		applySpecialIndex = toggle;
	}

	public void SetSpecialIndex (float index)
    {
		activeSpecialIndex = (int)index;
	}

	// ! ------------------------------------------------------------------------

	// ! Units ------------------------------------------------------------------

	public Pawn pawnPrefab;

	void CreatePawn()
	{
		HexCell cell = GetCellUnderCursor();
		if (cell) 
		{
			ArmedForceSpawner.Spawn(MovableType.Land, Instantiate(pawnPrefab), cell);
		}
	}

	void CreateUnit ()
    {
		HexCell cell = GetCellUnderCursor();
		if (cell && ! cell.Unit) {
			HexGrid.AddUnit(Instantiate(HexUnit.unitPrefab), cell, Random.Range(0f, 360f));
		}
	}

	void DestroyUnit ()
    {
		HexCell cell = GetCellUnderCursor();
		if (cell && cell.Unit) {
			HexGrid.RemoveUnit(cell.Unit);
		}
	}

	// ! -------------------------------------------------------------------------

}
