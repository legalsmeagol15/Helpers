using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>
    /// An array-backed min heap composed of unique items.  The heap is dynamic because an item can be forced to update its position in a O(log n) operation.
    /// </summary>    
    public class DynamicHeap<T> : ISet<T> where T : IComparable<T>
        //TODO:  validate all members of DynamicHeap
    {

        private const int DEFAULT_CAPACITY = 15;

        /// <summary>
        /// The array describing the partially-ordered heap.
        /// </summary>
        private List<T> _Table;

        /// <summary>
        /// The dictionary for finding the contents in the heap in an O(1) operation.
        /// </summary>
        private Dictionary<T, int> _Indices;

        public DynamicHeap(int capacity = DEFAULT_CAPACITY)
        {
            _Table = new List<T>(capacity);
            _Indices = new Dictionary<T, int>(capacity);
        }

        //****THIS WILL BE USED TO IMPLEMENT THE Z-INDEX IN Drawing *****


        #region DynamicHeap contents changing

        /// <summary>
        /// Ensures that the given item exists on the heap.  If the item already existed, returns true; otherwise, returns false.
        /// </summary>
        bool Add(T item)
        {
            if (_Indices.ContainsKey(item)) return false;
            _Table.Add(item);
            _Indices[item] = Count;
            PercolateUp(Count++);
            return true;
        }
        bool ISet<T>.Add(T item)
        {
            return this.Add(item);
        }
        void ICollection<T>.Add(T item)
        {
            if (!this.Add(item)) throw new ArgumentException("This item already exists on this dynamic heap.");
        }

       

        public void Clear()
        {
            _Indices.Clear();
            _Table.Clear();
            Count = 0;
        }


        public T DeleteMin()
        {
            T result = _Table[0];
            Swap(0, --Count);
            _Indices.Remove(result);
            _Table[Count] = default(T);
            PercolateDown(0);
            return result;
        }


        void ISet<T>.ExceptWith(IEnumerable<T> other)
        {
            //Removes all elements in the specified collection from the current heap.
            foreach (T exceptItem in other)
            {
                int idx;
                if (!_Indices.TryGetValue(exceptItem, out idx)) continue;
                Swap(idx, --Count);
                _Table[Count] = default(T);
                _Indices.Remove(exceptItem);
                PercolateDown(idx);
            }
        }


        public  T GetMin()
        {
            return _Table[0];
        }


        void ISet<T>.IntersectWith(IEnumerable<T> other)
        {
            //Removes all elements from this heap that do not appear in the given collection.
            HashSet<T> leaveIn = new HashSet<T>(other);
            for (int  i = 0; i< Count; i++)
            {
                T item = _Table[i];
                if (leaveIn.Contains(item)) continue;
                Swap(i, --Count);
                _Table[Count] = default(T);
                _Indices.Remove(item);
                PercolateDown(i);
            }
        }

        /// <summary>
        /// Forces the item at the given index to percolate down through the heap, if possible.  The index where it comes to rest is the value returned.
        /// </summary>
        private int PercolateDown(int index)
        {
            while (true)
            {
                int childIdx = GetChildIndex(index);
                if (childIdx >= Count) return index;
                if (childIdx + 1 < Count && _Table[childIdx].CompareTo(_Table[childIdx + 1]) > 0) childIdx++;   //ensures childIdx points to the lesser of the two children.
                if (_Table[index].CompareTo(_Table[childIdx]) <= 0) return index;
                Swap(index, childIdx);
                childIdx = GetChildIndex(index);
            }

        }
        /// <summary>
        /// Forces the item at the given index to percolate up through the heap, if possible.  the index where it comes to rest is the value returned.
        /// </summary>
        private int PercolateUp(int index)
        {            
            while (true)
            {
                if (index <= 0) return index;
                int parentIdx = GetParentIndex(index);
                if (_Table[index].CompareTo(_Table[parentIdx]) >= 0) return index;                    
                Swap(index, parentIdx);
                parentIdx = GetParentIndex(index);
            }            
        }

        /// <summary>
        /// Removes the given item from this heap.  If the item did not already exist on the heap, returns false; otherwise, returns true upon successful removal.
        /// </summary>
        public bool Remove(T item)
        {
            int idx;
            if (!_Indices.TryGetValue(item, out idx)) return false;
            T moveItem = _Table[--Count];
            _Table[idx] = moveItem;
            _Indices[moveItem] = idx;
            _Table[Count] = default(T);
            PercolateDown(idx);
            return true;            
        }

        /// <summary>
        /// Swaps the items at the two given indices.
        /// </summary>
        private void Swap(int indexA, int indexB)
        {
            T tempA = _Table[indexA];
            T tempB = _Table[indexB];
            _Table[indexA] = tempB;
            _Table[indexB] = tempA;
            _Indices[tempA] = indexB;
            _Indices[tempB] = indexA;
        }



        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
        {
            //XOR
            HashSet<T> otherSet = new HashSet<T>(other);
            for (int i = 0; i< Count; i++)
            {
                T item = _Table[i];
                if (!otherSet.Contains(item)) continue;
                Swap(i, --Count);
                _Table[Count] = default(T);
                _Indices.Remove(item);
                PercolateDown(i);
                otherSet.Remove(item);
            }

            //Add in what's left in other.
            foreach (T item in otherSet)
            {
                _Table.Add(item);
                _Indices[item] = Count;
                PercolateUp(Count++);
            }
            
        }

        void ISet<T>.UnionWith(IEnumerable<T> other)
        {
            foreach (T item in other)            
                Add(item);            
        }

        /// <summary>
        /// Ensures the given item is in the correct position in the heap.
        /// </summary>  
        public bool Update(T item)
        {
            int idx;
            if (!_Indices.TryGetValue(item, out idx)) return false;
            if (idx != PercolateUp(idx)) return true;
            return idx == PercolateDown(idx);
        }

        #endregion

        /// <summary>
        /// Returns the index of the given index's parent.
        /// </summary>
        private int GetParentIndex(int index)
        {
            return (index - 1) / 2;
        }
        /// <summary>
        /// Returns the index of the given index's lefthand child.
        /// </summary>
        private int GetChildIndex(int index)
        {
            return (index * 2) + 1;
        }


        #region DynamicHeap contents queries

        /// <summary>
        /// Returns the relative position of the two items on this heap.  
        /// </summary> 
        /// <returns>If the result is -1, then itemA is minimal compared to itemB; a result of 0 means they are equal; and a result of 1 means 
        /// itemA is maximal compared to itemB.</returns>
        /// <remarks>Throws a KeyNotFoundexception if either item is not contained in the heap.</remarks>
        public int Compare(T itemA, T itemB)
        {
            if (!_Indices.ContainsKey(itemA)) throw new KeyNotFoundException("The itemA does not exist in this heap.");
            if (!_Indices.ContainsKey(itemB)) throw new KeyNotFoundException("The itemB does not exist in this heap.");
            return itemA.CompareTo(itemB);
        }

        /// <summary>
        /// Returns whether this heap contains the given item.
        /// </summary>
        public bool Contains(T item)
        {
            return _Indices.ContainsKey(item);
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i< Count; i++)
            {
                if (arrayIndex >= array.Length) return;
                array[arrayIndex++] = _Table[i];
            }
        }

        /// <summary>
        /// The number of items in this heap.
        /// </summary>
        /// <remarks>Do not modify this method to return the _Table.Count directly, cuz the Count needs to be manipulated by various heap operations.</remarks>
        public int Count { get; private set; } = 0;
        int ICollection<T>.Count { get { return this.Count; } }


        bool ICollection<T>.IsReadOnly { get { return false; } }


      

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Table.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _Table.GetEnumerator();
        }


        bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.IsSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.IsSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }


        bool ISet<T>.Overlaps(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.SetEquals(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }


        #endregion

    }
}
