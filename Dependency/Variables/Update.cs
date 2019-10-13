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
        private readonly ConcurrentQueue<IDynamicItem> _Items = new ConcurrentQueue<IDynamicItem>();
        private readonly ConcurrentQueue<Task> _Tasks = new ConcurrentQueue<Task>();
        internal readonly IVariableInternal Origin;
        public readonly IEvaluateable NewContents;


        private Update(IVariableInternal var, IEvaluateable newContents)
        {
            this.Origin = var;
            if (newContents == null) newContents = Dependency.Null.Instance;
            else if (newContents is Expression exp) newContents = exp.Contents;
            this.NewContents = newContents;
        }

        internal static Update ForVariableInternal(IVariableInternal var, IEvaluateable newContents) => new Update(var, newContents);

        public static Update ForVariable(IVariable var, IEvaluateable newContents) => new Update((IVariableInternal)var, newContents);

        public void Await()
        {
            while (_Tasks.TryDequeue(out Task t))
            {
                t.Wait();
            }   
        }

        /// <summary>
        /// Updates this object's <seealso cref="Update.Origin"/> with the given <seealso cref="Update.NewContents"/>.
        /// </summary>
        /// <returns>Returns whether any change is made to the contents.</returns>
        public void Execute() => UpdateStructure();



        /// <summary>
        /// Starts the structure update for the <seealso cref="Origin"/> with the given <seealso cref="NewContents"/>.
        /// </summary>
        /// <returns>Returns true if the contents would change; otherwise, returns false.</returns>
        internal bool UpdateStructure()
        {
            
             try
            {
                StructureLock.EnterUpgradeableReadLock();
                if (Origin.Contents.Equals(NewContents)) return false;
                

                HashSet<Reference> newRefs = new HashSet<Reference>(Helpers.GetReferences(NewContents));
                try
                {
                    StructureLock.EnterWriteLock();
                    
                    // Cut out all the old references and replace them with the new references
                    if (Origin.References != null)
                    {
                        foreach (Reference oldRef in Origin.References)
                            if (oldRef.Head is IVariableInternal v)
                                v.RemoveListener(oldRef);
                        foreach (Reference newRef in newRefs)
                            if (newRef.Head is IVariableInternal v)
                                v.AddListener(newRef);
                    }
                    Origin.References = newRefs;


                    // Update the contents tree.
                    if (Origin.Contents is IDynamicItem idi_before) idi_before.Parent = null;
                    if (NewContents is IDynamicItem idi_after) idi_after.Parent = (IDynamicItem)Origin;

                    Origin.SetContents(NewContents);
                }
                finally { StructureLock.ExitWriteLock(); }

                IEvaluateable newValue = Helpers.Recalculate(NewContents);
                if (!Origin.Update(newValue)) return true;
                foreach (var listener in Origin.GetListeners())
                    _Tasks.Enqueue(Task.Run(() => _UpdateItem(listener)));
                _UpdateItem(Origin.Parent);
            }
            finally { StructureLock.ExitUpgradeableReadLock();  }
            
            return true;


            void _UpdateItem(IDynamicItem idi)
            {
                while (idi != null)
                {
                    IEvaluateable forcedValue = (idi.Equals(this.Origin)) ? new CircularityError(this.Origin) : null;
                    if (!idi.Update(forcedValue)) return;
                    if (idi is IVariableInternal iv)
                        foreach (var listener in iv.GetListeners())
                            _Tasks.Enqueue(Task.Run(() => _UpdateItem(listener)));
                    idi = idi.Parent;
                }
            }
        }



        private void UpdateOrigin(IEvaluateable newValue)
        {
            if (!Origin.Update(newValue)) return;
            foreach (var listener in Origin.GetListeners())
                _Tasks.Enqueue(Task.Run(() => _UpdateItem(listener)));
            _UpdateItem(Origin.Parent);

            void _UpdateItem(IDynamicItem idi)
            {
                while (idi != null)
                {
                    IEvaluateable forcedValue = (idi.Equals(this.Origin)) ? new CircularityError(this.Origin) : null;
                    if (!idi.Update(forcedValue)) return;
                    if (idi is IVariableInternal iv)
                        foreach (var listener in iv.GetListeners())
                            _Tasks.Enqueue(Task.Run(() => _UpdateItem(listener)));
                    idi = idi.Parent;
                }
            }
        }
        

    }
}
