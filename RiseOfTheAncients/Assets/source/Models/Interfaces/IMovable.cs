using System.Collections.Generic;

namespace ROTA.Models
{

public interface IMovable : IMoveModifier
{
    HexCell Destination { get; }
    Path Path { get; }
    bool CanTransverse(HexCell from, HexCell to, HexDirection direction);
    int TransversalCost(HexCell from, HexCell to);
    void MoveTo(HexCell destination);

}

}