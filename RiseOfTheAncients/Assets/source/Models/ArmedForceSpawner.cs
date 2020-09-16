using System.Collections.Generic;
using UnityEngine;

namespace ROTA.Models
{

public static class ArmedForceSpawner
{

    public static ArmedForce Spawn(MovableType type, Pawn pawn, HexCell location)
    {
        ArmedForce ret;
        if (type == MovableType.Land)
        {
            pawn.Init(location, Random.Range(0f, 360f));
            
            List<Unit> units = new List<Unit>();
            units.Add(new Unit(MovableType.Land, 3, 3));
            ret = new ArmedForce(location, pawn, MovableType.Land, units);
            return ret;
        }
        else
        {
            return null;
        }

    }
    
}

}