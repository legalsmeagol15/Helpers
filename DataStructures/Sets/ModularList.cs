using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>
    /// An array-backed list that supports O(1) operations to insert or delete at the beginning or at the end of the list, but also O(1) operations to read at any index.  Operations 
    /// to Insert or to RemoveAt any point in the middle will be O(1) in the best case and O(n) in the worst case.
    /// </summary>
    /// <remarks>
    /// This list works by allowing the starting item's index to vary to any point modded by the capacity of the list.
    /// </remarks>
    public sealed class ModularList<T> : IList<T>
        //TODO:  Validate ModularList.
    {
        private T[] _Table = new T[17];
        private int _Start = 0;
        

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count || Count == 0) throw new IndexOutOfRangeException();
                index += _Start;
                if (index >= _Table.Length) index -= _Table.Length;
                return _Table[index];
            }

            set
            {
                if (index < 0 || index >= Count || Count == 0) throw new IndexOutOfRangeException();
                index += _Start;
                if (index >= _Table.Length) index -= _Table.Length;
                _Table[index] = value;
            }
        }

        /// <summary>
        /// Gets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        public int Capacity { get { return _Table.Length; } }
        /// <summary>
        /// Gets the number of elements contained in the ModularList&lt;T&gt;.
        /// </summary>
        public int Count { get; private set; } = 0;
        int ICollection<T>.Count { get { return this.Count; } }

        bool ICollection<T>.IsReadOnly { get { return false; } }
        

        void ICollection<T>.Add(T item) { AddLast(item); }


        #region ModularList contents manipulation

        /// <summary>
        /// Adds an object to the beginning of the ModularList&lt;T&gt;.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        public void AddFirst(T item)
        {
            if (Count == _Table.Length)
            {
                IncreaseCapacity();
                _Start = _Table.Length - 1;
                _Table[_Start] = item;
            }
            else if (_Start > 0)
            {
                _Table[--_Start] = item;
            }
            else
            {
                _Start = _Table.Length - 1;
                _Table[_Start] = item;
            }
            Count++;
        }
        /// <summary>
        /// Adds an object to the end of the ModularList&lt;T&gt;.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        public void AddLast(T item)
        {
            if (Count == _Table.Length)
            {
                IncreaseCapacity();
                _Table[Count++] = item;
            }
            else
            {
                int idx = _Start + Count++;
                if (idx >= _Table.Length) idx -= _Table.Length;
                _Table[idx] = item;
            }
        }
        /// <summary>
        /// Removes all elements from the ModularList&lt;T&gt;.
        /// </summary>
        public void Clear()
        {
            _Table = new T[_Table.Length];
            Count = 0;
            _Start = 0;
        }
        /// <summary>
        /// Removes the element at the beginning of the ModularList&lt;T&gt;.
        /// </summary>
        /// <returns>Returns the element removed.</returns>
        public T RemoveFirst()
        {           
            T result = _Table[_Start++];
            if (--Count == 0 || _Start >= _Table.Length) _Start = 0;
            return result;
        }
        /// <summary>
        /// Removes the element at the end of the ModularList&lt;T&gt;.
        /// </summary>
        /// <returns>Returns the element removed.</returns>
        public T RemoveLast()
        {         
            int index = _Start + --Count;
            if (Count == 0) _Start = 0;
            if (index > _Table.Length) index -= _Table.Length;
            return _Table[index];
        }

        private void IncreaseCapacity()
        {

            T[] newTable = new T[_Table.Length * 2];
            int idx = 0;
            foreach (T item in this) _Table[idx++] = item;
            _Start = 0;
            _Table = newTable;
        }

        /// <summary>
        /// Inserts an element into the ModularList&lt;T&gt; at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert.</param>
        /// <param name="item">The item to insert.</param>
        public void Insert(int index, T item)
        {
            if (Count == _Table.Length) IncreaseCapacity();
            index += _Start;
            if (index >= _Table.Length) index -= _Table.Length;
            int end = index + ++Count;
            if (end >= _Table.Length)
            {
                end -= _Table.Length;
                for (int i = end - 1; i > 0; i--) _Table[i] = _Table[i - 1];
                _Table[0] = _Table[_Table.Length - 1];
                for (int i = _Table.Length - 1; i > index; i--) _Table[i] = _Table[i - 1];                
            }
            else
            {
                for (int i = end - 1; i > index; i--) _Table[i] = _Table[i - 1];
            }
            _Table[index] = item;
        }

        /// <summary>
        /// Removes the element at the specified index of the ModularList&lt;T&gt;.
        /// </summary>
        /// <param name="index">The index at which to remove.</param>
        public void RemoveAt(int index)
        {
            index += _Start;
            if (index >= _Table.Length) index -= _Table.Length;
            int end = index + --Count;
            if (end >= _Table.Length)
            {
                end -= _Table.Length;
                for (int i = index; i < _Table.Length - 1; i++) _Table[i] = _Table[i + 1];
                _Table[_Table.Length - 1] = _Table[0];
                for (int i = 0; i < end; i++) _Table[i] = _Table[i + 1];
            }
            else if (end < _Table.Length - 1)
            {
                for (int i = index; i < end; i++) _Table[i] = _Table[i + 1];
            }
            else
            {
                for (int i = index; i < end; i++) _Table[i] = _Table[i + 1];
                _Table[end] = _Table[0];
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the ModularList&lt;T&gt;.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        #endregion



        #region Modularlist contents queries

        /// <summary>
        /// Returns the item at the beginning of the ModularList&lt;T&gt;.
        /// </summary>
        public T First
        {
            get
            {
                if (Count == 0) throw new IndexOutOfRangeException();
                return _Table[_Start];
            }
        }
        /// <summary>
        /// Returns the item at the end of the ModularList&lt;T&gt;.
        /// </summary>
        public T Last
        {
            get
            {
                if (Count == 0) throw new IndexOutOfRangeException();
                int idx = _Start + Count - 1;
                if (idx >= _Table.Length) idx -= _Table.Length;
                return _Table[idx];
            }
        }
        /// <summary>
        /// Determines whether an element is in the ModularList&lt;T&gt;.
        /// </summary>  
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            foreach (T item in this) array[arrayIndex++] = item;
        }


        public IEnumerator<T> GetEnumerator()
        {
            int end = _Start + Count;
            if (end >= _Table.Length)
            {
                end -= _Table.Length;
                for (int i = _Start; i < _Table.Length; i++) yield return _Table[i];
                for (int i = 0; i < end; i++) yield return _Table[i];
            }
            else
            {
                for (int i = _Start; i < end; i++) yield return _Table[i];
            }            
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the entire ModularList&lt;T&gt;.
        /// </summary>
        /// <returns>If the object is contained in the list, returns the index of object.  Otherwise, returns -1.</returns>
        public int IndexOf(T item)
        {            
            int end = _Start + Count;
            if (end >= _Table.Length)
            {
                end -= _Table.Length;
                for (int i = _Start; i < _Table.Length; i++) if (_Table[i].Equals(item)) return i - _Start;
                for (int i = 0; i < end; i++) if (_Table[i].Equals(item)) return i + (_Table.Length - _Start);
            }
            else
            {
                for (int i = _Start; i < end; i++) if (_Table[i].Equals(item)) return i;
            }
            return -1;
        }

      
        
        #endregion


    }
}
