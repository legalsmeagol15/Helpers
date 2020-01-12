using DataStructures;
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
        // Like a SQL transaction

        internal static readonly ReaderWriterLockSlim StructureLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly ConcurrentQueue<ISyncUpdater> _Items = new ConcurrentQueue<ISyncUpdater>();
        private readonly ConcurrentQueue<Task> _Tasks = new ConcurrentQueue<Task>();
        private IVariable Starter;
        public readonly IEvaluateable NewContents;
        private IEnumerable<IEvaluateable> Indices = null;


        private Update(IVariable var, IEvaluateable newContents, IEnumerable<IEvaluateable> indices)
        {
            this.Starter = var ?? throw new ArgumentNullException("var");
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
        /// <param name="indices">Optional.  The indices of the <paramref name="var"/> that were 
        /// updated.</param>
        internal static Update ForVariable(IUpdatedVariable var, IEvaluateable newContents, IEnumerable<IEvaluateable> indices = null) 
            => new Update(var, newContents, indices);

        public static Update ForVariable(IVariable var, params IEvaluateable[] indices) => new Update(var, null, indices);

        public static Update ForVariable(IVariable var) => new Update(var, null, null);

        /// <summary>
        /// Updates this object's <seealso cref="Update.Starter"/> with the given <seealso cref="Update.NewContents"/>.
        /// </summary>
        /// <returns>Returns whether any change is made to the value of the <seealso cref="Update.Starter"/>.</returns>
        public bool Execute()
        {
            try
            {
                StructureLock.EnterUpgradeableReadLock();

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
                        // Update the contents tree.  The synchronous relations must be updated by hand, the asynchronous 
                        // relations will be wrapped up automatically by the garbage collection.
                        if (iuv.Contents is ISyncUpdater idi_before) idi_before.Parent = null;
                        if (NewContents is ISyncUpdater idi_after) idi_after.Parent = (ISyncUpdater)iuv;
                        iuv.SetContents(NewContents);
                    }
                    finally { StructureLock.ExitWriteLock(); }

                    // If the iuv is now part of a circularity, the new value will be a CircularityError
                    if (Helpers.TryFindCircularity(iuv))
                        newValue = new CircularityError(iuv);

                    // If the new value won't change the old value, no need to update listeners.
                    if (!iuv.SetValue(newValue))
                        return false;
                }

                // The value must have changed.  If Starter updates synchronously,  get the synchronous update 
                // started.
                if (Starter is ISyncUpdater isu)
                    Execute(Starter as IAsyncUpdater, isu.Parent);

                // Finally, if Starter updates asynchronously, kick off the asynchronous update.
                if (Starter is IAsyncUpdater iau)
                {
                    foreach (var listener in iau.GetListeners())
                        Enqueue(iau, listener);
                }

                // Force all the tasks to finish while the StructureLock is held.
                while (_Tasks.TryDequeue(out Task t))
                {
                    t.Wait();
                }
            }
            finally { StructureLock.ExitUpgradeableReadLock(); }

            // Done.
            return true;
        }

        internal void Enqueue(IAsyncUpdater source, ISyncUpdater listener) // TODO:  this should be done within the StructureLock read lock?
            => _Tasks.Enqueue(Task.Run(() => Execute(source, listener)));

        /// <summary>Executes this <see cref="Update"/> for the given 
        /// <seealso cref="ISyncUpdater"/> from the perspective of the given 
        /// <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The item called which executed the update for the 
        /// <paramref name="target"/>.  This  may be an <seealso cref="ISyncUpdater"/>, an 
        /// <seealso cref="IAsyncUpdater"/>, or an object that implements both.</param>
        /// <param name="target">The item which will be updated.  The <paramref name="target"/>'s 
        /// Parent will be the next item updated, and so on.</param>
        /// <returns>Returns true if any item's value was changed; otherwise, returns false.
        /// </returns>
        private bool Execute(object source, ISyncUpdater target)
        {
            ISyncUpdater start = target;
            ISyncUpdater updatedChild = source as ISyncUpdater;
            while (target != null)
            {
                // If nothing was updated, return false.
                if (!target.Update(this, updatedChild))
                    return !target.Equals(start);

                // Since target was updated, enqueue its listeners and proceed.
                if (target is IAsyncUpdater iv)
                    foreach (var listener in iv.GetListeners())
                        Enqueue(iv, listener);
                updatedChild = target;
                target = target.Parent;
            }
            return true;
        }

        /// <summary>Manages the lifetime and thread access to a set of listeners.</summary>
        internal sealed class ListenerManager : IEnumerable<ISyncUpdater>
        {
            private static readonly ISyncUpdater[] _Empty = new ISyncUpdater[0];
            private WeakReference<WeakReferenceSet<ISyncUpdater>> _ListenersRef = null;
            private WeakReferenceSet<ISyncUpdater> GetListeners()
            {
                WeakReferenceSet<ISyncUpdater> listeners;
                if (_ListenersRef == null)
                    _ListenersRef = new WeakReference<WeakReferenceSet<ISyncUpdater>>(listeners = new WeakReferenceSet<ISyncUpdater>());
                else if (!_ListenersRef.TryGetTarget(out listeners))
                    _ListenersRef.SetTarget(listeners = new WeakReferenceSet<ISyncUpdater>());
                return listeners;
            }
            public bool Add(ISyncUpdater listener)
            {
                StructureLock.EnterWriteLock();
                try { return GetListeners().Add(listener); }
                finally { StructureLock.ExitWriteLock(); }
            }
            public bool Remove(ISyncUpdater listener)
            {
                StructureLock.EnterWriteLock();
                try
                {
                    if (_ListenersRef == null) return false;
                    if (!_ListenersRef.TryGetTarget(out var listeners)) return false;
                    return listeners.Remove(listener);
                }
                finally { StructureLock.ExitWriteLock(); }
            }

            IEnumerator<ISyncUpdater> IEnumerable<ISyncUpdater>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            private IEnumerator<ISyncUpdater> GetEnumerator()
            {
                if (_ListenersRef == null) return (IEnumerator<ISyncUpdater>)_Empty.GetEnumerator();
                if (!_ListenersRef.TryGetTarget(out var listeners)) return (IEnumerator<ISyncUpdater>)_Empty.GetEnumerator();
                return listeners.GetEnumerator();
            }
        }
    }
}
