
/// <summary>
/// Represents one of the six hexagonal directions: NE, E, SE, SW, W and NW.
/// </summary>
public enum HexDirection {
    NE, E, SE, SW, W, NW
}

/// <summary>
/// This class defines extension methods for the enum HexDirection.
/// </summary>
public static class HexDirectionExtensions {

    /// <summary>
    /// Gets the opposite direction, e.g. NE.Opposite() = SW.
    /// </summary>
    public static HexDirection Opposite (this HexDirection direction) {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }

    /// <summary>
    /// Get the previous direction (counter-clockwise).
    /// </summary>
    public static HexDirection Previous (this HexDirection direction) {
		return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
	}
    
    /// <summary>
    /// Get the next direction (clockwise).
    /// </summary>
	public static HexDirection Next (this HexDirection direction) {
		return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
	}

    /// <summary>
    /// Get the direction before the previous direction (counter-clockwise).
    /// </summary>
    public static HexDirection Previous2 (this HexDirection direction) {
		direction -= 2;
		return direction >= HexDirection.NE ? direction : (direction + 6);
	}

	/// <summary>
	/// Get the direction after the next direction (clockwise).
	/// </summary>
	public static HexDirection Next2 (this HexDirection direction) {
		direction += 2;
		return direction <= HexDirection.NW ? direction : (direction - 6);
	}

}