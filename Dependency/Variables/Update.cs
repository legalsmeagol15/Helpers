using DataStructures;
using Dependency.Functions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    public sealed class Update
    {
        // Like a SQL transaction

        /// <summary>
        /// 
        /// </summary>
        internal static readonly ReaderWriterLockSlim StructureLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private ConcurrentQueue<Task> _Tasks = new ConcurrentQueue<Task>();
        internal readonly IVariableAsync Origin;
        private Update(IVariableAsync origin) { this.Origin = origin; }
        internal static Update ForValue(IVariableAsync origin) => new Update(origin);
        internal static Update ForContents(IVariableAsync variable, IEvaluateable newContents)
        {
            Update result = new Update(variable);
            result.UpdateStructure(newContents);
            return result;
        }


        internal void Await()        {            while (_Tasks.TryDequeue(out Task t)) t.Wait();        }

        public bool Execute(bool updateListeners = true) => UpdateValueAsync(updateListeners);



        internal void UpdateStructure(IEvaluateable newContents)
        {
            if (newContents == null) newContents = Dependency.Null.Instance;
            else if (newContents is Expression exp) newContents = exp.Contents;
            IDynamicItem idi_var = Origin as IDynamicItem;

            // Check for circular reference.
            HashSet<Reference> newRefs = new HashSet<Reference>(Helpers.GetReferences(newContents));
            try
            {
                StructureLock.EnterUpgradeableReadLock();
                if (TryFindCircularity(Origin, newRefs, out Deque<IVariable> path))
                    throw new CircularDependencyException(path);
                ISet<Reference> oldRefs = Origin.References;
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
                    Origin.References = newRefs;
                    if (Origin.Contents is IDynamicItem idi_before) idi_before.Parent = null;
                    if (newContents is IDynamicItem idi_after) idi_after.Parent = idi_var;
                    Origin.Contents = newContents;
                }
                finally { StructureLock.ExitWriteLock(); }

            }
            catch (CircularDependencyException) { throw; }
            finally { StructureLock.ExitUpgradeableReadLock(); }

            Recalculate(newContents);
        }
        
        internal bool UpdateValueAsync(bool updateListeners = true)
        {
            try
            {
                Origin.ValueLock.EnterUpgradeableReadLock();
                StructureLock.EnterReadLock();

                // Presume that a source Variable is already updated in its structure and its contents, since this 
                // method will be called a lot from the sources and we don't want an exponential function.
                IEvaluateable newValue = Origin.Contents.Value;
                if (newValue == null) newValue = Dependency.Null.Instance;

                // Rule out identical values.  If identical, return false so no further action will be invoked.
                IEvaluateable oldValue = Origin.Value;
                if (newValue.Equals(oldValue)) return false;

                // Set the value.
                Origin.ValueLock.EnterWriteLock();
                Origin.SetValue(newValue);
                Origin.ValueLock.ExitWriteLock();

                // Fire the event.
                Origin.FireValueChanged(oldValue, newValue);

                //Now update the listeners asynchronously or synchronously, as requested
                if (updateListeners)
                    foreach (IDynamicItem idi in Origin.GetListeners())
                        _Tasks.Enqueue(Task.Run(() => UpdateListener(idi)));
            }
            finally { StructureLock.ExitReadLock(); Origin.ValueLock.ExitUpgradeableReadLock(); }

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



        public static IEvaluateable Recalculate(IEvaluateable ieval)
        {
            return _RecursiveRecalc(ieval);

            IEvaluateable _RecursiveRecalc(IEvaluateable focus)
            {
                if (focus is ILiteral) return focus;
                if (focus is IFunction ifunc) foreach (var input in ifunc.Inputs) _RecursiveRecalc(input);
                else if (focus is IExpression iexp) return _RecursiveRecalc(iexp.Contents);
                if (focus is IVariableAsync iva) { Update.ForValue(iva).Execute(false); return iva.Value; }
                else if (focus is IDynamicItem idi) idi.Update();
                return focus.Value;
            }
        }
    }
}
