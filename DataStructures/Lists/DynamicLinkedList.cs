using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{

    /// <summary>
    /// A linked list that allows insertions and deletions mid-list.  Each such insertion or deletion is potentially an O(N) operation.
    /// <para/>
    [DebuggerDisplay("Count = {Count}")]
    [DefaultMember("Item")]
    public class DynamicLinkedList<T> : IEnumerable<T>, ICollection<T>
    {

        //TODO:  validate DynamicLinkedList

        private Node _FirstNode = null, _LastNode = null;

        /// <summary>The first item in the list.</summary>
        public T First => _FirstNode.Contents;

        /// <summary>The last item in the list.</summary>
        public T Last => _LastNode.Contents;



        public DynamicLinkedList()
        {

        }

        public DynamicLinkedList(IEnumerable<T> source)
        {
            foreach (T item in source) AddLast(item);
        }


        #region DynamicLinkedList contents management

        void ICollection<T>.Add(T item) => AddLast(item);

        /// <summary>Adds a new item to the list at the head position.</summary>
        public Node AddFirst(T item)
        {
            if (_FirstNode == null)
            {
                _FirstNode = new Node(item, this, null, null);
                _LastNode = _FirstNode;
                Count = 1;
            }
            else
            {
                _FirstNode = new Node(item, this, null, _FirstNode);
                _FirstNode.Next.Previous = _FirstNode;
                Count++;
            }
            return _FirstNode;
        }
        public Node AddFirst(Node node)
        {
            if (ReferenceEquals(this, node.List))
                throw new InvalidOperationException("A list's nodes cannot be concatenated with the same list.");
            this.Count += node.List.Count;
            node.List.Count = this.Count;
            throw new NotImplementedException();
        }
        /// <summary>Adds a new item to the list at the tail position.</summary>
        public Node AddLast(T item)
        {
            if (_FirstNode == null)
            {
                _FirstNode = new Node(item, this, null, null);
                _LastNode = _FirstNode;
                Count = 1;
            }
            else
            {
                _LastNode = new Node(item, this, _LastNode, null);
                _LastNode.Previous.Next = _LastNode;
                Count++;
            }
            return _LastNode;
        }



        /// <summary>
        /// Removes all items from this list.
        /// </summary>
        public void Clear()
        {
            _FirstNode = null;
            _LastNode = null;
            Count = 0;
        }


        bool ICollection<T>.Remove(T item)
        {
            Node n = GetNodes().FirstOrDefault(node => node.Contents.Equals(item));
            if (n == null) return false;
            n.Remove();
            return true;
        }
        public T RemoveFirst()
        {
            if (Count == 0) return default(T);
            T result = _FirstNode.Contents;
            _FirstNode = _FirstNode.Next;
            if (_FirstNode != null) _FirstNode.Previous = null;
            else _LastNode = null;
            Count--;
            return result;
        }
        public T RemoveLast()
        {
            if (Count == 0) return default(T);
            T result = _LastNode.Contents;
            _LastNode = _LastNode.Next;
            if (_LastNode != null) _LastNode.Next = null;
            else _FirstNode = null;
            Count--;
            return result;
        }

        #endregion



        #region DynamicLinkedList queries

        public int Count { get; private set; } = 0;

        public IEnumerator<T> GetEnumerator()
        {
            if (_FirstNode == null) yield break;
            Node focus = _FirstNode;
            yield return focus.Contents;
            while (focus.Next != null) { focus = focus.Next; yield return focus.Contents; }
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public Node FirstNode => _FirstNode;
        public Node LastNode => _LastNode;


        bool ICollection<T>.IsReadOnly => false;

        public IEnumerable<Node> GetNodes()
        {
            Node n = FirstNode;
            while (n != null) { yield return n; n = n.Next; }
        }



        /// <summary>This is an O(n) operation.</summary>
        public bool Contains(T item) => (item != null) ? this.Any(existing => existing.Equals(item)) : this.Any(existing => existing == null);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (arrayIndex < 0) return;
            foreach (T item in this)
            {
                if (arrayIndex >= array.Length) return;
                array[arrayIndex] = item;
            }
        }


        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        #endregion



        /// <summary>A lightweight data structure representing a position within a dynamic linked list.</summary>
        public class Node
        {
            /// <summary>
            /// 
            /// </summary>
            public T Contents { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public Node Previous { get; internal set; }

            /// <summary>
            /// 
            /// </summary>
            public Node Next { get; internal set; }

            /// <summary>
            /// 
            /// </summary>
            public DynamicLinkedList<T> List { get; private set; }

            /// <summary>
            /// 
            /// </summary>
            public Node(T contents, DynamicLinkedList<T> list, Node previous, Node next)
            {
                Contents = contents;
                List = list;
                Previous = previous;
                Next = next;
            }

            /// <summary>
            /// Removes the item associated with this node from the list, by removing the node.  Returns the item removed.
            /// </summary>
            public T Remove()
            {
                T result = Contents;

                //Update the host list to reflect this item is removed.
                if (Previous != null) Previous.Next = Next;
                else List._FirstNode = Next;

                if (Next != null) Next.Previous = Previous;
                else List._LastNode = Previous;

                List.Count--;

                //Update this node to reflect it has no list.
                List = null;
                Previous = null;
                Next = null;

                return result;
            }


            /// <summary>
            /// Inserts the set of items immediately after this node, and returns the node of the last (most tail-ward) item added.
            /// </summary>
            public Node InsertAfter(IEnumerable<T> items)
            {
                Node focus = this;
                foreach (T item in items) focus = focus.InsertAfter(item);
                return focus;
            }
            /// <summary>
            /// Inserts the item immediately after this node, and returns the node of the newly-added item.
            /// </summary>
            public Node InsertAfter(T item)
            {
                Node newNode = new Node(item, List, this, Next);
                if (Next == null)
                {
                    Next = newNode;
                    List._LastNode = newNode;
                }
                else
                {
                    newNode.Next.Previous = newNode;
                    Next = newNode;
                }
                List.Count++;
                return newNode;
            }
            /// <summary>
            /// Inserts the set of items immediately before this item node, and returns the node of the earliest (most head-ward) item added.
            /// </summary>
            public Node InsertBefore(IEnumerable<T> items)
            {
                Node previous = Previous;
                if (items.Count() == 0) return null;
                foreach (T item in items) InsertBefore(item);
                return previous == null ? List._FirstNode : previous.Next;
            }
            /// <summary>
            /// Inserts the item immediately before this node, and returns the node of the newly-added item.
            /// </summary>
            public Node InsertBefore(T item)
            {
                Node newNode = new Node(item, List, Previous, this);
                if (Previous == null)
                {
                    Previous = newNode;
                    List._FirstNode = newNode;
                }
                else
                {
                    newNode.Previous.Next = newNode;
                    Previous = newNode;
                }
                List.Count++;
                return newNode;
            }





            /// <summary>Removes from the containing list this Node, to the given end node.</summary>
            /// <param name="endNode">Optional.  If omitted or provided null, removes all nodes from this one to the end.</param>
            /// <returns>Returns the count of nodes removed from the containing list.</returns>
            public int RemoveRange(Node endNode = null)
            {
                if (this == endNode) { Remove(); return 1; }
                if (endNode != null && endNode.List != List)
                    throw new ArgumentException("Given endNode does not exist in the same list.");

                //Clean up refs to the start of the removed part.
                var list = List;
                if (Previous != null)
                    Previous.Next = (endNode == null) ? null : endNode.Next;
                if (list._FirstNode == this) list._FirstNode = endNode.Next;

                //Removes the range.
                int count = 0;
                Node focus = this;
                while (focus != endNode)
                {
                    count++;
                    focus.List = null;
                    focus = focus.Next;
                }

                //Clean up refs to the end of the removed part.
                if (endNode != null)
                {
                    endNode.Previous = this.Previous;
                    if (list._LastNode == endNode) list._LastNode = Previous;
                    endNode.Next = null;
                }

                Previous = null;
                list.Count -= count;
                return count;
            }

            /// <summary>
            /// 
            /// </summary>
            public override string ToString() => "Node " + ((Contents == null) ? "_" : Contents.ToString());

            /// <summary>Steps to the next node.</summary>
            public static Node operator ++(Node n) => n.Next;
            /// <summary>Steps to the previous node.</summary>
            public static Node operator --(Node n) => n.Previous;
        }



    }
}
