using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>
    /// A dictionary that uses a set of unordered items as a key for hashing.
    /// </summary>
    /// <typeparam name="TKeyPart">The type of the parts that compose a complete key, in whatever order.</typeparam>
    /// <typeparam name="TValue">The type of the values stored and returned by this dictionary.</typeparam>
    public sealed class CompositeKeyDictionary<TKeyPart, TValue> : IDictionary<ISet<TKeyPart>, TValue>

    //TODO:  Validate all members of CompositeKeyDictionary.
    {
        private const int DEFAULT_CAPACITY = 17;

        private ISet<TKeyPart>[] _Keys;
        private TValue[] _Values;
        private bool[] _Deleted;
        

        public CompositeKeyDictionary(int capacity = DEFAULT_CAPACITY)
        {
            capacity = Mathematics.Primes.GetNextPrime(capacity * 2);
            _Keys = new ISet<TKeyPart>[capacity];
            _Values = new TValue[capacity];
            _Deleted = new bool[capacity];           
            _KeyCollection = new KeyCollection(this);
            _ValueCollection = new ValueCollection(this);
        }


        TValue IDictionary<ISet<TKeyPart>, TValue>.this[ISet<TKeyPart> key]
        {
            get
            {
                int idx = FindIndex(key);
                if (idx == -1) throw new KeyNotFoundException("The given key does not exist on this CompositeKeyDictionary.");
                return _Values[idx];
            }

            set
            {
                int idx = FindIndex(key);
                if (idx == -1)
                {
                    idx = FindEmptyIndex(key);
                    if (idx == -1) throw new InvalidOperationException("This isn't happening... this isn't happening... this isn't happening...");
                }
                _Keys[idx] = key;
                _Values[idx] = value;
                _Deleted[idx] = false;
            }
        }



        #region CompositeKeyDictionary hashing

        private static int GetHash(IEnumerable<TKeyPart> key)
        {
            int hash = 0;
            foreach (TKeyPart part in key) hash += part.GetHashCode();
            return Math.Abs(hash);
        }

        /// <summary>
        /// Uses quadratic probing to return an empty index suitable for the given key to be added at.  An empty index will either be an index that has never been used, or an 
        /// index where another item once existed but has since been deleted.  If an identical key is encountered while probing the tables, the returned result will be -1.  
        /// Note that a return greater than -1 is NOT a guarantee that a matching key does not already exist, since probing may encounter a deleted item before the identical 
        /// key is examined.
        /// </summary>
        private int FindEmptyIndex(ISet<TKeyPart> key)
        {
            int hash = GetHash(key);
            for (int probe = 0; probe < 50000; probe++)
            {
                int idx = (hash + (probe * probe)) % _Keys.Length;
                if (idx < 0) throw new KeyNotFoundException("Given key could not be located and probing index overflowed.");
                ISet<TKeyPart> compareKey = _Keys[idx];
                if (compareKey == null || _Deleted[idx]) return idx;
                if (compareKey.Count == key.Count && hash == GetHash(compareKey) && key.SetEquals(compareKey))
                    return -1;
            }
            throw new KeyNotFoundException("Given key could not be located and probing exceeded allowed bounds.");
        }

        /// <summary>
        /// Uses quadratic probing to return the index of the given key, if it exists on this dictionary.  If it does not exist (or it did exist but has been deleted since), 
        /// returns -1.
        /// </summary>
        private int FindIndex(ISet<TKeyPart> key)
        {
            int hash = GetHash(key);
            for (int probe = 0; probe < 50000; probe++)
            {
                int idx = (hash + (probe * probe)) % _Keys.Length;
                ISet<TKeyPart> compareKey = _Keys[idx];
                if (compareKey == null) return -1;
                if (compareKey.Count == key.Count && hash == GetHash(compareKey) && key.SetEquals(compareKey))
                    return _Deleted[idx] ? -1 : idx;
            }
            throw new KeyNotFoundException("Given key could not be located and probing exceeded allowed bounds.");
        }

        /// <summary>
        /// Finds the next prime number greater than double the current size of the tables, and adjusts the capacity accordingly.
        /// </summary>
        private void IncreaseCapacity()
        {
            int newCapacity = Mathematics.Primes.GetNextPrime(_Keys.Length * 2);
            ISet<TKeyPart>[] oldKeys = _Keys;
            TValue[] oldValues = _Values;

            _Keys = new ISet<TKeyPart>[newCapacity];
            _Values = new TValue[newCapacity];

            for (int i = 0; i < oldKeys.Length; i++)
            {
                ISet<TKeyPart> key = oldKeys[i];
                if (key != null && !_Deleted[i])
                {
                    TValue value = oldValues[i];
                    int idx = FindEmptyIndex(key);
                    if (idx == -1) throw new InvalidOperationException("No!!! That's impossible!");
                    _Keys[idx] = key;
                    _Values[idx] = value;
                }
            }

            _Deleted = new bool[newCapacity];
        }

        #endregion



        #region CompositeKeyDictionary contents changing members

        /// <summary>
        /// Adds an entry with the given key and value to the dictionary.
        /// </summary>
        /// <remarks>If the key is null, throws an ArgumentNullException.  If the key already exists on the dictionary, throws an ArgumentException.</remarks>
        public void Add(ISet<TKeyPart> key, TValue value)
        {
            if (key == null) throw new ArgumentNullException("Key cannot be null.");

            //Is it time to increase the capacity of the storage tables?
            while (Count >= _Keys.Length / 2) IncreaseCapacity();

            //What is a good insertion index?
            int idx = FindEmptyIndex(key);
            if (idx == -1) throw new ArgumentException("An entry with an identical key set has already been added to this dictionary.");            

            //Store the new item.
            _Keys[idx] = key;
            _Values[idx] = value;
            _Deleted[idx] = false;

            //The number of items on the dictionary just increased.
            Count++;
        }


        void ICollection<KeyValuePair<ISet<TKeyPart>, TValue>>.Add(KeyValuePair<ISet<TKeyPart>, TValue> kvp)
        {
            this.Add(kvp.Key, kvp.Value);
        }

        /// <summary>
        /// Removes all items from the dictionary.
        /// </summary>
        public void Clear()
        {
            int capacity = _Keys.Length;
            _Keys = new ISet<TKeyPart>[capacity];
            _Values = new TValue[capacity];
            _Deleted = new bool[capacity];
            Count = 0;
        }

        /// <summary>
        /// Removes the entry for the given key, or the key that has the same key parts, if it exists on the dictionary.  If it existed and is removed, returns true.  Otherwise returns false.
        /// </summary>
        public bool Remove(ISet<TKeyPart> key)
        {
            int idx = FindIndex(key);
            if (idx == -1) return false;
            _Deleted[idx] = true;
            Count--;
            return true;
        }

        /// <summary>
        /// Removes all entries where the given key part appears in the key.
        /// </summary>
        /// <returns>Returns the count of entries removed.</returns>
        public int RemoveAll(TKeyPart keyPart)
        {
            List<ISet<TKeyPart>> toRemove = new List<ISet<TKeyPart>>();
            int removed = 0;
            for (int i = 0; i< _Keys.Length; i++)
            {
                if (!_Keys[i].Contains(keyPart) || _Deleted[i]) continue;
                removed++;
                _Deleted[i] = true;
                Count--;
            }
            return removed;
                
                
            
        }


        bool ICollection<KeyValuePair<ISet<TKeyPart>, TValue>>.Remove(KeyValuePair<ISet<TKeyPart>, TValue> kvp)
        {
            int idx = FindIndex(kvp.Key);
            if (idx == -1) return false;
            if (!object.Equals(kvp.Value, _Values[idx])) return false;
            _Deleted[idx] = true;
            Count--;
            return true;
        }


        #endregion



        #region CompositeKeyDictionary queries

        /// <summary>
        /// Returns whether a key identical to the given key exists on this dictionary.
        /// </summary> 
        public bool ContainsKey(ISet<TKeyPart> key) { return FindIndex(key) != -1; }

        /// <summary>
        /// Returns the count of items on this dictionary.
        /// </summary>
        public int Count { get; private set; } = 0;

        int ICollection<KeyValuePair<ISet<TKeyPart>, TValue>>.Count { get { return this.Count; } }

        void ICollection<KeyValuePair<ISet<TKeyPart>, TValue>>.CopyTo(KeyValuePair<ISet<TKeyPart>, TValue>[] array, int arrayIndex)
        {
            for (int i = 0; i < _Keys.Length; i++)
            {
                if (arrayIndex >= array.Length) return;
                ISet<TKeyPart> key = _Keys[i];
                if (key != null && !_Deleted[i])
                    array[arrayIndex++] = new KeyValuePair<ISet<TKeyPart>, TValue>(key, _Values[i]);
            }

        }




        /// <summary>
        /// Returns how full the hash tables current are for this dictionary.  Once lambda reaches 0.5, the tables' capacity will be adjusted in an O(N) operation to free up more space and 
        /// ensure efficient quadratic probing.
        /// </summary>
        public double Lambda { get { return ((double)Count) / _Keys.Length; } }


        bool ICollection<KeyValuePair<ISet<TKeyPart>, TValue>>.IsReadOnly { get { return false; } }


        /// <summary>
        /// Attempts to return the value associated with the given key, which will be stored in the 'out' return parameter.  Returns true or false whether the matching key was successfully 
        /// found.  
        /// </summary> 
        public bool TryGetValue(ISet<TKeyPart> key, out TValue value)
        {
            int idx = FindIndex(key);
            if (idx == -1)
            {
                value = default(TValue);
                return false;
            }
            value = _Values[idx];
            return false;
        }


        bool ICollection<KeyValuePair<ISet<TKeyPart>, TValue>>.Contains(KeyValuePair<ISet<TKeyPart>, TValue> kvp)
        {
            int idx = FindIndex(kvp.Key);
            if (idx == -1) return false;
            return (object.Equals(kvp.Value, _Values[idx]));
        }



        public IEnumerator<KeyValuePair<ISet<TKeyPart>, TValue>> GetEnumerator()
        {
            for (int i = 0; i < _Keys.Length; i++)
            {
                ISet<TKeyPart> key = _Keys[i];
                if (key != null && !_Deleted[i]) yield return new KeyValuePair<ISet<TKeyPart>, TValue>(key, _Values[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }



        #endregion


        private KeyCollection _KeyCollection;
        private ValueCollection _ValueCollection;
        public ICollection<ISet<TKeyPart>> Keys
        {
            get
            {
                return _KeyCollection;

            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return _ValueCollection;
            }
        }

        public sealed class KeyCollection : ICollection<ISet<TKeyPart>>, IReadOnlyCollection<ISet<TKeyPart>>
        {
            private CompositeKeyDictionary<TKeyPart, TValue> _Source;
            internal KeyCollection(CompositeKeyDictionary<TKeyPart, TValue> source)
            {
                _Source = source;
            }

            void ICollection<ISet<TKeyPart>>.Add(ISet<TKeyPart> item)
            {
                throw new NotSupportedException("Cannot add items to a read-only KeyCollection.");
            }

            void ICollection<ISet<TKeyPart>>.Clear()
            {
                throw new NotSupportedException("Cannot clear items from a read-only KeyCollection.");
            }

            bool ICollection<ISet<TKeyPart>>.Remove(ISet<TKeyPart> item)
            {
                throw new NotSupportedException("Cannot remove items from a read-only KeyCollection.");
            }

            public int Count { get { return _Source.Count; } }


            public bool IsReadOnly { get { return true; } }


            public bool Contains(ISet<TKeyPart> key) { return _Source.FindIndex(key) != -1; }

            void ICollection<ISet<TKeyPart>>.CopyTo(ISet<TKeyPart>[] array, int arrayIndex)
            {
                for (int i = 0; i < _Source._Keys.Length; i++)
                {
                    if (arrayIndex >= array.Length) return;
                    ISet<TKeyPart> key = _Source._Keys[i];
                    if (key != null && !_Source._Deleted[i]) array[arrayIndex++] = key;
                }
            }


            public IEnumerator<ISet<TKeyPart>> GetEnumerator()
            {
                for (int i = 0; i < _Source._Keys.Length; i++)
                {
                    ISet<TKeyPart> key = _Source._Keys[i];
                    if (key != null && !_Source._Deleted[i]) yield return key;
                }
            }


            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

        }

        public sealed class ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
        {
            private CompositeKeyDictionary<TKeyPart, TValue> _Source;
            internal ValueCollection(CompositeKeyDictionary<TKeyPart, TValue> source)
            {
                _Source = source;
            }


            void ICollection<TValue>.Add(TValue item)
            {
                throw new NotSupportedException("Cannot add items to a read-only ValueCollection.");
            }

            void ICollection<TValue>.Clear()
            {
                throw new NotSupportedException("Cannot clear items from a read-only ValueCollection.");
            }


            bool ICollection<TValue>.Remove(TValue item)
            {
                throw new NotSupportedException("Cannot remove items from a read-only ValueCollection.");
            }

            public int Count { get { return _Source._Values.Length; } }

            public bool IsReadOnly { get { return true; } }


            public bool Contains(TValue item)
            {
                for (int i = 0; i < _Source._Values.Length; i++)
                {
                    if (_Source._Keys[i] == null && !_Source._Deleted[i] && object.Equals(item, _Source._Values[i])) return true;
                }
                return false;
            }

            void ICollection<TValue>.CopyTo(TValue[] array, int arrayIndex)
            {
                for (int i = 0; i < _Source._Values.Length; i++)
                {
                    if (arrayIndex >= array.Length) return;
                    if (_Source._Keys[i] != null && !_Source._Deleted[i]) array[arrayIndex++] = _Source._Values[i];
                }
            }


            public IEnumerator<TValue> GetEnumerator()
            {
                for (int i = 0; i < _Source._Keys.Length; i++)
                {
                    if (_Source._Keys[i] != null && !_Source._Deleted[i]) yield return _Source._Values[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }


        }

    }
}
