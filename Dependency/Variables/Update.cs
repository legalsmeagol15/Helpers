﻿using DataStructures;
using Dependency.Functions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

namespace Dependency.Variables
{
    public sealed class Update
    {
        public static readonly ITrueSet<IEvaluateable> UniversalSet = NumberIntIntervalSet.Infinite();

        // Like a SQL transaction
        internal static readonly ReaderWriterLockSlim StructureLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly ConcurrentQueue<ISyncUpdater> _Items = new ConcurrentQueue<ISyncUpdater>();
        private readonly ConcurrentQueue<Task> _Tasks = new ConcurrentQueue<Task>();
        internal readonly IVariable Starter;
        public readonly IEvaluateable NewContents;
        private readonly IEnumerable<IEvaluateable> Indices = null;

        private static long _Updating = 0;
        public static long Updating => Interlocked.Read(ref _Updating);


        private Update(IVariable var, IEvaluateable newContents, IEnumerable<IEvaluateable> indices)
        {
            this.Starter = var ?? throw new ArgumentNullException(nameof(var));
            if (newContents == null) newContents = Dependency.Null.Instance;
            else if (newContents is Expression exp) newContents = exp.Contents;
            this.NewContents = newContents;
            this.Indices = indices;
        }

        /// <summary>Creates an updating transaction for the given <seealso cref="Variable"/>.</summary>
        /// <param name="var">The <seealso cref="Variable"/> whose value and listeners will need 
        /// to be updated.</param>
        /// <param name="newContents">The new contents for the <seealso cref="Variable"/>.  If 
        /// provided null, the <seealso cref="Variable"/> will not have its 
        /// <seealso cref="Variable.Contents"/> changed, but its <seealso cref="Variable.Value"/> 
        /// and all listeners will be updated.
        /// </param>
        public static Update ForVariable(IVariable var, IEvaluateable newContents = null) => new Update(var, newContents, null);

        public static Update ForCollection(IVariable var, params IEvaluateable[] indices) => new Update(var, null, indices);



        /// <summary>
        /// Updates this object's <seealso cref="Update.Starter"/> with the given <seealso cref="Update.NewContents"/>.
        /// This method is run for the starting <seealso cref="IVariable"/>, but also for each listener of that 
        /// <seealso cref="IVariable"/>, and so on.
        /// </summary>
        /// <returns>Returns whether any change is made to the value of the <seealso cref="Update.Starter"/>.</returns>
        public bool Execute(bool checkCircularity = true)
        {
            try
            {
                StructureLock.EnterUpgradeableReadLock();

                ITrueSet<IEvaluateable> indices = UniversalSet;
                if (Starter is IUpdatedVariable iuv)
                {
                    // If the new contents equal the old contents, it can't possibly matter.
                    if (iuv.Contents.Equals(NewContents)) return false;

                    // Evaluate the new contents.  This will potentially establish a reference between NewContents and 
                    // existing variables.
                    IEvaluateable newValue = Helpers.Recalculate(NewContents);

                    // Update the iuv's new contents.
                    StructureLock.EnterWriteLock();
                    try
                    {
                        // Update the content's parent pointer.
                        if (iuv.Contents is ISyncUpdater idi_before) idi_before.Parent = null;
                        if (NewContents is ISyncUpdater idi_after) idi_after.Parent = (ISyncUpdater)iuv;

                        // Commit the new contents
                        if (!iuv.CommitContents(NewContents))
                            throw new InvalidOperationException("Invalid contents: " + NewContents.ToString());
                    }
                    finally { StructureLock.ExitWriteLock(); }

                    // If the iuv is now part of a circularity, the new value will be a CircularityError.
                    // TODO:  should I check TryFindDependency(newValue, Starter, out var path) separately?
                    if (checkCircularity && Helpers.TryFindDependency(NewContents, Starter, out var path))
                        newValue = new CircularityError(iuv, path);

                    // If the new value is no different from the old value, no need to update listeners.
                    indices = iuv.CommitValue(newValue);
                    if (indices == null || indices.IsEmpty) return false;                    
                }

                // The value must have changed.  If Starter updates synchronously,  get the 
                // synchronous update started, but we DON'T CALL STARTER'S UPDATE() method because 
                // we might have forced the value to be a circularity error.
                if (Starter is ISyncUpdater isu)
                    _Execute(isu, isu.Parent, indices);

                // Finally, if Starter updates asynchronously, kick off the asynchronous update.
                if (Starter is IAsyncUpdater iau)
                {
                    foreach (var listener in iau.GetListeners())
                        Enqueue(iau, listener, indices);
                }
            }
            finally { StructureLock.ExitUpgradeableReadLock(); }

            // Force all the tasks to finish.  StructureLock should not be held.
            while (_Tasks.TryDequeue(out Task t))
            {
                t.Wait();
                if (t.Exception != null)
                    throw new Exception("TODO:  a meaningful  message", t.Exception);
            }

            // Done.
            return true;
        }

