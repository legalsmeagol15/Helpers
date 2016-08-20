using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public sealed class DisjointSet<T> : ISet<T>
    {

        //TODO:  finish implementing DisjointSet

        private Dictionary<T, Node> _Nodes;

        public DisjointSet()
        {
            _Nodes = new Dictionary<T, Node>();
        }

        #region DisjointSet contents manipulation members

        /// <summary>
        /// Ensures the given item exists on this set.  If the item did not already exist (and so was added), returns true.  Otherwise, returns false.
        /// </summary>
        public bool Add(T item)
        {
            if (_Nodes.ContainsKey(item)) return false;
            _Nodes.Add(item, new Node(item));
            return true;
        }

        void ICollection<T>.Add(T item)
        {
            this.Add(item);
        }

        /// <summary>
        /// Clears all items from this set.
        /// </summary>
        public void Clear()
        {
            _Nodes.Clear();
        }


        /// <summary>
        /// Ensures the given items are part of the same group.
        /// </summary>
        public void Join(T first, T second)
        {
            //Ensure that the given items exist on the set.
            Node node1, node2;
            if (!_Nodes.TryGetValue(first, out node1))
            {
                node1 = new Node(first);
                _Nodes.Add(first, node1);
            }
            if (!_Nodes.TryGetValue(second, out node2))
            {
                node2 = new Node(second);
                _Nodes.Add(second, node2);
            }

            //Which becomes a child of which?
            Stack<Node> referenceStack = new Stack<Node>();
            if (node1.Rank > node2.Rank)
            {
                node2.Parent = node1;
                node1.Rank = Math.Max(node1.Rank, node2.Rank) + 1;
                referenceStack.Push(node2);
            }
            else
            {
                node1.Parent = node2;
                node2.Rank = Math.Max(node1.Rank, node2.Rank) + 1;
                referenceStack.Push(node1);
            }

            //Path Compression - populate the stack with the hierarchy of this child, starting at the child.
            while (!referenceStack.Peek().Equals(referenceStack.Peek().Parent))
                referenceStack.Push(referenceStack.Peek().Parent);

            //Flatten everything on the referenceStack.
            Node finalParent = referenceStack.Pop();
            while (referenceStack.Count > 0) referenceStack.Pop().Parent = finalParent;
            
        }


        /// <summary>
        /// Ensures this set does not contain the given item.  If the set was changes as a result of this method call, returns true.  Otherwise, returns false.
        /// </summary>
        public bool Remove(T item)
        {
            //If the set doesn't contain the item, just return false.
            if (!_Nodes.ContainsKey(item)) return false;

            //The new parent for all of the removed item's children will be the item's parent (which might be the item itself).
            Node newParent = _Nodes[item].Parent;
            
            //If just removed someone's parent, flatten the structure            
            foreach (Node n in _Nodes.Values)
            {
                if (n.Parent.Data.Equals(item))
                {
                    if (newParent.Data.Equals(item)) newParent = n.Parent;
                    n.Parent = newParent;
                }
            }
            return _Nodes.Remove(item);
        }



        #endregion



        #region DisjointSet contents query members

        /// <summary>
        /// Returns the count of items on this set.
        /// </summary>
        public int Count { get { return _Nodes.Count; } }

        bool ICollection<T>.IsReadOnly { get { return false; } }

        /// <summary>
        /// Returns whether this set contains the given item.
        /// </summary>
        public bool Contains(T item) { return _Nodes.ContainsKey(item); }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            foreach (T item in _Nodes.Keys)
            {
                if (arrayIndex >= array.Length) break;
                array[arrayIndex++] = item;
            }            
        }

        void ISet<T>.ExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        void ISet<T>.IntersectWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
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

        /// <summary>
        /// Returns whether this set and the other set share items in common.
        /// </summary>
        public bool Overlaps(IEnumerable<T> other)
        {
            foreach (T otherItem in other)
                if (_Nodes.ContainsKey(otherItem)) return true;
            return false;
        }

        bool ISet<T>.SetEquals(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        public void UnionWith(IEnumerable<T> other)
        {            
            foreach (T otherItem in other)            
                Add(otherItem);
        }


        #endregion


        /// <summary>
        /// A lightweight data structure that associates a T item with its rank and parent.
        /// </summary>
        private class Node
        {
            public readonly T Data;
            public int Rank;
            public Node Parent;

            public Node(T data)
            {
                this.Data = data;
                Parent = this;
                Rank = 0;
            }

        }
    }
}
