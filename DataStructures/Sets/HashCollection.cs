using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>
    /// A hashing storage collection that may contain multiple copies of the same item.  The HashCollection implements support for O(1) contents determination and 
    /// O(1) object count retrieval.
    /// </summary>
    /// <remarks>This nonpermuted hash set uses chaining to resolve collisions. Two items will be deemed copies if their Equals() method returns true, 
    /// AND the items have the same hash code.  The objects themselves are not stored, but instead a count of objects with identical Equals() and 
    /// GetHashCode() results are stored.  For this reason, this data structure should not be used if two items are expected to report Equals() as 
    /// true when their members are actually different; however, the HashCollection is ideal for storing multiple instances of immutables like 
    /// Point structures or literal uint values.
    /// <para/>All members validated on 5/9/16, except explicitly implemented ICollection members.
    /// <para/>Author Wesley Oates</remarks>
    public sealed class HashCollection<T> : ICollection<T>        
    {
        private const int DEFAULT_TABLE_SIZE = 17;

        private Dictionary<T, int>[] _Table;
        private int _ChainSize;

        /// <summary>
        /// Returns the count of items (with each duplicate counting as a separate item) in the hash collection.
        /// </summary>
        public int Count { get; private set; }
        int ICollection<T>.Count { get { return Count; } }

        bool ICollection<T>.IsReadOnly { get { return false; } }

        /// <summary>
        /// Creates a new, empty hash collection.
        /// </summary>
        public HashCollection()
        {
            _Table = new Dictionary<T, int>[DEFAULT_TABLE_SIZE];
            _ChainSize = Arithmetic.Primes.GetNextPrime(DEFAULT_TABLE_SIZE);
            Count = 0;
        }

        /// <summary>
        /// Creates a new hash collection from the given set of items.
        /// </summary>
        /// <param name="items"></param>
        public HashCollection(IEnumerable<T> items) : this()
        {
            foreach (T item in items) Add(item);
        }

        #region HashCollection contents manipulation

        /// <summary>
        /// Adds the given item to this hash collection.  Returns the count of identical items once the add is complete.
        /// </summary>
        public int Add(T item, int copies = 1)
        {

            int idx = Hash(item);
            Dictionary<T, int> chain = _Table[idx];

            //If this is the first item in the chain, time to create the chain.
            if (chain == null)
            {
                _ChainSize = Arithmetic.Primes.GetNextPrime(_Table.Length);  //Forces the modularity of the chain to be different from table's modularity.
                chain = new Dictionary<T, int>(_ChainSize);
                _Table[idx] = chain;
            }
            //If it isn't null, but the chain set selected is too big, time to grow the whole thing.
            else if (chain.Count >= _Table.Length)
            {
                //Double the size of the storage.
                ChangeCapacity(_Table.Length * 2);

                //Update the index and focus chain.
                idx = Hash(item);
                chain = _Table[idx];
                if (chain == null)
                {
                    //Is this really possible?  If the capacity is increased so the mod is doubled, won't the hash & _Table.Length stay the same?
                    chain = new Dictionary<T, int>(_ChainSize);
                    _Table[idx] = chain;
                }
            }

            //Finally, add the item to the appropriate chain.
            if (chain.ContainsKey(item)) chain[item]+= copies;
            else chain.Add(item, copies);
            Count++;
            return chain[item];
        }
        void ICollection<T>.Add(T item)
        {
            //Not validated.
            Add(item);
        }

        /// <summary>
        /// Clears all items from this hash collection.
        /// </summary>
        public void Clear()
        {
            _Table = new Dictionary<T, int>[DEFAULT_TABLE_SIZE];
            Count = 0;
        }

        /// <summary>
        /// Changes the table capacity as indicated.
        /// </summary>        
        private void ChangeCapacity(int newCapacity)
        {
            
            Dictionary<T, int>[] oldTable = _Table;
            _Table = new Dictionary<T, int>[_Table.Length * 2];
            int chainSize = Arithmetic.Primes.GetNextPrime(_Table.Length);  //Forces the modularity of the chain to be different from new table's modularity.
            for (int oldIdx = 0; oldIdx < oldTable.Length; oldIdx++)
            {
                Dictionary<T, int> oldChain = oldTable[oldIdx];
                if (oldChain == null) continue;
                foreach (KeyValuePair<T, int> kvp in oldChain)
                {
                    int newIdx = Hash(kvp.Key);
                    Dictionary<T, int> newChain = _Table[newIdx];
                    if (newChain == null)
                    {
                        newChain = new Dictionary<T, int>(_ChainSize);
                        _Table[newIdx] = newChain;
                    }
                    newChain.Add(kvp.Key, kvp.Value);
                }
            }
        }

        private int Hash(T item)
        {
            return Math.Abs(item.GetHashCode() % _Table.Length);
        }


        /// <summary>
        /// Ensures the hash collection does not contain the given item.  If the item did not exist in the collection to begin with, returns false; otherwise, returns 
        /// true upon removal.
        /// </summary>
        public bool Remove(T item)
        {
            int idx = Hash(item);
            Dictionary<T, int> chain = _Table[idx];
            if (chain == null) return false;
            if (!chain.ContainsKey(item)) return false;
            if (--chain[item] < 1) chain.Remove(item);
            Count--;
            return true;
        }

        #endregion


        #region HashCollection contents query members

        /// <summary>
        /// Returns whether at least a single instance of the given item exist in this collection.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return CountOf(item) > 0;
        }


        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            //Not validated.
            foreach (T item in this)
            {
                if (arrayIndex >= array.Length) return;
                array[arrayIndex++] = item;
            }
        }
        /// <summary>
        /// Returns how many of the given item exist on this collection.
        /// </summary>
        public int CountOf(T item)
        {
            int idx = Hash(item);
            Dictionary<T, int> chain = _Table[idx];
            if (chain == null) return 0;
            int number;
            if (!chain.TryGetValue(item, out number)) return 0;
            return number;
        }

        /// <summary>
        /// This convenience method returns an array populated with the given item, copied a number of times equal to the number that exist on this collection.
        /// </summary>
        public T[] GetCopiesOf(T item)
        {
            int idx = Hash(item);
            Dictionary<T, int> chain = _Table[idx];
            if (chain == null) return new T[0];
            int copies;
            if (!chain.TryGetValue(item, out copies)) return new T[0];
            T[] result = new T[copies];
            for (int i = 0; i < copies; i++) result[i] = item;
            return result;
        }

        /// <summary>
        /// Iterates through each unique item contained in this collection.  Note that items with multiple copies will not be returned more than once.
        /// </summary>        
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _Table.Length; i++)
            {
                Dictionary<T, int> chain = _Table[i];
                if (chain == null) continue;
                foreach (T item in chain.Keys) yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            //Not validated
            return GetEnumerator();
        }

        #endregion

    }
}
