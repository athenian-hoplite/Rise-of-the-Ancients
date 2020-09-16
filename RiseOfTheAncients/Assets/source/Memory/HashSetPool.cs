using System.Collections.Generic;

namespace ROTA.Memory
{

/// <summary>
/// A global hashset pool. Uses a CollectionPool internally.
/// </summary>
public class HashSetPool<T>
{

    private static IPool<HashSet<T>> m_globalPool = new CollectionPool<HashSet<T>, T>();

    /// <summary>
    /// Get T from the global pool.
    /// </summary>
    public static HashSet<T> GLGet()
    {
        return m_globalPool.Get();
    }

    /// <summary>
    /// Restore T to the global pool.
    /// </summary>
    public static void GLRestore(HashSet<T> memory)
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