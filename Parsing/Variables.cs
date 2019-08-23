using System;
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

namespace Dependency
{
    public enum Mobility
    {
        // TODO:  more info will probably be needed
        None = 0,
        Column = 1,
        Row = 2,
        All = ~0
    }
    
    public sealed class Variable : IEvaluateable
    {
        // A variable is apt to have few sources, but many listeners (0 or 100,000 might be reasonable).

        public readonly IContext Context;
        public readonly string Name;
        /// <summary>
        /// A number the must be given to <seealso cref="SetContents(IEvaluateable, int)"/> to allow contents 
        /// modification.
        /// </summary>
        private readonly int ModLock;
        private ISet<Variable> _Sources = new HashSet<Variable>();
        private WeakReferenceSet<Variable> _Listeners = new WeakReferenceSet<Variable>();
        private IEvaluateable _Value = Null.Instance;  // Must be guaranteed never to be CLR null
        private readonly ReaderWriterLockSlim _ValueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private static readonly ReaderWriterLockSlim _StructureLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);


        public IEvaluateable Value
        {
            get
            {
                _ValueLock.EnterReadLock();
                try { return _Value; }
                finally { _ValueLock.ExitReadLock(); }
            }
        }
        private IEvaluateable _Contents;
        public IEvaluateable Contents
        {
            get
            {
                // Contents defines structure.
                _StructureLock.EnterReadLock();
                try { return _Contents; }
                finally { _StructureLock.ExitReadLock(); }
            }
            set
            {
                // No key is given when Contents is set this way.
                SetContents(value, 0);
            }
        }
        public void SetContents(IEvaluateable value, int modKey = 0)
        {
            if (ModLock != modKey)
                throw new Exception("This Variable has a modification lock.  The correct modification key must be provided.");

            if (value == null) value = Dependency.Null.Instance;

            // First, update the structure-related variables (contents and sources).
            ISet<Variable> newSources = Helpers.GetTerms(value);
            _StructureLock.EnterUpgradeableReadLock();  // Lock 'a' because the structure shouldn't change while we examine it.
            try
            {
                if (TryFindCircularity(this, newSources, out Deque<Variable> path)) throw new CircularDependencyException(path);
                Variable[] oldSources = _Sources.Except(newSources).ToArray();
                _StructureLock.EnterWriteLock();
                try
                {
                    foreach (Variable oldSrc in oldSources) oldSrc._Listeners.Remove(this);
                    foreach (Variable newSrc in newSources) newSrc._Listeners.Add(this);
                    _Sources = newSources;
                    _Contents = value;
                }
                finally { _StructureLock.ExitWriteLock(); }
            }
            catch (CircularDependencyException) { throw; }
            finally { _StructureLock.ExitUpgradeableReadLock(); }

            // Second, update the changed value
            UpdateValue();
        }
        
        public IEvaluateable UpdateValue()
        {
            // This method makes the guess that most updates will NOT change the value.
            IEvaluateable oldValue, newValue;
            _ValueLock.EnterUpgradeableReadLock();
            _StructureLock.EnterReadLock();
            try
            {
                // Presume that a source Variable is already updated, since this method will be 
                // called a lot from the sources and we don't want an exponential function.
                newValue = (_Contents is Variable v) ? v.Value : _Contents.UpdateValue();
                if (newValue == null) newValue = Dependency.Null.Instance;

                // Rule out identical values, which should not invoke any further action.
                oldValue = _Value;
                if (oldValue.Equals(newValue))
                    return oldValue;

                // Update the value.
                _ValueLock.EnterWriteLock();
                _Value = newValue;
                _ValueLock.ExitWriteLock();
                FireValueChanged(oldValue, newValue);

                //Now update the listeners
                List<Task> tasks = new List<Task>();
                foreach (Variable listener in _Listeners)
                    tasks.Add(Task.Run(() => listener.UpdateValue()));
                Task.WaitAll(tasks.ToArray());
            }
            finally { _StructureLock.ExitReadLock(); _ValueLock.ExitUpgradeableReadLock(); }

            //UpdateListeners();
            return newValue;
            
        }

        private static bool TryFindCircularity(Variable target, 
                                                IEnumerable<Variable> sources,
                                                out Deque<Variable> path)
        {
            // TODO:  use a Stack<> object instead of stack frames, because I get a stack overflow at approx. 5000 levels deep
            if (sources != null)
            {
                foreach (Variable src in sources)
                {
                    if (ReferenceEquals(target, src)) { path = new Deque<Variable>(); path.AddFirst(src); return true; }
                    else if (TryFindCircularity(target, src._Sources, out path)) { path.AddFirst(src); return true; }
                }
            }
            path = null;
            return false;
        }
        
        
        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IContext context, string name, IEvaluateable contents = null, int modLock = 0)
        {
            this.Context = context;
            this.Name = name;
            this.Contents = contents;
            this.ModLock = modLock;
        }
        
        internal string GetExpressionString(IContext perspective)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Variable> GetTerms() => _Sources;

        public override string ToString() => Name + "=" + Value.ToString();

        public event ValueChangedHandler<IEvaluateable> ValueChanged;
        private void FireValueChanged(IEvaluateable oldValue, IEvaluateable newValue)
            => ValueChanged?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, newValue));

    }
    

    /// <summary>An exception thrown when an invalid circular dependency is added to a DependencyGraph.</summary>
    public class CircularDependencyException : InvalidOperationException
    {
        IEnumerable<Variable> Path;
        /// <summary>Creates a new CircularDependencyException.</summary>
        public CircularDependencyException(IEnumerable<Variable> path, string message = "Circular reference identified.") : base(message) { this.Path = path; }
    }

}
