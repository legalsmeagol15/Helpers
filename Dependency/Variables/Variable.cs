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

            
        private readonly WeakReferenceSet<ISyncUpdater> _Listeners = new WeakReferenceSet<ISyncUpdater>();
        private IEvaluateable _Value = Null.Instance;  // Must be guaranteed never to be CLR null        
        protected readonly ReaderWriterLockSlim ValueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        
        

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
                ValueChanged?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, newValue));
            }
            finally { ValueLock.ExitWriteLock(); }
            return true;
        }
        
        private IEvaluateable _Contents = Null.Instance;
        public virtual IEvaluateable Contents
        {
            get
            {
                // Contents defines structure.
                Dependency.Variables.Update.StructureLock.EnterReadLock();
                try { return _Contents; }
                finally { Dependency.Variables.Update.StructureLock.ExitReadLock(); }
            }
            set
            {
                // Use an update to kick off Parent's and listeners' updates as well.
                var update = Dependency.Variables.Update.ForVariable(this, value);
                update.Execute();
            }
        }


        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IEvaluateable contents = null) { this.Contents = contents ?? Null.Instance; }
        



        public override string ToString()
        {
            if (Contents.Equals(Value)) return "{Variable} = " + Value.ToString();
            return Contents.ToString() + " = " + Value.ToString();
        }

        public event ValueChangedHandler<IEvaluateable> ValueChanged;
        
        //internal ISyncUpdater Parent { get; set; }

        bool IAsyncUpdater.AddListener(ISyncUpdater idi) => _Listeners.Add(idi);
        bool IAsyncUpdater.RemoveListener(ISyncUpdater idi) => _Listeners.Remove(idi);
        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => _Listeners;
        
        ISyncUpdater ISyncUpdater.Parent { get; set; }

        bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild) => Update();
        protected virtual bool Update() => SetValue(_Contents.Value);
        
        void IUpdatedVariable.SetContents(IEvaluateable newContents) => _Contents = newContents;

        bool IUpdatedVariable.SetValue(IEvaluateable newValue) => SetValue(newValue);
    }


    
}
