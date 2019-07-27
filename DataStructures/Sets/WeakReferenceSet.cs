using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>
    /// A set of <seealso cref="WeakReference"/> objects of type <typeparamref name="T"/>.  Existing on this set will 
    /// not prevent an object from being garbage-collected, but will allow users to iterate through the surviving 
    /// references.
    /// </summary>
    /// <remarks>Under the hood, this collection is based on a skip list which prunes dead references during 
    /// collection modification  and iteration.  Note that a <seealso cref="WeakReference"/>'s expiration will not 
    /// automatically remove the reference from this collection, so the collection's <see cref="Count"/> may reflect 
    /// more valid references than actually exist.  This is so until the collection is iterated over or some 
    /// modification points out the dead reference.
    /// <para/>This collection is not thread-safe.</remarks>
    /// <author>Wesley Oates</author>
    /// <date>Validated 7/27/19</date>
    public sealed class WeakReferenceSet<T> : ICollection<T> where T : class
    {
        private readonly List<Node> _Head = new List<Node> { null };
        private readonly Random _Random = new Random(0);

        /// <summary>Create a new <see cref="WeakReferenceSet{T}"/>.</summary>
        /// <param name="seed"></param>
        public WeakReferenceSet(int seed = 0) { this._Random = new Random(seed); }

        /// <summary>Adds a weak reference to the given item to this collection.</summary>
        /// <param name="item">The item to be added.</param>
        /// <returns>Returns true if the item is added to the collection.  If the item already existed (meaning its 
        /// hash code and the Equals() method reflects equivalency), false is returned.</returns>
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
        public bool Contains(T item) => GetNode(item, item.GetHashCode(), out _);

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

            // Descend while Head's references are higher than the item.
            Node node;
            T nodeData;
            while (true)
            {
                node = _Head[level];
                if (node != null)
                {
                    if (!node.Data.TryGetTarget(out nodeData))
                    { Remove(node); continue; }
                    else if (node.HashCode <= itemHash) break;
                }
                if (--level < 0) return false;
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

        /// <summary>Returns an enumerator which steps through the existing items on this collection.</summary>
        public IEnumerator<T> GetEnumerator()
        {
            if (_Head.Count == 0) yield break;
            Node n = _Head[0];
            while (n != null)
            {
                Node next = n.Next[0];
                if (!n.Data.TryGetTarget(out T existing))
                    Remove(n);
                else
                    yield return existing;
                n = next;
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
                if (!Data.TryGetTarget(out T existing)) return "->null";
                return existing.ToString();
            }

        }

    }
}
