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
        private readonly List<Node> _Head = new List<Node> { null };
        private readonly Random _Random = new Random(0);

        public WeakReferenceSet( int seed = 0)
        {   
            this._Random = new Random(seed);
        }

        public bool Add(T item)
        {
            int itemHash = item.GetHashCode();
            if (GetNode(item, itemHash, out Node[] prevNodes)) return false;

            int linkSize = 1;
            for (int i = 1; i < _Head.Count && (_Random.Next() & 1) != 1; i++) linkSize++;
            Node newNode = new Node(item, itemHash, linkSize);

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

        public void Clear()
        {
            _Head.Clear();
            _Head.Add(null);
            Count = 0;
        }

        public bool Contains(T item) => GetNode(item, item.GetHashCode(), out _);

        public int Count { get; private set; }

        public bool Remove(T item)
        {
            if (!GetNode(item, item.GetHashCode(), out Node[] prev)) return false;
            Remove(prev[0]);
            return true;
        }
        
        private bool GetNode(T item, int itemHash, out Node[] trail)
        {
            int level = _Head.Count - 1;
            trail = new Node[_Head.Count];
            if (trail.Length == 0) return false;

            // Find the topmost node referenced in Head.
            while (_Head[level] == null)
                if (--level < 0) return false;

            // Continue descending while Head's references are higher than the item.
            Node node;
            T nodeData;
            while (true)
            {
                node = _Head[level];
                if (!node.Data.TryGetTarget(out nodeData)) Remove(node);
                else if (node.HashCode <= itemHash) break;
                else if (--level < 0) return false;
            }

            // Okay, there's now a node to hang our hat on.
            while (level >= 0)
            {
                if (item.Equals(nodeData)) { for (; level >= 0; level--) trail[level] = node; return true; }

                Node next = node.Next[level];
                if (next == null || next.HashCode > itemHash) trail[level--] = node;
                else if (!next.Data.TryGetTarget(out T nextData)) Remove(next);                
                else { node = next; nodeData = nextData; }
            }
            return nodeData.Equals(item);
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
                    Remove(toRemove);
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
