using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>
    /// A link-based data structure which maintains its contents in sorted order, but finding the appropriate position for each item 
    /// added or removed.  The SkipList's asymptotic time complexity approaches O(log_2(n)), where 'n' is the number of items contained 
    /// on the list.  It achieves this complexity by maintaining links with each item that may or may not skip over other items to 
    /// expedite comparisons.  Duplicate items are not allowed.
    /// <para/>The links from item to item will usually point to their immediately adjacent neighbors, while the rest will point to 
    /// items with at least one item skipped.  An item cannot be skipped if it has the same or fewer links than a particular item.  The 
    /// number of links an item can maintain is set randomly.  Because of this, it is possible, though highly unlikely, that every item 
    /// will maintain the same number of links, and so all links will be adjacency links.  In such case, the complexity for adds, 
    /// removes, and accessing will be O(n).  Of course, it is also highly unlikely that the links for the first item will point 
    /// directly to a position being sought.  In such case, the complexity would be O(1).
    /// <para/>Due to the similarity in asymptotic time complexities, the Skip List is a very similar alternative to a binary tree.  
    /// Where a tree would usually employ a balancing strategy to enforce logarithmic time complexity, the Skip List relies on the 
    /// vagaries of randomness to approach logarithmic complexity.  The skip list has some advantages over a binary tree in that it can 
    /// return items preceding and following an existing item much more quickly, and enumeration has lower overhead.  In this respect it
    /// is also similar to a LinkedList, but with superior performance in all but the worst cases.  Further, the likelihood of a skip 
    /// can be dialed up or down by adjusting the AdjacencyFraction, as required, and this can be done while the list is currently 
    /// populated without the need to re-generate the list (though performance won't be affected until the list has been modified to 
    /// some degree using the new AdjacencyFraction).
    /// </summary>
    /// <remarks> As described by William Pugh, referenced by Scott Mitchell of Microsoft in 
    /// https://msdn.microsoft.com/en-us/library/ms379573(v=vs.80).aspx#datastructures20_4_topic2 .  Retrieved 8/9/16.    
    /// Validated 100% code coverage 8/20/16 except GetBeforeOrEqual().
    /// Broke RemoveMin() 4/8/19.
    /// </remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DefaultMember("Item")]
    public sealed class SkipList<T> : ICollection<T>
    {

        private IComparer<T> Comparer;
        private Node Head = null;
        private Node Tail = null;
        internal const int DEFAULT_MAXHEIGHT = 4;
        internal const double DEFAULT_ADJACENCY_FRACTION = 0.5;


        private double _AdjacencyFraction = DEFAULT_ADJACENCY_FRACTION;
        /// <summary>
        /// The value given will represent the expected fraction of items that reside in this SkipList with a "height" 
        /// of 1.  Of those with a higher "height", the same fraction will have a "height" of 2, and of those higher 
        /// than 2 the same fraction will have a height of "3", and so on.  A lower adjacency fraction will allow more 
        /// skipping, so this value can fine-tune the performance of the SkipList.
        /// <para/>
        /// For O(log_2) behavior, the default adjacency fraction will be 0.5.
        /// </summary>
        public double AdjacencyFraction
        {
            get
            {
                return _AdjacencyFraction;
            }
            set
            {
                if (value <= 0.0 || value > 1.0)
                    throw new ArgumentException("SkipList.AdjacencyFraction must be a positive real number (0.0, 1.0].");
                Denom = Math.Round(1 / value, 0);
                if (Denom != (double)((int)Denom))  //Are there any doubles where 1/d does not go to an int for d?
                    throw new InvalidOperationException("Invalid adjacency value:  1 / " + Denom + ".");
                _AdjacencyFraction = value;
            }
        }

        private double Denom = 1 / DEFAULT_ADJACENCY_FRACTION;



        #region SkipList constructors
        /// <summary>
        /// Creates a new SkipList.  If the type implements IComparable, a comparator of that type will be used.  If not, throws an 
        /// exception.>
        /// </summary>
        /// <param name="adjacencyFraction">Optional.  The value given will represent the expected fraction of items that reside in this 
        /// SkipList with a "height" of 1.  Of those with a higher "height", the same fraction will have a "height" of 2, and of those 
        /// higher than 2 the same fraction will have a height of "3", and so on.  A lower adjacency fraction will allow more skipping, 
        /// so this value can fine-tune the performance of the SkipList.</param>
        public SkipList(double adjacencyFraction = DEFAULT_ADJACENCY_FRACTION)
        {
            if (typeof(IComparable<T>).IsAssignableFrom(typeof(T))) Comparer = Comparer<T>.Default;
            else if (typeof(IComparable).IsAssignableFrom(typeof(T))) Comparer = Comparer<T>.Default;
            else
                throw new InvalidOperationException("An IComparer<" + typeof(T).Name + "> cannot be created automatically.");
            AdjacencyFraction = adjacencyFraction;
        }
        /// <summary>
        /// Creates a new SkipList that uses the given comparator for internal sorting.
        /// </summary>
        /// <param name="comparer">The comparator to use for sorting.</param>
        /// <param name="adjacencyFraction">Optional.  The value given will represent the expected fraction of items that reside in this 
        /// SkipList with a "height" of 1.  Of those with a higher "height", the same fraction will have a "height" of 2, and of those 
        /// higher than 2 the same fraction will have a height of "3", and so on.  A lower adjacency fraction will allow more skipping, 
        /// so this value can fine-tune the performance of the SkipList.</param>
        public SkipList(IComparer<T> comparer, double adjacencyFraction = DEFAULT_ADJACENCY_FRACTION)
        {
            AdjacencyFraction = adjacencyFraction;
            Comparer = comparer;
        }
        /// <summary>
        /// Creates a new SkipList and populates it with the given items.  If the type implements IComparable, a default comparator for that type 
        /// will be generated.  Otherwise, throws an exception.
        /// </summary>
        /// <param name="items">The items to populate this list with upon creation.</param>
        /// /// <param name="adjacencyFraction">Optional.  The value given will represent the expected fraction of items that reside in this 
        /// SkipList with a "height" of 1.  Of those with a higher "height", the same fraction will have a "height" of 2, and of those 
        /// higher than 2 the same fraction will have a height of "3", and so on.  A lower adjacency fraction will allow more skipping, 
        /// so this value can fine-tune the performance of the SkipList.</param>
        public SkipList(IEnumerable<T> items, double adjacencyFraction = DEFAULT_ADJACENCY_FRACTION) : this(adjacencyFraction)
        {
            foreach (T item in items)
                Add(item);
        }

        /// <summary>
        /// Returns the maximum allowed height, which is the Log base d of the given count, where d is the denominator of the adjacency 
        /// fraction.
        /// </summary>
        private int GetMaxHeight(int count)
        {
            return Math.Max(Mathematics.Int32.Log(count, Denom) + 1, DEFAULT_MAXHEIGHT);
        }
        #endregion




        #region SkipList contents modification

        /// <summary>
        /// Ensures that the given item exists on this SkipList.
        /// </summary>        
        /// <returns>Returns true if the list was changed in this operation; otherwise, returns false.</returns>
        public bool Add(T item)
        {
            //Case #0 - the list is currently empty.
            if (Head == null)
            {
                Head = Node.FromKnownHeight(DEFAULT_MAXHEIGHT, item);
                Tail = Head;
                Count = 1;
                return true;
            }

            Node priorOrEqual = _GetPriorOrEqual(item), result;
            int maxHeight = GetMaxHeight(Count + 1);
            //Case #1 - adding a new head.
            if (priorOrEqual == null)
            {
                //The result will be the new head.
                result = Node.FromKnownHeight(maxHeight, item);
                int rndHeight = Node.GetRandomHeight(maxHeight, AdjacencyFraction);
                Node[] replaceBackward = new Node[rndHeight], replaceForward = new Node[rndHeight];
                for (int i = 0; i < rndHeight; i++)
                {
                    replaceBackward[i] = result;
                    result.Forward[i] = Head;
                    replaceForward[i] = Head.Forward[i];
                }
                for (int i = rndHeight; i < Head.Forward.Length; i++)
                {
                    Node oldNext = Head.Forward[i];
                    result.Forward[i] = oldNext;
                    if (oldNext != null) oldNext.Backward[i] = result;
                }
                Head.Backward = replaceBackward;
                Head.Forward = replaceForward;
                Head = result;
            }
            //Case #2 - priorOrEqual is non-null, and its data equals the item being added.
            else if (Comparer.Compare(priorOrEqual.Data, item) == 0)
                return false;
            //Case #3 - adding a new tail.
            else if (priorOrEqual == Tail)
            {
                //The result will be the new tail.
                result = Node.FromKnownHeight(maxHeight, item);
                int rndHeight = Node.GetRandomHeight(maxHeight, AdjacencyFraction);
                Node[] replaceBackward = new Node[rndHeight], replaceForward = new Node[rndHeight];
                for (int i = 0; i < rndHeight; i++)
                {
                    replaceForward[i] = result;
                    result.Backward[i] = Tail;
                    replaceBackward[i] = Tail.Backward[i];
                }
                for (int i = rndHeight; i < Tail.Backward.Length; i++)
                {
                    Node oldPrev = Tail.Backward[i];
                    result.Backward[i] = oldPrev;
                    if (oldPrev != null) oldPrev.Forward[i] = result;
                }
                Tail.Backward = replaceBackward;
                Tail.Forward = replaceForward;
                Tail = result;
            }
            //Case #4 - adding somewhere in the middle
            else
            {
                result = Node.FromRandomHeight(maxHeight, item, AdjacencyFraction);
                result.UpdateForwardLinks(priorOrEqual.Next);
                result.UpdateBackwardLinks(priorOrEqual);
            }

            //Ensure that the Head and Tail are tall enough for the whole thing.
            ResizeHead(maxHeight);
            ResizeTail(maxHeight);


            //Step #5 - finally, since the list was changed, return true.
            Count++;

            return true;
        }
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        void ICollection<T>.Add(T item)
        {
            Add(item);
        }


        /// <summary>
        /// Clears all items from this skip list.
        /// </summary>
        public void Clear()
        {
            Head = null;
            Tail = null;
            Count = 0;
        }


        /// <summary>
        /// Ensures the given item does not appear in this skip list.
        /// </summary>        
        /// <returns>Returns true if the list was changed in this operation; otherwise, returns false.</returns>
        public bool Remove(T item)
        {
            //Does the item exist here?
            if (Count == 0) return false;
            Node toRemove = _GetPriorOrEqual(item);
            if (toRemove == null) return false;
            if (Comparer.Compare(toRemove.Data, item) != 0) return false;

            //From here, the item definitely exists to be removed.
            //Is it the sole item?
            if (Count == 1)
            {
                Clear();
                return true;
            }
            //Is the item to be removed the Tail?
            if (toRemove == Tail)
            {
                Node oldTail = Tail;
                Tail = oldTail.Backward[0];
                if (Tail.Height < oldTail.Height) ResizeTail(oldTail.Height);
            }
            //Is the item to be removed the Head?
            else if (toRemove == Head)
            {
                Node oldHead = Head;
                Head = oldHead.Forward[0];
                if (Head.Height < oldHead.Height) ResizeHead(oldHead.Height);
            }
            //The item exists somewhere in the middle.
            else
            {
                for (int i = 0; i < toRemove.Backward.Length; i++)
                {
                    Node forward = toRemove.Forward[i], backward = toRemove.Backward[i];
                    forward.Backward[i] = backward;
                    backward.Forward[i] = forward;
                }
            }

            //Done.  Count down and return true.           
            Count--;
            return true;
        }
        /// <summary>
        /// Removes the minimum from this skip list.
        /// </summary>
        /// <returns>Returns the item removed; if the list was already empty, returns the Default(T) of the type of this list.</returns>
        /// <remarks>The best case for this operation is the situation where the new Min is very "short", meaning, maintains only one 
        /// or a few links to another node.  The worst case is when the new Min is as tall as the original Min. This method is an O(log N) 
        /// operation in the worst case, and a O(1) operation in the best case.</remarks>
        public T RemoveMin()
        {
            if (Head == null) return default(T);
            T result = Head.Data;
            Node newHead = Head.Next;
            Count--;
            if (newHead != null)
            {
                for (int i = 0; i < newHead.Forward.Length; i++)
                {
                    Head.Forward[i] = newHead.Forward[i];
                    Head.Forward[i].Backward[i] = newHead;
                }
                for (int i = newHead.Forward.Length; i < Head.Forward.Length; i++)
                    Head.Forward[i].Backward[i] = newHead;

                newHead.Forward = Head.Forward;
                newHead.Backward = Head.Backward;
            }
            Head = newHead;
            ResizeHead(GetMaxHeight(Count));
            return result;
        }
        /// <summary>
        /// Removes the maximum from this skip list.
        /// </summary>
        /// <returns>Returns the item removed; if the list was already empty, returns the Default(T) of the type of this list.</returns>
        /// <remarks>The best case for this operation is the situation where the new Max is very "short", meaning, maintains only one 
        /// or a few links to another node.  The worst case is when the new Max is as tall as the original Max. This method is an O(log N) 
        /// operation in the worst case, and a O(1) operation in the best case.</remarks>
        public T RemoveMax()
        {
            if (Tail == null) return default(T);
            T result = Tail.Data;
            Node newTail = Tail.Prior;
            Count--;
            if (newTail != null)
            {
                for (int i = 0; i < newTail.Backward.Length; i++)
                {
                    Tail.Backward[i] = newTail.Backward[i];
                    Tail.Backward[i].Forward[i] = newTail;
                }
                for (int i = newTail.Backward.Length; i < Tail.Backward.Length; i++)
                    Tail.Backward[i].Forward[i] = newTail;

                newTail.Backward = Tail.Backward;
                newTail.Forward = Tail.Forward;
            }
            Tail = newTail;
            ResizeTail(GetMaxHeight(Count));
            return result;
        }

        /// <summary>
        /// Changes the size of the Head to the given height.  Note that the size of a Head should never be decreased, or logarithmic search 
        /// complexity will be impaired.
        /// </summary>
        private bool ResizeHead(int height)
        {
            if (height == Head.Forward.Length) return false;
            Head.Backward = new Node[height];
            Node[] oldHeadForward = Head.Forward;
            Head.Forward = new Node[height];
            Head.UpdateForwardLinks(oldHeadForward[0]);
            if (Head != Tail)
            {
                for (int i = oldHeadForward.Length; i < Head.Forward.Length; i++)
                {
                    Head.Forward[i] = Tail;
                    if (i < Tail.Backward.Length) Tail.Backward[i] = Head;
                }
            }
            return true;
        }

        /// <summary>
        /// Changes the size of the Tail to the given height.  Note that the size of a Tail should never be decreased, or logarithmic search 
        /// complexity will be impaired.
        /// </summary>
        private bool ResizeTail(int height)
        {
            if (height == Tail.Backward.Length) return false;
            Tail.Forward = new Node[height];
            Node[] oldTailBackward = Tail.Backward;
            Tail.Backward = new Node[height];
            //oldTailBackward.CopyTo(Tail.Backward, 0);
            Tail.UpdateBackwardLinks(oldTailBackward[0]);
            if (Head != Tail)
            {
                for (int i = oldTailBackward.Length; i < Tail.Backward.Length; i++)
                {
                    Tail.Backward[i] = Head;
                    if (i < Head.Forward.Length) Head.Forward[i] = Tail;
                }
            }
            return true;
        }


        /// <summary>
        /// A lightweight data structure that holds the included data as well as a set of links to the previous and next nodes.
        /// </summary>
        private class Node
        {
            /// <summary>
            /// The included item represented at this node.
            /// </summary>
            public readonly T Data;
            /// <summary>
            /// The height of this node, meaning the number of backward and forward links maintainable at this node.
            /// </summary>
            public int Height { get { return Forward.Length; } }
            /// <summary>
            /// The forward links of this node.
            /// </summary>
            public Node[] Forward;
            /// <summary>
            /// The backward links of this node.
            /// </summary>
            public Node[] Backward;
            /// <summary>
            /// The immediately succeeding node after this node.
            /// </summary>
            public Node Next { get { return Forward[0]; } }
            /// <summary>
            /// The immediately preceding node after this node.
            /// </summary>
            public Node Prior { get { return Backward[0]; } }

            //public bool IsHead { get { return Backward[0] == null; } }
            //public bool IsTail { get { return Forward[0] == null; } }

            private static Random _Rng = new Random((int)DateTime.Now.Ticks);
            //private static Random _Rng = new Random(0);

            /// <summary>
            /// Creates a new node with the given height and data.
            /// </summary>
            private Node(int height, T data)
            {
                Forward = new Node[height];
                Backward = new Node[height];
                Data = data;
            }

            /// <summary>
            /// A factory-type constructor which returns a new node with the given height and data.
            /// </summary>
            public static Node FromKnownHeight(int height, T data)
            {
                return new Node(height, data);
            }

            /// <summary>
            /// A factory-type construction which returns a new node with a random height, equal to or lesser than the given height.
            /// </summary>
            /// <param name="maxHeight">The maxHeight should be log_2(n) where n is the number of items in the Skip List and the 
            /// adjacency fraction is 1/2.</param>
            /// <param name="data">The data represented in this node.</param>
            /// <param name="adjacencyFraction"></param>
            public static Node FromRandomHeight(int maxHeight, T data, double adjacencyFraction)
            {

                return new Node(GetRandomHeight(maxHeight, adjacencyFraction), data);
            }
            public static int GetRandomHeight(int maxHeight, double adjacencyFraction)
            {
                //Find a random height.  The probability of Height=1 is 1/2, for Height=2 is 1/4, for Height=3 is 1/8, etc.
                int height = 1;
                while (_Rng.NextDouble() < adjacencyFraction && ++height < maxHeight) ;
                return height;
            }
            /// <summary>
            /// Updates all forward links of this node, starting with the link at Forward[0], which will be set to the given node.
            /// </summary>            
            /// <param name="next">The node to begin linking forward from this node.</param>
            public void UpdateForwardLinks(Node next)
            {
                for (int i = 0; i < Forward.Length; i++)
                {
                    if (i > 0)
                        while (next != null && i >= next.Backward.Length) next = next.Forward[i - 1];
                    this.Forward[i] = next;
                    if (next != null) next.Backward[i] = this;
                }
            }
            /// <summary>
            /// Updates all backward links of this node, starting with the link at Backward[0], which will be set to the given node.
            /// </summary>
            /// <param name="prior">The node to begin linking back from this node.</param>
            public void UpdateBackwardLinks(Node prior)
            {
                for (int i = 0; i < Backward.Length; i++)
                {
                    if (i > 0)
                        while (prior != null && i >= prior.Forward.Length) prior = prior.Backward[i - 1];
                    this.Backward[i] = prior;
                    if (prior != null) prior.Forward[i] = this;
                }
            }


            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            public override string ToString()
            {
                return Data.ToString();
            }

        }


        #endregion




        #region SkipList contents queries

        /// <summary>
        /// Returns whether this list contains the given item (or an item equal to the current item, according to the set's Comparer).
        /// </summary>
        /// <remarks>This method is an O(log n) operation, where n is the number of items contained in the list.</remarks>
        public bool Contains(T item)
        {
            return GetNode(item) != null;
        }


        /// <summary>
        /// Returns the number of items held in this skip set.
        /// </summary>
        public int Count { get; private set; } = 0;
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        int ICollection<T>.Count { get { return Count; } }


        /// <summary>
        /// Returns an enumerator that steps through the contents of this list, one item at a time.
        /// </summary>     
        [System.Diagnostics.DebuggerStepThrough]
        public IEnumerator<T> GetEnumerator()
        {

            //yield return Head.Data;
            Node current = Head;
            while (current != null)
            {
                yield return current.Data;
                current = current.Forward[0];
            }

        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns the node containing the given item as its data, if it exists.  If no such node exists, returns null.
        /// <para/>This method is an O(log n) operation.
        /// </summary>
        private Node GetNode(T item)
        {
            Node focus = _GetPriorOrEqual(item);
            if (focus == null) return null;
            return (Comparer.Compare(focus.Data, item) == 0) ? focus : null;
        }

        /// <summary>
        /// Returns the Node whose data is equal or immediately prior to the given item's node, according to the Skip List's Comparer.
        /// </summary>        
        /// <returns>Returns null if there exists no node whose data is lesser than or equal to the given item.</returns>
        /// <remarks>Depending on how many items must be skipped as the prior-or-equal is sought, this method ranged between an O(1) operation 
        /// or an O(n) operation depending on the distribution of random heights of the nodes, but the most common case will approach an 
        /// O(log n) operation.</remarks>
        private Node _GetPriorOrEqual(T item)
        {
            if (Head == null) return null;
            if (Comparer.Compare(item, Head.Data) < 0) return null;

            Node focus = Head;
            for (int i = Head.Height - 1; i >= 0; i--)
            {
                Node next = focus.Forward[i];
                while (next != null && Comparer.Compare(item, next.Data) >= 0)
                {
                    focus = next;
                    next = focus.Forward[i];
                }
            }
            return focus;
        }
        public Node GetPriorOrEqual(T item) => _GetPriorOrEqual(item);
        
        /// <summary>
        /// Attempts to find the item that occurs in the Skip List immediately before the given item.  If no such item exists, returns 
        /// false, and the return 'out' variable will be equal to the default for type T.  Otherwise, returns true, and the 'out' variable 
        /// will be the item returned.
        /// </summary>
        public bool TryGetBefore(T item, out T before)
        {   
            Node prior = _GetPriorOrEqual(item);
            if (prior == null)
            {
                before = default(T);
                return false;
            }
            if (Comparer.Compare(item, prior.Data) == 0)
            {
                prior = prior.Prior;
                if (prior == null)
                {
                    before = default(T);
                    return false;
                }
            }

            before = prior.Data;
            return true;
        }

        /// <summary>Returns the contents immediately before the given item, or equal to the given item.</summary>
        public bool TryGetBeforeOrEqual(T item, out T contents)
        {
            Node n = _GetPriorOrEqual(item);
            if (n == null) { contents = default(T); return false; }
            contents = n.Data;
            return true;
        }

        /// <summary>
        /// Returns the Node whose data is equal to or immediately after the given item's node, according to the Skip List's comparer.
        /// </summary>        
        /// <returns>Returns null if there exists no node whose data is greater than or equal to the given item.</returns>
        /// /// <remarks>Depending on how many items must be skipped as the next-or-equal is sought, this method ranged between an O(1) operation 
        /// or an O(n) operation, but the most common case will approach an O(log n) operation.</remarks>
        private Node GetNextOrEqual(T item)
        {

            if (Tail == null) return null;
            if (Comparer.Compare(item, Tail.Data) > 0) return null;

            Node focus = Tail;
            for (int i = Tail.Height - 1; i >= 0; i--)
            {
                Node next = focus.Backward[i];
                while (next != null && Comparer.Compare(item, next.Data) <= 0)
                {
                    focus = next;
                    next = focus.Backward[i];
                }
            }
            return focus;
        }

        /// <summary>
        /// Returns the minimum value in this list.
        /// </summary>
        /// <remarks>This is an O(1) operation, which makes it distinct from the standard binary search tree.</remarks>
        public T Min { get { return Head.Data; } }
        /// <summary>
        /// Returns the maximum value in this list.
        /// </summary>
        /// <remarks>This is an O(1) operation, which makes it distinct from the standard binary search tree.</remarks>
        public T Max { get { return Tail.Data; } }

        /// <summary>
        /// Attempts to find the item the occurs in the Skip List immediately after the given item.  If no such item exists, returns false, and 
        /// the return 'out' variable will be equal to the default for type T.  Otherwise, returns true, and the 'out' variable will be the 
        /// item returned.
        /// </summary>
        public bool TryGetAfter(T item, out T after)
        {
            Node next = GetNextOrEqual(item);
            if (next == null)
            {
                after = default(T);
                return false;
            }
            if (Comparer.Compare(item, next.Data) == 0)
            {
                next = next.Next;
                if (next == null)
                {
                    after = default(T);
                    return false;
                }
            }

            after = next.Data;
            return true;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public override string ToString() { return "Count = " + Count; }


        #endregion



        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            Node current = Head;
            while (current != null)
            {
                if (arrayIndex >= array.Length) return;
                array[arrayIndex++] = current.Data;
                current = current.Forward[0];
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        bool ICollection<T>.IsReadOnly { get { return false; } }


    }
}
