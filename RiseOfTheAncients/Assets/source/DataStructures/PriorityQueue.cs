using System.Collections.Generic;

using ROTA.Memory;
using ROTA.Utils;

namespace ROTA.DataStructures
{

/// <summary>
/// A priority queue implemented as a heap. Suports both max and min behaviour.
/// </summary>
public class PriorityQueue<T> : IResetable
{

    private const int DEFAULT_INITIAL_HEAP_SIZE = 1;

    /// <summary>
    /// Heap with which this priority queue works with, in the form of a list for performance reasons since
    /// it is internally a continous array of value types (struct Node).
    /// </summary>
    private List<Node> m_heap = null;

    /// <summary>
    /// Dictionary that maps an element of the priority queue to its index in the heap 
    /// (or indices if multiple instances exist). This allows for constant containment check and logarithmic removal,
    /// while it adds linear memory and unavoidable overhead.
    /// </summary>
    private Dictionary<T, HashSet<int>> m_map = null;

    /// <summary>
    /// Flag that indicates whether the priority queue functions internally as a min-heap or a max-heap.
    /// </summary>
    private bool m_isMinQueue = false;

    /// <summary>
    /// Create a priority queue, with default min queue behaviour.
    /// </summary>
    public PriorityQueue() : this(true)
    {
    }

    /// <summary>
    /// Create a priority queue with a specified initial size and 
    /// either a min or max queue, depending on minQueue value.
    /// </summary>
    public PriorityQueue(bool minQueue, int size = DEFAULT_INITIAL_HEAP_SIZE)
    {
        m_isMinQueue = minQueue;
        m_heap = new List<Node>(size);
        m_map = new Dictionary<T, HashSet<int>>(size);
    }

    /// <summary>
    /// Change priority queue behaviour to a minimum priority queue. This will completly clear
    /// the queue.
    /// 
    /// Complexity: O(n), if empty O(1)
    /// </summary>
    public void SetAsMinQueue()
    {
        Reset();
        m_isMinQueue = true;
    }

    /// <summary>
    /// Change priority queue behaviour to a maximum priority queue. This will completly clear
    /// the queue.
    /// 
    /// Complexity: O(n), if empty O(1)
    /// </summary>
    public void SetAsMaxQueue()
    {
        Reset();
        m_isMinQueue = false;
    }

    /// <summary>
    /// Returns true if there are no elements in the queue. Same as Size() == 0.
    /// 
    /// Complexity: O(1)
    /// </summary>
    public bool IsEmpty()
    {
        return m_heap.Count == 0;
    }

    /// <summary>
    /// Get the number of elements in the queue.
    /// 
    /// Complexity: O(1)
    /// </summary>
    public int Size()
    {
        return m_heap.Count;
    }

    /// <summary>
    /// Clears the priority queue.
    /// 
    /// Complexity: O(n), O(1) if empty.
    /// </summary>
    public void Reset()
    {
        m_heap.Clear(); // O(n)
        m_map.Clear(); // O(n)
    }

    /// <summary>
    /// Get the highest priority element, without removing it from the queue. If the queue is empty
    /// an empty Optional is returned.
    /// 
    /// Complexity: O(1)
    /// </summary>
    public Optional<T> Peek() 
    {
        if (IsEmpty())
        {
            return new Optional<T>();
        }
        else
        {
            return m_heap[0].element;
        }
    }

    /// <summary>
    /// Get the highest priority element in the priority queue, removing it from the queue.
    /// 
    /// Complexity: O(log(n))
    /// </summary>
    public Optional<T> Poll() 
    {
        return RemoveAt(0);
    }

    /// <summary>
    /// Check if the priority queue contains an element.
    /// 
    /// Complexity: O(1)
    /// </summary>
    public bool Contains(T elem) 
    {
        if (elem == null) return false;
        return m_map.ContainsKey(elem); // O(1)
    }

    /// <summary>
    /// Add an element to the priority queue with the given priority value.
    /// 
    /// Complexity: O(log(n))
    /// </summary>
    public void Add(T elem, int priority) 
    {
        if (elem == null) throw new System.ArgumentNullException();

        if ( ! m_isMinQueue) { priority = - priority; }

        m_heap.Add(new Node(elem, priority));

        int indexOfLastElem = m_heap.Count - 1;
        MapAdd(elem, indexOfLastElem); // O(1), but adds overhead

        Swim(indexOfLastElem); // O(log(n))
    }

    /// <summary>
    /// Compares the priorities of nodes at the given indices returning true if node at index i <= to node at index j.
    /// 
    /// Complexity: O(1)
    /// </summary>
    private bool Less(int i, int j) 
    {
        Node node1 = m_heap[i];
        Node node2 = m_heap[j];
        return node1.priority <= node2.priority;
    }

    /// <summary>
    /// Bubble-up (if possible) from the given index to enforce the heap invariant.
    /// 
    /// Complexity: O(log(n))
    /// </summary>
    private void Swim(int k) 
    {
        // Grab the index of the next parent node WRT to k
        int parent = (k - 1) / 2;

        // Keep swimming while we have not reached the
        // root and while we're less than our parent.
        while (k > 0 && Less(k, parent)) 
        {
            // Exchange k with the parent
            Swap(parent, k);
            k = parent;

            // Grab the index of the next parent node WRT to k
            parent = (k - 1) / 2;
        }
    }

