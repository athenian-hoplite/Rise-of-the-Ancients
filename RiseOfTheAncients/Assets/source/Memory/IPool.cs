namespace ROTA.Memory
{

/// <summary>
/// Generic interface of memory pool.
/// </summary>
public interface IPool<T>
{

    /// <summary>
    /// Get a T from the memory pool.
    /// </summary>
    T Get();

    /// <summary>
    /// Restore a T to the memory pool.
    /// </summary>
    void Restore(T memory);

    /// <summary>
    /// Signal memory managed by memory pool to be garbage collected.
    /// </summary>
    void SignalGC();

}

}