using System.Collections.Generic;
using ROTA.Utils;
using UnityEngine;

namespace ROTA.Models
{

public class MapPawn : IMovable, IViewer
{
    public HexCell Location { get { return m_location; } }
    public HexCell Destination { get { return m_destination; } }
    public Path Path { get { return m_path; } }
    public Pawn Pawn { get { return m_pawn; } }
    public MovableType MovableType { get { return m_movableType; } }
    public int MovementSpeed { get { return m_movementSpeed; } }
    public int ViewRange { get { return m_viewRange; } }

    protected HexCell m_location;
    protected HexCell m_destination = null;
    protected Path m_path = null;
    protected Pawn m_pawn;
    protected MovableType m_movableType;
    protected int m_movementSpeed;
    protected int m_viewRange;

    private MapPawn() {}

    public MapPawn(HexCell location, Pawn pawn, MovableType type)
    {
        m_location = location;
        m_pawn = pawn;
        location.pawn = this; // ! Stupid - Cell stores pawn present there ?? 
        m_movableType = type;
    }

    public bool CanSee(HexCell cell)
    {
        if ( ! cell.Explorable) return false;

        int distance = m_location.Coordinates.DistanceTo(cell.Coordinates);
        if (distance + cell.ViewElevation > m_viewRange) return false;

        return true;
    }

    public bool CanTransverse(HexCell from, HexCell to, HexDirection direction)
    {
        return m_movableType.CanTransverse(from, to, direction);
    }

    public int TransversalCost(HexCell from, HexCell to)
    {
        return 1;
    }

    public void MoveTo(HexCell destination)
    {
        if (destination == m_location) return;

        m_destination = destination;
        Optional<Path> op = Pathfinding.FindPath(this);
        if (op)
        {
            if (m_path != null) 
            {
                EndMove();
            }
            
            m_path = (Path) op;
            m_path.Show();
            DoMove();
        }
    }

    private void DoMove()
    {
        if (m_path.Forward())
        {
            PathNode node = (PathNode) m_path.Current();
            CommandQueue.AddCommand(this, WorldTime.Future(node.costTo), 
            () => 
            {
                ChangePositionTo(node.location);
                this.DoMove();
            });
        }
        else
        {
            EndMove();
        }
    }

    private void EndMove()
    {
        m_path.EndPath();
        m_path = null;
        m_destination = null;
    }

    private void ChangePositionTo(HexCell location)
    {
        FogOfWar.Hide(this);
        m_location = location;
        m_pawn.TeleportTo(location);
        FogOfWar.Show(this);
    }

}

}