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

namespace Dependency.Variables
{
    public sealed class Update
    {
        // Like a SQL transaction
        
        internal static readonly ReaderWriterLockSlim StructureLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly ConcurrentQueue<ISyncUpdater> _Items = new ConcurrentQueue<ISyncUpdater>();
        private readonly ConcurrentQueue<Task> _Tasks = new ConcurrentQueue<Task>();
        private IVariable_ Starter;
        public readonly IEvaluateable NewContents;


        private Update(IVariable_ var, IEvaluateable newContents)
        {
            this.Starter = var ?? throw new ArgumentNullException("var");
            if (newContents == null) newContents = Dependency.Null.Instance;
            else if (newContents is Expression exp) newContents = exp.Contents;
            this.NewContents = newContents;
        }
        
        public static Update ForVariable(IVariable var, IEvaluateable newContents) => new Update(var as IVariable_, newContents);

        public void Await()
        {
            while (_Tasks.TryDequeue(out Task t))
            {
                t.Wait();
            }   
        }

        /// <summary>
        /// Updates this object's <seealso cref="Update.Starter"/> with the given <seealso cref="Update.NewContents"/>.
        /// </summary>
        /// <returns>Returns whether any change is made to the value of the <seealso cref="Update.Starter"/>.</returns>
        public bool Execute() 
        {
            try
            {
                StructureLock.EnterUpgradeableReadLock();

                // If the new contents equal the old contents, it can't possibly matter.
                if (Starter.Contents.Equals(NewContents)) return false;

                // Evaluate the new contents.  This will potentially establish a reference between NewContents and 
                // existing variables.
                IEvaluateable newValue = Helpers.Recalculate(NewContents);

                // Update the Starter's new contents.
                StructureLock.EnterWriteLock();
                try
                {
                    // Update the contents tree.  The synchronous relations must be updated by hand, the asynchronous 
                    // relations will be wrapped up automatically by the garbage collection.
                    if (Starter.Contents is ISyncUpdater idi_before) idi_before.Parent = null;
                    if (NewContents is ISyncUpdater idi_after) idi_after.Parent = (ISyncUpdater)Starter;
                    Starter.SetContents(NewContents);
                }
                finally { StructureLock.ExitWriteLock(); }

                // If the Starter is now part of a circularity, the new value will be a CircularityError
                if (Helpers.TryFindCircularity(Starter))
                    newValue = new CircularityError(Starter);

                // If the new value won't change the old value, no need to update listeners.
                if (!Starter.SetValue(newValue))
                    return false;

                // The value must have changed.  If Starter updates synchronously,  get the synchronous update 
                // started.
                if (Starter is ISyncUpdater isu)
                    _UpdateItem(Starter as IAsyncUpdater, isu.Parent);

                // Finally, if Starter updates asynchronously, kick off the asynchronous update.
                if (Starter is IAsyncUpdater iau)
                {
                    foreach (var listener in iau.GetListeners())
                        _Tasks.Enqueue(Task.Run(() => _UpdateItem(iau, listener)));
                }
            }
            finally { StructureLock.ExitUpgradeableReadLock(); }

            // Done.
            return true;


            void _UpdateItem(object source, ISyncUpdater isu)
            {
                ISyncUpdater updatedChild = source as ISyncUpdater;
                while (isu != null)
                {
                    if (!isu.Update(updatedChild)) return;
                    if (isu is IAsyncUpdater iv)
                        foreach (var listener in iv.GetListeners())
                            _Tasks.Enqueue(Task.Run(() => _UpdateItem(iv, listener)));
                    updatedChild = isu;
                    isu = isu.Parent;
                }
            }
        }


        
        

    }
}
