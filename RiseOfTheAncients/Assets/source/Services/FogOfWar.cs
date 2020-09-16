using System.Collections.Generic;

using ROTA.DataStructures;
using ROTA.Memory;
using ROTA.Models;

public static class FogOfWar
{

    private static IPool<List<HexCell>> m_listPool = PoolFactory.GetPool<List<HexCell>, HexCell>();
    private static IPool<PriorityQueue<HexCell>> m_pqPool = PoolFactory.GetPool<PriorityQueue<HexCell>>();
    private static IPool<HashSet<HexCell>> m_hashSetPool = PoolFactory.GetPool<HashSet<HexCell>, HexCell>();
    private static IPool<Dictionary<HexCell, int>> m_mapPool = PoolFactory.GetPool<Dictionary<HexCell, int>, KeyValuePair<HexCell, int>>();

    public static void Show(IViewer viewer)
    {
        List<HexCell> cells = GetVisibleCells(viewer);

		for (int i = 0; i < cells.Count; i++) 
        {
			cells[i].IncreaseVisibility();
        }

		m_listPool.Restore(cells);
    }

    public static void Hide(IViewer viewer)
    {
        List<HexCell> cells = GetVisibleCells(viewer);

        for (int i = 0; i < cells.Count; i++) 
        {
			cells[i].DecreaseVisibility();
        }

        m_listPool.Restore(cells);
    }

    private static List<HexCell> GetVisibleCells(IViewer viewer)
    {
        List<HexCell> visibleCells = m_listPool.Get();
        PriorityQueue<HexCell> openSet = m_pqPool.Get();
        HashSet<HexCell> closedSet = m_hashSetPool.Get();
        Dictionary<HexCell, int> distances = m_mapPool.Get();

        openSet.Add(viewer.Location, 0);
        distances[viewer.Location] = 0;

        while ( ! openSet.IsEmpty())
        {

            HexCell current = (HexCell) openSet.Poll(); // Can never be empty optional
            closedSet.Add(current);
            visibleCells.Add(current);

            for (HexDirection dir = HexDirection.NE; dir <= HexDirection.NW; dir++)
            {
                HexCell neighbor = current.GetNeighbor(dir);

                if (neighbor == null || closedSet.Contains(neighbor)) continue;
                if ( ! viewer.CanSee(neighbor)) continue;

                distances[neighbor] = distances[current] + 1;

                if ( ! openSet.Update(neighbor, distances[neighbor])) openSet.Add(neighbor, distances[neighbor]);
            }

        }

        m_pqPool.Restore(openSet);
        m_hashSetPool.Restore(closedSet);
        m_mapPool.Restore(distances);

        return visibleCells;
    }

}