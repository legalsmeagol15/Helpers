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
        internal readonly IVariableInternal Starter;
        public readonly IEvaluateable NewContents;


        private Update(IVariableInternal var, IEvaluateable newContents)
        {
            this.Starter = var;
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
        /// Updates this object's <seealso cref="Update.Starter"/> with the given <seealso cref="Update.NewContents"/>.
        /// </summary>
        /// <returns>Returns whether any change is made to the contents.</returns>
        public void Execute() => UpdateStructure();

        internal bool TryFindCircularity(IVariable target)
        {
            // This method should be called with StructureLock readlock already engaged.

            Stack<IVariable> stack = new Stack<IVariable>();
            HashSet<IVariable> visited = new HashSet<IVariable>();
            stack.Push(target);
            visited.Add(target);
            while (stack.Count > 0)
            {
                IVariable v = stack.Pop();
                foreach (Reference r in Helpers.GetReferences(v))
                {
                    throw new NotImplementedException();
                }
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts the structure update for the <seealso cref="Starter"/> with the given <seealso cref="NewContents"/>.
        /// </summary>
        /// <returns>Returns true if the contents would change; otherwise, returns false.</returns>
        internal bool UpdateStructure()
        {
            
             try
            {
                StructureLock.EnterUpgradeableReadLock();
                if (Starter.Contents.Equals(NewContents)) return false;
                

                HashSet<Reference> newRefs = new HashSet<Reference>(Helpers.GetReferences(NewContents));
                try
                {
                    StructureLock.EnterWriteLock();
                    
                    // Cut out all the old references and replace them with the new references
                    if (Starter.References != null)
                    {
                        foreach (Reference oldRef in Starter.References)
                            if (oldRef.Head is IVariableInternal v)
                                v.RemoveListener(oldRef);
                        foreach (Reference newRef in newRefs)
                            if (newRef.Head is IVariableInternal v)
                                v.AddListener(newRef);
                    }
                    Starter.References = newRefs;


                    // Update the contents tree.
                    if (Starter.Contents is IDynamicItem idi_before) idi_before.Parent = null;
                    if (NewContents is IDynamicItem idi_after) idi_after.Parent = (IDynamicItem)Starter;

                    Starter.SetContents(NewContents);
                }
                finally { StructureLock.ExitWriteLock(); }

                IEvaluateable newValue = Helpers.Recalculate(NewContents);
                if (!Starter.Update(null)) return true;
                foreach (var listener in Starter.GetListeners())
                    _Tasks.Enqueue(Task.Run(() => _UpdateItem(Starter, listener)));
                _UpdateItem(null,Starter.Parent);
            } 
            finally { StructureLock.ExitUpgradeableReadLock();  }
            
            return true;


            void _UpdateItem(IVariableInternal source, IDynamicItem idi)
            {
                IDynamicItem updatedChild = null;
                while (idi != null)
                {
                    IEvaluateable forcedValue = (idi.Equals(this.Starter)) ? new CircularityError(this.Starter) : null;
                    if (!idi.Update(updatedChild)) return;
                    if (idi is IVariableInternal iv)
                        foreach (var listener in iv.GetListeners())
                            _Tasks.Enqueue(Task.Run(() => _UpdateItem(iv, listener)));
                    updatedChild = idi;
                    idi = idi.Parent;
                }
            }
        }


        
        

    }
}
