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
    
    public sealed class List<T> : IVariable<IList<T>>, ISyncUpdater, IAsyncUpdater, INotifyUpdates<IEvaluateable>, IIndexable, IUpdatedVariable
    {
        private enum Definition { SELF_DEFINED, MEMBER_DEFINED}
        private Definition _DefinedBy = Definition.SELF_DEFINED;
        private readonly object _Lock = new object();
        private readonly IConverter<T> _MemberConverter;
        private readonly Dictionary<int, IndexedVariable> _MemberVars;
        private IList<T> _CachedList = null;
        private IEvaluateable _Contents = Dependency.Null.Instance;
        private IEvaluateable _Value = Dependency.Null.Instance;
        bool IIndexable.ControlsReindex => true;


        /// <summary>
        /// A vector that always indicates a change in value, whenever asked.
        /// </summary>
        private class UnstableVector : ISyncUpdater
        {
            private readonly System.Collections.Generic.List<IEvaluateable> _Internal;
            public UnstableVector(IEnumerable<IEvaluateable> items)
            { 
                this._Internal = new System.Collections.Generic.List<IEvaluateable>(items);
            }

            public void Add(IEvaluateable item) => _Internal.Add(item);
            public void RemoveAt(int index) => _Internal.RemoveAt(index);
            public void Clear() => _Internal.Clear();
            public void InsertAt(int index, IEvaluateable item) => _Internal.Insert(index, item);
            public IEvaluateable this[int index] { get => _Internal[index]; set => _Internal[index] = value; }

            IEvaluateable IEvaluateable.Value => this;

            public override bool Equals(object obj) => false;
            public override int GetHashCode() => throw new InvalidOperationException();

            public ISyncUpdater Parent { get; set; }
            bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild) => true;
        }
        /// <summary>Wraps a <seealso cref="Variable{T}"/> to give it an index.</summary>
        private class IndexedVariable : IVariable<T>, ISyncUpdater, IComparable<IndexedVariable>
        {
            private readonly Variable<T> _Wrapped;
            public List<T> Parent => (List<T>)_Wrapped.Parent;
            public int Index { get; internal set; }
            public IndexedVariable(List<T> parent, IEvaluateable contents, int index)
            {
                this._Wrapped = new Variable<T>(contents, parent._MemberConverter) { Parent = parent };
                this.Index = index;
            }
            public T Native { get => _Wrapped.Native; set => _Wrapped.Native = value; }

            public IEvaluateable Contents
            {
                get => _Wrapped.Contents;
                set
                {
                    IEvaluateable oldContents = _Wrapped.Contents;
                    _Wrapped.Contents = value;
                    if (!oldContents.Equals(value))
                        Parent.MemberContentsChanged(this, oldContents, value);
                }
            }

            public IEvaluateable Value => _Wrapped.Value;
            ISyncUpdater ISyncUpdater.Parent { get=> _Wrapped.Parent; set => throw new InvalidOperationException(); }


            bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild)
                => _Wrapped.OnChildUpdated(caller, updatedChild);

            int IComparable<IndexedVariable>.CompareTo(IndexedVariable other)
                => Index.CompareTo(other.Index);
        }


        private List (IConverter<T> memberConverter = null)
        {
            this._MemberVars = new Dictionary<int, IndexedVariable>();
            this._MemberConverter = memberConverter ?? Dependency.Values.Converter<T>.Default;
            if (this._MemberConverter is Dependency.Values.Converter<T>)
                throw new ArgumentException("Invalid converter: " + typeof(T).Name);
        }
        public List(params T[] items) : this(items, null) { }
        public List(IEnumerable<T> items, IConverter<T> memberConverter) : this(memberConverter)
        {
            this.Contents = new Vector(items.Select(item => memberConverter.ConvertUp(item)));
        }

        private bool TryGetMember(int index, out IndexedVariable variable)
        {
            // TODO:  members should be weakly referenced.
            return _MemberVars.TryGetValue(index, out variable);
        }
        private void SetMember(int index, IndexedVariable variable)
        {
            // TODO:  members should be weakly referenced.
            _MemberVars[index] = variable;
        }

        public IList<T> Native
        {
            get
            {
                if (this._CachedList == null)
                    this._CachedList = new System.Collections.Generic.List<T>(this._MemberVars.Values.Select(v => this._MemberConverter.ConvertDown(v)));
                return this._CachedList;
            }
            set
            {
                this.Contents = new Vector(value.Select(item => this._MemberConverter.ConvertUp(item)));
            }
        }


        public IEvaluateable Contents
        {
            get => this._Contents;
            set => SelfContentsChanged(this._Contents, value);
        }
        private void SelfContentsChanged(IEvaluateable oldContents, IEvaluateable newContents)
        {
            // I would expect the user to supply a Vector, but that's not guaranteed.
            lock (_Lock)
            {
                if (oldContents.Equals(newContents)) return;
                Update.ForVariable(this, newContents).Execute();
                _MemberVars.Clear();
                _DefinedBy = Definition.MEMBER_DEFINED;
                //foreach (var listener in this.Listeners.OfType<Indexing>())
                //    listener.Reindex();
            }
        }

        private void MemberContentsChanged(IndexedVariable indexedVariable, IEvaluateable oldContents, IEvaluateable newContents)
        {
            Monitor.Enter(_Lock);
            try
            {
                if (_DefinedBy == Definition.SELF_DEFINED)
                {
                    SetMember(indexedVariable.Index, indexedVariable);
                    _DefinedBy = Definition.MEMBER_DEFINED;

                }
                else if (oldContents.Equals(newContents)) return;

            }
            finally { Monitor.Exit(_Lock); }
        }


        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
        {
            Monitor.Enter(_Lock);
            try
            {
                switch (_DefinedBy)
                {
                    case Definition.SELF_DEFINED when _Contents is IIndexable ii:
                        return ii.TryIndex(ordinal, out val);
                    case Definition.MEMBER_DEFINED when ordinal is Number n && n.IsInteger && TryGetMember((int)n, out IndexedVariable iv):
                        val = iv; return true;
                    default:
                        val = default; return false;
                }
            }
            finally { Monitor.Exit(_Lock); }            
        }

        bool IUpdatedVariable.CommitContents(IEvaluateable newContent)
        {
            if (_Contents.Equals(newContent)) return false;
            _Contents = newContent;
            return true;
        }

        public IEvaluateable Value
        {
            get => _Value;
            private set
            {
                IEvaluateable oldValue = _Value;
                _Value = value;
                Updated?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, value));
            }
        }
        public event ValueChangedHandler<IEvaluateable> Updated;
        ISyncUpdater ISyncUpdater.Parent { get; set; }
        bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild)
        {
            Monitor.Enter(_Lock);
            try
            {
                switch (_DefinedBy)
                {
                    case Definition.SELF_DEFINED: Value = _Contents.Value;
                }
            }
            finally { Monitor.Exit(_Lock); }
            
            Definition priorDef = _DefinedBy;
            switch (_DefinedBy)
            {
                case Definition.SELF_DEFINED_UPDATING_CONTENTS: return false;
                case Definition.SELF_DEFINED:
                    Debug.Assert(updatedChild.Equals(_Contents));
                    return CommitValue(_Contents.Value);                    
                case Definition.MEMBER_DEFINED:
                default:
                    throw new NotImplementedException();
            }
        }
            
        #region List IASyncUpdate members (listeners)

        internal readonly Update.ListenerManager Listeners = new Update.ListenerManager();
        bool IAsyncUpdater.RemoveListener(ISyncUpdater listener) => Listeners.Remove(listener);

        bool IAsyncUpdater.AddListener(ISyncUpdater listener) => Listeners.Add(listener);

        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => Listeners;

        #endregion

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