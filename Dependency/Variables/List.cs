using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dependency.Variables
{

}
//{
//    public sealed class List<T> : IVariable<IList<T>>, ISyncUpdater, IAsyncUpdater, INotifyUpdates<IEvaluateable>, IIndexable, IUpdatedVariable
//    {
//        internal enum Definition
//        {
//            CONTEXT_DEFINED, MEMBER_DEFINED
//        }
//        private Definition DefinedBy = Definition.CONTEXT_DEFINED;
//        private readonly object _Lock = new object();
//        private readonly IConverter<T> _MemberConverter;
//        private readonly Dictionary<int, IndexedVariable> _MemberVars;
//        private IList<T> _Cache = null;
//        private IEvaluateable _Contents, _Value = Dependency.Null.Instance;
//        public bool IsValid { get; private set; } = true;
//        internal VariableMode Mode { get; private set; } = VariableMode.NONE;

//        private List(IConverter<T> memberConverter = null)
//        {
//            this._MemberVars = new Dictionary<int, IndexedVariable>();
//            this._MemberConverter = memberConverter ?? Dependency.Values.Converter<T>.Default;
//            if (this._MemberConverter is Dependency.Values.Converter<T>)
//                throw new ArgumentException("Invalid converter: " + this._MemberConverter.GetType().Name);
//        }
//        public List(params T[] items) : this(items, null) { }
//        public List(IEnumerable<T> items, IConverter<T> memberConverter) : this(memberConverter)
//        {
//            Contents = new Vector(items.Select(i => memberConverter.ConvertUp(i)));
//        }
//        public IList<T> Native
//        {
//            get
//            {
//                if (this._Cache == null)
//                    this._Cache = new System.Collections.Generic.List<T>(this._MemberVars.Select(v => this._MemberConverter.ConvertDown(v.Value)));
//                return this._Cache;
//            }
//            set
//            {
//                throw new NotImplementedException();
//            }
//        }
//        public IEvaluateable Contents
//        {
//            get => this._Contents;
//            set
//            {
//                VariableMode priorMode = VariableMode.NONE;
//                try
//                {
//                    Monitor.Enter(_Lock);
//                    if ((priorMode = Mode) != VariableMode.NONE)
//                        throw new InvalidOperationException("Cannot changed contents while in state " + Mode.ToString());
//                    Mode = VariableMode.UPDATING_CONTENTS;

//                    // If new contents isn't a vector, the result is a conversion error for any 
//                    // indexed member.
//                    if (!(value is Vector v))
//                    {
//                        ConversionError ce = new ConversionError(value, typeof(Vector));
//                        foreach (var mv in _MemberVars) mv.Contents = ce;
//                    }
//                    else
//                    {
//                        for (int i = 0; i < v.Inputs.Count; i++)
//                        {
//                            if (i < _MemberVars.Count)
//                                _MemberVars[i].Contents = v[i];
//                            else
//                                _MemberVars.Add(new IndexedVariable(this, v[i], i));
//                        }
//                        while (_MemberVars.Count > v.Inputs.Count)
//                            _MemberVars.Remove(_MemberVars.Count - 1);
//                    }

//                    // Fire an update.  This should force all indexing references to this variable 
//                    // to re-index.
//                    Update.ForVariable(this, this._Contents).Execute();
//                }
//                finally { Mode = priorMode; Monitor.Exit(_Lock); }
//            }
//        }

//        bool IUpdatedVariable.CommitContents(IEvaluateable newContents)
//        {
//            if (_Contents.Equals(newContents)) return false;
//            _Contents = newContents;
//            return true;
//        }

//        public IEvaluateable Value
//        {
//            get => _Value;
//            private set
//            {
//                IEvaluateable oldValue = _Value;
//                _Value = value;
//                Updated?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, value));
//            }
//        }
//        public event ValueChangedHandler<IEvaluateable> Updated;
//        ISyncUpdater ISyncUpdater.Parent { get => Parent; set { Parent = value; } }
//        internal ISyncUpdater Parent { get; set; }

//        IEvaluateable IEvaluateable.Value => throw new NotImplementedException();

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



//        #region List IASyncUpdate members (listeners)

//        internal readonly Update.ListenerManager Listeners = new Update.ListenerManager();
//        bool IAsyncUpdater.RemoveListener(ISyncUpdater listener) => Listeners.Remove(listener);

//        bool IAsyncUpdater.AddListener(ISyncUpdater listener) => Listeners.Add(listener);

//        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => Listeners;

//        #endregion



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