using System.Collections.Generic;
using ROTA.Memory;
using ROTA.Utils;
using UnityEngine;

namespace ROTA.Models
{

public class Path
{
    /// <summary>
    /// The List of PathNodes representing the path.
    /// </summary>
    private List<PathNode> m_path;

    /// <summary>
    /// The current index of the active PathNode.
    /// </summary>
    private int m_index;

    /// <summary>
    /// Flag if path is being shown.
    /// </summary>
    private bool m_showingPath;

    public Path(List<PathNode> path)
    {
        m_path = path;
        m_index = 0;
        m_showingPath = false;
    }

    /// <summary>
    /// Returns true if EndPath() was called or the currently active PathNode is the last.
    /// </summary>
    public bool IsFinished()
    {
        return m_path == null || m_index == m_path.Count - 1;
    }

    /// <summary>
    /// Gets the currently active PathNode.
    /// 
    /// Note: If Path.EndPath() was called this returns an empty Optional.
    /// </summary>
    public Optional<PathNode> Current()
    {
        if (m_path == null) return new Optional<PathNode>();
        else return m_path[m_index];
    }

    /// <summary>
    /// Moves the path forward, making the next node the currently active path node.
    /// Returns true if there was another node in the path, false if the path has finished (active node unaltered).
    /// </summary>
    public bool Forward()
    {
        if (m_path == null || IsFinished()) return false;

        m_index++;

        if (m_showingPath) 
        {
            if (m_index != 0) m_path[m_index - 1].location.DisableHighlight();
        }

        return true;
    }

    /// <summary>
    /// Signal that path has ended. This will hide the graphical representation
    /// and restore memory to the list pool. This should be called everytime before disposing of a path.
    /// </summary>
    public void EndPath()
    {
        Hide();
        ListPool<PathNode>.GLRestore(m_path);
        m_path = null;
    }

    /// <summary>
    /// Show the path graphical representation.
    /// </summary>
    public void Show()
    {
        if (m_path == null || m_index >= m_path.Count) return;

        m_showingPath = true;

        for (int i = m_index; i < m_path.Count; i++)
        {
            m_path[i].location.EnableHighlight(Color.white);
        }

        m_path[m_path.Count - 1].location.EnableHighlight(Color.red);
    }

    /// <summary>
    /// Hide the path graphical representation.
    /// </summary>
    public void Hide()
    {
        if (m_path == null || m_index >= m_path.Count) return;

        m_showingPath = false;

        for (int i = m_index; i < m_path.Count; i++)
        {
            m_path[i].location.DisableHighlight();
        }
    }

}

}