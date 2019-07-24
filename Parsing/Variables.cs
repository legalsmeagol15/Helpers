using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using System.Runtime.CompilerServices;
using System.Collections;
using DataStructures;

namespace Dependency
{
    public enum Mobility
    {
        // TODO:  more info will probably be needed
        None = 0,
        Column = 1,
        Row = 2,
        All = ~0
    }

    
    
    public sealed class Variable : IEvaluateable
    {
        
        public readonly IContext Context;

        public readonly string Name;

        
        private ISet<Variable> _Terms;

        private IEvaluateable _Value;
        public IEvaluateable Value
        {
            get => _Value;
            private set
            {
                IEvaluateable oldValue = Value;
                Value = value;
                if (oldValue != Value)
                {
                    NotifyListeners();
                    ValueChanged?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, Value));
                }
            }
        }
        private IEvaluateable _Contents;
        public IEvaluateable Contents
        {
            get => _Contents;
            set
            {
                if (_Terms == null)
                    _Terms = Helpers.GetTerms(value);
                else
                {
                    ISet<Variable> newTerms = Helpers.GetTerms(value);
                    //foreach (Variable oldTerm in _Terms.Except(newTerms))
                    //    RemoveListener(this);
                    //foreach (Variable newTerm in newTerms.Except(_Terms))
                    //    AddListener(this);
                    this._Terms = newTerms;
                }
                _Contents = value;
                Value = value.UpdateValue();
            }
        }

        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IContext context, string name, IEvaluateable contents = null) {
            this.Context = context;
            this.Name = name;
            this.Contents = contents ?? Dependency.Null.Instance;
        }
        
        void NotifyListeners()
        {
            Task.Run(() => NotifyListenersAsync());
        }
        private void NotifyListenersAsync()
        {
            // TODO:  notify the dependency listeners.
        }

        public event ValueChangedHandler<IEvaluateable> ValueChanged;


        internal string GetExpressionString(IContext perspective)
        {
            throw new NotImplementedException();
        }

        public IEvaluateable UpdateValue() => Value = _Contents.UpdateValue();

        public IEnumerable<Variable> GetTerms()
        {
            throw new NotImplementedException();
        }

        private readonly WeakReferenceSet<Variable> Listeners = new WeakReferenceSet<Variable>();
        
        
        /// <summary>
        /// This is a weird collection where it has to maintain and compact itself.  I don't expect to use it 
        /// elsewhere or I would add it to the <seealso cref="DataStructures"/> namespace, but it conceivably could be 
        /// used elsewhere.  That's why it is written to be generic over <typeparamref name="T"/>.
        /// <para/>The set's add or remove functions are O(n) operations in the worst case, where the set must be 
        /// compacted for dead <seealso cref="WeakReference{T}"/> objects.
        /// </summary>
        internal class WeakReferenceSet<T> : IEnumerable<T> where T : class
        {
            private int _TabledBits;
            private int _ModMask;
            private DynamicLinkedList<WeakReference<T>>[] _Table;
            public int Count { get; private set; }
            public WeakReferenceSet(IEnumerable<T> items)
            {
                _TabledBits = Mathematics.Int32.Log_2(items.Count());
                _Table = CopyTable(_TabledBits, items.Select(item => new WeakReference<T>(item)), out _ModMask, out int count);
                this.Count = count;
            }
            public WeakReferenceSet(int capacity = 1)
            {
                if (capacity < 0) throw new ArgumentException("Capacity cannot be negative.");
                _TabledBits = Mathematics.Int32.Log_2(capacity);
                _ModMask = GetModMask(_TabledBits);                
                _Table = new DynamicLinkedList<WeakReference<T>>[2];
                this.Count = 0;
            }

            /// <summary>Adds a weak reference to the given item.  Returns true if the item was successfully added, or 
            /// false if it already existed on the set.</summary>
            public bool Add(T item)
            {
                int idx;
                DynamicLinkedList<WeakReference<T>> list = null;

                // Should the table be copied to something larger?
                bool copyTable = false;
                if (Count >= (_Table.Length >> 1))
                    copyTable = true;
                else
                {
                    idx = item.GetHashCode() & _ModMask;
                    list = _Table[idx];
                    _Table[idx] = (list = new DynamicLinkedList<WeakReference<T>>());
                    if (list != null && list.Count >= (_Table.Length >> 2)) copyTable = true;
                }

                // If copy is appropriate, do so.
                if (copyTable)
                {
                    _Table = CopyTable(++_TabledBits, this.GetReferences(), out int modMask, out int count);
                    this._ModMask = modMask;
                    this.Count = count;
                    idx = item.GetHashCode() & _ModMask;
                    list = _Table[idx];
                }

                // Walk through the nodes at this list and look for expirations, and also matches to the given object.
                foreach (var node in list.GetNodes())
                {
                    var wr = node.Contents;
                    if (!wr.TryGetTarget(out T existing)) { node.Remove(); this.Count--; continue; }
                    else if (existing.Equals(item)) return false;
                }

                // Add the item to the given list.
                list.AddLast(new WeakReference<T>(item));
                this.Count++;
                return true;
            }

            /// <summary>Clears the set of all weak references.</summary>
            public void Clear()
            {
                _TabledBits = 1;
                _ModMask = GetModMask(_TabledBits);
                Count = 0;
                _Table = new DynamicLinkedList<WeakReference<T>>[2];
            }

            /// <summary>Forces the set to prune itself of all dead weak references.</summary>
            public void Compact()
            {
                foreach (var list in _Table)
                {
                    foreach (var node in list.GetNodes())
                    {
                        if (!node.Contents.TryGetTarget(out _)) { node.Remove(); this.Count--; }
                    }
                }
            }

            /// <summary>Returns whether the given item exists on this set.</summary>
            public bool Contains(T item)
            {
                int idx = item.GetHashCode() & _ModMask;
                var list = _Table[idx];
                if (list == null) return false;
                foreach (var node in list.GetNodes())
                {
                    var wr = node.Contents;
                    if (!wr.TryGetTarget(out T existing)) { node.Remove(); this.Count--; return true; }
                    else if (existing.Equals(item)) return true;
                }
                return false;
            }

            /// <summary>Ensures the given item does not exist on this set.  Returns true if the item was removed, 
            /// false if the item did not exist on the set to begin with.</summary>
            public bool Remove(T item)
            {
                int idx = item.GetHashCode() & _ModMask;
                var list = _Table[idx];
                if (list == null) return false;
                foreach (var node in list.GetNodes())
                {
                    var wr = node.Contents;
                    if (!wr.TryGetTarget(out T existing) || existing.Equals(item)) { node.Remove(); this.Count--; return true; }
                }
                return false;
            }


            /// <summary>Returns the <seealso cref="WeakReference{T}"/> objects used in this set.</summary>
            public IEnumerable<WeakReference<T>> GetReferences()
            {
                foreach (var list in _Table)
                {
                    foreach (var node in list.GetNodes())
                    {
                        if (!node.Contents.TryGetTarget(out _)) { node.Remove(); this.Count--; continue; }
                        else yield return node.Contents;
                    }
                }
            }


            private static int GetModMask(int bits)
            {
                int m = 0;
                for (int i = 0; i < bits; i++) m |= (1 << i);
                return m;
            }
            private DynamicLinkedList<WeakReference<T>>[] CopyTable(int bits, IEnumerable<WeakReference<T>> items, out int modMask, out int count)
            {
                // Does NOT compact the source list, because the reason to copy the table is to throw the old table away.
                modMask = GetModMask(bits);
                DynamicLinkedList<WeakReference<T>>[] result = new DynamicLinkedList<WeakReference<T>>[1 << bits];
                count = 0;
                foreach (WeakReference<T> wr in items)
                {
                    if (!wr.TryGetTarget(out T existing))
                        continue;
                    int index = existing.GetHashCode() & modMask;
                    DynamicLinkedList<WeakReference<T>> list = result[index];
                    if (list == null)
                        result[index] = (list = new DynamicLinkedList<WeakReference<T>>());
                    list.AddLast(wr);
                    count++;
                }
                return result;
            }

            public IEnumerator<T> GetEnumerator()
            {
                for (int i = 0; i < _Table.Length; i++)
                {
                    var list = _Table[i];
                    if (list == null) continue;
                    foreach (var node in list.GetNodes())
                    {
                        if (!node.Contents.TryGetTarget(out T existing)) { node.Remove(); this.Count--; continue; }
                        else yield return existing;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }



}
