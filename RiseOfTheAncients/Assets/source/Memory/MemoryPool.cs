
using System;
using System.Collections.Generic;

namespace ROTA.Memory
{

/// <summary>
/// A generic memory pool for avoiding constant memory allocations.
/// </summary>
public class MemoryPool<T> : IPool<T>
{
    
    /// <summary>
    /// Returns the number of T available in the pool.
    /// </summary>
    public int Size { get { return m_memoryPool.Count; } }

    /// <summary>
    /// The pool of available T.
    /// </summary>
    protected Stack<T> m_memoryPool;

    /// <summary>
    /// Function that allocates T.
    /// </summary>
    protected Func<T> m_allocator;

    /// <summary>
    /// Function that resets T when adding back to the pool.
    /// </summary>
    protected Action<T> m_reseter;

    /// <summary>
    /// Creates a memory pool. <para />
    /// Parameter "allocator" is the function that allocates T and returns it.
    /// Parameter "reseter" is the function that resets T when it is added back into the pool.
    /// Parameter "initialSize" is the initial size of the memory pool.
    /// </summary>
    public MemoryPool(Func<T> allocator, Action<T> reseter, int initialSize = 1)
    {   
        m_memoryPool = new Stack<T>(initialSize);
        m_allocator = allocator;
        m_reseter = reseter;
    }

    /// <summary>
    /// Retrieve T from the memory pool.
    /// </summary>
    public T Get()
    {
        if (m_memoryPool.Count == 0) return m_allocator();
        else return m_memoryPool.Pop();
    }

    /// <summary>
    /// Restore T to the memory pool. 
    /// </summary>
    public void Restore(T memory)
    {
        if (memory != null)
        {
            m_reseter(memory);
            m_memoryPool.Push(memory);
        }
    }

    /// <summary>
    /// Signal internal memory managed by the pool to be garbage collected. Of course, only the 
    /// memory curently in the pool is affected.
    /// </summary>
    public void SignalGC()
    {
        m_memoryPool = new Stack<T>();
    }

}

}