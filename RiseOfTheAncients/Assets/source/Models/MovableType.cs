namespace ROTA.Models
{
    
public enum MovableType 
{
    Land = 0, Naval = 1, Air = 2
}

public static class MovableTypeExtensions
{

    public static bool CanTransverse(this MovableType type, HexCell from, HexCell to, HexDirection direction)
    {
        if ( ! to.IsExplored) return false;

        if (type == MovableType.Land)
        {
            return  from.GetEdgeType(to) != HexEdgeType.Cliff && ! to.IsUnderwater;
        }
        else if (type == MovableType.Naval)
        {
            return to.IsUnderwater;
        }
        else if (type == MovableType.Air)
        {
            return true; // Air units can walk anywhere
        }
        else
        {
            throw new System.NotImplementedException();
        }
    }

}

}