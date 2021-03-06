﻿using System;
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
using Mathematics;
using System.Runtime.Serialization;

namespace Dependency.Variables
{
    [Serializable]
    public class Variable : IAsyncUpdater, ISyncUpdater, IUpdatedVariable, IVariable, ISerializedVariable
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
        
        [field: NonSerialized]
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
                _Value = newValue;
                OnValueChanged();
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
        bool IUpdatedVariable.CommitContents(IEvaluateable newContents) => _Contents.Equals(newContents) ? false : CommitContents(newContents); 



        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IEvaluateable contents = null) { this.Contents = contents ?? Null.Instance; }

        public override string ToString()
        {
            if (Contents.Equals(Value)) return "{Variable} = " + Value.ToString();
            return "{Variable "+Contents.ToString()+" } = " + Value.ToString();
        }

        public event EventHandler Updated;

        /// <summary>Called immediately after the cached value is updated.</summary>
        protected virtual void OnValueChanged()
        {
            // Cannot be a ValueChangedHandler because there is no guarantee that the last-most 
            // change will be received last by any consumers.
            if (Updated != null)
                Events.InvokeAsynchronous(this, Updated, new EventArgs());
        }


        public bool DependsOn(IVariable other) => Helpers.TryFindDependency(this, other, out var _);

        #region Variable connection members
        internal virtual bool OnAddListener(ISyncUpdater isu) => Listeners.Add(isu);
        internal virtual bool OnRemoveListener(ISyncUpdater isu) => Listeners.Remove(isu);
        
        bool IAsyncUpdater.AddListener(ISyncUpdater isu) => OnAddListener(isu);
        bool IAsyncUpdater.RemoveListener(ISyncUpdater isu) => OnRemoveListener(isu);
        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => Listeners;
        [NonSerialized]
        internal readonly Update.ListenerManager Listeners = new Update.ListenerManager();

        /// <summary>
        /// Usually a <see cref="Variable"/> won't have a parent, because the variable is intended 
        /// to represent the top level of a synchronous unit.  But some variables are composed of 
        /// sub-variables (such as, e.g., <seealso cref="Array"/>s), and in those cases, the sub-
        /// variables must have a parent.
        /// </summary>
        internal ISyncUpdater Parent { get; set; }
        ISyncUpdater ISyncUpdater.Parent { get => Parent; set { Parent = value; } }

        ITrueSet<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild, ITrueSet<IEvaluateable> updatedDomain)
            => OnChildUpdated(caller, updatedChild) ? Update.UniversalSet : null;
        internal virtual bool OnChildUpdated(Update caller, ISyncUpdater updatedChild) => CommitValue(Evaluate());
        internal virtual IEvaluateable Evaluate() => _Contents.Value;


        #endregion


        #region Variable serialization/deserialzation members

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) => Serialization.GetObjectData(this, info, context);
        void ISerializedVariable.GetObjectData(SerializationInfo info, ISerializationManager manager) => GetObjectData(info, manager);
        protected virtual void GetObjectData(SerializationInfo info, ISerializationManager manager)
        {
            info.AddValue(nameof(_Contents), _Contents);
            info.AddValue(nameof(_Value), _Value);
            info.AddValue(nameof(Parent), Parent);
            info.AddValue(nameof(Listeners), Listeners.ToArray());
        }

        protected Variable(SerializationInfo info, StreamingContext context) : this()
        {
            this._Contents = (IEvaluateable)info.GetValue(nameof(_Contents), typeof(IEvaluateable));
            this._Value = (IEvaluateable)info.GetValue(nameof(_Value), typeof(IEvaluateable));
            this.Parent = (ISyncUpdater)info.GetValue(nameof(Parent), typeof(ISyncUpdater));
            foreach (ISyncUpdater listener in (ISyncUpdater[]) info.GetValue(nameof(Listeners), typeof(ISyncUpdater[])))
                Listeners.Add(listener);
        }

        #endregion

    }

    [Serializable]
    public sealed class Variable<T> : Variable, IVariable<T>
    {   

        [NonSerialized]
        private T _Cache = default;

        public Variable(T startingValue)
        {
            if (typeof(IEvaluateable).IsAssignableFrom(typeof(T)))
                throw new TypeInitializationException("On " + typeof(Variable<T>).Name + ", type " + nameof(T) + " cannot implement " + nameof(IEvaluateable) + ". Use standard " + nameof(Variable) + " for type " + typeof(T).Name + " instead.", null);

            this._Converter = GetDefaultConverter();
            this.TypeGuarantee = (this._Converter is ITypeGuarantee itg) ? itg.TypeGuarantee : TypeFlags.Any;
            this.Contents = this._Converter.ConvertUp(startingValue);
        }
        private static IConverter<T> GetDefaultConverter() => Dependency.Values.Converter<T>.Default;
        public Variable(IEvaluateable contents = null)  // Don't call base.Contents(it will try to 
                                                        // update Value before a IConverter is ready)
        {
            this._Converter = GetDefaultConverter();
            this.TypeGuarantee = (this._Converter is ITypeGuarantee itg) ? itg.TypeGuarantee : TypeFlags.Any;
            this.Contents = contents ?? Null.Instance;
        }
        [NonSerialized]
        private readonly IConverter<T> _Converter;      // This should never be null
        public readonly TypeFlags TypeGuarantee;

        public T Native
        {
            get => _Cache;
            set { this.Contents = _Converter.ConvertUp(value); }
        }
        public T Get() => Native;   // For compatibility
        public void Set(T value) => Native = value;
        public static implicit operator T(Variable<T> v) { v._Converter.TryConvertDown(v.Value, out T result); return result; }

        protected override void OnValueChanged()
        {
            T oldCache = _Cache;
            _Converter.TryConvertDown(this.Value, out _Cache);
            if (oldCache ==  null) { if (_Cache == null) return; }                
            else if (oldCache.Equals(_Cache)) return;
            base.OnValueChanged(); // Fires Updated event
        }

        #region Variable<T> serialization/deserialzation members

        protected override void GetObjectData(SerializationInfo info, ISerializationManager manager)
        {
            base.GetObjectData(info, manager);
            info.AddValue(nameof(TypeGuarantee), TypeGuarantee); 
        }

        private Variable(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            this.TypeGuarantee = (TypeFlags)info.GetValue(nameof(TypeGuarantee), typeof(TypeFlags));
            this._Converter = GetDefaultConverter();
            OnValueChanged();
        }

        #endregion

    }



}