        internal void Enqueue(IAsyncUpdater source, ISyncUpdater listener, ITrueSet<IEvaluateable> indices) // TODO:  this should be done within the StructureLock read lock?
        {
            Interlocked.Increment(ref _Updating);
            _Tasks.Enqueue(Task.Run(() => _Execute(source as ISyncUpdater, listener, indices)));
        }

        /// <summary>Executes this <see cref="Update"/> for the given 
        /// <seealso cref="ISyncUpdater"/> from the perspective of the given 
        /// <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The item called which executed the update for the 
        /// <paramref name="target"/>.  This  may be an <seealso cref="ISyncUpdater"/>, an 
        /// <seealso cref="IAsyncUpdater"/>, or an object that implements both.</param>
        /// <param name="target">The item which will be updated.  The <paramref name="target"/>'s 
        /// Parent will be the next item updated, and so on.</param>
        /// <param name="indices">The domain indexes that were updated below.</param>
        /// <returns>Returns true if any item's value was changed; otherwise, returns false.
        /// </returns>
        private bool _Execute(object source, ISyncUpdater target, ITrueSet<IEvaluateable> indices)
        {
            if (indices == null || indices.IsEmpty) return false;
            ISyncUpdater first_target = target, child = source as ISyncUpdater;
            bool result = true;
            while (target is ISyncUpdater parent)
            {
                var newIndices = indices;
                // Indexers get first crack at updating.
                if (parent is IIndexedUpdater indexed_parent)
                {
                    indices = indexed_parent.UpdateIndexed(this, child, indices);                    
                }
                else if (!indices.IsUniversal)
                {
                    // This is here just as a clue that implementation isn't right.
                    Debug.Fail("Non-universal indices shouldn't be handled by " + target.GetType().Name);
                }
                else
                {
                    // Most cases will be non-indexers.
                    indices = parent.Update(this, child);                    
                }

                if (indices == null || indices.IsEmpty)
                    break;

                // We're done with updating the target.  Since target was updated, enqueue its 
                // listeners and proceed.
                if (target is IAsyncUpdater iv)
                    foreach (var listener in iv.GetListeners())
                        Enqueue(iv, listener, indices);
                child = target;
                target = target.Parent;
            }

            result = first_target == null ? target != null : !first_target.Equals(target);
            Interlocked.Decrement(ref _Updating);
            return result;
        }

        /// <summary>Manages the thread access to a set of listeners.</summary>
        internal sealed class ListenerManager : IEnumerable<ISyncUpdater>
        {
            // This can NOT be the weak-weak pattern (a WeakReference<WeakReferenceSet<...>>).
            private readonly WeakReferenceSet<ISyncUpdater> _Listeners = new WeakReferenceSet<ISyncUpdater>();

            public int Count => _Listeners.Count;
            private WeakReferenceSet<ISyncUpdater> GetListeners() => _Listeners;
            public bool Add(ISyncUpdater listener)
            {
                StructureLock.EnterWriteLock();
                try { return _Listeners.Add(listener); }
                finally { StructureLock.ExitWriteLock(); }
            }
            public bool Remove(ISyncUpdater listener)
            {
                StructureLock.EnterWriteLock();
                try
                { return _Listeners.Remove(listener); }
                finally { StructureLock.ExitWriteLock(); }
            }

            IEnumerator<ISyncUpdater> IEnumerable<ISyncUpdater>.GetEnumerator() => _Listeners.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _Listeners.GetEnumerator();
        }

        /// <summary>
        /// Refactors all references to the given <paramref name="context"/> to the new string 
        /// <paramref name="after"/>.
        /// </summary>
        public static int Refactor(IContext context, string after)
        {
            int count = 0;
            StructureLock.EnterWriteLock();
            try
            {
                // We must take any properties of the given context, or of its sub-contexts, check 
                // if those properties have listening references, and then refactor each of those 
                // references.
                HashSet<object> visited = new HashSet<object>();
                HashSet<Reference> refs = new HashSet<Reference>();
                Stack<object> stack = new Stack<object>();
                stack.Push(context);
                while (stack.Count > 0)
                {
                    object focus = stack.Pop();
                    if (!visited.Add(focus))
                        continue;
                    foreach (System.Reflection.FieldInfo fInfo in focus.GetType().GetFields(System.Reflection.BindingFlags.Instance
                                                                                            | System.Reflection.BindingFlags.Public
                                                                                            | System.Reflection.BindingFlags.NonPublic))
                    {
                        object val = fInfo.GetValue(focus);

                        // If the property is a subcontext, that subcontext might have listener 
                        // References that name the given context.
                        if (val is IContext sub_ctxt)
                            stack.Push(sub_ctxt);

                        // Any property that has async listeners might have References naming the 
                        // given context.
                        if (val is IAsyncUpdater iau)
                            foreach (var listener in iau.GetListeners().OfType<Reference>())
                                refs.Add(listener);
                    }
                }

                // Now that we know which listening References exist, tell them to refactor the 
                // given context.
                foreach (Reference r in refs)
                    if (r.Refactor(context, after))
                        count++;

                // Don't know who might want this, but it's data that we have.
                return count;
            }
            finally { StructureLock.ExitWriteLock(); }
        }
    }
}
