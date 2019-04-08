using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>
    /// A data structure that pairs the speed of hashing lookup, add, and remove with a constant-time index.  <para/> This structure does not allow for duplicate items.
    /// </summary>
    /// <remarks>This structure works by maintaining both a list and a hash map internally.  Most defined operations work in constant time.  However, the defined operation Remove(), as well as the 
    /// interface-defined methods Insert() and RemoveAt() are still O(n) operations in the worst case.  </remarks>    
    public sealed class HashList<T> : IList<T>, ISet<T>, IEnumerable
        //TODO:  Validate all members of HashList
    {

        private Dictionary<T, int> _Indices;
        
        private List<T> _List;

        /// <summary>
        /// Creates a new HashList.
        /// </summary>
        public HashList()
        {
            _Indices = new Dictionary<T, int>();
            _List = new List<T>();
        }

        /// <summary>
        /// Creates a new HashList with the given initial capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public HashList(int capacity)
        {
            _Indices = new Dictionary<T, int>(capacity);
            _List = new List<T>(capacity);
        }

        /// <summary>
        /// Creates a new HashList from the given items.
        /// </summary>
        /// <param name="items"></param>
        public HashList(IEnumerable<T> items) : this()
        {
            foreach (T item in items)
            {
                Add(item);
            }
        }
        

        public T this[int index]
        {
            get
            {
                return _List[index];
            }

            set
            {
                T oldItem = _List[index];
                _Indices.Remove(oldItem);
                _Indices.Add(value, index);
                _List[index] = value;
            }
        }


        #region HashList contents changing members

        /// <summary>
        /// Ensures the given item exists on this hash list.  If it already existed, returns false; otherwise, returns true.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Add(T item)
        {
            if (_Indices.ContainsKey(item)) return false;
            _Indices.Add(item, _List.Count);
            _List.Add(item);
            return true;
        }
        void ICollection<T>.Add(T item)
        {
            this.Add(item);
        }

        /// <summary>
        /// Removes all items from this hash list.
        /// </summary>
        public void Clear()
        {
            _Indices.Clear();
            _List.Clear();
        }

        /// <summary>
        /// Ensures the given item exists at the index specified.  If the item was already present on this hash list, swaps the given item with the item previously 
        /// at that location.  Otherwise, inserts the item into that location.
        /// </summary>
        void IList<T>.Insert(int index, T item)
        {
            //If the item is already here, move it.
            int existingIndex;
            if (_Indices.TryGetValue(item, out existingIndex))
            {
                if (existingIndex > index)
                {
                    for (int i = index; i < existingIndex; i++)                    
                        _Indices[_List[i]]++;
                    _List.RemoveAt(existingIndex);
                    _List.Insert(index, item);
                    _Indices[item] = index;
                }
                else if (existingIndex < index)
                {
                    for (int i = existingIndex; i < index; i++)
                        _Indices[_List[i]]--;
                    _List.RemoveAt(existingIndex);
                    _List.Insert(index, item);
                    _Indices[item] = index;
                }
                //Do nothing if existingIndex==index.
                return;
            }
            
            //Otherwise, insert it.
            for (int i = index; i < _List.Count; i++)
                _Indices[_List[i]]++;
            _Indices.Add(item, index);
            _List[index] = item;            
        }

        /// <summary>
        /// Ensures the given item does not exist on this hash last.  If the item existed before this method was called, and was removed, returns true.  Otherwise, 
        /// returns false.
        /// </summary>
        public bool Remove(T item)
        {
            int idx;
            if (!_Indices.TryGetValue(item, out idx)) return false;
            for (int i = idx + 1; i < _List.Count; i++)
                _Indices[_List[i]]--;
            _List.RemoveAt(idx);
            _Indices.Remove(item);
            return true;
        }

        
        void IList<T>.RemoveAt(int index)
        {
            RemoveAt(index);         
        }
        /// <summary>
        /// Removes the item at the given index.
        /// </summary>    
        public T RemoveAt(int index)
        {
            T item = _List[index];
            for (int i = index + 1; i < _List.Count; i++)
                _Indices[_List[i]]--;
            _List.RemoveAt(index);
            _Indices.Remove(item);
            return item;
        }

        /// <summary>
        /// Removes all elements that match the conditions defined by the specified predicate from a HashList`1 collection.
        /// </summary>
        /// <param name="match">The System.Predicate`1 delegate that defines the conditions of the elements to remove.</param>
        /// <returns>The number of elements that were removed from the HashList`1 collection.</returns>
        public int RemoveWhere(Predicate<T> match)
        {
            int removed = 0;            
            for (int i = 0; i < _List.Count - removed; i++)
            {
                T item = _List[i];
                if (match(item))
                {
                    removed++;
                    _Indices.Remove(item);
                }
                else
                {
                    _Indices[item] -= removed;
                }
                _List[i] = _List[i + removed];                
            }

            _List.RemoveRange(_List.Count - removed, removed);

            return removed;
        }

        /// <summary>
        /// Resets all indices according to the items' appearance in the list.
        /// </summary>
        private void ResetIndices()
        {
            _Indices = new Dictionary<T, int>(_List.Capacity);
            for (int i = 0; i < _List.Count; i++) _Indices.Add(_List[i], i);
        }




        void ISet<T>.ExceptWith(IEnumerable<T> other)
        {
            HashSet<T> compareSet = new HashSet<T>(other);
            _List.RemoveAll((item) => compareSet.Contains(item));
            ResetIndices();
        }


        void ISet<T>.IntersectWith(IEnumerable<T> other)
        {
            HashSet<T> compareSet = new HashSet<T>(other);
            _List.RemoveAll((item) => !compareSet.Contains(item));
            ResetIndices();
        }

        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
        {
            HashSet<T> compareSet = new HashSet<T>(other);
            _List.RemoveAll((item) => !(compareSet.Contains(item) ^ _Indices.ContainsKey(item)));
            ResetIndices();
        }


        void ISet<T>.UnionWith(IEnumerable<T> other)
        {
            foreach (T otherItem in other)
                Add(otherItem);
        }



        #endregion



        #region HashList contents query members


        public IReadOnlyList<T> AsReadOnly()
        {
            return _List.AsReadOnly();
        }
        

        public int Count { get { return _List.Count; } }

        bool ICollection<T>.IsReadOnly { get { return false; } }


        public bool Contains(T item)
        {
            return _Indices.ContainsKey(item);
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            _List.CopyTo(array, arrayIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _List.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _List.GetEnumerator();
        }

        /// <summary>
        /// Returns the index of the given item.  This is an O(1) operation.  If the item is not contained on this list, returns -1.
        /// </summary>        
        public int IndexOf(T item)
        {
            int result;
            if (_Indices.TryGetValue(item, out result)) return result;
            return -1;            
        }


        bool ISet<T>.IsSubsetOf(IEnumerable<T> other)
        {
            HashSet<T> otherSet = new HashSet<T>();
            foreach (T item in _List)
                if (!otherSet.Contains(item)) return false;
            return true;
        }

        bool ISet<T>.IsSupersetOf(IEnumerable<T> other)
        {
            foreach (T item in other)
                if (!_Indices.ContainsKey(item)) return false;
            return true;
        }

        bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other)
        {
            HashSet<T> examinedOther = new HashSet<T>();            
            foreach (T otherItem in other)
            {
                if (!_Indices.ContainsKey(otherItem)) return false;
                examinedOther.Add(otherItem);
            }
            return _List.Count > examinedOther.Count;
        }

        bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other)
        {
            HashSet<T> otherSet = new HashSet<T>();
            HashSet<T> examinedThis = new HashSet<T>();
                        
            foreach (T item in _List)
            {
                if (!otherSet.Contains(item)) return false;
                examinedThis.Add(item);                
            }
            return otherSet.Count > examinedThis.Count;
        }

        bool ISet<T>.Overlaps(IEnumerable<T> other)
        {
            foreach (T otherItem in other)
                if (_Indices.ContainsKey(otherItem)) return true;
            return false;
        }

        bool ISet<T>.SetEquals(IEnumerable<T> other)
        {
            HashSet<T> different = new HashSet<T>(_List);
            foreach (T otherItem in other)            
                if (!different.Remove(otherItem)) return false;
            
            return different.Count == 0;
        }

        #endregion

        
    }
}
