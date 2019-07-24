using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.DataStructures.Sets
{
    public sealed class WeakReferenceSet<T> : ICollection<T> where T:class
    {
        private readonly List<Node> _Head = new List<Node>();
        private static readonly Random _Random = new Random();
        

        public bool Add(T item)
        {
            if (GetNode(item, out Node[] prevNodes)) return false;

            Node newNode = new Node(item);
            while (newNode.Next.Length > _Head.Count) _Head.Add(null);
            for (int i = 0; i < newNode.Next.Length; i++)
            {
                Node prevAtLevel = prevNodes[i];
                if (prevAtLevel == null)
                {
                    // It was a tall insertion after an empty or emptied _Head
                    newNode.Prev[i] = null;
                    newNode.Next[i] = null;
                    _Head[i] = newNode;
                }
                else if (prevAtLevel.Next[i] == null)
                {
                    // It was an insertion higher preceding node.
                    newNode.Prev[i] = prevNodes[i];
                    newNode.Next[i] = null;
                    prevAtLevel.Next[i] = newNode;
                }
                else
                {
                    // It was an insertion between two nodes.
                    newNode.Prev[i] = prevAtLevel;
                    newNode.Next[i] = prevAtLevel.Next[i];
                    prevAtLevel.Next[i].Prev[i] = newNode;
                    prevAtLevel.Next[i] = newNode;
                }
            }
            Count++;
            return true;
        }

        public void Clear()
        {
            _Head.Clear();
            Count = 0;
        }

        public bool Contains(T item) => GetNode(item, out _);

        public int Count { get; private set; }

        

        public bool Remove(T item)
        {
            if (!GetNode(item, out Node[] prev)) return false;
            Compact(prev[0]);
            return true;
        }

        /// <summary>
        /// Returns true if a node matching the given item exists.  In that case, prev[0] will be that node.  
        /// Otherwise, prev will be populated with the preceding nodes at each level.
        /// </summary>
        private bool GetNode(T item, out Node[] prev)
        {
            prev = _Head.ToArray();

            int level = _Head.Count - 1;
            int itemHash = item.GetHashCode();

            Node node = _Head[level];
            while (true)
            {
                while (node == null || node.HashCode > itemHash)
                {
                    if (--level < 0) return false;
                    node = prev[level];
                }
                if (node.HashCode == itemHash)
                {
                    if (!node.Data.TryGetTarget(out T existing))
                    {
                        Node toRemove = node;
                        if ((node = node.Prev[level]) == null)
                        {
                            while (_Head[level] == null) if (--level < 0) return false;
                            node = _Head[level];
                        }
                        Compact(toRemove);
                    }
                    else if (existing.Equals(item)) return true;
                    else { node = node.Next[level = 0]; prev[level] = node; }
                }
                else
                {
                    while (node.Next[level] == null || node.Next[level].HashCode > itemHash) if (--level < 0) return false;
                    node = node.Next[level];
                    prev[level] = node;
                }
            }
        }

        private void Compact(Node node)
        {
            for (int  i = 0; i < node.Next.Length; i++)
            {
                Node next = node.Next[i];
                Node prev = node.Prev[i];
                if (next != null) next.Prev[i] = node.Prev[i];
                if (prev != null) prev.Next[i] = node.Next[i];
            }
            Count--;
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
            public Node(T data) {
                this.Data = new WeakReference<T>(data);
                this.HashCode = this.GetHashCode();
                int linkSize = 0;
                while ((_Random.Next() & 1) == 1) linkSize++;
                Prev = new Node[linkSize];
                Next = new Node[linkSize];
            }

            public override string ToString()
            {
                if (!Data.TryGetTarget(out T existing)) return HashCode + " / null";
                return HashCode + " / " + existing.ToString();
            }
        }
    }
}
