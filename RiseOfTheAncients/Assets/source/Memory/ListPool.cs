using System;
using System.Collections.Generic;

namespace ROTA.Memory
{

/// <summary>
/// A global list pool. Uses a CollectionPool internally.
/// </summary>
public static class ListPool<T>
{

    private static IPool<List<T>> m_globalPool = new CollectionPool<List<T>, T>();

    /// <summary>
    /// Get T from the global pool.
    /// </summary>
    public static List<T> GLGet()
    {
        return m_globalPool.Get();
    }

    /// <summary>
    /// Restore T to the global pool.
    /// </summary>
    public static void GLRestore(List<T> memory)
    {
        m_globalPool.Restore(memory);
    }

    /// <summary>
    /// Signal garbage collection for the global pool.
    /// </summary>
    public static void GLSignalGC()
    {
        m_globalPool.SignalGC();
    }

}

}