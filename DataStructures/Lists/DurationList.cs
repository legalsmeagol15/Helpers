using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>
    /// A class similar to a duration, except that every add has a beginning and an ending position, and adding an item to the list may force 
    /// other items to be excluded.  This data structure is well suited for timelines and similar notions.
    /// <para/>In the back end, this data structure is base on a Skip List, which handily maintains items in sequential order and enforces 
    /// O(log n) access by skipping items in a search in a semi-random fashion.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    
    public sealed class DurationList <TKey, TValue> //: IDictionary<TKey, TValue> 
    {
        //TODO:  validate Duration List.
        private IComparer<TKey> Comparer;
        private Node Head = null;
        private Node Tail = null;
        internal const int DEFAULT_MAXHEIGHT = 4;
        internal const double DEFAULT_ADJACENCY_FRACTION = 0.5;


        private double _AdjacencyFraction = DEFAULT_ADJACENCY_FRACTION;
        public double AdjacencyFraction
        {
            get
            {
                return _AdjacencyFraction;
            }
            set
            {
                if (value <= 0.0 || value > 1.0)
                    throw new ArgumentException("DurationList.AdjacencyFraction must be a positive real number (0.0, 1.0].");
                Denom = Math.Round(1 / value, 0);
                if (Denom != (double)((int)Denom))  //Are there any doubles where 1/d does not go to an int for d?
                    throw new InvalidOperationException("Invalid adjacency value:  1 / " + Denom + ".");
                _AdjacencyFraction = value;
            }
        }

        private double Denom = 1 / DEFAULT_ADJACENCY_FRACTION;



        #region DurationList constructors
        /// <summary>
        /// Creates a new DurationList.
        /// </summary>
        public DurationList()
        {
            if (typeof(IComparable<TKey>).IsAssignableFrom(typeof(TKey))) Comparer = Comparer<TKey>.Default;
            else if (typeof(IComparable).IsAssignableFrom(typeof(TKey))) Comparer = Comparer<TKey>.Default;
            else
                throw new InvalidOperationException("An IComparer<" + typeof(TKey).Name + "> cannot be created automatically.");
        }
      
        ///// <summary>
        ///// Creates a new DurationList and populates it with the given items.  If the type implements IComparable, a default comparator for that 
        ///// type 
        ///// will be generated.  Otherwise, throws an exception.
        ///// </summary>
        ///// <param name="items"></param>
        //public DurationList(IEnumerable<TKey> items) : this()
        //{
        //    foreach (TKey item in items)
        //        Add(item);
        //}

        /// <summary>
        /// Creates a new DurationList that uses the given comparator for internal sorting.
        /// </summary>
        /// <param name="comparer">The comparator to use for sorting.</param>
        /// <param name="adjacencyFraction">Optional.  The value given will represent the expected fraction of items that reside in this 
        /// DurationList with a "height" of 1.  Of those with a higher "height", the same fraction will have a "height" of 2, and of those 
        /// higher than 2 the same fraction will have a height of "3", and so on.  A lower adjacency fraction will allow more skipping, 
        /// so this value can fine-tine the performance of the DurationList.</param>
        public DurationList(IComparer<TKey> comparer, double adjacencyFraction = DEFAULT_ADJACENCY_FRACTION)
        {
            AdjacencyFraction = adjacencyFraction;
            Comparer = comparer;
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




        #region DurationList contents modification

        /// <summary>
        /// Adds the given item with the given range to this DurationList, but only if doing so would not override items already present.
        /// </summary>
        /// <param name="start">The starting key for this item.</param>
        /// <param name="end">The ending key for this item.</param>
        /// <param name="item">The item to add to this DurationList.</param>
        /// <returns>Returns whether this operation changes the list.  If an item exists that conflicts with the given range, will return 
        /// false; otherwise, returns true.</returns>
        public bool Add(TKey start, TKey end, TValue item)
        {
            //If we're just adding a point, follow the special logic there.
            if (Comparer.Compare(start, end) == 0) return Add(start, item);

            //If the list is empty, just add.
            if (Head== null)
            {
                Head = Node.FromKnownHeight(DEFAULT_MAXHEIGHT, start, end, item);
                Tail = Head;
                Count = 1;
                return true;
            }
                        
            Node prior, after, conflict = GetNode(start, out prior, out after);

            if (Insert(prior, start, item, end, after) == null)
            {
                if (IsSingleton(after) && Insert(after, start, item, end, after.Forward[0]) != null) return true;
                return false;
            }

            ////Is there a conflict with what already exists?
            //if (conflict != null)
            //{
            //    if (Comparer.Compare(conflict.End, start) == 0) //orange bracket
            //    {
            //        if (after != null && Comparer.Compare(after.Start, start) == 0) //blue bracket
            //        {
            //            if (Comparer.Compare(after.Start, after.End) == 0)
            //            {
            //                if (Comparer.Compare(start, end) == 0) return false;
            //                Node forward = after.Forward[0];
            //                if (forward != null && Comparer.Compare(forward.Start, start) == 0) return false;
            //                prior = conflict;
            //                conflict = null;
            //            }
            //            else if (Comparer.Compare(start, end) < 0) return false;
            //            else if (Comparer.Compare(conflict.Start, conflict.End) == 0) return false;
            //            else
            //            {
            //                prior = conflict;
            //                conflict = null;
            //            }
            //        }
            //        else if (Comparer.Compare(conflict.Start, conflict.End) < 0)
            //        {
            //            prior = conflict;
            //            conflict = null;
            //        }
            //        else if (Comparer.Compare(start, end) == 0) return false;
            //        else
            //        {
            //            prior = conflict;
            //            conflict = null;
            //        }
            //    }
            //    else if (Comparer.Compare(conflict.Start, conflict.End) < 0) //purple bracket
            //    {
            //        if (Comparer.Compare(start, end) < 0) return false;
            //        after = conflict;
            //        conflict = null;
            //    }
            //    else if (Comparer.Compare(conflict.Start, conflict.End) == 0)
            //    {
            //        after = conflict;
            //        conflict = null;
            //    }
            //    else
            //        return false;
            //}



            return true;
        }
        /// <summary>
        /// Ensures that the given item exists on this DurationList, at the given key point.
        /// </summary>        
        /// <returns>Returns true if the list was changed in this operation; otherwise, returns false.</returns>
        public bool Add(TKey key, TValue item)
        {
            //If the list is empty, just add.
            if (Head == null)
            {
                Head = Node.FromKnownHeight(DEFAULT_MAXHEIGHT, key, key, item);
                Tail = Head;
                Count = 1;
                return true;
            }

            Node prior, next, conflict = GetNode(key, out prior, out next);
            return Insert(prior, key, item, key, next) != null;

            ////Is there a conflict?
            //if (conflict != null)
            //{
            //    if (Comparer.Compare(conflict.Start, conflict.End) == 0)
            //        return false;
            //    if (Comparer.Compare(conflict.Start, key) == 0)
            //    {
            //        next = conflict;
            //        conflict = null;
            //    }
            //    else if (Comparer.Compare(conflict.End, key) == 0)
            //    {
            //        prior = conflict;
            //        conflict = null;
            //    }
            //    else
            //        return false;
            //}


        }


        /// <summary>
        /// Deletes the given node from the list.
        /// </summary>        
        private void DeleteNode(Node node)
        {
            if (node == Head)
            {
                if (node== Tail)
                {
                    Head = null;
                    Tail = null;                
                }
                else
                {
                    Node newHead = Head.Forward[0];
                    newHead.Backward = Head.Backward;
                    for (int i = newHead.Forward.Length; i < Head.Forward.Length; i++) Head.Forward[i].Backward[i] = newHead;
                    for (int i = 0; i < newHead.Forward.Length; i++) Head.Forward[i] = newHead.Forward[i];
                    newHead.Forward = Head.Forward;
                }
            }
            else if (node == Tail)
            {
                Node newTail = Tail.Backward[0];
                newTail.Forward = Tail.Forward;
                for (int i = newTail.Backward.Length; i < Tail.Backward.Length; i++) Tail.Backward[i].Forward[i] = newTail;
                for (int i = 0; i < newTail.Backward.Length; i++) Tail.Backward[i] = newTail.Backward[i];
                newTail.Backward = Tail.Backward;
            }
            else
            {
                for (int i = 0; i < node.Backward.Length; i++)
                {
                    Node back = node.Backward[i], front = node.Forward[i];
                    if (back != null) back.Forward[i] = node.Forward[i];
                    if (front != null) front.Backward[i] = node.Backward[i];
                }
                
            }
            Count--;
        }
        

        /// <summary>
        /// Inserts a new node with the given characteristics in between the given bracketing nodes.
        /// </summary>
        /// <param name="prior">The node to precede the new node.</param>
        /// <param name="start">The starting key of the new node.</param>
        /// <param name="item">The data item to be contained at the new node.</param>
        /// <param name="end">The ending key of the new node.</param>
        /// <param name="after">The node to follow the new node.</param>
        /// <returns>Returns the new Node created.  However, if inserting the node would cause a sequencing violation, returns null.</returns>
        /// <remarks>Throws an InvalidOperationException in the following cases: 1) BOTH bracketing nodes are null; and 2) the bracketing 
        /// nodes are non-adjacent; </remarks>
        private Node Insert(Node prior, TKey start, TValue item, TKey end,  Node after)
        {
            if (prior!= null)
            {
                int c = Comparer.Compare(prior.End, start);
                if (c > 0) return null;
                if (c == 0 && IsSingleton(prior) && Comparer.Compare(start, end) == 0) return null;
                if (prior.Forward[0] != after) throw new InvalidOperationException("Cannot insert between non-adjacent nodes.");
            }
            else if (after!= null)
            {
                int c = Comparer.Compare(end, after.Start);
                if (c > 0) return null;
                if (c == 0 && IsSingleton(after) && Comparer.Compare(start, end) == 0) return null;
                if (after.Backward[0] != prior) throw new InvalidOperationException("Cannot insert between non-adjacent nodes.");
            }
            else
            {
                throw new InvalidOperationException("Cannot insert between two null nodes.");
            }
            

            int maxHeight = GetMaxHeight(++Count);
            Node result;

            //Adding at the head?
            if (prior == null)
            {                
                result = new Node(start, end, item);
                result.Backward = Head.Backward;
                result.Forward = Head.Forward;
                int headHeight = Node.GetRandomHeight(Count, AdjacencyFraction);
                Head.Backward = new Node[headHeight];
                Head.Forward = new Node[headHeight];
                for (int i = 0; i < headHeight; i++)
                {
                    Head.Forward[i] = result.Forward[i];
                    Head.Backward[i] = result;
                    result.Forward[i] = Head;
                }
                for (int i = headHeight; i < result.Forward.Length; i++)
                    result.Forward[i].Backward[i] = result;
                Head = result;
            }
            
            //Adding at the tail?
            else if (after == null)
            {                
                result = new Node(start, end, item);
                result.Backward = Tail.Backward;
                result.Forward = Tail.Forward;
                int tailHeight = Node.GetRandomHeight(Count, AdjacencyFraction);
                Tail.Backward = new Node[tailHeight];
                Tail.Forward = new Node[tailHeight];
                for (int i = 0; i < tailHeight; i++)
                {
                    Tail.Backward[i] = result.Backward[i];
                    Tail.Forward[i] = result;
                    result.Backward[i] = Tail;
                }
                for (int i = tailHeight; i < result.Backward.Length; i++)
                    result.Backward[i].Forward[i] = result;
                Tail = result;
            }

            //Otherwise, adding somewhere in the middle.
            else
            {
                result = Node.FromRandomHeight(maxHeight, start, end, item, AdjacencyFraction);
                result.UpdateBackwardLinks(prior);
                result.UpdateForwardLinks(after);
            }

            //Finally, check that the head and tail are big enough.
            if (Head.Height < maxHeight) ResizeHead(maxHeight);
            if (Tail.Height < maxHeight) ResizeTail(maxHeight);

            return result;
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
        /// Removes a singleton, if one occurs, from this list at the given key point.
        /// </summary>
        /// <returns>Returns true if a singleton was removed; otherwise, there was no change, and the method returns false.</returns>
        public bool Remove(TKey key)
        {
            Node prior, next, focus = GetNode(key, out prior, out next);
            if (IsSingleton(focus))
            {
                DeleteNode(focus);
                return true;
            }
            if (Comparer.Compare(next.End, key) == 0)
            {
                DeleteNode(next);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Removals all item ranges between or bordering on the given keys.  If a singleton occurs at the start or end, the singleton will 
        /// be removed as well.
        /// </summary>
        public bool Remove(TKey start, TKey end)
        {            
            Node prior, after, focus = GetNode(start, out prior, out after);
            if (focus == null)
            {
                if (after != null && Comparer.Compare(end, after.Start) >= 0) focus = after;
                else return false;
            }

            List<Node> toRemove = new List<Node>();
            bool changed = false;

            while (focus != null)
            {
                int cStart = Comparer.Compare(focus.Start, start), cEnd = Comparer.Compare(focus.End, end);

                if (cStart >= 0)
                {
                    if (cEnd <= 0) //Removal entirely encloses the focus.
                    {
                        focus = focus.Forward[0];
                        toRemove.Add(focus);
                    }
                    else   //Removal clips the focus on the start side - reached the end of the removal.
                    {
                        focus.Start = end;
                        changed = true;
                        break;
                    }
                }
                else
                {
                    if (cEnd <= 0)  //Removal clips the focus on the end side.
                    {
                        focus.End = start;
                        focus = focus.Forward[0];
                    }
                    else  //Bisect the focus - reached the end of the removal.
                    {
                        TKey newEnd = focus.End;
                        focus.End = start;
                        Insert(focus, end, focus.Value, newEnd, focus.Forward[0]);
                        changed = true;
                        break;
                    }
                }
            }

            if (toRemove.Count == 0) return changed;

            foreach (Node dead in toRemove)
                DeleteNode(dead);

            return true;
        }
        /// <summary>
        /// Ensures that all instances of the item are removed from the list.
        /// </summary>
        /// <returns>Returns true if the list was changed, false if it does not.</returns>
        public bool Remove(TValue item)
        {
            Node n = Head;
            bool changed = false;
            while (n!= null)
            {
                if (n.Value.Equals(item))
                {
                    DeleteNode(n);
                    changed = true;
                }
                n = n.Forward[0];
            }
            return changed;
        }
        /// <summary>
        /// Removes the minimum from this skip list.
        /// </summary> 
        /// <remarks>The best case for this operation is the situation where the new Min is very "short", meaning, maintains only one 
        /// or a few links to another node.  The worst case is when the new Min is as tall as the original Min. This method is an O(log N) 
        /// operation in the worst case, and a O(1) operation in the best case.</remarks>
        public void RemoveMin()
        {
            DeleteNode(Head);
        }
        /// <summary>
        /// Removes the maximum from this skip list.
        /// </summary>        
        /// <remarks>The best case for this operation is the situation where the new Max is very "short", meaning, maintains only one 
        /// or a few links to another node.  The worst case is when the new Max is as tall as the original Max. This method is an O(log N) 
        /// operation in the worst case, and a O(1) operation in the best case.</remarks>
        public void RemoveMax()
        {
            DeleteNode(Tail);
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
        /// A lightweight data structure that holds the included data, its start and end keys, and a set of links to the previous and next nodes.
        /// </summary>
        private class Node
        {
            /// <summary>
            /// The starting key of the item represented at this node.
            /// </summary>
            public TKey Start;
            /// <summary>
            /// The ending key of the item represented at this node.
            /// </summary>
            public TKey End;
            /// <summary>
            /// The value represented by this node.
            /// </summary>
            public readonly TValue Value;
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

            //private static Random _Rng = new Random((int)DateTime.Now.Ticks);
            private static Random _Rng = new Random(0);

            /// <summary>
            /// Creates a new node with the given height and data.
            /// </summary>
            private Node(int height, TKey start, TKey end, TValue value) : this(start,end, value)
            {
                Forward = new Node[height];
                Backward = new Node[height];                
            }
            /// <summary>
            /// Creates a node with uninitialized Forward[] and Backward[] links.
            /// </summary>
            public Node (TKey start, TKey end, TValue value)
            {
                Start = start;
                End = end;
                Value = value;
            }

            
            /// <summary>
            /// A factory-type constructor which returns a new node with the given height and data.
            /// </summary>
            public static Node FromKnownHeight(int height, TKey start, TKey end, TValue value)
            {
                return new Node(height, start, end, value);
            }

            /// <summary>
            /// A factory-type construction which returns a new node with a random height, equal to or lesser than the given height.
            /// </summary>
            /// <param name="maxHeight">The maxHeight should be log_2(n) where n is the number of items in the Skip List and the 
            /// adjacency fraction is 1/2.</param>
            /// <param name="data">The data represented in this node.</param>
            public static Node FromRandomHeight(int maxHeight, TKey start, TKey end, TValue value, double adjacencyFraction)
            {

                return new Node(GetRandomHeight(maxHeight, adjacencyFraction), start, end, value);
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
                    while (next != null && i >= next.Backward.Length) next = next.Forward[i - 1];
                    this.Forward[i] = next;
                    if (next != null) next.Backward[i] = this;
                }
            }
            /// <summary>
            /// Updates all backward links of this node, starting with the link at Backward[0], which will be set to the given node.
            /// </summary>
            /// <param name="prior">The node to begin linking backward from this node.</param>
            public void UpdateBackwardLinks(Node prior)
            {
                for (int i = 0; i < Backward.Length; i++)
                {
                    while (prior != null && i >= prior.Forward.Length) prior = prior.Backward[i - 1];
                    this.Backward[i] = prior;
                    if (prior != null) prior.Forward[i] = this;
                }
            }


            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            public override string ToString()
            {
                return "[" + Start.ToString() + ".." + Value.ToString() + ".." + End.ToString() + "]";
            }

        }


        #endregion




        #region DurationList contents queries

        /// <summary>
        /// Returns whether this list contains the given item (or an item equal to the current item, according to the set's Comparer).
        /// </summary>
        /// <remarks>This method is an O(log n) operation, where n is the number of items contained in the list.</remarks>
        public bool Contains(TKey item)
        {
            Node prior, next;
            return GetNode(item, out prior, out next) != null;
        }


        /// <summary>
        /// Returns the number of distinct item values held in this skip set.  The items themselves may be identical, but if they occur with 
        /// other items (or blank spaces) interposed in between, they are counted separately.
        /// </summary>
        public int Count { get; private set; } = 0;
        
              
        /// <summary>
        /// Returns the node containing the given key as withi it, if it exists.  If no such node exists, returns null.  If the key falls on 
        /// the boundary start or end of a node, the node will be returned in this order of preference:  1) nodes ending at the key; 2) 
        /// nodes whose range is only the key; and 3) nodes that begin at the key.
        /// <para/>This method is an O(log n) operation.
        /// </summary>
        private Node GetNode(TKey key, out Node prior, out Node next)
        {
            prior = null;
            next = null;
            if (Count == 0) return null;
            if ( Comparer.Compare(key, Head.Start) < 0)
            {
                //Prior to the head.
                next = Head;
                return null;
            }
            if (Comparer.Compare(key, Tail.End) > 0)
            {
                //After the tail.
                prior = Tail;
                return null;
            }

            //First, find which node's start immediately precedes the item.
            Node focus = Head;
            for (int i = Head.Height - 1; i >= 0; i--)
            {
                //prior = focus.Backward[0];
                next = focus.Forward[i];
                while (next != null && Comparer.Compare(key, next.Start) <= 0)
                {                    
                    focus = next;
                    next = focus.Forward[i];
                }                
            }
            

            //Now, if the focus doesn't contain the item, advance the prior and set focus to null.  Otherwise, just set the prior to the 
            //focus's previous.
            if (Comparer.Compare(key, focus.End) > 0)
            {
                prior = focus;
                focus = null;
            }
            else
                prior = focus.Backward[0];


            //finally, return the focus.
            return focus;
        }

        
        private bool IsSingleton(Node node) { return Comparer.Compare(node.Start, node.End) == 0; }

        /// <summary>
        /// Returns the minimum value in this list.
        /// </summary>
        /// <remarks>This is an O(1) operation, which makes it distinct from the standard binary search tree.</remarks>
        public TKey Min { get { return Head.Start; } }
        /// <summary>
        /// Returns the maximum value in this list.
        /// </summary>
        /// <remarks>This is an O(1) operation, which makes it distinct from the standard binary search tree.</remarks>
        public TKey Max { get { return Tail.Start; } }
        

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return "Count = " + Count;
        }

        //public string HeightProfile()
        //{
        //    int[] counts = new int[Head.Height+5];
        //    Node current = Head;
        //    while (current != null) counts[current++.Height-1]++;
        //    string str = "";
        //    for (int i = 0; i < counts.Length; i++) str += i + " : " + counts[i] + "\r\n";
        //    return str;

        //}

        #endregion
            

    }
}
