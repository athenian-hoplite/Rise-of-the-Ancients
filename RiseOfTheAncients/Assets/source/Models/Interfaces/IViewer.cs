namespace ROTA.Models
{

public interface IViewer : IViewModifier, IMapObject
{
    
    bool CanSee(HexCell cell);

}

}