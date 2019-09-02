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

namespace Dependency.Variables
{
    

    public sealed class Variable : IDynamicItem, IVariable, IEvaluateable
    // DO NOT implement IDisposable to clean up listeners.  The listeners will expire via garbage collection.
    {
        // A variable is apt to have few sources, but many listeners (0 or 100,000 might be reasonable).

        IDynamicItem IDynamicItem.Parent { get => null; set => throw new InvalidOperationException(); }
        private readonly WeakReferenceSet<Reference> _Listeners = new WeakReferenceSet<Reference>();
        private IEvaluateable _Value = Null.Instance;  // Must be guaranteed never to be CLR null
        private readonly ReaderWriterLockSlim _ValueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private static readonly ReaderWriterLockSlim _StructureLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        bool IVariable.AddListener(Reference r) => _Listeners.Add(r);
        bool IVariable.RemoveListener(Reference r) => _Listeners.Remove(r);
        private HashSet<Reference> _References = new HashSet<Reference>();
        IEnumerable<Reference> IVariable.GetReferences() => _References;

        internal IEnumerable<Reference> GetReferences()
        {
            if (Contents == null) yield break;
            Stack<IEvaluateable> stack = new Stack<IEvaluateable>();
            stack.Push(this.Contents);
            while (stack.Count > 0)
            {
                IEvaluateable focus = stack.Pop();
                switch (focus)
                {
                    case IExpression exp: stack.Push(exp.Contents); continue;
                    case IFunction f: foreach (var input in f.Inputs) stack.Push(f); continue;
                    case Reference r: yield return r; continue;
                }

            }
        }
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
                if (value is Expression exp) value = exp.Contents;
                SetContents(value, 0);
            }
        }


        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IEvaluateable contents = null)
        {
            this.Contents = contents;
        }

        public void SetContents(IEvaluateable newContents, int modKey = 0)
        {
            if (newContents == null) newContents = Dependency.Null.Instance;

            // First, update the structure-related variables (contents and sources).
            HashSet<Reference> newRefs = new HashSet<Reference>(Helpers.GetReferences(newContents));
            if (TryFindCircularity(this, newRefs, out Deque<IVariable> path))
                throw new CircularDependencyException(path);
            _StructureLock.EnterUpgradeableReadLock();  // Lock 'a' because the structure shouldn't change while we examine it.
            try
            {
                ISet<Reference> oldRefs = _References;

                //if (TryFindCircularity(this, newSources, out Deque<IVariable> path)) throw new CircularDependencyException(path);
                _StructureLock.EnterWriteLock();
                try
                {
                    foreach (Reference oldRef in oldRefs)
                    {
                        if (!newRefs.Contains(oldRef) && oldRef.HeadProperty != null && oldRef.HeadProperty is IVariable v)
                            v.RemoveListener(oldRef);
                    }
                    foreach (Reference newRef in newRefs)
                        if (!oldRefs.Contains(newRef) && newRef != null && newRef.HeadProperty is IVariable v)
                            v.AddListener(newRef);
                    _References = newRefs;
                    if (_Contents is IDynamicItem idi_before) idi_before.Parent = null;
                    _Contents = newContents;
                    if (_Contents is IDynamicItem idi_after) idi_after.Parent = this;
                }
                finally { _StructureLock.ExitWriteLock(); }
            }
            catch (CircularDependencyException) { throw; }
            finally { _StructureLock.ExitUpgradeableReadLock(); }

            // Second, update the changed value
            Update();
        }

        public bool Update()
        {
            // This method makes the guess that most updates will NOT change the value.
            IEvaluateable oldValue, newValue;
            _ValueLock.EnterUpgradeableReadLock();
            _StructureLock.EnterReadLock();
            try
            {
                // Presume that a source Variable is already updated, since this method will be 
                // called a lot from the sources and we don't want an exponential function.
                newValue = _Contents.Value;
                if (newValue == null) newValue = Dependency.Null.Instance;

                // Rule out identical values, which should not invoke any further action.
                oldValue = _Value;
                if (oldValue.Equals(newValue))
                    return false;

                // Update the value.
                _ValueLock.EnterWriteLock();
                _Value = newValue;
                _ValueLock.ExitWriteLock();
                FireValueChanged(oldValue, newValue);

                //Now update the listeners
                List<Task> tasks = new List<Task>();
                foreach (Reference r in _Listeners)
                    tasks.Add(Task.Run(() => r.Update()));
                Task.WaitAll(tasks.ToArray());
            }
            finally { _StructureLock.ExitReadLock(); _ValueLock.ExitUpgradeableReadLock(); }

            return true;


        }

        private static bool TryFindCircularity(IVariable target, IEnumerable<Reference> startRefs, out Deque<IVariable> path)
        {
            HashSet<IVariable> visited = new HashSet<IVariable>();
            Queue<Node> queue = new Queue<Node>();
            foreach (var r in startRefs)
                if (r.HeadProperty is IVariable iv)
                    queue.Enqueue(new Node { Item = iv, Refs = iv.GetReferences(), Prior = null });
            while (queue.Count > 0)
            {
                Node n = queue.Dequeue();
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
                        if (r.HeadProperty is IVariable iv)
                            queue.Enqueue(new Node { Item = iv, Refs = iv.GetReferences(), Prior = n });
                }
            }
            path = null;
            return false;
        }
        private class Node
        {
            public IVariable Item;
            public IEnumerable<Reference> Refs;
            public Node Prior;
        }
        //private static bool TryFindCircularity(Variable target,
        //                                        IEnumerable<IVariable> sources,
        //                                        out Deque<IVariable> path)
        //{
        //    // TODO:  use a Stack<> object instead of stack frames, because I get a stack overflow at approx. 5000 levels deep
        //    // TODO:  I'm not caching the sources anywhere, so it would optimize this method to cache the variables' sources.
        //    if (sources != null)
        //    {
        //        foreach (IVariable src in sources)
        //        {
        //            if (ReferenceEquals(target, src)) { path = new Deque<IVariable>(); path.AddFirst(src); return true; }
        //            else if (TryFindCircularity(target, Helpers.GetTerms(src), out path)) { path.AddFirst(src); return true; }
        //        }
        //    }
        //    path = null;
        //    return false;
        //}



        public override string ToString() => Contents.ToString() + " = " + Value.ToString();

        public event ValueChangedHandler<IEvaluateable> ValueChanged;
        private void FireValueChanged(IEvaluateable oldValue, IEvaluateable newValue)
            => ValueChanged?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, newValue));


    }




    /// <summary>An exception thrown when an invalid circular dependency is added to a DependencyGraph.</summary>
    internal class CircularDependencyException : InvalidOperationException
    {
        IEnumerable<IVariable> Path;
        /// <summary>Creates a new CircularDependencyException.</summary>
        public CircularDependencyException(IEnumerable<IVariable> path, string message = "Circular reference identified.") : base(message) { this.Path = path; }
    }

}
