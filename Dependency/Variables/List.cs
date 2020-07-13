using DataStructures;
using Dependency.Functions;
using Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    public sealed class List<T> : IVariable<IList<T>>, ISyncUpdater, IAsyncUpdater, INotifyUpdates<IEvaluateable>, IIndexedDynamic, IUpdatedVariable
    {
        private IEvaluateable _Contents;
        private Dictionary<int, WeakReference< Variable<T>>> _Members;
        private readonly IConverter<T> _MemberConverter;

        public List(IConverter<T> converter = null, params IEvaluateable[] contents)
        {
            this._MemberConverter = converter ?? Dependency.Values.Converter<T>.Default;
            this.Contents = new Vector(contents);
        }

        public IEvaluateable Contents
        {
            get => _Contents;
            set
            {
                Update.ForVariable(this, value).Execute();
                if (value is Vector new_vec)
                {
                    foreach (var kvp in _Members)
                    {
                        if (!kvp.Value.TryGetTarget(out Variable<T> memberVar))
                            continue;
                        int idx = kvp.Key;
                        if (idx >= 0 && idx < new_vec.Count)
                            memberVar.Contents = new_vec[idx];
                        else if (!(memberVar.Contents is IndexingError))
                            memberVar.Contents = new IndexingError(this, null, "Invalid index " + idx.ToString());
                    }
                }
            }
        }
        
        public IEvaluateable Value => _Contents.Value;

        private IList<T> _Native = null;
        public IList<T> Native
        {
            
        }
        IList<T> IVariable<IList<T>>.Native { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        bool IIndexable.ControlsReindex => true;

        void IIndexedDynamic.Reindex(IEnumerable<IEvaluateable> keys)
        {
            throw new NotImplementedException();
        }

        void IIndexedDynamic.Reindex(int start, int end)
        {
            throw new NotImplementedException();
        }

        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
        {
            throw new NotImplementedException();
        }
    }
}
//{
//    public sealed class List<T> : IVariable<IList<T>>, ISyncUpdater, IAsyncUpdater, INotifyUpdates<IEvaluateable>, IIndexable, IUpdatedVariable
//    {

//        bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild)
//        {
//            VariableMode priorMode = VariableMode.NONE;
//            try
//            {
//                Monitor.Enter(_Lock);

//                // If the contents are being updated, don't pass a child's change further up.
//                if (Mode != VariableMode.NONE)
//                    return false;

//                // If the update comes from the contents itself, update with the new value.
//                if (updatedChild.Equals(_Contents))
//                    return CommitValue(_Contents.Value);

//                // If the update comes from a child, force contents to change.
//                priorMode = Mode;
//                IndexedVariable indexedChild = (IndexedVariable)updatedChild;
//                Mode = VariableMode.UPDATING_CHILD_VALUE;
//                if (_Contents is Vector v)
//                {
//                    return v.InternalSet(indexedChild.Index, indexedChild.Value);
//                }
//                else
//                    throw new NotImplementedException("Create a new Vector with the _MemberVars values as contents.");
//            }
//            finally { Mode = priorMode; Monitor.Exit(_Lock); }
//        }
//        private bool CommitValue(IEvaluateable newValue)
//        {
//            try
//            {
//                Monitor.Enter(_Lock);
//                bool changed = false;
//                if (_MemberConverter.TryConvertDown(newValue, out T clr))
//                {
//                    _Cache = null;
//                    changed = true;
//                    IsValid = true;
//                }
//                else
//                    IsValid = false;
//                if (!Value.Equals(newValue)) { Value = newValue; changed = true; }
//                return changed;
//            }
//            finally { Monitor.Exit(_Lock); }
//        }
//        bool IUpdatedVariable.CommitValue(IEvaluateable newValue) => CommitValue(newValue);






//        #region List indexing members

//        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
//        {
//            if (ordinal is Number n)
//            {
//                try
//                {
//                    Monitor.Enter(_Lock);
//                    int idx = (int)n;
//                    if (idx < 0 || idx >= _MemberVars.Count)
//                    {
//                        val = new IndexingError(this, null, "Index " + ordinal.ToString() + " is out of range.");
//                    }
//                    val = _MemberVars[idx];
//                    return true;
//                } finally { Monitor.Exit(_Lock); }
//            }
//            val = new IndexingError(this, null, "Index " + ordinal.ToString() + " is invalid.");
//            return false;
//        }


//        /// <summary>Wraps a <seealso cref="Variable{T}"/> to give it an index.</summary>
//        private class IndexedVariable : IVariable<T>, ISyncUpdater, IComparable<IndexedVariable>
//        {
//            private readonly Variable<T> _Wrapped;
//            public int Index { get; internal set; }
//            public IndexedVariable(List<T> parent, IEvaluateable contents, int index)
//            {
//                this.Parent = parent;
//                this._Wrapped = new Variable<T>(contents, parent._MemberConverter);
//                this.Index = index;
//            }
//            public T Native { get => _Wrapped.Native; set => _Wrapped.Native = value; }

//            public IEvaluateable Contents { get => _Wrapped.Contents; set => _Wrapped.Contents = value; }

//            public IEvaluateable Value => _Wrapped.Value;

//            public ISyncUpdater Parent { get => _Wrapped.Parent; set => _Wrapped.Parent = value; }

//            bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild)
//                => _Wrapped.OnChildUpdated(caller, updatedChild);

//            int IComparable<IndexedVariable>.CompareTo(IndexedVariable other)
//                => Index.CompareTo(other.Index);
//        }

//        #endregion

//        #region List contents control

//        public IVariable<T> Add(T item)
//        {
//            try
//            {
//                Monitor.Enter(_Lock);
//                IndexedVariable v =
//                    new IndexedVariable(this, _MemberConverter.ConvertUp(item), _MemberVars.Count);
//                _MemberVars.Add(v);
//                Update.ForVariable(v, _MemberConverter.ConvertUp(item)).Execute();
//                return v;
//            } finally { Monitor.Exit(_Lock); }
//        }


//        public T RemoveAt(int index)
//        {
//            try
//            {
//                Monitor.Enter(_Lock);
//                IndexedVariable v = _MemberVars[index];
//                _MemberVars.RemoveAt(index);

//                System.Collections.Generic.List<IEvaluateable> ievs =
//                       new System.Collections.Generic.List<IEvaluateable>(vec.Inputs.Take(index));
//                ievs.AddRange(vec.Inputs.Skip(index + 1));
//                Update.ForVariable(this, new Vector(ievs)).Execute();
//                v.Parent = null;
//                return v.Native;
//            } finally { Monitor.Exit(_Lock); }
//        }

//        public IVariable<T> Insert(int index, T item)
//        {
//            try
//            {
//                Monitor.Enter(_Lock);
//                IndexedVariable v = new IndexedVariable(this, _MemberConverter.ConvertUp(item), index);
//                _MemberVars.Insert(index, v);
//                for (int i = index + 1; i < _MemberVars.Count; i++)
//                    _MemberVars[i].Index = i;




//            }
//            finally { Monitor.Exit(_Lock); }
//        }

//        #endregion

//    }