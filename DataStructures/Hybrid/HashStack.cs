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
    /// A HashStack is a custom data structure that behaves like a stack, but only allows one instance of a given item to appear on the 
    /// stack.  On instantiation, the duplicate-handling strategy for the stack can be specified.  When pushing a new item on the stack, 
    /// the strategy of RemoveOld strategy will pull any duplicate copy already existing and place the item in the last-in position; with 
    /// the IgnoreNew strategy, and attempt to push a duplicate item onto the stack will fail.
    /// </summary>
    /// <typeparam name="T">The type of object held in this stack.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [DefaultMember("Item")]
    public class HashStack<T> : IEnumerable<T>, IEnumerable, ICollection, IReadOnlyCollection<T>
    {
        //TODO:  Validate all members of HashStack
        private List<Node> _List = new List<Node>();
        private Dictionary<T, int> _Indices = new Dictionary<T, int>();
        private int _Index = 0;

        public enum Strategy { RemoveOld, IgnoreNew };
        public Strategy DuplicateStrategy { get; set; }
        private const Strategy DEFAULT_STRATEGY = Strategy.RemoveOld;

        /// <summary>
        /// Creates a new stack.
        /// </summary>
        /// <param name="strategy">The duplicate-handling strategy this stack will use.</param>
        public HashStack(Strategy strategy = DEFAULT_STRATEGY)
        {
        }
        /// <summary>
        /// Creates a new stack.
        /// </summary>
        /// <param name="items">The items to add to the stack upon instantiation (in the order they will be added, with the last out being 
        /// the last item in the IEnumerable).</param>
        /// <param name="strategy">The duplicate-handling strategy this stack will use.</param>
        public HashStack(IEnumerable<T> items, Strategy strategy = DEFAULT_STRATEGY)
        {
            foreach (T item in items) Push(item);
        }



        #region HashStack contents modification

        /// <summary>Inserts an object at the top of the <see cref="HashStack{T}"/>.</summary>
        /// <returns>Returns true if the item was successfully placed on the top of the stack; otherwise, returns false.  Push can fail 
        /// if the duplicate-handling strategy is IgnoreNew and the stack already contains an identical object (as determined via the 
        /// Equals() method).</returns>
        /// <remarks>This method is an O(1) operation.</remarks>
        public bool Push(T item)
        {
            //Is the new item a duplicate?
            if (_Indices.ContainsKey(item))
            {
                if (DuplicateStrategy == Strategy.IgnoreNew)
                    return false;
                else
                    _List[_Indices[item]] = new Node();
            }

            //Add the new item's node at the current index.
            _Indices[item] = _Index;
            if (_Index >= _List.Count) _List.Add(new Node(item));
            else _List[_Index] = new Node(item);

            //Advance the index and return successfully.
            _Index++;
            return true;
        }
        /// <summary>Removes and returns the object at the top of the <see cref="HashStack{T}"/>.</summary>  
        /// <throws>Throws an InvalidOperationException if the stack is empty.</throws>
        /// <remarks>This method is an O(1) operation.</remarks>
        public T Pop()
        {
            while (--_Index >= 0 && _List[_Index].IsIncluded == false) ;
            if (_Index < 0)
            {
                _Index = 0;
                throw new InvalidOperationException("The HashStack is empty.");
            }
            T result = _List[_Index].Data;
            _List[_Index] = new Node();
            while (_Index > 0 && _List[_Index - 1].IsIncluded == false) _Index--;
            _Indices.Remove(result);
            return result;
        }

        /// <summary>
        /// Removes all items from this stack.
        /// </summary>
        /// <remarks>This method is an O(1) operation.</remarks>
        public void Clear()
        {
            _List.Clear();
            _Indices.Clear();
            _Index = 0;
        }

        #endregion



        #region HashStack contents queries

        /// <summary>
        /// Returns whether the given item is contains on this stack.
        /// </summary>
        /// <remarks>This method is an O(1) operation.</remarks>
        public bool Contains(T item) { return _Indices.ContainsKey(item); }

        /// <summary>
        /// Returns the count of items on this stack.
        /// </summary>
        public int Count { get { return _Indices.Count; } }


        object ICollection.SyncRoot { get { return this; } }
        bool ICollection.IsSynchronized { get { return false; } }

        /// <summary>Returns the object at the top of the <see cref="HashStack{T}"/> without removing it.</summary>        
        /// <throws>Throws an InvalidOperationException if the stack is empty.</throws>
        /// <remarks>This method is an O(1) operation.</remarks>
        public T Peek()
        {
            int i = _Index;
            while (--i >= 0 && _List[i].IsIncluded == false) ;
            if (i < 0) throw new InvalidOperationException("The HashStack is empty.");
            return _List[i].Data;
        }

        /// <summary>
        /// Returns an enumerator that walks over the stack, from top (the last item in) to the bottom.
        /// </summary>        
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = _List.Count - 1; i >= 0; i--)
            {
                if (_List[i].IsIncluded) yield return _List[i].Data;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        #endregion



        /// <summary>
        /// A lightweight data structure that associates data with whether it is still contained.
        /// </summary>
        private struct Node
        {
            public readonly T Data;
            public bool IsIncluded { get; set; }
            public Node(T data) { Data = data; IsIncluded = true; }
        }


        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }



    }
}
