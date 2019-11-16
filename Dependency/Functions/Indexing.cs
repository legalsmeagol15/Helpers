using Dependency.Variables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    internal sealed class Indexing : IFunction, IReference, IDisposable
    {
        private IEvaluateable[] _Inputs;
        internal IEvaluateable[] Inputs
        {
            get => _Inputs;
            set
            {
                Debug.Assert(_Inputs == null);
                _Inputs = value;
                if (_Inputs[0] is ISyncUpdater isu_base) isu_base.Parent = this;
                if (_Inputs[1] is ISyncUpdater isu_ordinal) isu_ordinal.Parent = this;
            }
        }
        internal IEvaluateable Base => Inputs[0];
        internal IEvaluateable Ordinal => Inputs[1];
        internal IAsyncUpdater Head => Inputs.Length > 2 ? (IAsyncUpdater)Inputs[2] : null;

        // Cache these because updates have to have StructureLock held
        private IEvaluateable _BaseValue;
        private IEvaluateable _CachedOrdinalValue = Dependency.Null.Instance;

        public ISyncUpdater Parent { get; set; }

        IList<IEvaluateable> IFunction.Inputs => this.Inputs;

        public IEvaluateable Value { get; private set; } = Dependency.Null.Instance;


        bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild, IEnumerable<IEvaluateable> updatedIndices)
        {
            IEvaluateable newValue;

            // Most common case - the head value is the one that was changed.
            if (updatedChild != null && updatedChild.Equals(Head))
            {
                if (updatedChild.Value.Equals(Head.Value)) return false;
                Value = updatedChild.Value;
                return true;
            }

            // Did the base change?
            else if (updatedChild == null || updatedChild.Equals(Base))
            {
                // Did an indexed item OF the base change?
                if (updatedIndices != null && updatedIndices.Any())
                {
                    foreach (IEvaluateable updated in updatedIndices)
                    {
                        if (Ordinal.Value.Equals(updated)) continue;
                        else if (updated is Values.Range r && r.Contains(Ordinal.Value)) continue;
                        return false;
                    }
                }

                // Check if it's really a new base.
                else
                {
                    
                    IEvaluateable newBaseValue = Base.Value;
                    if (newBaseValue.Equals(_BaseValue)) return false;
                    _BaseValue = newBaseValue;
                    _CachedOrdinalValue = Ordinal.Value;
                }
                
            }

            // Last case - the singular ordinal.  Just check for a non-change.
            else if (Ordinal.Value.Equals(_CachedOrdinalValue))
                return false;

            _CachedOrdinalValue = Ordinal.Value;

            // In all cases, must re-index.  The new value could be a variable itself.
            if (!(_BaseValue is IIndexable idxable))
                newValue = new IndexingError(this, Inputs, "Base of type " + _BaseValue.GetType().Name + " is not indexable.");
            else if (!idxable.TryIndex(_CachedOrdinalValue, out newValue))
                // Usually, newValue will be set here.
                newValue = new IndexingError(this, Inputs, "Invalid index " + _CachedOrdinalValue.ToString() + " for " + _BaseValue.GetType().Name + ".");
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
                newValue = new IndexingError(this, Inputs, "Indexing cannot refer to object of type " + newValue.GetType().Name + ".");
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

        IEnumerable<IEvaluateable> IReference.GetComposers() => _Inputs;



        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_Inputs[0] is IAsyncUpdater iau_base) iau_base.RemoveListener(this);
                    if (_Inputs[1] is IAsyncUpdater iau_ordinal) iau_ordinal.RemoveListener(this);
                    if (_Inputs.Length > 2 && _Inputs[2] is IAsyncUpdater iau_head) iau_head.RemoveListener(this);
                }
                disposedValue = true;
            }
        }

        void IDisposable.Dispose() { Dispose(true); }
        #endregion
    }

}
