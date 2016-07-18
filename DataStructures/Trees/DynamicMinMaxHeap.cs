using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Mathematics.Int32;

namespace DataStructures
{
    /// <summary>
    /// A heap that maintains references to both the maximum and the minimum item.  The heap is dynamic because calling Update() forces the given item to update its position in 
    /// the heap without being deleted.
    /// </summary>    
    public class DynamicMinMaxHeap<T> : ISet<T> where T : IComparable<T>
        //TODO:  Validate all members of DynamicMinMaxHeap
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

        public DynamicMinMaxHeap(int capacity = DEFAULT_CAPACITY)
        {
            _Table = new List<T>(capacity);
            _Indices = new Dictionary<T, int>(capacity);
        }

        //****THIS WILL BE USED TO IMPLEMENT THE Z-INDEX IN Drawing *****


        #region DynamicMinMaxHeap contents changing

        /// <summary>
        /// Ensures that the given item exists on the heap.  If the item already existed, returns true; otherwise, returns false.
        /// </summary>
        bool Add(T item)
        {
            if (_Indices.ContainsKey(item)) return false;
            _Table.Add(item);
            _Indices[item] = Count;
            Percolate(Count++);
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

        /// <summary>
        /// Deletes and returns the minimum item in the heap.
        /// </summary>        
        public T DeleteMin()
        {
            T result = _Table[0];
            Swap(0, --Count);
            _Indices.Remove(result);
            _Table[Count] = default(T);
            Percolate(0);
            return result;
        }

        /// <summary>
        /// Deletes and returns the maximum item in the heap.
        /// </summary>
        /// <returns></returns>
        public T DeleteMax()
        {
            
            int idx;
            if (Count <= 1) idx = 0;
            else if (Count == 2) idx = 1;
            else idx = (_Table[1].CompareTo(_Table[2]) > 0) ? 1 : 2;
            T result = _Table[idx];
            Swap(idx, --Count);
            _Indices.Remove(result);
            _Table[Count] = default(T);
            Percolate(idx);
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
                Percolate(idx);
            }
        }

