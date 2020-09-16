using System.Collections.Generic;
using ROTA.DataStructures;
using ROTA.Memory;
using ROTA.Models;
using ROTA.Utils;

public struct PathNode
{
    public HexCell location;
    public int costTo;

    public PathNode(HexCell location, int costTo)
    {
        this.location = location;
        this.costTo = costTo;
    }
}

public static class Pathfinding
{
    public static Optional<Path> FindPath(MapPawn mapPawn)
    {
        HexCell start = mapPawn.Location;
        HexCell dest = mapPawn.Destination;

        // For node n, cameFrom[n] is the node immediately preceding it on the cheapest path from start
        // to n currently known.
        Dictionary<HexCell, PathNode> cameFrom = MapPool<HexCell, PathNode>.GLGet();

        // For node n, gScore[n] is the cost of the cheapest path from start to n currently known.
        Dictionary<HexCell, int> gScore = MapPool<HexCell, int>.GLGet();
        gScore[start] = 0;

        // For node n, fScore[n] = gScore[n] + h(n). fScore[n] represents our current best guess as to
        // how short a path from start to finish can be if it goes through n.
        int startFScore = Heuristic(start, dest);

        // The set of discovered nodes that may need to be (re-)expanded.
        PriorityQueue<HexCell> openSet = ObjectPool<PriorityQueue<HexCell>>.GLGet();
        openSet.Add(start, startFScore);

        // The set of nodes for which a shortest path has already been found
        HashSet<HexCell> closedSet = HashSetPool<HexCell>.GLGet();

        while( ! openSet.IsEmpty())
        {
            HexCell current = (HexCell) openSet.Poll(); // Explicit cast because we know this can never be null
            if (current == dest)
            {
                ObjectPool<PriorityQueue<HexCell>>.GLRestore(openSet);
                MapPool<HexCell, int>.GLRestore(gScore);
                HashSetPool<HexCell>.GLRestore(closedSet);
                return ReconstructPath(cameFrom, current);
            }

            closedSet.Add(current);

            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++) 
            {
				HexCell neighbor = current.GetNeighbor(dir);

                if (neighbor == null || closedSet.Contains(neighbor)) continue; // Dont waste time with already evaluated nodes

                if ( ! mapPawn.CanTransverse(current, neighbor, dir)) continue; // We cannot go there

                int transversalCost = mapPawn.TransversalCost(current, neighbor);
                int tentative_gScore = gScore[current] + transversalCost;

                int neighbor_gScore = gScore.ContainsKey(neighbor) ? gScore[neighbor] : int.MaxValue;
                if (tentative_gScore < neighbor_gScore)
                {
                    cameFrom[neighbor] = new PathNode(current, transversalCost);
                    gScore[neighbor] = tentative_gScore;
                    int fScore = tentative_gScore + Heuristic(neighbor, dest);

                    if ( ! openSet.Update(neighbor, fScore)) openSet.Add(neighbor, fScore);
                }
            }
        }

        // Open set is empty but goal was never reached
        ObjectPool<PriorityQueue<HexCell>>.GLRestore(openSet);
        MapPool<HexCell, PathNode>.GLRestore(cameFrom);
        MapPool<HexCell, int>.GLRestore(gScore);
        HashSetPool<HexCell>.GLRestore(closedSet);
        return null;
    }

    private static int Heuristic(HexCell current, HexCell destination)
    {
        return current.Coordinates.DistanceTo(destination.Coordinates);
    }

    private static Path ReconstructPath(Dictionary<HexCell, PathNode> cameFrom, HexCell current)
    {
        List<PathNode> path = ListPool<PathNode>.GLGet();

        path.Add(new PathNode(current, cameFrom[current].costTo)); // Last Node
        current = cameFrom[current].location;

        while(cameFrom.ContainsKey(current))
        {
            PathNode node = cameFrom[current];
            path.Add(new PathNode(current, node.costTo));
            current = node.location;
        }   
        path.Add(new PathNode(current, 0)); // Start Node

        path.Reverse(); // ! O(n)

        MapPool<HexCell, PathNode>.GLRestore(cameFrom); // Restore memory

        return new Path(path);
    }

}