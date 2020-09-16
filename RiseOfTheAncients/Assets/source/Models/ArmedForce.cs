using System.Collections;
using System.Collections.Generic;

using ROTA.DataStructures;
using ROTA.Utils;
using UnityEngine;

namespace ROTA.Models
{

public class ArmedForce : MapPawn
{

    private PriorityQueue<IMoveModifier> m_moveModifiers = new PriorityQueue<IMoveModifier>(true); // Min queue
    private PriorityQueue<IViewModifier> m_viewModifiers = new PriorityQueue<IViewModifier>(false); // Max queue
    private List<Unit> m_units = new List<Unit>();

    public ArmedForce(HexCell location, Pawn pawn, MovableType type, List<Unit> units) : base(location, pawn, type)
    {
        foreach (Unit unit in units)
        {
            AddUnit(unit);
        }

        FogOfWar.Show(this); // Depends on units being added
    }

    public void AddUnit(Unit unit)
    {
        m_moveModifiers.Add(unit, unit.MovementSpeed);
        m_viewModifiers.Add(unit, unit.ViewRange);
        m_units.Add(unit);

        m_movementSpeed = m_moveModifiers.Peek().Value.MovementSpeed;
        m_viewRange = m_viewModifiers.Peek().Value.ViewRange;
    }

    public void RemoveUnit(Unit unit)
    {
        m_moveModifiers.Remove(unit);
        m_viewModifiers.Remove(unit);
        m_units.Remove(unit); // ! O(n)

        if (m_units.Count != 0)
        {
            m_movementSpeed = m_moveModifiers.Peek().Value.MovementSpeed;
            m_viewRange = m_viewModifiers.Peek().Value.ViewRange;
        }
        else
        {
            throw new System.NotImplementedException();
        }
    }

}

}