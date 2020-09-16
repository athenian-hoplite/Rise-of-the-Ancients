using ROTA.Models;
using ROTA.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour {

    public WorldTime WorldTime;
	public HexGrid grid;
    HexCell currentCell;
    HexUnit selectedUnit;
    MapPawn selectedPawn;

    List<HexCell> Path;
    List<HexCell> OldPath;

    void Update () {
		if ( ! EventSystem.current.IsPointerOverGameObject()) {
			if (Input.GetMouseButtonDown(0)) {
				DoSelection();
			}
            else if (selectedUnit || selectedPawn != null) {
				if (Input.GetMouseButtonDown(1)) {
					DoMove();
				}
				else {
					DoPathfinding();
				}
			}

            if (Input.GetKeyDown(KeyCode.Space)) WorldTime.TogglePause();
		}
	}

    public void SetEditMode (bool toggle) {
		enabled = ! toggle;
		grid.ShowUI( ! toggle);
        grid.ClearPath();
		if (toggle) {
			Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
		}
		else {
			Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
		}
	}

    /// <summary>
    /// Updates the currently hovered cell. If it is different from the previously hovered return true,
    /// false otherwise.
    /// </summary>
    bool UpdateCurrentCell () {
		HexCell cell = grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
		if (cell != currentCell) {
			currentCell = cell;
			return true;
		}
		return false;
	}

    void DoSelection () {
        grid.ClearPath();
		UpdateCurrentCell();
		if (currentCell) {
			selectedUnit = currentCell.Unit;
            selectedPawn = currentCell.pawn;
		}
	}

    void DoPathfinding () {
		if (UpdateCurrentCell()) {
            /*
			if (currentCell && selectedUnit.IsValidDestination(currentCell)) {
                // grid.FindPath(selectedUnit.Location, currentCell, selectedUnit);

                
                Optional<List<HexCell>> op = Pathfinding.FindPath(
                    selectedUnit.Location,
                    currentCell, 
                    (HexCell cur, HexCell dest) => 
                    {
                        return cur.Coordinates.DistanceTo(dest.Coordinates);
                    },
                    (HexCell cur, HexCell dest, HexDirection dir) =>
                    {
                        if ( ! cur.IsExplored) return false;
                        if (dest.IsUnderwater) return false;
                        if (cur.GetEdgeType(dir) == HexEdgeType.Cliff) return false;
                        return true;
                    },
                    (HexCell cur, HexCell neighbor) =>
                    {
                        return 1;
                    }
                );

                if (op)
                {
                    Path = (List<HexCell>) op;
                    ClearPath();
                    ShowPath();
                }
			}
			else { // If no cell hovered (aka out of map) clear path
                // grid.ClearPath();
                ClearPath();
            }*/

            

            
		}
	}

    void DoMove () {
        if (currentCell && selectedPawn != null)
        {
            selectedPawn.MoveTo(currentCell);
        }
            /*
		if (Path != null) {
			selectedUnit.Travel(Path);
            // grid.ClearPath();
            ClearPath();
        }*/
	}

    public void ClearPath()
    {
        if (OldPath != null)
        {
            foreach (HexCell cell in OldPath)
            {
                cell.DisableHighlight();
            }
        }
    }

}