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
using Dependency.Functions;

namespace Dependency.Variables
{
    public class Variable : IDynamicItem, IVariableAsync
    {
        // DO NOT implement IDisposable to clean up listeners.  The listeners will expire via garbage collection.
        // Also, References clean themselves up from their sources through their own implementation  of 
        // IDisposable.

        // A variable is apt to have few sources, but many listeners (0 or 100,000 might be reasonable).

        private readonly WeakReferenceSet<IDynamicItem> _Listeners = new WeakReferenceSet<IDynamicItem>();
        private IEvaluateable _Value = Null.Instance;  // Must be guaranteed never to be CLR null        
        private readonly ReaderWriterLockSlim _ValueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        internal static readonly ReaderWriterLockSlim StructureLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

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
                StructureLock.EnterReadLock();
                try { return _Contents; }
                finally { StructureLock.ExitReadLock(); }
            }
            set
            {
                UpdateStructure(this, value);
                _Contents = value;
                Helpers.Recalculate(_Contents);
                UpdateValueAsync(this);
            }
        }


        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IEvaluateable contents = null) { this.Contents = contents ?? Null.Instance; }

        

        internal static void UpdateStructure(IVariable var, IEvaluateable newContents)
        {
            if (newContents == null) newContents = Dependency.Null.Instance;
            else if (newContents is Expression exp) newContents = exp.Contents;
            IDynamicItem idi_var = var as IDynamicItem;

            // Check for circular reference.
            HashSet<Reference> newRefs = new HashSet<Reference>(Helpers.GetReferences(newContents));
            try
            {
                StructureLock.EnterUpgradeableReadLock();
                if (TryFindCircularity(var, newRefs, out Deque<IVariable> path))
                    throw new CircularDependencyException(path);
                ISet<Reference> oldRefs = var.References;
                try
                {
                    StructureLock.EnterWriteLock();

                    // Ensure the reference list (the refs this variable  listens to) are up-to-date.
                    if (oldRefs != null)
                    {
                        foreach (Reference oldRef in oldRefs)
                            if (!newRefs.Contains(oldRef) && oldRef.Head is IVariable v)
                                v.RemoveListener(oldRef);
                        foreach (Reference newRef in newRefs)
                            if (!oldRefs.Contains(newRef) && newRef.Head is IVariable v)
                                v.AddListener(newRef);
                    }


                    // Update references, contents, and evaluation trees.
                    var.References = newRefs;
                    if (var.Contents is IDynamicItem idi_before) idi_before.Parent = null;
                    if (newContents is IDynamicItem idi_after) idi_after.Parent = idi_var;
                }
                finally { StructureLock.ExitWriteLock(); }

            }
            catch (CircularDependencyException) { throw; }
            finally { StructureLock.ExitUpgradeableReadLock(); }
        }

        internal static bool UpdateValueAsync(IVariableAsync var, bool updateListeners = true)
        {
            try
            {
                var.ValueLock.EnterUpgradeableReadLock();
                StructureLock.EnterReadLock();

                // Presume that a source Variable is already updated in its structure and its contents, since this 
                // method will be called a lot from the sources and we don't want an exponential function.
                IEvaluateable newValue = var.Contents.Value;
                if (newValue == null) newValue = Dependency.Null.Instance;

                // Rule out identical values.  If identical, return false so no further action will be invoked.
                IEvaluateable oldValue = var.Value;
                if (newValue.Equals(oldValue)) return false;

                // Set the value.
                var.SetValue(newValue);

                // Fire the event.
                var.FireValueChanged(oldValue, newValue);

                //Now update the listeners asynchronously
                if (updateListeners)
                {
                    List<Task> tasks = new List<Task>();
                    foreach (IDynamicItem idi in var.GetListeners())
                        tasks.Add(Task.Run(() => UpdateListener(idi)));
                    Task.WaitAll(tasks.ToArray());
                }
            }
            finally { StructureLock.ExitReadLock(); var.ValueLock.ExitUpgradeableReadLock(); }

            return true;
        }
        private static void UpdateListener(IDynamicItem listener)
        {
            while (listener != null && listener.Update()) listener = listener.Parent;
        }

        private static bool TryFindCircularity(IVariable target, IEnumerable<Reference> startRefs, out Deque<IVariable> path)
        {
            HashSet<IVariable> visited = new HashSet<IVariable>();

            // Start off the queue with the indicated items.
            Queue<CircularSearchNode> queue = new Queue<CircularSearchNode>();
            foreach (var r in startRefs)
                if (r.Head is IVariable iv)
                    queue.Enqueue(new CircularSearchNode { Item = iv, Refs = iv.References, Prior = null });

            // Now search.
            while (queue.Count > 0)
            {
                CircularSearchNode n = queue.Dequeue();
                if (!visited.Add(n.Item))
                    continue;
                else if (ReferenceEquals(n.Item, target))
                {
                    path = new Deque<IVariable>();
                    while (n != null) { path.AddFirst(n.Item); n = n.Prior; }
                    path.AddFirst(target);
                    return true;
                }
                else
                {
                    foreach (var r in n.Refs)
                        if (r.Head is IVariable iv)
                            queue.Enqueue(new CircularSearchNode { Item = iv, Refs = iv.References, Prior = n });
                }
            }
            path = null;
            return false;
        }
        private class CircularSearchNode
        {
            public IVariable Item;
            public IEnumerable<Reference> Refs;
            public CircularSearchNode Prior;
        }


        public override string ToString()
        {
            if (Contents.Equals(Value)) return "{Variable} = " + Value.ToString();
            return Contents.ToString() + " = " + Value.ToString();
        }

        public event ValueChangedHandler<IEvaluateable> ValueChanged;
        


        bool IVariable.AddListener(IDynamicItem idi) => _Listeners.Add(idi);
        bool IVariable.RemoveListener(IDynamicItem idi) => _Listeners.Remove(idi);
        IEnumerable<IDynamicItem> IVariable.GetListeners() => _Listeners;
        ISet<Functions.Reference> IVariable.References { get; set; }
        void IVariable.SetValue(IEvaluateable value) => Value = value;
        void IVariable.FireValueChanged(IEvaluateable oldValue, IEvaluateable newValue)
            => ValueChanged?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, newValue));
        ReaderWriterLockSlim IVariableAsync.ValueLock => _ValueLock;
        IDynamicItem IDynamicItem.Parent { get; set; }
        bool IDynamicItem.Update() =>UpdateValueAsync(this);


    }




    /// <summary>An exception thrown when an invalid circular dependency is added to a DependencyGraph.</summary>
    internal class CircularDependencyException : InvalidOperationException
    {
        IEnumerable<IVariable> Path;
        /// <summary>Creates a new CircularDependencyException.</summary>
        public CircularDependencyException(IEnumerable<IVariable> path, string message = "Circular reference identified.") : base(message) { this.Path = path; }
    }

}
