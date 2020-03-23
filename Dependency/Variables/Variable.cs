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
    public class Variable : IAsyncUpdater, ISyncUpdater, IUpdatedVariable, INotifyUpdates<IEvaluateable>
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
        private readonly ReaderWriterLockSlim _ValueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public IEvaluateable Value
        {
            get
            {
                _ValueLock.EnterReadLock();
                try { return _Value; }
                finally { _ValueLock.ExitReadLock(); }
            }
        }

        private bool SetValue(IEvaluateable newValue)
        {
            _ValueLock.EnterWriteLock();
            try
            {
                if (_Value.Equals(newValue)) return false;
                IEvaluateable oldValue = _Value;
                _Value = newValue;
                OnValueChanged(oldValue, newValue);
                return true;
            }
            finally { _ValueLock.ExitWriteLock(); }
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
                // Use an update to kick off Parent's and listeners' updates as well.
                var update = Update.ForVariable(this, value);
                update.Execute();
            }
        }
        /// <summary>Sets contents without starting a new update.</summary>
        protected void SetContents(IEvaluateable newContents) => _Contents = newContents;
        void IUpdatedVariable.SetContents(IEvaluateable newContents) => SetContents(newContents);



        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IEvaluateable contents = null) { this.Contents = contents ?? Null.Instance; }

        public override string ToString()
        {
            if (Contents.Equals(Value)) return "{Variable} = " + Value.ToString();
            return Contents.ToString() + " = " + Value.ToString();
        }

        public event ValueChangedHandler<IEvaluateable> Updated;

        /// <summary>Called immediately after the cached value is updated, from within a 
        /// <seealso cref="Variable._ValueLock"/> write lock.  Invokes the 
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

        bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild) => SetValue(_Contents.Value);
        #endregion

        public static Variable<T> Typed<T>(IEvaluateable contents = null, IConverter<T> converter = null)
            => new Variable<T>(contents, converter);
    }


    public sealed class Variable<T> : Variable
    {
        // TODO:  does this replace a Blended<T> ?
        private T _Cache = default(T);
        public Variable(T startingValue)
        {
            this._Converter = Dependency.Values.Converter<T>.Default;
            this._TypeGuarantee = (this._Converter is ITypeGuarantee itg) ? itg.TypeGuarantee : TypeFlags.Any;
            this.Contents = this._Converter.ConvertFrom(startingValue);
        }
        public Variable(IEvaluateable contents = null, IConverter<T> converter = null) : base(contents)
        {
            this._Converter = converter ?? Dependency.Values.Converter<T>.Default;
            this._TypeGuarantee = (this._Converter is ITypeGuarantee itg) ? itg.TypeGuarantee : TypeFlags.Any;
        }
        private readonly IConverter<T> _Converter;
        private readonly TypeFlags _TypeGuarantee;
        public T Get() => _Cache;
        public void Set(T newContents) => this.Contents = _Converter.ConvertFrom(newContents);
        public static implicit operator T(Variable<T> v) => v._Converter.ConvertTo(v.Value);

        protected override void OnValueChanged(IEvaluateable oldValue, IEvaluateable newValue)
        {
            _Cache = (_Converter == null) ? default(T) : _Converter.ConvertTo(this.Value);
            base.OnValueChanged(oldValue, newValue);
        }
    }



}