    /// <summary>
    /// Bubble down (if possible) from the given index to enforce the heap invariant.
    /// 
    /// Complexity: O(log(n))
    /// </summary>
    private void Sink(int k) 
    {
        int heapSize = m_heap.Count;

        while (true) 
        {
            int left = 2 * k + 1; // Left  node
            int right = 2 * k + 2; // Right node
            int smallest = left; // Assume left is the smallest node of the two children

            // Find which is smaller left or right
            // If right is smaller set smallest to be right
            if (right < heapSize && Less(right, left)) smallest = right;

            // Stop if we're outside the bounds of the tree
            // or stop early if we cannot sink k anymore
            if (left >= heapSize || Less(k, smallest)) break;

            // Move down the tree following the smallest node
            Swap(smallest, k);
            k = smallest;
        }
    }

    /// <summary>
    /// Swap two elements in the heap.
    /// 
    /// Complexity: O(1)
    /// </summary>
    private void Swap(int i, int j) 
    {
        Node i_node = m_heap[i];
        Node j_node = m_heap[j];

        m_heap[i] = j_node;
        m_heap[j] = i_node;

        MapSwap(i_node.element, j_node.element, i, j);
    }

    /// <summary>
    /// Erase an element from the priority queue.
    /// 
    /// Complexity: O(log(n))
    /// </summary>
    public bool Remove(T element) 
    {
        if (element == null) return false;

        int index = MapGet(element); // O(1)
        if (index >= 0) RemoveAt(index); // O(log(n))
        return index >= 0;
    }

    /// <summary>
    /// Update a given element with a new priority value. If the element does not exist false is returned
    /// and nothing is done. If the element exists it is updated and true is returned.
    /// 
    /// Coplexity: O(log(n)), O(1) if element does not exist in the priority queue.
    /// </summary>
    public bool Update(T element, int newPriority)
    {
        if (m_heap.Count == 0 || ! Contains(element)) return false;

        // Get "first" value in the index hashset
        var enumerator = m_map[element].GetEnumerator();
        enumerator.MoveNext();

        int index = enumerator.Current;
        Node node = m_heap[index];
        node.priority = newPriority;

        // Try sinking element
        Sink(index);

        // If sinking not possible then try swimming
        if (m_heap[index].element.Equals(node.element)) Swim(index);

        return true;
    }

    /// <summary>
    /// Remove, and return, an element from the priority queue at the given index.
    /// 
    /// Complexity: O(log(n))   
    /// </summary>
    private Optional<T> RemoveAt(int i) 
    {
        if (IsEmpty()) return new Optional<T>();

        int indexOfLastElem = m_heap.Count - 1;
        T removed_data = m_heap[i].element;
        Swap(i, indexOfLastElem);

        // Obliterate the value
        m_heap.RemoveAt(indexOfLastElem);
        MapRemove(removed_data, indexOfLastElem);

        // Removed last element
        if (i == indexOfLastElem) return new Optional<T>(removed_data);

        T elem = m_heap[i].element;

        // Try sinking element
        Sink(i);

        // If sinking not possible then try swimming
        if (m_heap[i].element.Equals(elem)) Swim(i);

        return new Optional<T>(removed_data);
    }

    /// <summary>
    /// Add a key-value pair to the index map.
    /// 
    /// Complexity: O(1)
    /// </summary>
    private void MapAdd(T value, int index) 
    {
        if (m_map.ContainsKey(value))
        {
            m_map[value].Add(index);
        }
        else
        {
            HashSet<int> set = new HashSet<int>();
            set.Add(index);
            m_map[value] = set;
        }
    }

    /// <summary>
    /// Remove an index from the given value in the index map.
    /// 
    /// Complexity: O(1)
    /// </summary>
    private void MapRemove(T value, int index) 
    {
        HashSet<int> set = m_map[value];
        set.Remove(index); // O(1)
        if (set.Count == 0) m_map.Remove(value);
    }

    /// <summary>
    /// Returns the index, or -1 if not present, of a value in the priority queue.
    /// In case the value is present more than once, the largest index is returned.
    /// 
    /// Complexity: O(1)
    /// </summary>
    private int MapGet(T value) 
    {
        if (m_map.ContainsKey(value))
        {
            var enumerator = m_map[value].GetEnumerator();
            enumerator.MoveNext();
            return enumerator.Current; 
        }
        else
        {
            return -1;
        }
    }

    /// <summary>
    /// Swaps two values in the index map. This is used when swimming or sinking in which element
    /// positions are swapped in the heap.
    /// 
    /// Complexity: O(1)
    /// </summary>
    private void MapSwap(T val1, T val2, int val1Index, int val2Index) 
    {
        HashSet<int> set1 = m_map[val1];
        HashSet<int> set2 = m_map[val2];

        // O(1)
        set1.Remove(val1Index);
        set2.Remove(val2Index);

        // O(1)
        set1.Add(val2Index);
        set2.Add(val1Index);
    }

    /// <summary>
    /// Struct used to pair element T with its priority value.
    /// </summary>
    private struct Node
    {
        public T element;
        public int priority;

        public Node(T element, int priority)
        {
            this.element = element;
            this.priority = priority;
        }
    }

}

}