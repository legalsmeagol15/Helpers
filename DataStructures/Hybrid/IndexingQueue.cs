using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Structures
{
    /// <summary>
    /// A generic queue which can get or set the item identified at a particular index in O(1) time.  Also, just like a traditional 
    /// <seealso cref="Queue{T}"/>, items can be added to or removed from this queue in O(1) time.
    /// </summary>
    public class IndexingQueue<T> : IEnumerable<T>, ICollection, IEnumerable
    {
        private const int DEFAULT_CAPACITY = 16;
        private T[] _Table;
        private int _Start, _End;

        /// <summary>Creates a new <see cref="IndexingQueue{T}"/>.</summary>
        /// <param name="capacity">Optional.  The beginning capacity of the <see cref="IndexingQueue{T}"/>.  If omitted, will be set to 
        /// <see cref="DEFAULT_CAPACITY"/>.</param>
        public IndexingQueue(int capacity = DEFAULT_CAPACITY)
        {
            _Table = new T[capacity];
            _Start = _End = 0;
        }

        #region IndexingQueue contents modification

        /// <summary>Gets or sets the item at the given index.</summary>
        public T this[int index]
        {
            get
            {   
                if (index < 0 || index >= Count) throw new IndexOutOfRangeException("Index(" + index + ") was out of valid range(0 to " + (Count - 1) + ").");
                index += _Start;
                if (index > _Table.Length) index -= _Table.Length;
                return _Table[index];
            }
            set
            {
                if (index < 0 || index >= Count) throw new IndexOutOfRangeException("Index(" + index + ") was out of valid range(0 to " + (Count - 1) + ").");
                index += _Start;
                if (index > _Table.Length) index -= _Table.Length;
                _Table[index] = value;
            }
        }


        /// <summary>Adds an item to the end of the queue.</summary>        
        public void AddLast(T item)
        {
            _Table[_End++] = item;
            if (_End == _Table.Length) _End = 0;
            if (_End == _Start) EnlargeTable();
        }
        /// <summary>Adds an item to the end of the queue.</summary> 
        public void AddFirst(T item)
        {
            if (--_Start < 0) _Start = _Table.Length - 1;
            _Table[_Start] = item;
            if (_Start == _End) EnlargeTable();            
        }

        private void EnlargeTable()
        {
            T[] newTable = new T[_Table.Length * 2];
            int i = 0;
            // Can't just foreach this because the state must have every slot filled with _Start == _End.  The foreach loop presumes that 
            // if _Start == _End, it's an empty queue.
            for (; _Start < _End; _Start++) newTable[i++] = _Table[_Start];
            if (_End <= _Start)
            {
                for (int j = _Start; j < _Table.Length; j++) newTable[i++] = _Table[j];
                for (int j = 0; j < _End; j++) newTable[i++] = _Table[j];
            }
            _End = _Table.Length;
            _Start = 0;
            _Table = newTable;
        }

        /// <summary>Removes all items from this queue.</summary>
        public void Clear() => _Start = _End = 0;


        /// <summary>
        /// Removes and returns the first-in item in this queue.  This is equivalent to a <seealso cref="Queue{T}.Dequeue"/> operation.
        /// </summary>        
        public T RemoveFirst()
        {
            if (_Start == _End) throw new InvalidOperationException("This " + this.GetType().Name + " is empty.");
            T item = _Table[_Start++];
            if (_Start >= _Table.Length) _Start = 0;
            return item;
        }
        /// <summary>
        /// Removes and returns the last-in item in this queue.  This is equivalent to a <seealso cref="Stack{T}.Pop"/> operation.
        /// </summary>
        public T RemoveLast()
        {
            if (_Start == _End) throw new InvalidOperationException("This " + this.GetType().Name + " is empty.");
            T item = _Table[--_End];
            if (_End < 0) _End = _Table.Length;
            return item;
        }

        #endregion



        #region IndexingQueue contents queries

        public bool Contains(T item) { foreach (T existing in this) { if (existing.Equals(item)) return true; } return false; }


        public int Count => (_Start <= _End) ? (_End - _Start) : _End + (_Table.Length - _Start);
        int ICollection.Count => this.Count;


        void ICollection.CopyTo(Array array, int arrayIndex) { foreach (T item in this) array.SetValue(item, arrayIndex++); }


        public IEnumerator<T> GetEnumerator()
        {
            for (int i = _Start; i < _End; i++) yield return _Table[i];
            if (_Start > _End)
            {
                for (int i = _Start; i < _Table.Length; i++) yield return _Table[i];
                for (int i = 0; i < _End; i++) yield return _Table[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns the index of the given item, if it exists on this <see cref="IndexingQueue{T}"/>.  If it does not, returns -1.
        /// <para/>This is an O(n) operation, where 'n' is the size of this <see cref="IndexingQueue{T}"/>
        /// </summary>
        public int IndexOf(T item)
        {
            int i = 0;
            foreach (T existing in this)
                if (existing.Equals(item)) return i;
                else i++;
            return -1;
        }



        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => throw new NotImplementedException("Use some other locking pattern.  The SyncRoot pattern sux.");



        #endregion
        
    }
}
