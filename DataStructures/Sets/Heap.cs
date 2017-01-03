using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Sets
{
    /// <summary>
    /// A basic heap data structure, which maintains all items in partially sorted order.  The head of the heap is always 
    /// guaranteed to be the smallest item, according to the comparer specified at instantiation.
    /// </summary>
    /// <author> Wesley Oates</author>
    /// <date>Dec 16, 2016.</date>
    /// <typeparam name="T"></typeparam>
    public class Heap<T> : IPriorityQueue<T>
    {
        //TODO:  validate the heap.

        private readonly IComparer<T> _Comparer;
        internal const int DEFAULT_CAPACITY=17;
        private T[] table;


        /// <summary>
        /// Creates a new, empty heap with an IComparable type.
        /// </summary>
        /// <param name="capacity">The starting capacity of the heap.</param>
        public Heap(int capacity = DEFAULT_CAPACITY) : this((IEnumerable<T>)null, capacity) { }
        /// <summary>
        /// Creates a new heap with an IComparable type.
        /// </summary>
        /// <param name="items">The items to enqueued to the heap.  If null, an empty heap will be created.</param>
        /// <param name="capacity">The starting capacity of the heap.  If less than the count of items to enqueue, this will be 
        /// adjusted to accommodate the given items.</param>
        public Heap(IEnumerable<T> items, int capacity = DEFAULT_CAPACITY)
        {
            //Ensure a correct comparer.
            if (!typeof(IComparable<T>).IsAssignableFrom(typeof(T)))
            {
                throw new InvalidCastException("Cannot create a heap for a non-IComparable type without explicitly "
                                             + "specifying an IComparable object.");
            }
            else if (typeof(T) == typeof(string))
            {
                //Documentation recommends using a StringComparer if T is a string, something about culture-specific comparisons. 
                //Not sure if it's necessary, but doing it anyway.
                StringComparer sc = StringComparer.InvariantCulture;
                Comparison<T> c = (T a, T b) => sc.Compare(a, b);
                _Comparer = Comparer<T>.Create(c);
            }
            else
            {
                _Comparer = Comparer<T>.Default;
            }


            //Create and add items to the table, if appropriate.
            if (items == null)
            {
                table = new T[capacity];
                Count = 0;
            }
            else
            {
                table = new T[Math.Max(capacity, items.Count())];
                foreach (T item in items) Enqueue(item);
                Count = items.Count();
            }            
        }

        /// <summary>
        /// Creates a new, empty heap whose sort will depend on the given IComparer.
        /// </summary>
        /// <param name="comparer">The IComparer object to use for priority sorting.  For any two items of type T given (a,b), if 
        /// a&gt;b the IComparer should return 1, if a&lt;b the IComparer should return -1, and if a==b the IComparer should return 
        /// 0.
        /// </param>
        /// <param name="capacity">The starting capacity of the heap.</param>
        public Heap(IComparer<T> comparer, int capacity = DEFAULT_CAPACITY) : this(null, comparer, capacity) { }
        /// <summary>
        /// Creates a new heap whose sort will depend on the given IComparer.
        /// </summary>
        /// <param name="items">The items to enqueued to the heap.  If null, an empty heap will be created.</param>
        /// <param name="comparer">The IComparer object to use for priority sorting.  For any two items of type T given (a,b), if 
        /// a&gt;b the IComparer should return 1, if a&lt;b the IComparer should return -1, and if a==b the IComparer should return 
        /// 0.
        /// </param>
        /// <param name="capacity">The starting capacity of the heap.  If less than the count of items to enqueue, this will be 
        /// adjusted to accommodate the given items.</param>
        public Heap(IEnumerable<T> items, IComparer<T> comparer, int capacity = DEFAULT_CAPACITY)
        {
            _Comparer = comparer;
            if (items == null)
            {
                table = new T[capacity];
                Count = 0;
            }
            else
            {
                table = new T[Math.Max(capacity, items.Count())];
                foreach (T item in items) Enqueue(item);
                Count = items.Count();
            }            
        }


        /// <summary>
        /// The number of items currently held in the heap.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Adds the given item to the heap.
        /// </summary>        
        public void Enqueue(T item)
        {
            if (table.Length <= Count)
            {
                T[] newTable = new T[table.Length * 2];
                for (int i = 0; i < Count; i++) newTable[i] = table[i];
                table = newTable;
            }
            table[Count] = item;
            PercolateUp(Count++);            
        }

        /// <summary>
        /// Removes and returns the given item from the heap.
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            T result = table[0];
            table[0] = table[--Count];
            PercolateDown(0);
            return result;
        }

        /// <summary>
        /// Returns the head item from the heap without making any changes.
        /// </summary>        
        public T Peek() { return table[0]; }


        private int PercolateDown(int index)
        {            
            //Handle as long as there are two children.
            int childIdx = (index * 2) + 1;
            while (childIdx < Count)
            {
                T parent = table[index], rightChild = table[childIdx], leftChild = table[childIdx - 1];
                if (_Comparer.Compare(leftChild, parent) < 0)
                {
                    Swap(index, childIdx - 1);
                    index = childIdx - 1;
                }
                else if (_Comparer.Compare(rightChild, parent) < 0)
                {
                    Swap(index, childIdx);
                    index = childIdx;
                }
                else
                {
                    break;
                }
                childIdx = (index * 2) + 1;
            }

            //Handle the case of only the left child being on the table.
            childIdx = index * 2;
            if (childIdx < Count && _Comparer.Compare(table[childIdx], table[index]) < 0)
            {
                Swap(index, childIdx);
                index = childIdx;
            }

            return childIdx;

        }
        private int PercolateUp(int index)
        {
            while (index > 0)
            {
                int parentIdx = ((index-1) / 2);
                if (_Comparer.Compare(table[index], table[parentIdx]) >= 0) break;
                Swap(index, parentIdx);
                index = parentIdx;
            }
            return index;
        }
        private void Swap(int indexA, int indexB)
        {
            T temp = table[indexA];
            table[indexA] = table[indexB];
            table[indexB] = temp;
        }

    }
}
