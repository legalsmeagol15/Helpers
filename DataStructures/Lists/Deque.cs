using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>
    /// An array-backed list that supports O(1) operations to insert or delete at the beginning or at the end of the 
    /// list, but also O(1) operations to read at any index.  Operations to Insert or to RemoveAt any point in the 
    /// middle will be O(1) in the best case and O(n) in the worst case.
    /// </summary>
    /// <remarks>
    /// This list works by allowing the starting item's index to vary to any point modded by the capacity of the list.
    /// Validated 3/17/19
    /// </remarks>
    /// <author>Wesley Oates</author>
    public sealed class Deque<T> : IList<T>
    {
        private T[] _Table;
        private static readonly int DEFAULT_SIZE = 16;
        private int _Start;

        /// <summary>
        /// Creates a <see cref="Deque{T}"/> that is empty (if the items argument is omitted), or that 
        /// contains the given items.
        /// </summary>
        /// <param name="items">Optional.  The items to be contained in this list on construction.  If omitted, this 
        /// list will start empty.</param>
        public Deque(IEnumerable<T> items = null)
        {
            if (items == null)
            {
                _Table = new T[DEFAULT_SIZE];
                Count = 0;
            }                
            else
            {
                Count = items.Count();
                _Table = new T[Mathematics.Int32.RoundUpPow2(Count-1)];
                int i = 0;
                foreach (T item in items) _Table[i++] = item;
            }
            _Start = 0;
        }
        /// <summary>
        /// Creates a <see cref="Deque{T}"/> that is empty (if the items argument is omitted), or that 
        /// contains the given items.
        /// </summary>
        /// <param name="items">Optional.  The items to be contained in this list on construction.  If omitted, this 
        /// list will start empty.</param>
        public Deque(params T[] items) : this((IEnumerable<T>)items) { }
        /// <summary>Creates an empty <see cref="Deque{T}"/>, that will start with the given capacity./>.
        /// </summary>
        public Deque(int capacity)
        {
            _Table = new T[capacity];
            Count = 0;
            _Start = 0;
        }

        /// <summary>Gets or sets the item at the given index.</summary>
        public T this[int index]
        {
            get
            {
                if (Count == 0)
                    throw new ArgumentOutOfRangeException("Empty " + nameof(Deque<T>));
                index += _Start;
                if (index >= _Table.Length) index -= _Table.Length;
                return _Table[index];
            }
            set
            {
                if (index >= Count || index < 0)
                    throw new ArgumentOutOfRangeException("Index " + index + " is invalid for this " + nameof(Deque<T>));
                index += _Start;
                if (index >= _Table.Length) index -= _Table.Length;
                _Table[index] = value;
            }
        }

        /// <summary>The number of items contained in this list.</summary>
        public int Count { get; private set; }

        /// <summary>The current capacity of the table.  Adding more items than its capacity will cause its capacity 
        /// to increase automatically.</summary>
        public int Capacity { get => _Table.Length; }

        /// <summary>Returns the first item in this <see cref="Deque{T}"/>.</summary>
        public T First => this[0];

        /// <summary>Returns the last item in this <see cref="Deque{T}"/>.</summary>
        public T Last => this[Count - 1];

        bool ICollection<T>.IsReadOnly => false;

        void ICollection<T>.Add(T item) => AddLast(item);
        /// <summary>Adds the given item to the beginning of this list.</summary>
        public void AddFirst(T item)
        {
            if (Count == _Table.Length)
            {
                int idx = _Start;
                T[] newTable = new T[_Table.Length * 2];
                for (int i = 1; i <= Count; i++)
                {
                    newTable[i] = _Table[idx++];
                    if (idx >= _Table.Length) idx -= _Table.Length;
                }
                newTable[_Start = 0] = item;
                _Table = newTable;
            }
            else
            {
                if (--_Start < 0) _Start += _Table.Length;
                _Table[_Start] = item;
            }
            Count++;
        }
        /// <summary>Adds the given item to the end of this list.</summary>
        public void AddLast(T item)
        {

            if (Count == _Table.Length)
            {
                int idx = _Start;
                T[] newTable = new T[_Table.Length * 2];
                for (int i = 0; i < Count; i++)
                {
                    newTable[i] = _Table[idx++];
                    if (idx >= _Table.Length) idx -= _Table.Length;
                }
                _Start = 0;
                newTable[Count] = item;
                _Table = newTable;
            }
            else
            {
                int idx = _Start + Count;
                if (idx >= _Table.Length) idx -= _Table.Length;
                _Table[idx] = item;
            }
            Count++;
        }


        /// <summary>Removes all items from this list.</summary>
        public void Clear()
        {
            Count = 0;
            _Start = 0;
        }

        /// <summary>Returns whether an item Equals() to the given item is contained in this list.</summary>
        public bool Contains(T item) => IndexOf(item) >= 0;

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            int idx = _Start;
            for (int i = 0; i < Count; i++)
            {
                if (arrayIndex >= array.Length) return;
                array[arrayIndex++] = _Table[idx++];
                if (idx >= _Table.Length) idx -= _Table.Length;
            }            
        }

        /// <summary>The in-order enumerator for this list.</summary>
        public IEnumerator<T> GetEnumerator()
        {
            int idx = _Start;
            for (int i = 0; i < Count; i++)
            {
                yield return _Table[idx++];
                if (idx >= _Table.Length) idx -= _Table.Length;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        
        /// <summary>Returns the index of the first item Equals() to the given item, if one exists.  If not, returns
        /// -1.</summary>
        public int IndexOf(T item)
        {
            int idx = _Start;
            for (int i = 0; i < Count; i++)
            {
                if (_Table[idx++].Equals(item)) return i;
                if (idx >= _Table.Length) idx -= _Table.Length;
            }
            return -1;
        }

        /// <summary>Inserts the given item at the given index.</summary>
        public void Insert(int index, T item)
        {
            if (index > Count || index < 0)
                throw new IndexOutOfRangeException();         

            else if (Count == _Table.Length)
            {
                T[] newTable = new T[_Table.Length * 2];
                int oldIdx = _Start;
                for (int i = 0; i < index; i++)
                {
                    newTable[i] = _Table[oldIdx++];
                    if (oldIdx >= _Table.Length) oldIdx -= _Table.Length;
                }
                newTable[index] = item;
                Count++;
                for (int i = index + 1; i < Count; i++)
                {
                    newTable[i] = _Table[oldIdx++];
                    if (oldIdx >= _Table.Length) oldIdx -= _Table.Length;
                }
                _Start = 0;
                _Table = newTable;
            }
            else
            {
                int oldIdx = index + _Start;
                if (oldIdx >= _Table.Length) oldIdx -= _Table.Length;
                T oldItem = _Table[oldIdx];
                _Table[oldIdx] = item;                
                for (; index < Count; index++)
                {
                    if (++oldIdx >= _Table.Length) oldIdx -= _Table.Length;
                    T swap = _Table[oldIdx];
                    _Table[oldIdx] = oldItem;
                    oldItem = swap;
                }
                Count++;
            }
        }

        /// <summary>Removes the first instance of the given item from this list.  If no Equals() item exists, returns 
        /// false.  Otherwise, returns true.</summary>
        public bool Remove(T item)
        {
            int idx = IndexOf(item);
            if (idx < 0) return false;
            RemoveAt(idx);
            return true;
        }
        /// <summary>Removes the item at the given index.</summary>
        public T RemoveAt(int index)
        {
            index += _Start;
            if (index >= _Table.Length) index -= _Table.Length;
            T result = _Table[index];
            int end = _Start + --Count;
            if (end >= _Table.Length) end -= _Table.Length;
            while (index != end)
            {                
                int nextIdx = index + 1;
                if (nextIdx >= _Table.Length) nextIdx -= _Table.Length;
                _Table[index] = _Table[nextIdx];
                if (++index >= _Table.Length) index -= _Table.Length;
            }
            return result;
        }
        void IList<T>.RemoveAt(int index) => RemoveAt(index);
        /// <summary>Removes and returns the last item from this list.</summary>
        public T RemoveLast() => this[--Count];
        /// <summary>Removes and returns the first item from this list.</summary>
        public T RemoveFirst()
        {   
            T result = this[0];
            _Start++;            
            if (_Start >= _Table.Length) _Start -= _Table.Length;
            Count--;
            return result;
        }
        
    }
}
