using System;
using System.Collections.Generic;

namespace ROTA.Memory
{

/// <summary>
/// Unified memory pool creation interface.
/// </summary>
public class PoolFactory
{

    /// <summary>
    /// Create a memory pool of ICollections such that TCollection is an ICollection of T.
    /// 
    /// Note: Uses default public constructor that all specifications of ICollection have as the allocator.
    /// Uses ICollection.Clear() as the reseter.
    /// </summary>
    public static IPool<TCollection> GetPool<TCollection, T>(int initialSize = 1) where TCollection : ICollection<T>, new()
    {
        return new CollectionPool<TCollection, T>();
    }

    /// <summary>
    /// Create a memory pool of T such that T has a public default constructor (used as allocator) and implements IResetable
    /// (used as reseter).
    /// </summary>
    public static IPool<T> GetPool<T>(int initialSize = 1) where T : IResetable, new()
    {
        return (IPool<T>) new ObjectPool<T>(initialSize);
    }

    /// <summary>
    /// Create a memory pool of T wich uses the provided allocator and reseter functions.
    /// </summary>
    public static IPool<T> GetPool<T>(Func<T> allocator, Action<T> reseter, int initialSize = 1)
    {
        return new MemoryPool<T>(allocator, reseter, initialSize);
    }

}

}