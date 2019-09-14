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

namespace Dependency.Variables
{
    public class Variable : IDynamicItem, IVariableAsync
    {
        // DO NOT implement IDisposable to clean up listeners.  The listeners will expire via garbage collection.
        // Also, References clean themselves up from their sources through their own implementation  of 
        // IDisposable.

        // A variable is apt to have few sources, but many listeners (0 or 100,000 might be reasonable).

        // Invariant rule:  No evaluation tree Update() or dependency Update() should EVER push a value forward 
        // because this would be an automatic race condition.  Example:  Variable updates to value 1, calls its 
        // listeners to Update.  Variable then updates to value 2, calls its listeners to update.  Listeners update 
        // with the pushed value 2, and evaluate themselves accordingly.  Then listeners update with pushed value 
        // 1, and evaluate accordingly.  Listeners are now inconsistent with Variable's current value, whcih is 2.


        private readonly WeakReferenceSet<IDynamicItem> _Listeners = new WeakReferenceSet<IDynamicItem>();
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
            private set
            {
                _ValueLock.EnterWriteLock();
                try { _Value = value; }
                finally { _ValueLock.ExitWriteLock(); }
            }
        }
        
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
                var update = Update.ForContents(this, value);
                update.Execute();
                update.Await();
                
            }
        }


        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IEvaluateable contents = null) { this.Contents = contents ?? Null.Instance; }


        public  Update SetContentsAsync(IEvaluateable newContents)
        {
            Update result = Update.ForContents(this, newContents);
            result.Execute();
            return result;
        }



        public override string ToString()
        {
            if (Contents.Equals(Value)) return "{Variable} = " + Value.ToString();
            return Contents.ToString() + " = " + Value.ToString();
        }

        public event ValueChangedHandler<IEvaluateable> ValueChanged;
        
        internal IDynamicItem Parent { get; set; }

        bool IVariable.AddListener(IDynamicItem idi) => _Listeners.Add(idi);
        bool IVariable.RemoveListener(IDynamicItem idi) => _Listeners.Remove(idi);
        IEnumerable<IDynamicItem> IVariable.GetListeners() => _Listeners;
        ISet<Functions.Reference> IVariable.References { get; set; }
        void IVariable.SetValue(IEvaluateable value) => Value = value;
        void IVariable.FireValueChanged(IEvaluateable oldValue, IEvaluateable newValue)
            => ValueChanged?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, newValue));
        ReaderWriterLockSlim IVariableAsync.ValueLock => _ValueLock;
        IDynamicItem IDynamicItem.Parent { get => Parent; set => Parent = value; }
        bool IDynamicItem.Update() => Update.ForValue(this).Execute();


    }




    /// <summary>An exception thrown when an invalid circular dependency is added to a DependencyGraph.</summary>
    internal class CircularDependencyException : InvalidOperationException
    {
        IEnumerable<IVariable> Path;
        /// <summary>Creates a new CircularDependencyException.</summary>
        public CircularDependencyException(IEnumerable<IVariable> path, string message = "Circular reference identified.") : base(message) { this.Path = path; }
    }

}
