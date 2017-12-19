using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{

    /// <summary>
    /// A linked list that allows replacements, insertions, and deletions mid-list.  Each such insertion or deletion is potentially an O(N) operation.
    /// </summary>
    public class DynamicLinkedList<T> : IEnumerable<T>, ICollection<T>
    {

        //TODO:  validate DynamicLinkedList

        /// <summary>The Node containing the first item in this list.</summary>
        public Node FirstNode { get; private set; } = null;

        /// <summary>The Node containing the last item in this list.</summary>
        public Node LastNode { get; private set; } = null;

        /// <summary>The first item in the list.</summary>
        public T First => FirstNode.Contents;

        /// <summary>The last item in the list.</summary>
        public T Last => LastNode.Contents;

        


        /// <summary>Creates a new dynamic linked list.</summary>
        public DynamicLinkedList() { }

        /// <summary>Creates a new dynamic linked list from the given <paramref name="source"/>.</summary>
        /// <param name="source">The items that will be added to the list, in the order added.</param>
        public DynamicLinkedList(IEnumerable<T> source) { foreach (T item in source) AddLast(item); }



        #region DynamicLinkedList contents management

        /// <summary>Adds a new item to the list at the head position.  Returns a reference to the added node.</summary>
        public Node AddFirst(T item)
        {
            if (FirstNode == null)
            {
                FirstNode = new Node(item, this, null, null);
                LastNode = FirstNode;
                Count = 1;
                return LastNode;
            }
            FirstNode = new Node(item, this, null, FirstNode);
            FirstNode.Next.Previous = FirstNode;
            Count++;
            return FirstNode;
        }
        /// <summary>Adds a new item to the list at the tail position.  Returns a reference to the added node.</summary>
        public Node AddLast(T item)
        {
            if (FirstNode == null)
            {
                FirstNode = new Node(item, this, null, null);
                LastNode = FirstNode;
                Count = 1;
                return FirstNode;
            }
            LastNode = new Node(item, this, LastNode, null);
            LastNode.Previous.Next = LastNode;
            Count++;
            return LastNode;
        }

        /// <summary>Returns the first item in the list where the given <paramref name="comparer"/> returns true.</summary>
        /// <param name="comparer">The function that will return true or false for a match.</param>        
        public Node GetFirstMatch(Func<T, bool> comparer)
        {
            Node focus = FirstNode;
            while (focus != null && !comparer(focus.Contents)) focus = focus.Next;
            return focus;
        }
        /// <summary>Returns the last item in the list where the given <paramref name="comparer"/> returns true.</summary>
        /// <param name="comparer">The function that will return true or false for a match.</param>        
        public Node GetLastMatch(Func<T, bool> comparer)
        {
            Node focus = LastNode;
            while (focus != null && !comparer(focus.Contents)) focus = focus.Previous;
            return focus;
        }


        /// <summary>
        /// Removes all items from this list.
        /// </summary>
        public void Clear()
        {
            FirstNode = null;
            LastNode = null;
            Count = 0;
        }

        /// <summary>Removes the first item from this list.</summary>
        /// <returns>Returns the item removed.  If the list was empty, returns the default value of T.</returns>
        public T RemoveFirst()
        {
            if (Count == 0) return default(T);
            T result = FirstNode.Contents;
            FirstNode = FirstNode.Next;
            if (FirstNode != null) FirstNode.Previous = null;
            else LastNode = null;
            Count--;
            return result;
        }
        /// <summary>Removes the last item from this list.</summary>
        /// <returns>Returns the item removed.  If the list was empty, returns the default value of T.</returns>
        public T RemoveLast()
        {
            if (Count == 0) return default(T);
            T result = LastNode.Contents;
            LastNode = LastNode.Next;
            if (LastNode != null) LastNode.Next = null;
            else FirstNode = null;
            Count--;
            return result;
        }


        void ICollection<T>.Add(T item) { AddLast(item); }

        bool ICollection<T>.Contains(T item) => GetFirstMatch((existing) => existing.Equals(item)) != null;

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            Node node = FirstNode;
            while (arrayIndex < array.Length && node != null) array[arrayIndex++] = node.Contents;
        }

        bool ICollection<T>.Remove(T item)
        {
            Node node = GetFirstMatch((existing) => existing.Equals(item));
            if (node == null) return false;
            node.Remove();
            return true;
        }

        #endregion



        #region DynamicLinkedList queries

        /// <summary>Returns the count of the number of items in this list.</summary>
        public int Count { get; private set; } = 0;


        bool ICollection<T>.IsReadOnly => false;


        /// <summary>Returns an enumerator that steps through this list.</summary>
        public IEnumerator<T> GetEnumerator()
        {
            if (FirstNode == null) yield break;
            Node focus = FirstNode;
            yield return focus.Contents;
            while (focus.Next != null) { focus = focus.Next; yield return focus.Contents; }
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        /// <summary>Returns the list's content nodes, in order.</summary>        
        public IEnumerable<Node> Nodes()
        {
            Node n = FirstNode;
            while (n != null) { yield return n; n = n.Next; }
        }

        /// <summary>Returns the list's content nodes, in reversed order.</summary>    
        public IEnumerable<Node> NodesReversed()
        {
            Node n = LastNode;
            while (n != null) { yield return n; n = n.Previous; }
        }


        #endregion



        /// <summary>A lightweight data structure representing a position within a dynamic linked list.</summary>
        public class Node
        {
            /// <summary>The value associated with this Node.</summary>
            public T Contents { get; set; }

            /// <summary>The previous Node in the list.  If this node is the beginning of the list, the reference will be null.</summary>
            public Node Previous { get; internal set; }

            /// <summary>The next Node in the list.  If this Node is the end of the list, the reference will be null.</summary>
            public Node Next { get; internal set; }

            /// <summary>A reference to the DynamicLinkedList that contains this Node.</summary>
            public DynamicLinkedList<T> List { get; private set; }

            /// <summary>
            /// Creates a new Node in the given list, specifying the given <paramref name="previous"/> Node and the 
            /// <paramref name="next"/> node.
            /// </summary>
            /// <param name="value">The value being added to the list, which will be contained in this Node.</param>
            /// <param name="list">A reference to the list being modified.</param>
            /// <param name="previous">The previous Node in this list.</param>
            /// <param name="next">The next node in this list.</param>
            internal Node(T value, DynamicLinkedList<T> list, Node previous, Node next)
            {
                Contents = value;
                List = list;
                Previous = previous;
                Next = next;
            }

            /// <summary>Removes the item and node associated with this node from the list.  Returns the item removed.</summary>
            public T Remove()
            {
                T result = Contents;

                //Update the host list to reflect this item is removed.
                if (Previous != null) Previous.Next = Next;
                else List.FirstNode = Next;

                if (Next != null) Next.Previous = Previous;
                else List.LastNode = Previous;

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


            /// <summary>Inserts the item immediately after this node, and returns the node of the newly-added item.</summary>
            public Node InsertAfter(T item)
            {
                Node newNode = new Node(item, List, this, Next);
                if (Next == null)
                {
                    Next = newNode;
                    List.LastNode = newNode;
                }
                else
                {
                    newNode.Next.Previous = newNode;
                    Next = newNode;
                }
                List.Count++;
                return newNode;
            }


            /// <summary>Inserts the set of items immediately before this item node, and returns the node of the earliest (most head-ward) 
            /// item added.</summary>
            public Node InsertBefore(IEnumerable<T> items)
            {
                Node previous = Previous;
                if (items.Count() == 0) return null;
                foreach (T item in items) InsertBefore(item);
                return previous == null ? List.FirstNode : previous.Next;

            }
            /// <summary>Inserts the item immediately before this node, and returns the node of the newly-added item.</summary>
            public Node InsertBefore(T item)
            {
                Node newNode = new Node(item, List, Previous, this);
                if (Previous == null)
                {
                    Previous = newNode;
                    List.FirstNode = newNode;
                }
                else
                {
                    newNode.Previous.Next = newNode;
                    Previous = newNode;
                }
                List.Count++;
                return newNode;
            }

        }
    }
}
