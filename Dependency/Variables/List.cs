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
    public class List<T> : IList<T>, IIndexable,  IContext, IVariable
    {
        System.Collections.Generic.List<RandomAccessIndex> _List = new System.Collections.Generic.List<RandomAccessIndex>();
        private readonly ReaderWriterLockSlim _ValueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        internal IConverter<T> Converter;
        private WeakReference<Source<int>> _SizeRef;
        private readonly Stack<RandomAccessIndex> _OutOfRange = new Stack<RandomAccessIndex>();

        public int Count => _List.Count;

        bool ICollection<T>.IsReadOnly => false;

        
        public T this[int index]
        {

            get
            {
                Update.StructureLock.EnterReadLock();
                try
                {
                    RandomAccessIndex n = _List[index];  // Might throw an exception
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
                    RandomAccessIndex n = _List[index];  // Might throw an exception
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
                else src.Set(_List.Count);
            }
            finally { Update.StructureLock.ExitReadLock(); }
        }

        public int IndexOf(T item)
        {
            Update.StructureLock.EnterReadLock();
            try
            {
                for (int i = 0; i < _List.Count; i++) if (_List[i].Item.Equals(item)) return i;
                return -1;
            }
            finally { Update.StructureLock.ExitReadLock(); }
        }

        public void Insert(int index, T item)
        {
            Update.StructureLock.EnterWriteLock();
            try
            {
                RandomAccessIndex rai;
                if (_OutOfRange.Count > 0 && _OutOfRange.Peek().Index == _List.Count)
                    rai = _OutOfRange.Pop();
                else
                    rai = new RandomAccessIndex(this, _List.Count, item);
                _List.Add(rai);
                for (int i = _List.Count - 1; i > index; i--)
                    _List[i].CopyTo(_List[i - 1]);
                _List[index].Item = item;
            }
            finally { Update.StructureLock.ExitWriteLock(); }
            if (index == _List.Count-1)
                Update.ForVariable(this, new Number(index)).Execute();
            else
                Update.ForVariable(this, new Values.Range(new Number(index), new Number(_List.Count - 1))).Execute();
            NotifySizeChange();
        }

        public void RemoveAt(int index)
        {
            Update.StructureLock.EnterWriteLock();
            try
            {
                _List.RemoveAt(index);
            }
            finally { Update.StructureLock.ExitWriteLock(); }
            if (index == _List.Count)
                Update.ForVariable(this, new Number(index)).Execute();
            else
                Update.ForVariable(this, new Values.Range(new Number(index), new Number(_List.Count))).Execute();
            NotifySizeChange();
        }

        public void Add(T item)
        {
            Update.StructureLock.EnterWriteLock();
            try
            {
                _List.Add(new RandomAccessIndex(this, _List.Count, item));
            }
            finally { Update.StructureLock.ExitWriteLock(); }
            Update.ForVariable(this, new Number(_List.Count-1)).Execute();
            NotifySizeChange();
        }

        public void Clear()
        {
            int lastIdx = _List.Count - 1;
            if (_List.Count == 0) return;
            
            Update.StructureLock.EnterWriteLock();
            try
            {
                for (int i = _List.Count-1; i >=0; i--)
                {
                    RandomAccessIndex rai = _List[i];
                    _OutOfRange.Push(rai);
                    IndexingError error = new IndexingError(this, new IEvaluateable[] { new Number(rai.Index) }, "No index " + rai.Index + " exists.");
                    rai.Contents = error;
                }
                
            }
            finally { Update.StructureLock.ExitWriteLock(); }

            Update.ForVariable(this, new Values.Range(Number.Zero, new Number(lastIdx))).Execute();
            NotifySizeChange();
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
                NotifySizeChange();
                return false;
            } finally { Update.StructureLock.ExitUpgradeableReadLock(); }
            
        }

        IEvaluateable IEvaluateable.Value 
            => throw new InvalidOperationException("A " + nameof(List<T>) + " has no " + nameof(IEvaluateable) + " value.");

        IEvaluateable IVariable.Contents 
            => throw new InvalidOperationException("A " + nameof(List<T>) + " has no " + nameof(IEvaluateable) + " contents.");

        public IEnumerator<T> GetEnumerator() => _List.Select(n => n.Item).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
        {
            if (ordinal is Number no && no.IsInteger)
            {
                int index = (int)no;
                Update.StructureLock.EnterReadLock();
                try
                {
                    if (index >= 0 && index < _List.Count)
                    {
                        val = _List[index];
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
                            _SizeRef = new WeakReference<Source<int>>(new Source<int>(_List.Count));
                        if (!_SizeRef.TryGetTarget(out Source<int> src))
                            _SizeRef.SetTarget(src = new Source<int>(_List.Count));
                        source = src;
                        return true;
                    } finally { Update.StructureLock.ExitWriteLock(); }                    
                default:
                    source = null;
                    return false;
            }
        }

        bool IContext.TryGetSubcontext(string path, out IContext ctxt) { ctxt = null; return false; }

        private class RandomAccessIndex : Variable
        {
            private readonly List<T> _List;
            public readonly int Index;
            public bool Override;
            private T _Item;
            public T Item
            {
                get => _Item;
                set
                {
                    this.Contents = (_List.Converter.ConvertFrom(value));
                    _Item = value;
                }
            }

            public override IEvaluateable Contents
            {
                get => base.Contents;

                // If the value is being set to null, that means the override is being released.
                set
                {
                    if (value != null)
                    {
                        base.Contents = value;
                        Override = true;
                    }   
                    else if (_List.Converter.TryConvert(Value, out T item))
                    {
                        _Item = item;
                        base.Contents = Value;
                        Override = false;
                    }
                    else
                    {
                        ConversionError error = new ConversionError(Value, typeof(T));
                        base.Contents = error;
                        Override = true;
                    }
                    
                }
            }

            public RandomAccessIndex(List<T> list, int index, T item = default(T))
            {
                this._List = list;
                this.Index = index;
                this._Item = item;
            }

            internal void CopyTo(RandomAccessIndex other)
            {
                other._Item = _Item;
                other.Contents = Contents;
            }

        }
       
    }
}
