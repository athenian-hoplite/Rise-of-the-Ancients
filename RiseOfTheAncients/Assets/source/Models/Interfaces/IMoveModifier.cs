namespace ROTA.Models
{

public interface IMoveModifier
{

    MovableType MovableType { get; }
    int MovementSpeed { get; }

}

}