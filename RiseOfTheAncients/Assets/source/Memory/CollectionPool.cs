using System.Collections.Generic;

namespace ROTA.Memory
{

/// <summary>
/// A memory pool for ICollections, using empty constructor as allocator and ICollection.Clear() as reseter.
/// </summary>
public class CollectionPool<TCollection, T> : MemoryPool<TCollection> where TCollection : ICollection<T>, new()
{
    
    public CollectionPool(int initialSize = 1) : base(() => { return new TCollection(); }, (TCollection memory) => { memory.Clear(); }, initialSize)
    {
    }

}

}