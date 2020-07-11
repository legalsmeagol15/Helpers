using Dependency.Variables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Functions
{
    internal delegate void IndexingChangedHandler(IIndexable sender, IndexingChangedArgs e);
    internal sealed class IndexingChangedArgs : EventArgs
    {
        public readonly ICollection<IEvaluateable> Items;
        public IndexingChangedArgs(ICollection<IEvaluateable> items)
        {
            this.Items = items;
        }
    }
    /// <summary>
    /// A function which takes a base indexable object (like a <seealso cref="Vector"/>, or a 
    /// <seealso cref="Reference"/> which points to a property <seealso cref="IAsyncUpdater"/>), and an ordinal 
    /// value 'n', and returns the 'nth' item associated with that base object.
    /// </summary>
    internal sealed class Indexing : IFunction, IReference, IDisposable
    {
        private readonly IEvaluateable[] _Inputs = new IEvaluateable[3];
        internal IEvaluateable[] Inputs => this._Inputs;
        IList<IEvaluateable> IFunction.Inputs => this._Inputs;
        /// <summary>
        /// The base value.  For example, in "Points[A1]", this would be the Points object.  The 
        /// value of the Points object can change.  If the base changes, the 
        /// <see cref="Indexing"/> will have to be re-indexed.
        /// </summary>
        internal IEvaluateable Base => _Inputs[0];
        /// <summary>
        /// The ordinal value.  For example, in "Points[A1]", this would be the A1 variable 
        /// object.  If the ordinal changes, the <see cref="Indexing"/> will have to be re-indexed.
        /// </summary>
        internal IEvaluateable Ordinal => _Inputs[1];

        /// <summary>The indexed value.  For example, the <see cref="Head"/> in "Points[A1]" would 
        /// be the point found at the A1 spot within Points.</summary>
        internal IAsyncUpdater Head => _Inputs.Length > 2 ? (IAsyncUpdater)_Inputs[2] : null;

        // Cache these because updates must have StructureLock held
        private IEvaluateable _BaseValue;
        private IEvaluateable _CachedOrdinalValue;

        public ISyncUpdater Parent { get; set; }

        public IEvaluateable Value { get; private set; }

        public Indexing()
        {
            this.OutOfRange = new IndexingError(this, _Inputs, "Indexing out of range.");
            this.NonIndexable = new IndexingError(this, _Inputs, "Base is non-indexable.");
            this._CachedOrdinalValue = NonIndexable;
        }
        private readonly IndexingError OutOfRange;
        private readonly IndexingError NonIndexable;
        private bool Reindex()
        {
            Update.StructureLock.EnterWriteLock();
            try
            {
                if (!(Base is IIndexable baseIndexable))
                {
                    if (_Inputs[2] != NonIndexable || Value != NonIndexable)
                    {
                        _Inputs[2] = Value = NonIndexable;
                        return true;
                    }                    
                }                    
                else if (baseIndexable.TryIndex(_CachedOrdinalValue, out IEvaluateable newHead))
                {
                    if (newHead.Equals(_Inputs[2])) return false;
                    if (_Inputs[2] is IAsyncUpdater iau_old) iau_old.RemoveListener(this);
                    if (newHead is IAsyncUpdater iau_new) iau_new.AddListener(this);
                    _Inputs[2] = newHead;
                    Value = _Inputs[2].Value;
                    return true;
                }
                return false;
            }
            finally { Update.StructureLock.ExitWriteLock(); }
        }
        bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild)
        {            
            // Most common case - the head's value is the one that was changed.
            if (Head.Equals(updatedChild))
            {
                if (updatedChild.Value.Equals(Head.Value)) return false;
                Value = updatedChild.Value;
                return true;
            }

            // Did the base's value change?
            else if (updatedChild == null || updatedChild.Equals(Base))
            {
                IEvaluateable newBaseValue = Base.Value;
                if (newBaseValue.Equals(_BaseValue)) return false;

                ((IIndexable)Base).IndexChanged -= Base_IndexChanged;
                _BaseValue = newBaseValue;
                if (_BaseValue is IIndexable baseIndexable)
                    baseIndexable.IndexChanged += Base_IndexChanged;
                _CachedOrdinalValue = Ordinal.Value;
            }

            // Last case - the ordinal's value changed.  Rule out a non-change.
            else if (Ordinal.Value.Equals(_CachedOrdinalValue))
                return false;
            else 
                _CachedOrdinalValue = Ordinal.Value;

            // In all other cases, must re-index.  The new value could be a variable itself.
            return Reindex();
        }

        private void Base_IndexChanged(IIndexable sender, IndexingChangedArgs e)
        {
            // Sometimes, the Base's value doesn't change, but its indexing scheme does.
            if (e.Items.Contains(_CachedOrdinalValue))
                Reindex();
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
