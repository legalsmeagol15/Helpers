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
        /// <returns>Returns whether any change is made to the contents.</returns>
        public void Execute() => UpdateStructure();
        

        /// <summary>
        /// Starts the structure update for the <seealso cref="Starter"/> with the given <seealso cref="NewContents"/>.
        /// </summary>
        /// <returns>Returns true if the contents would change; otherwise, returns false.</returns>
        private bool UpdateStructure()
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

                // IF the Starter is now part of a circularity, set the Starter's value to an error (unless it's already an error).
                if (Helpers.TryFindCircularity(Starter))
                {
                    if (Starter.Value is CircularityError ce && ce.Origin.Equals(Starter)) return true;
                    Starter.SetError(new CircularityError(Starter));
                }

                // IF Starter updates synchronously,  get that started (and update asynchronously if appropriate).
                else if (Starter is ISyncUpdater isu)
                {
                    if (!isu.Update(null)) return true;
                    if (Starter is IAsyncUpdater iau) // (check this here so it can be done BEFORE isu.Parent)
                    {
                        foreach (var listener in iau.GetListeners())
                            _Tasks.Enqueue(Task.Run(() => _UpdateItem(iau, listener)));
                    }
                    _UpdateItem(null, isu.Parent);
                    return true;
                }

                // Finally, if Starter only updates asynchronously, kick off the asynchronous update.
                else if (Starter is IAsyncUpdater iau)
                {
                    foreach (var listener in iau.GetListeners())
                        _Tasks.Enqueue(Task.Run(() => _UpdateItem(iau, listener)));
                }
            }
            finally { StructureLock.ExitUpgradeableReadLock(); }

            // Done.
            return true;


            void _UpdateItem(IAsyncUpdater source, ISyncUpdater isu)
            {
                ISyncUpdater updatedChild = null;
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
