namespace ROTA.Models
{

public class Unit : IMoveModifier, IViewModifier 
{

    public MovableType MovableType { get { return m_movableType; } }
    public int MovementSpeed { get { return m_movementSpeed; } }
    public int ViewRange { get { return m_viewRange; } }

    private MovableType m_movableType;
    private int m_movementSpeed;
    private int m_viewRange;

    public Unit(MovableType movableType, int movementSpeed, int viewRange)
    {
        m_movableType = movableType;
        m_movementSpeed = movementSpeed;
        m_viewRange = viewRange;
    }
    
}

}