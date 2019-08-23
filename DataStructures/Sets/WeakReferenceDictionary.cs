using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.DataStructures.Sets
{
    /// <summary>
    /// A dictionary where all the key references are weak references, but all references to the values are strong.
    /// <para/>Be careful, one could introduce memory leaks if the dictionary is not <seealso cref="Compact"/>ed 
    /// enough.
    /// </summary>
    /// <remarks>TODO:  validate the WeakReferenceDictionary</remarks>
    public sealed class WeakReferenceDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : class
    {
        private readonly List<Node> _Head = new List<Node> { null };
        private readonly Random _Random = new Random(0);
        private readonly KeyCollection _Keys;
        private readonly ValueCollection _Values;

        /// <summary>Create a new <see cref="WeakReferenceDictionary{TKey, TValue}"/>.</summary>
        /// <param name="seed"></param>
        public WeakReferenceDictionary(int seed = 0)
        {
            this._Random = new Random(seed);
            this._Keys = new KeyCollection(this);
            this._Values = new ValueCollection(this);
        }

        /// <summary>Adds a weak reference to the given item to this collection.</summary>
        /// <param name="key">The item to be added.</param>
        /// <param name="value">The value associated with the weakly-referenced key.</param>
        /// <returns>Returns true if the item is added to the collection.  If the item already existed (meaning its 
        /// hash code and the Equals() method reflects equivalency), false is returned.</returns>
        public bool Add(TKey key, TValue value)
        {
            int itemHash = key.GetHashCode();
            if (GetNode(key, itemHash, out Node[] prevNodes)) return false;

            int linkSize = 1;
            for (int i = 1; i < _Head.Count && (_Random.Next() & 1) != 1; i++) linkSize++;
            Node newNode = new Node(key, value, itemHash, linkSize);

            int maxLevels = Math.Min(prevNodes.Length, newNode.Next.Length);
            for (int lvl = 0; lvl < maxLevels; lvl++)
            {
                Node prevAtLevel = prevNodes[lvl];
                newNode.Prev[lvl] = prevAtLevel;
                Node oldNext;
                if (prevAtLevel == null)
                {
                    oldNext = _Head[lvl];
                    _Head[lvl] = newNode;
                }
                else
                {
                    oldNext = prevAtLevel.Next[lvl];
                    prevAtLevel.Next[lvl] = newNode;
                }

                if (oldNext != null) { oldNext.Prev[lvl] = newNode; newNode.Next[lvl] = oldNext; }
            }
            while (newNode.Next.Length > _Head.Count)
                _Head.Add(newNode);
            if (++Count >= (1 << _Head.Count))
                _Head.Add(null);
            return true;
        }

        /// <summary>Removes all items from this collection.</summary>
        public void Clear()
        {
            _Head.Clear();
            _Head.Add(null);
            Count = 0;
        }

        /// <summary>Force the collection to remove all dead references.</summary>
        public void Compact()
        {
            Node n = _Head[0];
            while (n != null)
            {
                Node next = n.Next[0];
                if (!n.Data.TryGetTarget(out _)) Remove(n);
                n = next;
            }
        }

        /// <summary>Returns whether the given item is contained in this collection.</summary>
        public bool ContainsKey(TKey key) => GetNode(key, key.GetHashCode(), out _);

        /// <summary>
        /// The count of live or dead references remaining on this collection.  Because expired 
        /// <seealso cref="WeakReference{T}"/> objects do not automatically fall off this collection, it is possible 
        /// for dead references to be counted with the living.  Such references will remain until the collection is 
        /// iterated over, or until some modification alerts the collection to the dead reference.
        /// </summary>
        public int Count { get; private set; }
        
        /// <summary>Removes the given item from the collection.</summary>
        /// <returns>Returns true if the item was removed.  If the item did not exist on the collection (or if its 
        /// reference had somehow expired) returns false.</returns>
        public bool Remove(TKey key)
        {
            if (!GetNode(key, key.GetHashCode(), out Node[] prev)) return false;
            Remove(prev[0]);
            return true;
        }

        private bool GetNode(TKey key, int itemHash, out Node[] trail)
        {
            int level = _Head.Count - 1;
            trail = new Node[_Head.Count];
            if (trail.Length == 0) return false;

            // Descend while Head's references are higher than the item.
            Node node;
            TKey nodeKey;
            while (true)
            {
                node = _Head[level];
                if (node != null)
                {
                    if (!node.Data.TryGetTarget(out nodeKey))
                    { Remove(node); continue; }
                    else if (node.HashCode <= itemHash) break;
                }
                if (--level < 0) return false;
            }

            // Okay, there's now a node to hang our hat on.
            while (level >= 0)
            {
                if (key.Equals(nodeKey)) { for (; level >= 0; level--) trail[level] = node; return true; }

                Node next = node.Next[level];
                if (next == null || next.HashCode > itemHash) trail[level--] = node;
                else if (!next.Data.TryGetTarget(out TKey nextData)) Remove(next);
                else { node = next; nodeKey = nextData; }
            }
            return nodeKey.Equals(key);
        }

        private void Remove(Node node)
        {
            for (int i = 0; i < node.Next.Length; i++)
            {
                Node next = node.Next[i];
                Node prev = node.Prev[i];
                if (next != null) next.Prev[i] = prev;
                if (prev != null) prev.Next[i] = next;
                else _Head[i] = next;
            }
            Count--;
        }


        public TValue this[TKey key]
        {
            get
            {
                if (!GetNode(key, key.GetHashCode(), out Node[] trail)) throw new KeyNotFoundException();
                return trail[0].Value;
            }
            set
            {
                if (GetNode(key, key.GetHashCode(), out Node[] trail))
                    trail[0].Value = value;
                throw new NotImplementedException();
            }
        }


        public ICollection<TKey> Keys => _Keys;

        public class KeyCollection : ICollection<TKey>
        {
            private readonly WeakReferenceDictionary<TKey, TValue> _Host;
            public KeyCollection(WeakReferenceDictionary<TKey, TValue> host) { this._Host = host; }

            public int Count => _Host.Count;

            bool ICollection<TKey>.IsReadOnly => true;
            
            public bool Contains(TKey key) => _Host.ContainsKey(key);

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                foreach (var kvp in _Host)
                {
                    if (arrayIndex >= array.Length) return;
                    array[arrayIndex++] = kvp.Key;
                }
            }

            public IEnumerator<TKey> GetEnumerator() { foreach (var kvp in _Host) yield return kvp.Key; }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<TKey>.Add(TKey item) => throw new InvalidOperationException();
            void ICollection<TKey>.Clear() => throw new InvalidOperationException();
            bool ICollection<TKey>.Remove(TKey item) => throw new InvalidOperationException();
        }

        public ICollection<TValue> Values => _Values;

        private class ValueCollection : ICollection<TValue>
        {
            private readonly WeakReferenceDictionary<TKey, TValue> _Host;
            public ValueCollection(WeakReferenceDictionary<TKey, TValue> host) { this._Host = host; }

            public int Count => _Host.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            public bool Contains(TValue value)
            {
                foreach (var kvp in _Host)
                {
                    if (kvp.Value.Equals(value)) return true;
                }
                return false;
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                foreach (var kvp in _Host)
                {
                    if (arrayIndex >= array.Length) return;
                    array[arrayIndex++] = kvp.Value;
                }
            }

            public IEnumerator<TValue> GetEnumerator() { foreach (var kvp in _Host) yield return kvp.Value; }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<TValue>.Add(TValue item) => throw new InvalidOperationException();
            void ICollection<TValue>.Clear() => throw new InvalidOperationException();
            bool ICollection<TValue>.Remove(TValue item) => throw new InvalidOperationException();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;


        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => Add(key, value);

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!GetNode(key, key.GetHashCode(), out Node[] trail)) { value = default(TValue); return false; }
            value = trail[0].Value;
            return true;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            if (!GetNode(item.Key, item.Key.GetHashCode(), out Node[] trail)) return false;
            return trail[0].Value.Equals(item.Value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (_Head.Count == 0) return;
            Node n = _Head[0];
            while (n != null)
            {
                
                Node next = n.Next[0];
                if (!n.Data.TryGetTarget(out TKey existing))
                    Remove(n);
                else
                {
                    if (arrayIndex >= array.Length) return;
                    array[arrayIndex++] = new KeyValuePair<TKey, TValue>(existing, n.Value);
                }
                n = next;
            }
            
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!GetNode(item.Key, item.Key.GetHashCode(), out Node[] trail)) return false;
            if (!trail[0].Value.Equals(item.Value)) return false;
            Remove(trail[0]);
            return true;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (_Head.Count == 0) yield break;
            Node n = _Head[0];
            while (n != null)
            {

                Node next = n.Next[0];
                if (!n.Data.TryGetTarget(out TKey existing))
                    Remove(n);
                else
                    yield return new KeyValuePair<TKey, TValue>(existing, n.Value);
                n = next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        

        private class Node
        {
            public readonly WeakReference<TKey> Data;
            public TValue Value;
            public readonly int HashCode;
            public Node[] Prev, Next;
            public Node(TKey data, TValue value, int hashCode, int linkSize)
            {
                this.Data = new WeakReference<TKey>(data);
                this.Value = value;
                this.HashCode = hashCode;
                Prev = new Node[linkSize];
                Next = new Node[linkSize];
            }

            public override string ToString()
            {
                if (!Data.TryGetTarget(out TKey existing)) return "->null";
                return existing.ToString();
            }

        }

    }
}
