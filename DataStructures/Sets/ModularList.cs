using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>
    /// An array-backed list that supports O(1) operations to insert at the beginning or add at the end, but also O(1) operations to read at any index.
    /// </summary>
    /// <remarks>
    /// This list works by allowing the starting item's index to vary to any point modded by the capacity of the list.
    /// </remarks>
    public sealed class ModularList<T> : IList<T>
        //TODO:  finish implementing modular list.
    {
        private T[] _Table = new T[17];
        private int _Start = 0;
                

        T IList<T>.this[int index]
        {
            get
            {
                index = _Start + index;
                if (index >= _Table.Length) index -= _Table.Length;
                return _Table[index];
            }

            set
            {
                if (index == Count)
                {
                    Add(value);
                    return;
                }
                index = _Start + index;
                if (index >= _Table.Length) index -= _Table.Length;
                _Table[index] = value;
            }
        }

        public int Count { get; private set; } = 0;
        int ICollection<T>.Count { get { return this.Count; } }

        bool ICollection<T>.IsReadOnly { get { return false; } }

        public void Add(T item)
        {
            if (Count == _Table.Length) IncreaseCapacity();
            int idx = _Start + Count;
            if (idx > _Table.Length) idx -= _Table.Length;
            _Table[idx] = item;
            Count++;
        }

        public void Clear()
        {
            _Table = new T[_Table.Length];
            Count = 0;
            _Start = 0;            
        }

        private void IncreaseCapacity()
        {
            T[] oldTable = _Table;
            _Table = new T[_Table.Length * 2];
                       
            //Copy everything from the old table to the new, resetting start to 0.
            int newIdx = 0;
            int end = _Start + Count;
            if (end >= _Table.Length) end -= _Table.Length;
            if (end> _Start)            
                for (int i = _Start; i<end; i++) _Table[newIdx++] = oldTable[i];
            else
            {
                for (int i = _Start; i < _Table.Length; i++) _Table[newIdx++] = oldTable[i];
                for (int i = 0; i < end; i++) _Table[newIdx++] = oldTable[i];
            }
            
        }

        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex>=array.Length) return;
            int end = _Start + Count;
            if (end >= _Table.Length) end -= _Table.Length;
            if (end > _Start)
                for (int i = _Start; i < end; i++) array[arrayIndex++] = _Table[i];
            else
            {
                for (int i = _Start; i < _Table.Length; i++) array[arrayIndex++] = _Table[i];
                for (int i = 0; i < end; i++) array[arrayIndex++] = _Table[i];
            }
        }


        public IEnumerator<T> GetEnumerator()
        {
            int end = _Start + Count;
            if (end >= _Table.Length) end -= _Table.Length;
            if (end > _Start)
                for (int i = _Start; i < end; i++) yield return _Table[i];
            else
            {
                for (int i = _Start; i < _Table.Length; i++) yield return _Table[i];
                for (int i = 0; i < end; i++) yield return _Table[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Returns the index of the given item.
        /// </summary>
        public int IndexOf(T item)
        {
            if (_Table[_Start + Count - 1].Equals(item)) return Count - 1;

            int end = _Start + Count;
            if (end >= _Table.Length) end -= _Table.Length;
            if (end > _Start)
            {
                for (int i = _Start; i < end; i++) if (_Table[i].Equals(item)) return i - _Start;
            }
            else
            {
                for (int i = _Start; i < _Table.Length; i++) if (_Table[i].Equals(item)) return i - _Start;
                for (int i = 0; i < end; i++) if (_Table[i].Equals(item)) return (_Table.Length - _Start) + i;
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            
        }

        public void Swap(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= Count) throw new IndexOutOfRangeException("Given initial index does not exist on this list.");
            if (toIndex < 0 || toIndex >= Count) throw new IndexOutOfRangeException("Given destination index does not exist on this list.");


        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
           
        }
    }
}
