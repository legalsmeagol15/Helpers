using Dependency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using DataStructures;
using System.Threading;

namespace Dependency.Variables
{
    class List<T> : IList<T>, IIndexable,  IContext, IAsyncUpdater, IVariable, ISyncUpdater
    {
        System.Collections.Generic.List<Node> _List = new System.Collections.Generic.List<Node>();
        private readonly ReaderWriterLockSlim _ValueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        internal IConverter<T> Converter;

        public int Count => _List.Count;

        bool ICollection<T>.IsReadOnly => false;

        public T this[int index]
        {

            get
            {
                Update.StructureLock.EnterReadLock();
                try
                {
                    Node n = _List[index];  // Might throw an exception
                    return n.Item;
                }
                finally { Update.StructureLock.ExitReadLock(); }
            }
            set
            {
                // Only need to use the readlock because we're not actually changing the dependency structure.
                Update.StructureLock.EnterReadLock();
                try
                {
                    Node n = _List[index];  // Might throw an exception
                    n.Item = value;
                }
                finally { Update.StructureLock.ExitReadLock(); }
            }
        }



        public int IndexOf(T item)
        {
            Update.StructureLock.EnterReadLock();
            try
            {
                for (int i = 0; i < _List.Count; i++) if (_List[i].Equals(item)) return i;
                return -1;
            }finally { Update.StructureLock.ExitReadLock(); }
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(T item)
        {
            Update.StructureLock.EnterWriteLock();
            try
            {
                _List.Add(new Node() { Item = item });
            }
            finally { Update.StructureLock.ExitWriteLock(); }
            Update.ForVariable(this).Execute();
        }

        void ICollection<T>.Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item) => IndexOf(item) != -1;

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            Update.StructureLock.EnterReadLock();
            try
            {
                for (int i = 0; i < _List.Count && arrayIndex < array.Length; i++)
                    array[arrayIndex++] = _List[i].Item;
            }
            finally { Update.StructureLock.ExitReadLock(); }
        }

        public bool Remove(T item)
        {
            Update.StructureLock.EnterUpgradeableReadLock();
            try
            {
                for (int i = 0; i < _List.Count; i++)
                {
                    if (!_List[i].Item.Equals(item)) continue;
                    Update.StructureLock.EnterWriteLock();
                    try
                    {
                        _List.RemoveAt(1);
                    }finally { Update.StructureLock.ExitWriteLock(); }
                    Update.ForVariable(this).Execute();
                }
                return false;
            } finally { Update.StructureLock.ExitUpgradeableReadLock(); }
        }

        public IEnumerator<T> GetEnumerator() => _List.Select(n => n.Item).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();



        /// <summary>Like a source with special handling of updates.</summary>
        private class Node : ISyncUpdater, IAsyncUpdater, IVariable, IUpdatedVariable
        {
            public readonly List<T> List;
            private readonly WeakReferenceSet<ISyncUpdater> _Listeners = new WeakReferenceSet<ISyncUpdater>();
            private T _Item;
            public T Item
            {
                get => _Item;
                set
                {
                    _Item = value;
                    if (_Listeners.Any())
                    {
                        IEvaluateable iev = List.Converter.ConvertFrom(value);
                        Update.ForVariable(this, iev).Execute();
                    } else
                    {
                        _Contents = null;
                    }
                    
                }
            }

            // Can't have a parent (not even the List, which would then be updated when it 
            // shouldn't be for a mere member update).  But, it can have a child content.
            ISyncUpdater ISyncUpdater.Parent { get => null; set { /*do nothing*/ } }
            public void SetContents(IEvaluateable overrideContents)
            {
                Update.StructureLock.EnterUpgradeableReadLock();
                try
                {
                    if (!Update.ForVariable(this, overrideContents).Execute()) return;
                    if (List.Converter.TryConvert(Value, out T result))
                        _Item = result;

                }
                finally { Update.StructureLock.ExitUpgradeableReadLock(); }
            }


            private IEvaluateable _Contents;
            private IEvaluateable _Value;
            public IEvaluateable Value
            {
                get
                {
                    List._ValueLock.EnterReadLock();
                    try { return _Value; }finally { List._ValueLock.ExitReadLock(); }
                }
                private set
                {
                    SetValue(value);
                }
            }

            IEvaluateable IVariable.Contents => _Contents;

            IEvaluateable IEvaluateable.Value => throw new NotImplementedException();


            // This update would be called if the node's source is updated.  The node cannot 
            // have a parent, but it can enqueue its variable for update.
            bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild)
            {
                foreach (ISyncUpdater listener in this._Listeners) caller.Enqueue(List, listener);
                return false;
            }

            bool IAsyncUpdater.RemoveListener(ISyncUpdater r) => _Listeners.Remove(r);

            bool IAsyncUpdater.AddListener(ISyncUpdater r) => _Listeners.Add(r);

            IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => _Listeners;

            void IUpdatedVariable.SetContents(IEvaluateable newContent) { _Contents = newContent; }

            private bool SetValue(IEvaluateable newValue)
            {
                List._ValueLock.EnterWriteLock();
                try
                {
                    if (newValue.Equals(_Value)) return false;
                    _Value = newValue;
                    return true;
                }
                finally { List._ValueLock.ExitWriteLock(); }
                

            }
            bool IUpdatedVariable.SetValue(IEvaluateable newValue) => SetValue(newValue);
        }
    }
}
