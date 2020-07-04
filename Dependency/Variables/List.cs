using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    public sealed class List<T> : IVariable<IList<T>>, ISyncUpdater, IAsyncUpdater, INotifyUpdates<IEvaluateable>, IIndexable, IUpdatedVariable
    {
        private readonly object _Lock = new object();
        private readonly IConverter<T> _Converter;
        private readonly System.Collections.Generic.List<IndexedVariable> _MemberVars;
        private IList<T> _Cache = null;
        private IEvaluateable _Contents, _Value = Dependency.Null.Instance;

        internal VariableMode Mode { get; private set; } = VariableMode.NONE;

        private List(IConverter<T> converter = null)
        {
            this._Converter = converter ?? Dependency.Values.Converter<T>.Default;
            if (this._Converter is Dependency.Values.Converter<T>)
                throw new ArgumentException("Invalid converter: " + this._Converter.GetType().Name);
        }
        public List(params T[] items) : this(items, null) { }
        public List(IEnumerable<T> items, IConverter<T> converter) : this(converter)
        {
            Contents = new Vector(items.Select(i => converter.ConvertUp(i)));
        }
        public IList<T> Native
        {
            get
            {
                if (this._Cache == null)
                    this._Cache = new System.Collections.Generic.List<T>(this._MemberVars.Select(v => this._Converter.ConvertDown(v.Value)));
                return this._Cache;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public IEvaluateable Contents
        {
            get => this._Contents;
            set
            {
                VariableMode priorMode = VariableMode.NONE;
                try
                {
                    Monitor.Enter(_Lock);
                    if ((priorMode = Mode) != VariableMode.NONE)
                        throw new InvalidOperationException("Cannot changed contents while in state " + Mode.ToString());
                    Mode = VariableMode.UPDATING_CONTENTS;

                    // If this isn't a vector, the result is a conversion error for any indexed member.
                    if (!(value is Vector v))
                    {
                        ConversionError ce = new ConversionError(value, typeof(Vector));
                        foreach (var mv in _MemberVars) mv.Contents = ce;
                    }
                    else
                    {
                        for (int i = 0; i < v.Inputs.Count; i++)
                        {
                            if (i < _MemberVars.Count)
                                _MemberVars[i].Contents = v[i];
                            else
                                _MemberVars.Add(new IndexedVariable(v[i], i, _Converter) { Parent = this });
                        }
                        while (_MemberVars.Count > v.Inputs.Count)
                            _MemberVars.RemoveAt(_MemberVars.Count - 1);
                    }

                    // Fire an update.  This should force all indexing references to this variable 
                    // to re-index.
                    Update.ForVariable(this, this._Contents).Execute();
                }
                finally { Mode = priorMode; Monitor.Exit(_Lock); }
            }
        }

        bool IUpdatedVariable.CommitContents(IEvaluateable newContents)
        {
            if (_Contents.Equals(newContents)) return false;
            _Contents = newContents;
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
        ISyncUpdater ISyncUpdater.Parent { get => Parent; set { Parent = value; } }
        internal ISyncUpdater Parent { get; set; }

        IEvaluateable IEvaluateable.Value => throw new NotImplementedException();

        bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild)
        {
            VariableMode priorMode = VariableMode.NONE;
            try
            {
                Monitor.Enter(_Lock);
                IndexedVariable indexedChild = (IndexedVariable)updatedChild;
                priorMode = Mode;

                // If the update comes from the contents itself, update with the new value.
                if (updatedChild.Equals(_Contents))
                    return CommitValue(_Contents.Value);

                // If the contents are being updated, don't pass a child's change further up.
                else if (Mode == VariableMode.UPDATING_CONTENTS)
                    return false;

                // If the update comes from a child, force contents to change.
                Mode = VariableMode.UPDATING_CHILD_VALUE;
                if (_Contents is Vector v)
                {
                    return v.InternalSet(indexedChild.Index, indexedChild.Value);
                }
                else
                    throw new NotImplementedException("Create a new Vector with the _MemberVars values as contents.");
            }
            finally { Mode = priorMode; Monitor.Exit(_Lock); }
        }
        private bool CommitValue(IEvaluateable newValue)
        {
            try
            {
                Monitor.Enter(_Lock);
                bool changed = false;
                if (_Converter.TryConvertDown(newValue, out T clr)) { _Cache = null; changed = true; }
                if (!Value.Equals(newValue)) { Value = newValue; changed = true; }
                return changed;
            }
            finally { Monitor.Exit(_Lock); }
        }
        bool IUpdatedVariable.CommitValue(IEvaluateable newValue) => CommitValue(newValue);



        #region List IASyncUpdate members (listeners)

        internal readonly Update.ListenerManager Listeners = new Update.ListenerManager();
        bool IAsyncUpdater.RemoveListener(ISyncUpdater listener) => Listeners.Remove(listener);

        bool IAsyncUpdater.AddListener(ISyncUpdater listener) => Listeners.Add(listener);

        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => Listeners;

        #endregion



        #region List indexing members

        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
        {
            if (ordinal is Number n)
            {
                try
                {
                    Monitor.Enter(_Lock);
                    int idx = (int)n;
                    if (idx < 0 || idx >= _MemberVars.Count)
                    {
                        val = new IndexingError(this, null, "Index " + ordinal.ToString() + " is out of range.");
                    }
                    val = _MemberVars[idx];
                    return true;
                } finally { Monitor.Exit(_Lock); }
            }
            val = new IndexingError(this, null, "Index " + ordinal.ToString() + " is invalid.");
            return false;
        }


        /// <summary>Wraps a <seealso cref="Variable{T}"/> to give it an index.</summary>
        private class IndexedVariable : IVariable<T>, ISyncUpdater
        {
            private readonly Variable<T> _Wrapped;
            public int Index { get; internal set; }
            public IndexedVariable(IEvaluateable contents, int index, IConverter<T> converter)
            {
                this._Wrapped = new Variable<T>(contents, converter);
                this.Index = index;
            }
            public T Native { get => _Wrapped.Native; set => _Wrapped.Native = value; }

            public IEvaluateable Contents { get => _Wrapped.Contents; set => _Wrapped.Contents = value; }

            public IEvaluateable Value => _Wrapped.Value;

            public ISyncUpdater Parent { get => _Wrapped.Parent; set => _Wrapped.Parent = value; }

            bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild)
                => _Wrapped.OnChildUpdated(caller, updatedChild);
        }

        #endregion

    }
}
