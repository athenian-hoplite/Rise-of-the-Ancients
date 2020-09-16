using UnityEngine;
using System.Collections.Generic;

public class HexCellShaderData : MonoBehaviour {
	
	Texture2D cellTexture;
    Color32[] cellTextureData;

    public bool ImmediateMode { get; set; }
    List<HexCell> transitioningCells = new List<HexCell>();
    const float transitionSpeed = 350f;

    bool needsVisibilityReset;
    public HexGrid Grid { get; set; }

    /// <summary>
    /// Runs only when object is enabled. This means no matter how many times a refresh is requested in a frame
    /// the data will only be updated once during the LateUpdate update cycle.
    /// </summary>
    void LateUpdate () {
        if (needsVisibilityReset) {
			needsVisibilityReset = false;
			Grid.ResetVisibility();
		}

        int delta = (int) (Time.deltaTime * transitionSpeed);
        if (delta == 0) {
			delta = 1;
		}

        for (int i = 0; i < transitioningCells.Count; i++) {
			if ( ! UpdateCellData(transitioningCells[i], delta)) {
                // Remove the current cell if it has finished transitioning
                // This way is faster than using RemoveAt since no shifting occurs when removing last element
				transitioningCells[i--] = transitioningCells[transitioningCells.Count - 1];
				transitioningCells.RemoveAt(transitioningCells.Count - 1);
			}
		}

		cellTexture.SetPixels32(cellTextureData);
		cellTexture.Apply();

         // If visibility transitions need to apply then keep updating
		enabled = transitioningCells.Count > 0;
	}

    public void Initialize (int x, int z) {
        if (cellTexture) {
			cellTexture.Resize(x, z);
		}
        else {
            cellTexture = new Texture2D(x, z, TextureFormat.RGBA32, false, true); // No mipmaps, linear color space
		    cellTexture.filterMode = FilterMode.Point; // No blending
		    cellTexture.wrapModeU = TextureWrapMode.Repeat; // Needed for wrapping (E-W)
			cellTexture.wrapModeV = TextureWrapMode.Clamp;

            Shader.SetGlobalTexture("_HexCellData", cellTexture); // Make this texture globally available
        }

        // Create manually what shaders normally create for all textures but not for global ones
        // A Vector4 with inverse of size and size
        Shader.SetGlobalVector("_HexCellData_TexelSize", new Vector4(1f / x, 1f / z, x, z));

        if (cellTextureData == null || cellTextureData.Length != x * z) {
			cellTextureData = new Color32[x * z];
		}
		else {
			for (int i = 0; i < cellTextureData.Length; i++) {
				cellTextureData[i] = new Color32(0, 0, 0, 0); // Clear old data
			}
		}

        transitioningCells.Clear();
        enabled = true; // Trigger update of data
	}

    public void RefreshTerrain (HexCell cell) {
        // Terrain type is stored in the alpha channel
		cellTextureData[cell.Index].a = (byte) cell.TerrainTypeIndex;

        enabled = true; // Trigger update of data
	}

    public void RefreshVisibility (HexCell cell) {
        int index = cell.Index;

        if (ImmediateMode) {
             // Visibility is stored in the red channel of cell data
            // 1 = visible, 0 = Fog of War, HexCellShaderData works with bytes
			cellTextureData[index].r = cell.IsVisible ? (byte)255 : (byte)0;
            // Whether a cell is explored or not is stored in the green channel
			cellTextureData[index].g = cell.IsExplored ? (byte)255 : (byte)0;
		}
		else if (cellTextureData[index].b != 255) { // The blue channel stores wether the cell is already in transition
			cellTextureData[index].b = 255; // Mark cell as transitioning
			transitioningCells.Add(cell);
		}

		enabled = true; // Trigger update of data
	}

    bool UpdateCellData (HexCell cell, int delta) {
		int index = cell.Index;
		Color32 data = cellTextureData[index];
		bool stillUpdating = false;

        // If is explored but green channel (where exploration is stored)
        // Is not yet 255 then it is still transitioning exploration
        if (cell.IsExplored && data.g < 255) {
			stillUpdating = true;

            int t = data.g + delta;
			data.g = t >= 255 ? (byte)255 : (byte)t;
		}

        // If is visible but red channel (where visibility is stored)
        // Is not yet 255 then it is still transitioning fog of war
        if (cell.IsVisible) {
			if (data.r < 255) {
				stillUpdating = true;
				int t = data.r + delta;
				data.r = t >= 255 ? (byte)255 : (byte)t;
			}
		} // If not visible but R larger than zero then its transitioning from visible to fog of war
		else if (data.r > 0) {
			stillUpdating = true;
			int t = data.r - delta;
			data.r = t < 0 ? (byte)0 : (byte)t;
		}

        if ( ! stillUpdating) {
			data.b = 0; // Set blue channel to singal that cell is no longer transitioning
		}
		cellTextureData[index] = data;
		return stillUpdating;
	}

    public void ViewElevationChanged () {
		needsVisibilityReset = true;
		enabled = true;
	}

	public void SetMapData (HexCell cell, float data) {
		// ! Z is used for pathfinding. Dont do this
		cellTextureData[cell.Index].b = data < 0f ? (byte)0 : (data < 1f ? (byte)(data * 254f) : (byte)254);
		enabled = true;
	}

}