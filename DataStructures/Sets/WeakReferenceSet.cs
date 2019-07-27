using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public sealed class WeakReferenceSet<T> : ICollection<T> where T : class
    {
        private readonly List<Node> _Head = new List<Node>();
        private readonly Random _Random = new Random(0);
        private int _MaxLinks;

        public WeakReferenceSet(int capacity = 0, int seed = 0)
        {
            this._MaxLinks = (capacity < 1) ? 1 : Mathematics.Int32.Log_2(capacity);
            this._Random = new Random(seed);
        }

        public bool Add(T item)
        {
            int linkSize = 1;
            for (int i = 1; i < _MaxLinks && (_Random.Next() & 1) != 1; i++) linkSize++;
            Node newNode = new Node(item, item.GetHashCode(), linkSize);
            while (newNode.Next.Length > _Head.Count) _Head.Add(null);

            if (GetNode(item, out Node[] prevNodes, newNode.HashCode)) return false;

            for (int lvl = 0; lvl < newNode.Next.Length; lvl++)
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
            if (++Count > (1 << _MaxLinks)) _MaxLinks++;
            return true;
        }

        public void Clear()
        {
            _Head.Clear();
            Count = 0;
            _MaxLinks = 1;
        }

        public bool Contains(T item) => GetNode(item, out _);

        public int Count { get; private set; }

        public bool Remove(T item)
        {
            if (!GetNode(item, out Node[] prev)) return false;
            Compact(prev[0]);
            return true;
        }

        private bool GetNode(T item, out Node[] prev, int itemHash = int.MinValue)
        {
            prev = _Head.ToArray();
            if (prev.Length == 0) return false;

            if (itemHash == int.MinValue) itemHash = item.GetHashCode();

            int level = prev.Length - 1;

            while (prev[level] != null && prev[level].HashCode > itemHash) { prev[level] = null; if (--level < 0) return false; }
            Node node = prev[level];
            while (true)
            {
                while (node == null || node.HashCode > itemHash)
                {
                    if (level <= 0) return false;
                    node = prev[--level];
                }
                if (node.HashCode == itemHash)
                {
                    if (!node.Data.TryGetTarget(out T existing))
                    {
                        Node toRemove = node;
                        if ((node = node.Prev[level]) == null)
                        {
                            while (_Head[level] == null)
                            {
                                prev[level] = null;
                                if (--level < 0) return false;
                            }
                            node = _Head[level];
                        }
                        Compact(toRemove);
                    }
                    else if (existing.Equals(item)) return true;
                    else
                    {
                        for (; level > 0; level--) prev[level] = node;
                        node = node.Next[0];
                    }
                }
                else
                {
                    while (node.Next[level] == null || node.Next[level].HashCode > itemHash)
                    {
                        prev[level] = node;
                        if (--level < 0) return false;
                    }
                    node = node.Next[level];
                    prev[level] = node;
                }
            }
        }

        private void Compact(Node node)
        {
            for (int i = 0; i < node.Next.Length; i++)
            {
                Node next = node.Next[i];
                Node prev = node.Prev[i];
                if (next != null) next.Prev[i] = node.Prev[i];
                if (prev != null) prev.Next[i] = node.Next[i];
            }
            if (--Count < (1 << (_MaxLinks - 1)))
                if (--_MaxLinks < 1)
                    _MaxLinks = 1;
        }

        void ICollection<T>.Add(T item) => this.Add(item);

        bool ICollection<T>.IsReadOnly => false;

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            foreach (T item in this)
            {
                if (arrayIndex >= array.Length) return;
                array[arrayIndex++] = item;
            }
        }


        public IEnumerator<T> GetEnumerator()
        {
            if (_Head.Count == 0) yield break;
            Node n = _Head[0];
            while (n != null)
            {
                if (!n.Data.TryGetTarget(out T existing))
                {
                    Node toRemove = n;
                    n = n.Next[0];
                    Compact(toRemove);
                }
                else
                {
                    yield return existing;
                    n = n.Next[0];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private class Node
        {
            public readonly WeakReference<T> Data;
            public readonly int HashCode;
            public Node[] Prev, Next;
            [DebuggerStepThrough]
            public Node(T data, int hashCode, int linkSize)
            {
                this.Data = new WeakReference<T>(data);
                this.HashCode = hashCode;
                Prev = new Node[linkSize];
                Next = new Node[linkSize];
            }

            public override string ToString()
            {
                if (!Data.TryGetTarget(out T existing)) return "null";
                return existing.ToString();
            }

        }

    }
}