        void ISet<T>.IntersectWith(IEnumerable<T> other)
        {
            //Removes all elements from this heap that do not appear in the given collection.
            HashSet<T> leaveIn = new HashSet<T>(other);
            for (int i = 0; i < Count; i++)
            {
                T item = _Table[i];
                if (leaveIn.Contains(item)) continue;
                Swap(i, --Count);
                _Table[Count] = default(T);
                _Indices.Remove(item);
                Percolate(i);
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
            Percolate(idx);
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
            for (int i = 0; i < Count; i++)
            {
                T item = _Table[i];
                if (!otherSet.Contains(item)) continue;
                Swap(i, --Count);
                _Table[Count] = default(T);
                _Indices.Remove(item);
                Percolate(i);
                otherSet.Remove(item);
            }

            //Add in what's left in other.
            foreach (T item in otherSet)
            {
                _Table.Add(item);
                _Indices[item] = Count;
                Percolate(Count++);
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
            return idx != Percolate(idx);            
        }

        #endregion



        #region DynamicMinMaxHeap percolation members



        private int PercolateDownMax(int index)
        {
            while (true)
            {
                int childIdx = GetGrandChildIndex(index);
                if (childIdx >= Count) return index;

                //Get the max among the index's (grand)children.
                T item = _Table[childIdx];
                int childPlus4 = childIdx + 4;
                for (int i = childIdx + 1; i < childPlus4; i++)
                {
                    if (_Table[i].CompareTo(item) > 0)
                    {
                        item = _Table[i];
                        childIdx = i;
                    }
                }
                if (_Table[index].CompareTo(item) >= 0) return index;   //Cannot percolate any further down in max levels.

                Swap(index, childIdx);
            }
        }

        /// <summary>
        /// Percolates down towards the base level, if possible.  
        /// </summary>
        private int PercolateDownMin(int index)
        {
            while (true)
            {
                int childIdx = GetGrandChildIndex(index);
                if (childIdx >= Count) return index;

                //Get the min among the index's (grand)children.
                T item = _Table[childIdx];
                int childPlus4 = childIdx + 4;
                for (int i = childIdx + 1; i < childPlus4; i++)
                {
                    if (_Table[i].CompareTo(item) < 0)
                    {
                        item = _Table[i];
                        childIdx = i;
                    }
                }
                if (_Table[index].CompareTo(item) <= 0) return index;   //Cannot percolate any further down in min levels.

                Swap(index, childIdx);
            }
        }

        private int PercolateUpMax(int index)
        {
            while (true)
            {
                if (index <= 2) return index;
                int parentIdx = GetParentIndex(index);
                if (_Table[index].CompareTo(_Table[parentIdx]) <= 0) return index;
                Swap(index, parentIdx);
                parentIdx = GetParentIndex(index);
            }
        }

        private int PercolateUpMin(int index)
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
        /// Call this method to compel the item in the gien index to percolate to the correct position.
        /// </summary>
        private int Percolate(int index)
        {

            int originalIndex = index;

            //Are we starting on a min level, or on a max level?
            int level = Mathematics.Int32.Log_2(index + 1);

            if (Mathematics.Int32.IsEven(level)) //STARTING ON A MIN LEVEL.
            {
                //Check if it can percolate up to the min top.
                index = PercolateUpMin(index);
                if (index != originalIndex) return index;

                //Check if it can percolate down on the min levels.
                index = PercolateDownMin(index);

                //Check if it should hop from min level to max level.
                if (index >= 3)
                {
                    int parentIdx = (index - 1) / 2;
                    if (_Table[index].CompareTo(_Table[parentIdx]) > 0)
                    {
                        Swap(index, parentIdx);
                        index = parentIdx;
                    }
                }
                if (index == originalIndex) return index;   //The item was already in the correct spot.

                //Finally, since it percolated down the min's, and hopped to the maxs, percolate up to the max top.
                return PercolateUpMax(index);
            }
            else        //STARTING ON A MAX LEVEL.
            {
                //Check if it can percolate up to the max top.
                index = PercolateUpMax(index);
                if (index != originalIndex) return index;

                //Check if it can percolate down on the max levels.
                index = PercolateDownMax(index);

                //Check if it should hop from max level to min level.
                if (index >= 1)
                {
                    int parentIdx = (index - 1) / 2;
                    if (_Table[index].CompareTo(_Table[parentIdx]) > 0)
                    {
                        Swap(index, parentIdx);
                        index = parentIdx;
                    }
                }
                if (index == originalIndex) return index;   //The item was already in the correct spot.

                //Finally, since it percolated down the max's, and hopped to the mins, percolate up to the min top.
                return PercolateUpMin(index);
            }
        }



        /// <summary>
        /// Returns the index of the given index's parent.
        /// </summary>
        private int GetParentIndex(int index)
        {
            return (index - 3) / 4;
        }
        /// <summary>
        /// Returns the index of the given index's lefthand child.
        /// </summary>
        private int GetGrandChildIndex(int index)
        {
            return (index * 4) + 3;
        }



        #endregion




        #region DynamicMinMaxHeap contents queries

        /// <summary>
        /// Returns whether this heap contains the given item.
        /// </summary>
        public bool Contains(T item)
        {
            return _Indices.ContainsKey(item);
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; i++)
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


        /// <summary>
        /// Returns the minimum item in the heap, without changing the heap.
        /// </summary>        
        public T GetMin()
        {
            return _Table[0];
        }
        /// <summary>
        /// Returns the maximum item in the heap, without changing the heap.
        /// </summary>        
        public T GetMax()
        {
            if (Count <= 1) return _Table[0];   //Will throw an IndexNotFoundException if Count==0.
            if (Count == 2) return _Table[1];
            return (_Table[1].CompareTo(_Table[2]) > 0) ? _Table[1] : _Table[2];
        }


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
