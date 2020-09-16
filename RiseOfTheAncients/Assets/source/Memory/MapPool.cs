using System.Collections.Generic;

namespace ROTA.Memory
{

/// <summary>
/// A global map pool. Uses CollectionPool internally.
/// </summary>
public class MapPool<K, V>
{
    private static IPool<Dictionary<K,V>> m_globalPool = new CollectionPool<Dictionary<K,V>, KeyValuePair<K,V>>();

    /// <summary>
    /// Get T from the global pool.
    /// </summary>
    public static Dictionary<K,V> GLGet()
    {
        return m_globalPool.Get();
    }

    /// <summary>
    /// Restore T to the global pool.
    /// </summary>
    public static void GLRestore(Dictionary<K,V> memory)
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