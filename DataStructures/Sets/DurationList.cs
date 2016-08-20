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
    
    public sealed class DurationList <TKey, TValue> : IDictionary<TKey, TValue>
    {
        //TODO:  Validate DurationList
        internal const int DEFAULT_MAXHEIGHT = 4;
        internal const double DEFAULT_ADJACENCY_FRACTION = 0.5;
        public IComparer<TKey> Comparer { get; private set; }
        private double _AdjacencyFraction = DEFAULT_ADJACENCY_FRACTION;
        public double AdjacencyFraction
        {
            get
            {
                return _AdjacencyFraction;
            }
            private set
            {
                if (value < 0.0 || value > 1.0)
                    throw new ArgumentException("SkipList.AdjacencyFraction must be a positive real number (0.0, 1.0].");
                _AdjacencyFraction = value;
                _Denom = Math.Round(1 / value, 0);
                if (_Denom != (double)((int)_Denom))
                    throw new InvalidOperationException("Invalid adjacency value:  1 / " + _Denom + ".");
            }
        }
        private double _Denom = 1 / DEFAULT_ADJACENCY_FRACTION;
        private Node _Head = null;
        private Node _Tail = null;

      
        /// <summary>
        /// A lightweight data structure that holds the included data as well as a set of links to the previous and next nodes.
        /// </summary>
        private class Node
        {
            /// <summary>
            /// The start of the inclusive range for this value.
            /// </summary>
            public TKey Start;
            /// <summary>
            /// The end of the inclusive range for this value.
            /// </summary>
            public TKey End;
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

            private static Random _Rng = new Random((int)DateTime.Now.Ticks);
            //private static Random _Rng = new Random(0);

            /// <summary>
            /// Creates a new node with the given height and data.
            /// </summary>
            protected Node(int height, TKey start, TKey end, TValue item)
            {
                Start = start;
                End = end;
                Forward = new Node[height];
                Backward = new Node[height];
                Value = item;
            }

            /// <summary>
            /// A factory-type constructor which returns a new node with the given height and data.
            /// </summary>
            public static Node FromKnownHeight(int height, TKey start, TKey end, TValue item)
            {
                return new Node(height, start, end, item);
            }
            /// <summary>
            /// A factory-type construction which returns a new node with a random height, equal to or lesser than the given height.
            /// </summary>
            /// <param name="maxHeight">The maxHeight should be log_2(n) where n is the number of items in the Skip List and the 
            /// adjacency fraction is 1/2.</param>
            /// <param name="item">The data represented in this node.</param>
            public static Node FromRandomHeight(int maxHeight, TKey start, TKey end, TValue item, double adjacencyFraction)
            {
                //Find a random height.  The probability of Height=1 is 1/2, for Height=2 is 1/4, for Height=3 is 1/8, etc.
                int height = 1;
                while (_Rng.NextDouble() < adjacencyFraction && ++height < maxHeight) ;
                return new Node(height, start, end, item);
            }
            /// <summary>
            /// Updates all forward links of this node, starting with the link at Forward[0], which will be set to the given node.
            /// </summary>       
            /// <param name="next">The next node to start with to update the links.</param>
            /// <param name="startLevel">If 0 is not the first level to update, will find the node that should be updated at that level.  Links at 
            /// lower level will not be updated.</param>
            public void UpdateForwardLinks(Node next, int startLevel = 0)
            {                
                for (int i = startLevel; i < Forward.Length; i++)
                {
                    while (next != null && i >= next.Forward.Length) next = next.Forward[i - 1];
                    this.Forward[i] = next;
                    if (next!=null) next.Backward[i] = this;
                }
            }
            /// <summary>
            /// Updates all backward links of this node, starting with the link at Backward[0], which will be set to the given node.
            /// </summary>
            /// <param name="prior">The prior node to start with to update the links.</param>
            /// <param name="startLevel">If 0 is not the first level to update, will find the node that should be updated at that level.  Links 
            /// at lower level will not be updated.</param>
            public void UpdateBackwardLinks(Node prior, int startLevel = 0)
            {
                for (int i = startLevel; i < Backward.Length; i++)
                {
                    while (prior != null && i >= prior.Backward.Length) prior = prior.Backward[i - 1];
                    this.Backward[i] = prior;
                    if (prior!=null) prior.Forward[i] = this;
                }
            }
            public void IncreaseHeight(int newHeight)
            {
                Node[] oldForward = Forward, oldBackward = Backward;
                Forward = new Node[newHeight];
                Backward = new Node[newHeight];
                oldForward.CopyTo(Forward, 0);
                oldBackward.CopyTo(Backward, 0);
                UpdateForwardLinks(oldForward[0], oldForward.Length);
                UpdateBackwardLinks(oldBackward[0], oldBackward.Length);
            }


            /// <summary>
            /// Returns the next node in the chain.
            /// </summary>
            public static Node operator ++(Node n)
            {
                return n.Forward[0];
            }
            /// <summary>
            /// Returns the prior node in the chain.
            /// </summary>
            public static Node operator --(Node n)
            {
                return n.Backward[0];
            }

            public override string ToString()
            {
                return Start.ToString();
            }

        }



        #region DurationList constructors

        public DurationList()
        {
            if (typeof(IComparable<TKey>).IsAssignableFrom(typeof(TKey))) Comparer = Comparer<TKey>.Default;
            else if (typeof(IComparable).IsAssignableFrom(typeof(TKey))) Comparer = Comparer<TKey>.Default;
            else
                throw new InvalidOperationException("An IComparer<" + typeof(TKey).Name + "> cannot be created automatically.");
        }
        public DurationList(IComparer<TKey> comparer, double adjacencyFraction = DEFAULT_ADJACENCY_FRACTION)
        {
            AdjacencyFraction = adjacencyFraction;
            Comparer = comparer;
        }
     
        private int GetMaxHeight(int count)
        {
            return Math.Max(Mathematics.Int32.Log(Count + 1, _Denom) + 1, DEFAULT_MAXHEIGHT);
        }

        #endregion


        #region DurationList contents modification

        /// <summary>
        /// Adds an items at the given point.
        /// </summary>
        public bool Add(TKey key, TValue item)
        {
            return Add(key, key, item);
        }
        /// <summary>
        /// Adds an item at the 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Add(TKey start, TKey end, TValue item)
        {
            //Step #1 - is the head null?  If so, add at the head.
            if (_Head== null)
            {
                _Head = Node.FromKnownHeight(DEFAULT_MAXHEIGHT, start,  end, item);
                _Tail = _Head;
                Count = 1;
                return true;
            }

            //Step #2 - handle the special cases of adding before the Head, or after the Tail, or an identical item to what exists.
            Node inserted, next, prior = GetPriorOrEqual(start);
            int maxHeight = GetMaxHeight(Count + 1);
            //Will this insertion be the new head?        
            if (prior == null)
            {
                //Must not only add a new head, but must also replace the old head with a random-height head, or else will lose the 
                //O(log n) characteristics for this list.                
                inserted = Node.FromKnownHeight(maxHeight, start, end, item);                
                Node replacement = Node.FromRandomHeight(maxHeight, _Head.Start, _Head.End, _Head.Value, AdjacencyFraction);
                replacement.UpdateBackwardLinks(inserted);
                replacement.UpdateForwardLinks(_Head.Forward[0]);                
                if (_Head.Forward[0] != null) _Head.Forward[0].UpdateBackwardLinks(replacement);
                if (_Tail == _Head) _Tail = replacement;
                next = replacement;
            }
            //Will this insertion lap an item that is already there and equivalent?
            else if (item.Equals(prior.Value))
            {
                if (Comparer.Compare(end, prior.End) <= 0) return false;  //The insertion is enclosed by equal item
                prior.End = end;
                inserted = prior;
                next = prior.Forward[0];
            }
            //Will this insertion be the new end?
            else if (_Tail == prior)
            {                
                //maxHeight = Math.Max(Mathematics.Int32.Log(++Count, _Denom) + 1, DEFAULT_MAXHEIGHT);
                inserted = Node.FromKnownHeight(maxHeight, start, end, item);
                Node replacement = Node.FromRandomHeight(maxHeight, _Tail.Start, _Tail.End, _Tail.Value, AdjacencyFraction);
                replacement.UpdateForwardLinks(inserted);
                replacement.UpdateBackwardLinks(_Tail.Backward[0]);
                inserted.UpdateBackwardLinks(replacement);
                if (_Tail.Backward[0] != null) _Tail.Backward[0].UpdateForwardLinks(replacement);
                if (Comparer.Compare(replacement.End, start) > 0) replacement.End = start;
                _Tail = inserted;
                if (_Head == _Tail) _Head = inserted;
                Count++;
                return true;        //No need to check if the insertion overlaps the next item, cuz there is no next item.
            }
            //Otherwise, this is but an insertion in the middle of the list.
            else
            {                
                //Otherwise do an insertion.
                inserted = Node.FromRandomHeight(maxHeight, start, end, item, AdjacencyFraction);
                next = prior.Forward[0];               

                //Does the prior bracket the to-be-inserted node on the right?
                if (Comparer.Compare(prior.End, end) > 0)
                {
                    next = Node.FromRandomHeight(maxHeight, end, prior.End, prior.Value, AdjacencyFraction);
                    next.UpdateForwardLinks(prior.Forward[0]);
                    if (prior.Forward[0] != null) prior.Forward[0].UpdateBackwardLinks(next);
                }
                
                //Does the prior bracket the to-be-inserted node on the left?
                if (Comparer.Compare(prior.Start, start) < 0)
                {
                    Node newPrior = Node.FromRandomHeight(maxHeight, prior.Start, start, prior.Value, AdjacencyFraction);
                    newPrior.UpdateBackwardLinks(prior.Backward[0]);
                    if (prior.Backward[0] != null) prior.Backward[0].UpdateForwardLinks(newPrior);
                    prior = newPrior;
                }
            }

            //Step #3 - does the new insertion overlap the following items?   Items that are completely enclosed must be removed.        
            while (next!=null && Comparer.Compare(inserted.End, next.Start) > 0)
            {
                //If the insertion only partially overlaps the next, then just update next's start to insert's end.
                if (Comparer.Compare(inserted.End, next.End) < 0)
                {
                    next.Start = inserted.End;
                    break;
                }
                //Otherwise, the insertion completely overrides the next.  Set next to the next item, and Count--.
                next = next.Forward[0];
                Count--;                
            }


            //Step #4 - finally, update all the links for the inserted item.
            Count++;            
            inserted.UpdateBackwardLinks(prior);
            inserted.UpdateForwardLinks(next);
            if (prior != null) prior.UpdateForwardLinks(inserted);
            else _Head = inserted;
            if (next != null) next.UpdateBackwardLinks(inserted);
            else _Tail = inserted;
            
            //Step #5 - ensure that the current Tail and Head are tall enough, which might not be true if either was removed for 
            //being enclosed in the new insertion (or at least, true for the Tail but impossible for the Head?).
            if (_Head.Forward.Length < maxHeight) _Head.IncreaseHeight(maxHeight);
            if (_Tail.Backward.Length < maxHeight) _Head.IncreaseHeight(maxHeight);

            //Step #5 - last step - return true.
            return true;
        }

        public void Clear()
        {
            _Head = null;
            _Tail = null;
            Count = 0;
            //The whole list will now be garbage collectioned.
        }

        /// <summary>
        /// If there exists a point item at the given key, removes that item.  Otherwise, this method can make no changes.
        /// </summary>
        public bool Remove(TKey point)
        {
            Node focus = GetNode(point);            
            if (focus == null) return false;
            if (Comparer.Compare(focus.Start, point) != 0) return false;
            if (Comparer.Compare(focus.End, point) != 0) return false;
            

            if (Count==1)
            {
                Clear();
                return true;
            }

            int maxHeight = GetMaxHeight(Count--);            
            Node prior = focus.Backward[0], next = focus.Forward[0];
            if (prior== null)
            {
                _Head = next;
                if (_Head.Forward.Length < maxHeight) _Head.IncreaseHeight(maxHeight);
                else _Head.UpdateForwardLinks(_Head.Forward[0], 0);         
            }
            else if (next== null)
            {
                _Tail = prior;
                if (_Tail.Backward.Length < maxHeight) _Tail.IncreaseHeight(maxHeight);
                else _Tail.UpdateBackwardLinks(_Tail.Backward[0], 0);                
            }
            else
            {
                prior.UpdateForwardLinks(next);
                next.UpdateBackwardLinks(prior);
            }
            
            return true;
        }
        public bool Remove(TKey start, TKey end)
        {
            if (Count == 0) return false;

            int maxHeight = GetMaxHeight(Count - 1);
            Node prior = GetPriorOrEqual(start), next;

            //Step #1 - did the removal occur entirely before the list?
            if (Comparer.Compare(_Head.Start, end) >= 0) return false;

            //Step #2 - if prior is null, then the removal starts before the list.
            bool changed = false;
            if (prior == null)
            {
                while (_Head != null && Comparer.Compare(_Head.End, end) <= 0)
                {
                    _Head = _Head.Forward[0];
                    Count--;
                    changed = true;
                }

                if (_Head == null)
                {
                    _Tail = null;
                    Count = 0;
                    changed = true;
                }
                else if (Comparer.Compare(_Head.Start, end) < 0)
                {
                    _Head.Start = end;
                    changed = true;
                }

                if (_Head.Forward.Length < maxHeight) _Head.IncreaseHeight(maxHeight);
                else _Head.UpdateForwardLinks(_Head.Forward[0], 0);

                return changed;
            }

            //Step #3 - otherwise, the removal starts after the beginning of the list.
            if (Comparer.Compare(_Tail.End, start) <= 0) return false;

            next = prior.Forward[0];
            while (next != null && Comparer.Compare(next.End, end) <= 0)
            {
                next = next.Forward[0];
                Count--;
                changed = true;
            }
            if (next == null)
            {
                if (_Tail!= prior)
                {
                    _Tail = prior;
                    if (_Tail.Backward.Length < maxHeight) _Tail.IncreaseHeight(maxHeight);
                    else _Tail.UpdateBackwardLinks(prior, 0);
                    return true;
                }
                return false;
            }
            else if (Comparer.Compare(next.Start, end) < 0)
            {
                next.Start = end;
                changed = true;
            }

            //Step #4 - now that the prior and the next are sorted out, and neither is null, update their links.
            prior.UpdateForwardLinks(next, 0);
            next.UpdateBackwardLinks(prior, 0);

            //Step #5 - finally, return whether the list has changed.
            return changed;            
        }


        #endregion


        #region DurationList contents queries

        /// <summary>
        /// Returns the value item whose range encloses the given key, if it exists.  If it does not, throws a KeyNotFoundException.
        /// </summary>
        public TValue this[TKey key]
        {
            get
            {
                Node node = GetNode(key);
                if (node == null) throw new KeyNotFoundException();
                return node.Value;
            }
            set { Add(key, value); }
        }

        /// <summary>
        /// Returns the number of distinct item-ranges in this list.
        /// </summary>
        public int Count { get; private set; } = 0;

     

        int ICollection<KeyValuePair<TKey, TValue>>.Count { get { return Count; } }

        /// <summary>
        /// Returns the range boundaries, in order.  This method makes no distinction between starting boundaries and ending boundaries.
        /// </summary>        
        public ICollection<TKey> GetBounds()
        {
            List<TKey> result = new List<TKey>(Count);
            if (Count == 0) return result;
            result.Add(_Head.Start);
            TKey prior = _Head.Start;
            if (Comparer.Compare(prior, _Head.End) != 0)
            {
                prior = _Head.End;
                result.Add(prior);
            }
            Node focus = _Head.Forward[0];
            while (focus != null)
            {
                TKey next = focus.Start;
                if (Comparer.Compare(next, prior) != 0)
                {
                    prior = next;
                    result.Add(prior);
                    next = focus.End;
                }
                if (Comparer.Compare(next, prior) != 0)
                {
                    prior = next;
                    result.Add(prior);
                }
                focus = focus.Forward[0];
            }
            return result.AsReadOnly();
        }

        /// <summary>
        /// Returns all the value items from this list,  in order.  Duplicate items may be returned.
        /// </summary>        
        public ICollection<TValue> GetItems()
        {
            List<TValue> result = new List<TValue>(Count);
            Node focus = _Head;
            while (focus != null)
            {
                result.Add(focus.Value);
                focus = focus.Forward[0];
            }
            return result.AsReadOnly();
        }

        /// <summary>
        /// Returns the Node whose data is equal to or immediately after the given item's node end, according to the list's comparer.
        /// </summary>        
        /// <returns>Returns null if there exists no node whose data is greater than or equal to the given item.</returns>
        /// /// <remarks>Depending on how many items must be skipped as the next-or-equal is sought, this method ranged between an O(1) operation 
        /// or an O(N) operation, but the most common case will be an O(log N) operation.</remarks>
        private Node GetNextOrEqual(TKey key)
        {
            if (_Tail == null) return null;
            if (Comparer.Compare(key, _Tail.End) < 0) return null;

            Node focus = _Tail;
            for (int i = _Tail.Height - 1; i >= 0; i--)
            {
                Node next = focus.Backward[i];
                while (next != null && Comparer.Compare(key, next.End) >= 0)
                {
                    focus = next;
                    next = focus.Backward[i];
                }
            }
            return focus;
        }

        /// <summary>
        /// Returns the node whose range contains the given key.  If a key happens to fall on a border shared by two ranges, returns the 
        /// earlier of the two value items.
        /// </summary>
        /// <returns>Returns the containing Node, or if there is no node that contains it, returns null.</returns>
        private Node GetNode(TKey key)
        {
            if (_Tail == null) return null;
            if (Comparer.Compare(key, _Tail.End) > 0) return null;

            Node focus = GetPriorOrEqual(key);
            if (focus == null) return null;
            if (Comparer.Compare(focus.End, key) < 0) return null;

            //Check if there is a point range hiding here too.
            if (Comparer.Compare(focus.Start, key) == 0 && focus.Next != null && Comparer.Compare(focus.Next.End, key) == 0) return focus.Next;            
            
            return focus;
        }

        /// <summary>
        /// Returns the Node whose data is equal or immediately prior to the given item's node start, according to the list's Comparer.
        /// </summary>        
        /// <returns>Returns null if there exists no node whose data is lesser than or equal to the given item.</returns>
        /// <remarks>Depending on how many items must be skipped as the prior-or-equal is sought, this method ranged between an O(1) operation 
        /// or an O(N) operation, but the most common case will be an O(log N) operation.</remarks>
        private Node GetPriorOrEqual(TKey key)
        {
            if (_Head == null) return null;
            if (Comparer.Compare(key, _Head.Start) < 0) return null;

            Node prior = _Head;
            for (int i = _Head.Height - 1; i >= 0; i--)
            {
                Node next = prior.Forward[i];
                while (next != null && Comparer.Compare(key, next.Start) >= 0)
                {
                    prior = next;
                    next = prior.Forward[i];
                }
            }
            return prior;
        }


        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly { get { return false; } }


        ICollection<TKey> IDictionary<TKey, TValue>.Keys { get { return GetBounds(); } }


        ICollection<TValue> IDictionary<TKey, TValue>.Values { get { return GetItems(); } }


        
        /// <summary>
        /// Returns whether the given key is contained on this list within a value item's duration range.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            Node focus = GetPriorOrEqual(key);
            if (focus == null) return false;
            return Comparer.Compare(focus.End, key) >= 0;
        }

      

        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> kvp) { Add(kvp.Key, kvp.Value); }
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) { Add(key, value); }

        bool IDictionary<TKey, TValue>.Remove(TKey key) { return Remove(key); }


        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> kvp)
        {
            Node focus = GetNode(kvp.Key);
            if (focus == null) return false;
            if (!kvp.Value.Equals(_Head.Value)) return false;
            return Remove(kvp.Key);
            
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            Node focus = _Head;
            while (focus!= null)
            {
                yield return new KeyValuePair<TKey, TValue>(focus.Start, focus.Value);
                focus = focus.Forward[0];
            }
            
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
