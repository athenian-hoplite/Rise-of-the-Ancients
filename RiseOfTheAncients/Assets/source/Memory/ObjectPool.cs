namespace ROTA.Memory
{

/// <summary>
/// Specialization of MemoryPool that accepts objects that have an empty public constructor and
/// implement IResetable such that they have a predefined allocator and reseter.
/// </summary>
public class ObjectPool<T> : MemoryPool<T> where T : IResetable, new()
{

    public ObjectPool(int initialSize = 1) : base(() => { return new T(); }, (T memory) => { memory.Reset(); }, initialSize) {}

    // The global generic pool
    private static ObjectPool<T> m_globalPool = new ObjectPool<T>();

    /// Methods for acessing the global pool

    /// <summary>
    /// Get T from the global pool.
    /// </summary>
    public static T GLGet()
    {
        return m_globalPool.Get();
    }

    /// <summary>
    /// Restore T to the global pool.
    /// </summary>
    public static void GLRestore(T memory)
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