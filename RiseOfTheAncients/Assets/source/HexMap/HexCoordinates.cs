using UnityEngine;
using System.IO;

/// <summary>
/// Represents the hexagon coordinate system.
/// </summary>
[System.Serializable]
public struct HexCoordinates {

    [SerializeField]
    private int x, z;

	public int X { get { return x; } }

    public int Y { get { return -X - Z; } }

	public int Z { get { return z; } }

	/// <summary>
	/// X and Z are taken at face value ! No conversion takes place.
	/// </summary>
	public HexCoordinates (int x, int z) {
		if (HexMetrics.Wrapping) {
			int oX = x + z / 2;
			if (oX < 0) {
				x += HexMetrics.WrapSize;
			}
			else if (oX >= HexMetrics.WrapSize) {
				x -= HexMetrics.WrapSize;
			}
		}
		this.x = x;
		this.z = z;
	}

    /// <summary>
    /// Creates a HexCoordinates object from regular x, z coordinates.
    /// </summary>
    public static HexCoordinates FromOffsetCoordinates (int x, int z) {
		return new HexCoordinates(x - z / 2, z);
	}

    /// <summary>
    /// Creates a HexCoordinates object from a 3D position.
    /// </summary>
    public static HexCoordinates FromPosition (Vector3 position) {

        // Get X and Y
        float x = position.x / HexMetrics.InnerDiameter;
		float y = -x;

        // Shift along Z axis
		float offset = position.z / (HexMetrics.OuterRadius * 3f);
		x -= offset;
		y -= offset;

        // Round and derive Z
        int iX = Mathf.RoundToInt(x);
		int iY = Mathf.RoundToInt(y);
		int iZ = Mathf.RoundToInt(-x -y);

        // Detect inconsistency due to rounding error
        // Reconstruct from lowest rounding delta
        if (iX + iY + iZ != 0) {
			float dX = Mathf.Abs(x - iX);
			float dY = Mathf.Abs(y - iY);
			float dZ = Mathf.Abs(-x -y - iZ);

			if (dX > dY && dX > dZ) {
				iX = -iY - iZ;
			}
			else if (dZ > dY) {
				iZ = -iX - iY;
			}
		}

		return new HexCoordinates(iX, iZ);
	}

	public int DistanceTo (HexCoordinates other) {
		// Absolute coordinate distance
		int xy = (x < other.x ? other.x - x : x - other.x) + (Y < other.Y ? other.Y - Y : Y - other.Y);
		
		// Needs to account for wrapping
		// Since determining if wrapping produces a shorter distance is not trivial
		// Simple compute the absolute first, then wrapping on both sides and return the shortest
		if (HexMetrics.Wrapping) {
			other.x += HexMetrics.WrapSize;
			int xyWrapped = (x < other.x ? other.x - x : x - other.x) +(Y < other.Y ? other.Y - Y : Y - other.Y);
			if (xyWrapped < xy) {
				xy = xyWrapped;
			}
			else {
				other.x -= 2 * HexMetrics.WrapSize;
				xyWrapped = (x < other.x ? other.x - x : x - other.x) + (Y < other.Y ? other.Y - Y : Y - other.Y);
				if (xyWrapped < xy) {
					xy = xyWrapped;
				}
			}
		}

		return (xy + (z < other.z ? other.z - z : z - other.z)) / 2;
	}

    public override string ToString () {
		return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
	}

	public string ToStringOnSeparateLines () {
		return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
	}

	public void Save (BinaryWriter writer) {
		writer.Write(x);
		writer.Write(z);
	}

	public static HexCoordinates Load (BinaryReader reader) {
		HexCoordinates c;
		c.x = reader.ReadInt32();
		c.z = reader.ReadInt32();
		return c;
	}
}
