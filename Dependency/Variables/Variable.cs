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
    [Serializable]
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

        internal virtual bool CommitValue(IEvaluateable newValue)
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
        bool IUpdatedVariable.CommitValue(IEvaluateable newValue) => CommitValue(newValue);

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
        internal virtual bool CommitContents(IEvaluateable newContents) { _Contents = newContents; return true; }
        bool IUpdatedVariable.CommitContents(IEvaluateable newContents) => (_Contents.Equals(newContents)) ? false : CommitContents(newContents); 



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
        
        bool IAsyncUpdater.AddListener(ISyncUpdater isu) => OnAddListener(isu);
        bool IAsyncUpdater.RemoveListener(ISyncUpdater isu) => OnRemoveListener(isu);
        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => Listeners;
        internal readonly Update.ListenerManager Listeners = new Update.ListenerManager();

        /// <summary>
        /// Usually a <see cref="Variable"/> won't have a parent, because the variable is intended 
        /// to represent the top level of a synchronous unit.  But some variables are composed of 
        /// sub-variables (such as, e.g., <seealso cref="Array"/>s), and in those cases, the sub-
        /// variables must have a parent.
        /// </summary>
        internal ISyncUpdater Parent { get; set; }
        ISyncUpdater ISyncUpdater.Parent { get => Parent; set { Parent = value; } }

        bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild) => OnChildUpdated(caller, updatedChild);
        internal virtual bool OnChildUpdated(Update caller, ISyncUpdater updatedChild) => CommitValue(Evaluate());
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
            this.TypeGuarantee = (this._Converter is ITypeGuarantee itg) ? itg.TypeGuarantee : TypeFlags.Any;
            this.Contents = this._Converter.ConvertUp(startingValue);
        }
        public Variable(IEvaluateable contents = null, IConverter<T> converter = null)  // Don't call base.Contents(it will try to 
                                                                                        // update Value before a IConverter is ready)
        {
            this._Converter = converter ?? Dependency.Values.Converter<T>.Default;
            this.TypeGuarantee = (this._Converter is ITypeGuarantee itg) ? itg.TypeGuarantee : TypeFlags.Any;
            this.Contents = contents ?? Null.Instance;
        }
        private readonly IConverter<T> _Converter;      // This should never be null
        public readonly TypeFlags TypeGuarantee;

        /// <summary>
        /// Returns the current <typeparamref name="T"/> value, or the last valid 
        /// <typeparamref name="T"/> value, of this <see cref="Variable{T}"/>.
        /// </summary>
        public T Get() => _Cache;
        public void Set(T newContents) => this.Contents = _Converter.ConvertUp(newContents);
        public static implicit operator T(Variable<T> v) => v._Converter.ConvertDown(v.Value);

        protected override void OnValueChanged(IEvaluateable oldValue, IEvaluateable newValue)
        {
            T oldCache = _Cache;            
            _Cache =  _Converter.ConvertDown(this.Value);
            if (oldCache ==  null) { if (_Cache == null) return; }                
            else if (oldCache.Equals(_Cache)) return;
            base.OnValueChanged(oldValue, newValue); // Fires Updated event
        }
    }



}
