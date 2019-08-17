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
        private ISet<Variable> _Sources;
        private WeakReferenceSet<Variable> _Listeners;
        private IEvaluateable _Value = Null.Instance;
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
                // This method makes the guess that with new contents, the value will most likely change.

                // First, update the structure-related variables (contents and sources).
                ISet<Variable> newSources = Helpers.GetTerms(value);
                _StructureLock.EnterUpgradeableReadLock();  // Lock 'a' because the structure shouldn't change while we examine it.
                try
                {
                    if (TryFindCircularity(this, out IEnumerable<Variable> path)) throw new CircularDependencyException(path);
                    _StructureLock.EnterWriteLock();
                    try
                    {
                        foreach (Variable src in _Sources.Except(newSources)) src._Listeners.Remove(this);
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
        }


        public IEvaluateable UpdateValue()
        {
            // This method makes the guess that most updates will NOT change the value.
            IEvaluateable oldValue, newValue;
            _ValueLock.EnterUpgradeableReadLock();
            _StructureLock.EnterReadLock();
            try
            {
                oldValue = _Value;
                newValue = _Contents.UpdateValue();
                if (oldValue == null)
                {
                    if (newValue == null) return null;
                }
                else if (oldValue.Equals(newValue)) return oldValue;

                _ValueLock.EnterWriteLock();
                _Value = newValue;
                _ValueLock.ExitWriteLock();
            }
            finally { _StructureLock.ExitReadLock(); _ValueLock.ExitUpgradeableReadLock(); }

            // Notify of the value change.  Immediately is just as good as later.
            OnValueChanged(oldValue, newValue);

            // Now notify the listeners.

            UpdateListeners();
            return newValue;
        }

        private bool TryFindCircularity(Variable target, out IEnumerable<Variable> dependees)
        {
            Deque<Variable> path = null;
            HashSet<Variable> visited = new HashSet<Variable>();
            if (_TryIt(this)) { dependees = path; return true; }
            else { dependees = null; return false; }

            bool _TryIt(Variable focus)
            {
                if (!visited.Add(focus)) return false;
                if (ReferenceEquals(focus, target)) { path = new Deque<Variable>(); return true; }
                foreach (Variable src in focus._Sources)
                    if (_TryIt(src)) { path.AddFirst(focus); return true; }
                return false;
            }
        }

        // It stands to reason that the variable with the most inputs should, in most cases, be the last one updated.  
        // These static datastructures are used to ensure that prioritization.
        private class VariableSorter : IComparer<Variable>
        {
            int IComparer<Variable>.Compare(Variable x, Variable y) => x._Sources.Count - y._Sources.Count;
        }
        private const int UPDATE_THREADS = 4;   // Ration the threads that can be assigned to updates a little.
        private static readonly HashSet<Variable> _Updates = new HashSet<Variable>();
        private static readonly Heap<Variable> _UpdateHeap = new Heap<Variable>((v) => v._Sources.Count);
        private static int _UpdatesRunning = 0;
        private static readonly object _WorkLock = new object();

        private void UpdateListeners()
        {
            List<Task> tasks = new List<Task>();
            _StructureLock.EnterReadLock();
            try
            {
                lock (_WorkLock)
                {
                    foreach (Variable l in _Listeners)
                        if (_Updates.Add(l))
                            _UpdateHeap.Enqueue(l);
                    while (_UpdatesRunning < UPDATE_THREADS - 1)
                    {
                        Task t = Task.Run((Action)_DoUpdate);
                        _UpdatesRunning++;
                    }
                }
            }
            finally { _StructureLock.ExitReadLock(); }


            // Run in a task.
            void _DoUpdate()
            {
                while (true)
                {
                    Variable v;
                    lock (_WorkLock)
                    {
                        if (_UpdateHeap.Count == 0) { _UpdatesRunning--; return; }
                        v = _UpdateHeap.Dequeue();
                        _Updates.Remove(v);
                    }
                    v.UpdateValue();
                }
            }
        }

        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IContext context, string name, IEvaluateable contents = null)
        {
            this.Context = context;
            this.Name = name;
            this.Contents = contents ?? Dependency.Null.Instance;
        }


        public event ValueChangedHandler<IEvaluateable> ValueChanged;
        private void OnValueChanged(IEvaluateable oldValue, IEvaluateable newValue)
        {
            if (ValueChanged == null) return;
            ValueChanged(this, new ValueChangedArgs<IEvaluateable>(oldValue, newValue));
        }


        internal string GetExpressionString(IContext perspective)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Variable> GetTerms() => _Sources;

    }



    /// <summary>An exception thrown when an invalid circular dependency is added to a DependencyGraph.</summary>
    public class CircularDependencyException : InvalidOperationException
    {
        IEnumerable<Variable> Path;
        /// <summary>Creates a new CircularDependencyException.</summary>
        public CircularDependencyException(IEnumerable<Variable> path, string message = "Circular reference identified.") : base(message) { this.Path = path; }
    }

}
