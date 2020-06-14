using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using System.Runtime.CompilerServices;
using System.Collections;
using DataStructures;
using System.Threading;
using Dependency.Functions;
using System.Diagnostics;

namespace Dependency.Variables
{
    public class Variable : IAsyncUpdater, ISyncUpdater, IUpdatedVariable, INotifyUpdates<IEvaluateable>, IVariable
    {
        // DO NOT implement IDisposable to clean up listeners.  The Variable should should clean up only when its 
        // listeners are already gone anyway.
        // A variable is apt to have few sources, but many listeners (0 or 100,000 might be reasonable).

        // Invariant rule:  No evaluation tree Update() or dependency Update() should EVER push a value forward 
        // because this would be an automatic race condition.  Example:  Variable updates to value 1, calls its 
        // listeners to Update.  Variable then updates to value 2, calls its listeners to update.  Listeners update 
        // with the pushed value 2, and evaluate themselves accordingly.  Then listeners update with pushed value 
        // 1, and evaluate accordingly.  Listeners are now inconsistent with Variable's current value, whcih is 2.

        private IEvaluateable _Value = Null.Instance;  // Must be guaranteed never to be CLR null        
        internal readonly ReaderWriterLockSlim ValueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public IEvaluateable Value
        {
            get
            {
                ValueLock.EnterReadLock();
                try { return _Value; }
                finally { ValueLock.ExitReadLock(); }
            }
        }

        private bool SetValue(IEvaluateable newValue)
        {
            ValueLock.EnterWriteLock();
            try
            {
                if (_Value.Equals(newValue)) return false;
                IEvaluateable oldValue = _Value;
                _Value = newValue;
                OnValueChanged(oldValue, newValue);
                return true;
            }
            finally { ValueLock.ExitWriteLock(); }
        }
        bool IUpdatedVariable.SetValue(IEvaluateable newValue) => SetValue(newValue);

        private IEvaluateable _Contents = Null.Instance;
        public IEvaluateable Contents
        {
            get
            {
                // Contents defines structure.
                Update.StructureLock.EnterReadLock();
                try { return _Contents; }
                finally { Update.StructureLock.ExitReadLock(); }
            }
            set
            {
                var update = Update.ForVariable(this, value);
                update.Execute();
            }
        }

        /// <summary>Sets contents without starting a new update.</summary>
        protected virtual bool SetContents(IEvaluateable newContents) { _Contents = newContents; return true; }
        bool IUpdatedVariable.SetContents(IEvaluateable newContents) => SetContents(newContents);



        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IEvaluateable contents = null) { this.Contents = contents ?? Null.Instance; }

        public override string ToString()
        {
            if (Contents.Equals(Value)) return "{Variable} = " + Value.ToString();
            return Contents.ToString() + " = " + Value.ToString();
        }

        public event ValueChangedHandler<IEvaluateable> Updated;

        /// <summary>Called immediately after the cached value is updated, from within a 
        /// <seealso cref="Variable.ValueLock"/> write lock.  Invokes the 
        /// <seealso cref="Updated"/> event.</summary>
        protected virtual void OnValueChanged(IEvaluateable oldValue, IEvaluateable newValue)
        {
            Updated?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, newValue));
        }



        #region Variable connection members
        internal virtual bool OnAddListener(ISyncUpdater isu) => Listeners.Add(isu);
        internal virtual bool OnRemoveListener(ISyncUpdater isu) => Listeners.Remove(isu);
        internal virtual void OnParentChanged(ISyncUpdater oldParent, ISyncUpdater newParent)
            => _Parent = newParent;
        bool IAsyncUpdater.AddListener(ISyncUpdater isu) => OnAddListener(isu);
        bool IAsyncUpdater.RemoveListener(ISyncUpdater isu) => OnRemoveListener(isu);
        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => Listeners;
        internal readonly Update.ListenerManager Listeners = new Update.ListenerManager();

        private ISyncUpdater _Parent = null;
        /// <summary>
        /// Usually a <see cref="Variable"/> won't have a parent, because the variable is intended 
        /// to represent the top level of a synchronous unit.  But some variables are composed of 
        /// sub-variables (such as, e.g., <seealso cref="Array"/>s), and in those cases, the sub-
        /// variables must have a parent.
        /// </summary>
        internal ISyncUpdater Parent
        {
            get => _Parent;
            set
            {
                if (_Parent == null)
                    if (value == null) return;
                if (_Parent == value) return;
                OnParentChanged(_Parent, value);
            }
        }
        ISyncUpdater ISyncUpdater.Parent { get => Parent; set { Parent = value; } }

        bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild) => SetValue(Evaluate());
        internal virtual IEvaluateable Evaluate() => _Contents.Value;
        #endregion

    }


    public sealed class Variable<T> : Variable
    {
        // TODO:  does this replace a Blended<T> ?
        private T _Cache = default;
        public Variable(T startingValue)
        {
            this._Converter = Dependency.Values.Converter<T>.Default;
            this._TypeGuarantee = (this._Converter is ITypeGuarantee itg) ? itg.TypeGuarantee : TypeFlags.Any;
            this.Contents = this._Converter.ConvertFrom(startingValue);
        }
        public Variable(IEvaluateable contents = null, IConverter<T> converter = null)  // Don't call base.Contents(it will try to 
                                                                                        // update Value before a IConverter is ready)
        {
            this._Converter = converter ?? Dependency.Values.Converter<T>.Default;
            this._TypeGuarantee = (this._Converter is ITypeGuarantee itg) ? itg.TypeGuarantee : TypeFlags.Any;
            this.Contents = contents ?? Null.Instance;
        }
        private readonly IConverter<T> _Converter;      // This should never be null
        private readonly TypeFlags _TypeGuarantee;
        public T Get() => _Cache;
        public void Set(T newContents) => this.Contents = _Converter.ConvertFrom(newContents);
        public static implicit operator T(Variable<T> v) => v._Converter.ConvertTo(v.Value);

        protected override void OnValueChanged(IEvaluateable oldValue, IEvaluateable newValue)
        {
            T oldCache = _Cache;            
            _Cache =  _Converter.ConvertTo(this.Value);
            if (oldCache ==  null) { if (_Cache == null) return; }                
            else if (oldCache.Equals(_Cache)) return;
            base.OnValueChanged(oldValue, newValue);
            ValueChanged?.Invoke(this, new ValueChangedArgs<T>(oldCache, _Cache));
        }
        
        public event ValueChangedHandler<T> ValueChanged;
    }



}
