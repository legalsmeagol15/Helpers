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
        private static readonly Random _Random = new Random();

        public bool Add(T item)
        {
            if (GetNode(item, out Node node, out Node[] traversals)) return false;
            Node newNode = new Node(item);
            while (Head.Links.Count < newNode.Links.Count)
                Head.Links.Add(null);
            int i = 0;
            for (; i < newNode.Links.Count && i < Head.Links.Count; i++)
            {
                newNode.Links[i] = Head.Links[i];
                Head.Links[i] = newNode;
            }
            for (;  i < traversals.Length && i < newNode.Links.Count; i++)
            {
                newNode.Links[i] = traversals[i].Links[i];
                traversals[i] = newNode;
            }
            Count++;
            return true;
        }

        void ICollection<T>.Add(T item) => this.Add(item);

        public void Clear()
        {
            for (int i = 0; i < Head.Links.Count; i++) Head.Links[i] = null;
            Count = 0;
        }

        public bool Contains(T item) => GetNode(item, out _, out _);

        public int Count { get; private set; }

        bool ICollection<T>.IsReadOnly => false;

        public bool Remove(T item)
        {
            if (!GetNode(item, out Node node, out Node[] traversals)) return false;
            Remove(node, traversals);
            return true;
        }

        /// <summary>Obtains the containing node, and traversal set used to obtain this node.</summary>
        /// <param name="item">The item whose node is sought.</param>
        /// <param name="node">Guaranteed to be either the containing node, or null if none contained the item.</param>
        /// <param name="traversedLinks">The links that may require updating if a node is removed or changed at this 
        /// item's hash.</param>
        /// <returns>Returns true of a containing node is identified (which will be stored in the 
        /// <paramref name="node"/> out variable).</returns>
        private bool GetNode(T item, out Node node, out Node[] traversedLinks)
        {
            int itemHash = item.GetHashCode();
            traversedLinks = new Node[Head.Links.Count];
            node = Head;
           
            // Find the first level that Head doesn't point at the end.
            int level = traversedLinks.Length - 1;
            for (; level >= 0; level--)
            {
                traversedLinks[level] = Head;
                if (Head.Links[level] != null) { node = Head.Links[level]; break; }
            }

            // Do the skipping.
            while (level >= 0 && node != null)
            {
                // On a hash-match, either return this node's contents or walk to the next node.
                if (node.HashCode == itemHash)
                {
                    // If we must remove, delete and then go back to the prior node.  A WeakReference is guaranteed to 
                    // exist because Head has been ruled out.
                    if (!node.WeakReference.TryGetTarget(out T target))
                    {
                        Remove(node, traversedLinks);
                        node = traversedLinks[level];
                    }

                    // If this is a genuine match, return success.
                    else if (target.Equals(item)) return true;

                    // Otherwise, this is a hash match, but not a genuine match.  This node would fill all above 
                    // traversals, and allow for walk at level 0.
                    for (; level > 0; level--) traversedLinks[level] = node;                    
                }
                // If this level would point at the end, go down a level yet.
                else if (node.Links[level] == null)
                    level--;
                // Time to skip
                else
                {
                    traversedLinks[level] = node;
                    node = node.Links[level];
                }
                
            }
            node = null;
            return false;
        }
        private void Remove(Node node)
        {
            for (int  i = 0; i < node.Next.Count; i++)
            {
                Node next = node.Next[i], prev = node.Prev[i];
                if (next != null) next.Prev[i] = prev;
                if (prev != null) prev.Next[i] = next;                
            }            
            Count--;
        }
        

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            // Must have both a next and a prior link set to do compacting iteration.

            Node node = Head.Links[0];
            while (node != null)
            {
                if (!node.WeakReference.TryGetTarget(out _))
                {
                    Node nextNode = node.Links[0];
                    
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        private sealed class Node
        {
            internal readonly int HashCode;
            internal int Skip; // TODO:  implement indexing.
            internal readonly WeakReference<T> WeakReference;
            internal readonly IList<Node> Next;
            internal readonly IList<Node> Prev;

            
            internal Node() { this.Skip = 0; this.WeakReference = null; this.Next = new List<Node>(); }
            internal Node(T item)
            {
                this.HashCode = item.GetHashCode();
                this.WeakReference = new WeakReference<T>(item);
                this.Skip = 0;

                int linkSize = 0;
                while (_Random.Next(2) == 1) linkSize++;
                this.Next = new Node[linkSize];
                this.Prev = new Node[linkSize];
            }
            public override int GetHashCode() => HashCode;
            
        }
    }
}
