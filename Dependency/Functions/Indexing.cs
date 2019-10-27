using Dependency.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Functions
{
    /// <summary>
    /// A function which takes a base indexable object (like a <seealso cref="Vector"/>, or a 
    /// <seealso cref="Reference"/> which points to a property <seealso cref="IAsyncUpdater"/>), and an ordinal 
    /// value 'n', and returns the 'nth' item associated with that base object.
    /// </summary>
    internal sealed class Indexing : IFunction
    {
        internal IEvaluateable[] Inputs;
        internal IEvaluateable Base => Inputs[0];
        internal IEvaluateable Ordinal => Inputs[1];
        internal IAsyncUpdater Head => Inputs.Length > 2 ? (IAsyncUpdater)Inputs[2] : null;
        private IEvaluateable _BaseValue, _CachedOrdinalValue; // Cache these because updates have to have StructureLock held

        public ISyncUpdater Parent { get; set; }

        IList<IEvaluateable> IFunction.Inputs => this.Inputs;

        public IEvaluateable Value { get; private set; } = Dependency.Null.Instance;


        bool ISyncUpdater.Update(ISyncUpdater updatedChild)
        {
            IEvaluateable newValue;

            Update.StructureLock.EnterUpgradeableReadLock();
            try
            {
                // Most common case - the head value is the one that was changed.
                if (updatedChild != null && updatedChild.Equals(Head))
                {
                    if (updatedChild.Value.Equals(Head.Value)) return false;
                    Value = updatedChild.Value;
                    return true;
                }

                // Did the base change?
                if (updatedChild == null || updatedChild.Equals(Base))
                {
                    // Check if it's really a new base.
                    IEvaluateable newBaseValue = Base.Value;
                    if (newBaseValue.Equals(_BaseValue)) return false;
                    _BaseValue = newBaseValue;
                }

                // In all cases, must re-index.  The new value could 
                if (!(_BaseValue is IIndexable idxable))
                    newValue = new IndexingError(this, "Base of type " + _BaseValue.GetType().Name + " is not indexable.");
                else if (!idxable.TryIndex(_CachedOrdinalValue, out newValue))
                    // Usually, newValue will be set here.
                    newValue = new IndexingError(this, "Invalid index " + _CachedOrdinalValue.ToString() + " for " + _BaseValue.GetType().Name + ".");
                else if (newValue is IAsyncUpdater iv)
                {
                    // Probably a variable.
                    if (!iv.Equals(Head))
                    {
                        Update.StructureLock.EnterWriteLock();
                        try
                        {
                            if (Inputs.Length > 2) { Head.RemoveListener(this); Inputs[2] = iv; }
                            else Inputs = new IEvaluateable[] { Base, Ordinal, iv };
                            iv.AddListener(this);
                        }
                        finally { Update.StructureLock.ExitWriteLock(); }
                    }
                    newValue = iv.Value;
                }
                else if (newValue is ISyncUpdater)
                    newValue = new IndexingError(this, "Indexing cannot refer to object of type " + newValue.GetType().Name + ".");
                else if (Inputs.Length > 2)
                {
                    Update.StructureLock.EnterWriteLock();
                    try { Head.RemoveListener(this); Inputs = new IEvaluateable[] { Base, Ordinal }; }
                    finally { Update.StructureLock.ExitWriteLock(); }
                }
                else if (newValue == null)
                    newValue = Dependency.Null.Instance;

                if (newValue.Equals(Value)) return false;
                Value = newValue;
                return true;
            }
            finally { Update.StructureLock.ExitUpgradeableReadLock(); }

        }
    }

}
