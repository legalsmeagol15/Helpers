using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Sets
{
    public sealed class IndexingBinaryTree<T> : IList<T> where T : IComparable<T>
        //TODO:  fully implement  IndexingBinaryTree.
    {
        private Node _Root = null;
        //private int _LevelSize = 0;

        T IList<T>.this[int index]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        #region IndexingBinaryTree contents manipulation members

        public bool Add(T item)
        {
            throw new NotImplementedException();
        }

        void ICollection<T>.Add(T item) { this.Add(item); }

        public void Clear()
        {
            _Root = null;
            Count = 0;
        }

        public bool Insert(int index, T item)
        {
            throw new NotImplementedException();
        }
        void IList<T>.Insert(int index, T item) { this.Insert(index, item); }


        bool ICollection<T>.Remove(T item)
        {
            throw new NotImplementedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }


        #endregion

        #region IndexingBinaryTree contents queries

        public int Count { get; private set; }

        int ICollection<T>.Count { get { return this.Count; } }

        bool ICollection<T>.IsReadOnly { get { return false; } }

        public bool Contains(T item) { return GetContainingNode(item) != null; }

        public void CopyTo(T[] array, int arrayIndex)
        {
            
        }

        private Node GetContainingNode(T item)
        {
            if (_Root == null) return null;

            Node focus = _Root;
            while (true)
            {
                if (item.Equals(focus.Data)) return focus;
                int c = item.CompareTo(focus.Data);
                
                if (c > 0)
                {
                    if (focus.Right == null) return null;
                    focus = focus.Right;
                }
                else if (c < 0)
                {
                    if (focus.Left == null) return null;
                    focus = focus.Left;
                }
                else if (c == 0) return null;
            }            
        }

        private int GetContainingIndex(T item)
        {
            if (_Root == null) return -1;

            //int route = 1;
            Node focus = _Root;
            while (true)
            {
                
            }
        }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }

        public IEnumerator<T> GetEnumerator()
        {
            if (_Root == null) yield break;            
            Stack<Node> stack = new Stack<Node>();
            stack.Push(_Root);
            while (stack.Count > 0)
            {
                Node focus = stack.Pop();
                yield return focus.Data;
                if (focus.Left != null) stack.Push(focus.Left);
                if (focus.Right != null) stack.Push(focus.Right);
            }
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

       
        #endregion

        private class Node
        {
            public readonly T Data;
            public readonly Node Right = null;
            public readonly Node Left = null;
            public readonly int Depth;
            public Node(T data, int depth)
            {
                this.Data = data;
                this.Depth = depth;
            }
            
        }
    }
}
