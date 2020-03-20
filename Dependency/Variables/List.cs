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
    class List<T> : IList<T>, IIndexable,  IContext, IVariable
    {
        System.Collections.Generic.List<Node> _InnerList = new System.Collections.Generic.List<Node>();
        private readonly ReaderWriterLockSlim _ValueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly IConverter<T> _Converter;
        private WeakReference<Source<int>> _SizeRef;

        public int Count => _InnerList.Count;

        bool ICollection<T>.IsReadOnly => false;

        public List(IEnumerable<T> items = null) : this(null, items) { }
        public List(IConverter<T> converter,  IEnumerable<T> items = null)
        {
            this._Converter = converter ?? Dependency.Values.Converter<T>.Default;
            this._InnerList = items.Select(item => new Node(this, item)).ToList();
        }

        public T this[int index]
        {

            get
            {
                Update.StructureLock.EnterReadLock();
                try
                {
                    Node n = _InnerList[index];  // Might throw an exception
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
                    Node n = _InnerList[index];  // Might throw an exception
                    n.Item = value;
                }
                finally { Update.StructureLock.ExitReadLock(); }
            }
        }

        private void NotifySizeChange()
        {
            Update.StructureLock.EnterReadLock();
            try
            {
                if (_SizeRef == null) return;
                else if (!_SizeRef.TryGetTarget(out Source<int> src)) return;
                else src.Set(_InnerList.Count);
            }
            finally { Update.StructureLock.ExitReadLock(); }
        }

        public int IndexOf(T item)
        {
            Update.StructureLock.EnterReadLock();
            try
            {
                for (int i = 0; i < _InnerList.Count; i++) if (_InnerList[i].Equals(item)) return i;
                return -1;
            }
            finally { Update.StructureLock.ExitReadLock(); }
        }

        public void Insert(int index, T item)
        {
            Update.StructureLock.EnterWriteLock();
            try
            {
                _InnerList.Insert(index, new Node(this, item));
            }
            finally { Update.StructureLock.ExitWriteLock(); }
            if (index == _InnerList.Count-1)
                Update.ForCollection(this, new Number(index)).Execute();
            else
                Update.ForCollection(this, new Values.Range(new Number(index), new Number(_InnerList.Count - 1))).Execute();
            NotifySizeChange();
        }

        public void RemoveAt(int index)
        {
            Update.StructureLock.EnterWriteLock();
            try
            {
                _InnerList.RemoveAt(index);
            }
            finally { Update.StructureLock.ExitWriteLock(); }
            if (index == _InnerList.Count)
                Update.ForCollection(this, new Number(index)).Execute();
            else
                Update.ForCollection(this, new Values.Range(new Number(index), new Number(_InnerList.Count))).Execute();
            NotifySizeChange();
        }

        public void Add(T item)
        {
            Update.StructureLock.EnterWriteLock();
            try
            {
                _InnerList.Add(new Node(this, item));
            }
            finally { Update.StructureLock.ExitWriteLock(); }
            Update.ForCollection(this, new Number(_InnerList.Count-1)).Execute();
            NotifySizeChange();
        }

        void ICollection<T>.Clear()
        {
            int lastIdx = _InnerList.Count - 1;
            if (_InnerList.Count == 0) return;
            Update.StructureLock.EnterWriteLock();
            try
            {
                _InnerList.Clear();
            }
            finally { Update.StructureLock.ExitWriteLock(); }

            Update.ForCollection(this, new Values.Range(Number.Zero, new Number(lastIdx))).Execute();
            NotifySizeChange();
        }

        public bool Contains(T item) => IndexOf(item) != -1;

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            Update.StructureLock.EnterReadLock();
            try
            {
                for (int i = 0; i < _InnerList.Count && arrayIndex < array.Length; i++)
                    array[arrayIndex++] = _InnerList[i].Item;
            }
            finally { Update.StructureLock.ExitReadLock(); }
        }

        public bool Remove(T item)
        {
            Update.StructureLock.EnterUpgradeableReadLock();
            try
            {
                for (int i = 0; i < _InnerList.Count; i++)
                {
                    if (!_InnerList[i].Item.Equals(item)) continue;
                    Update.StructureLock.EnterWriteLock();
                    try
                    {
                        _InnerList.RemoveAt(1);
                    }finally { Update.StructureLock.ExitWriteLock(); }
                    Update.ForVariable(this).Execute();
                }
                NotifySizeChange();
                return false;
            } finally { Update.StructureLock.ExitUpgradeableReadLock(); }
            
        }

        IEvaluateable IEvaluateable.Value 
            => throw new InvalidOperationException("A " + nameof(List<T>) + " has no " + nameof(IEvaluateable) + " value.");

        IEvaluateable IVariable.Contents 
            => throw new InvalidOperationException("A " + nameof(List<T>) + " has no " + nameof(IEvaluateable) + " contents.");

        public IEnumerator<T> GetEnumerator() => _InnerList.Select(n => n.Item).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
        {
            if (ordinal is Number no && no.IsInteger)
            {
                int index = (int)no;
                Update.StructureLock.EnterReadLock();
                try
                {
                    if (index >= 0 && index < _InnerList.Count)
                    {
                        val = _InnerList[index];
                        return true;
                    }
                    
                } finally { Update.StructureLock.ExitReadLock(); }
            }
            val = null;
            return false;
        }

        bool IContext.TryGetProperty(string path, out IEvaluateable source)
        {
            switch (path.ToLower())
            {
                case "size":
                case "count":
                case "length":
                    Update.StructureLock.EnterWriteLock();
                    try
                    {
                        if (_SizeRef == null)
                            _SizeRef = new WeakReference<Source<int>>(new Source<int>(_InnerList.Count));
                        if (!_SizeRef.TryGetTarget(out Source<int> src))
                            _SizeRef.SetTarget(src = new Source<int>(_InnerList.Count));
                        source = src;
                        return true;
                    } finally { Update.StructureLock.ExitWriteLock(); }                    
                default:
                    source = null;
                    return false;
            }
        }

        bool IContext.TryGetSubcontext(string path, out IContext ctxt) { ctxt = null; return false; }


        /// <summary>Like a source with special handling of updates.</summary>
        private class Node : IAsyncUpdater, IVariable, IUpdatedVariable
        {
            public readonly List<T> List;
            
            private T _Item;
            public T Item
            {
                get => _Item;
                set
                {
                    _Item = value;
                    if (Listeners.Any())
                    {
                        IEvaluateable iev = List._Converter.ConvertFrom(value);
                        Update.ForVariable(this, iev).Execute();
                    } else
                    {
                        _Contents = null;
                    }
                    
                }
            }

            public Node(List<T> host, T item) { this.List = host; this._Item = item; }

            
            public void SetContents(IEvaluateable overrideContents)
            {
                Update.StructureLock.EnterUpgradeableReadLock();
                try
                {
                    if (!Update.ForVariable(this, overrideContents).Execute()) return;
                    if (List._Converter.TryConvert(Value, out T result))
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



            bool IAsyncUpdater.AddListener(ISyncUpdater idi) => Listeners.Add(idi);
            bool IAsyncUpdater.RemoveListener(ISyncUpdater idi) => Listeners.Remove(idi);
            IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => Listeners;
            public readonly Update.ListenerManager Listeners = new Update.ListenerManager();

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
