using Dependency.Variables;
using Mathematics;
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
    internal sealed class Indexing : IFunction, IReference
    {
        internal IEvaluateable[] Inputs;
        IList<IEvaluateable> IFunction.Inputs => this.Inputs;
        /// <summary>
        /// The base value.  For example, in "Points[A1]", this would be the Points object.  The 
        /// value of the Points object can change.  If the base changes, the 
        /// <see cref="Indexing"/> will have to be re-indexed.
        /// </summary>
        internal IEvaluateable Base => Inputs[0];
        /// <summary>
        /// The ordinal value.  For example, in "Points[A1]", this would be the A1 variable 
        /// object.  If the ordinal changes, the <see cref="Indexing"/> will have to be re-indexed.
        /// </summary>
        internal IEvaluateable Ordinal => Inputs[1];

        /// <summary>The indexed value.  For example, the <see cref="Head"/> in "Points[A1]" would 
        /// be the point found at the A1 spot within Points.</summary>
        internal IEvaluateable Head => Inputs[2];

        // Cache these because updates must have StructureLock held
        private IEvaluateable _CachedBaseValue;
        private IEvaluateable _CachedOrdinalValue;

        public ISyncUpdater Parent { get; set; }

        public IEvaluateable Value { get; private set; }

        public Indexing(IEvaluateable @base, IEvaluateable ordinal)
        {
            this.OutOfRange = new IndexingError(this, Inputs, "Indexing out of range.");
            this.NonIndexable = new IndexingError(this, Inputs, "Base is non-indexable.");
            this._CachedOrdinalValue = Dependency.Null.Instance;
            this.Inputs = new IEvaluateable[] { @base, ordinal, NonIndexable };

            if (@base is ISyncUpdater isu_base && isu_base.Parent == null)
                isu_base.Parent = this;
            else if (@base is IAsyncUpdater iau_base)
                iau_base.AddListener(this);
            if (ordinal is ISyncUpdater isu_ordinal && isu_ordinal.Parent == null)
                isu_ordinal.Parent = this;
            else if (ordinal is IAsyncUpdater iau_ordinal)
                iau_ordinal.AddListener(this);

            _CachedBaseValue = @base.Value;
            _CachedOrdinalValue = ordinal.Value;
            Reindex();
        }

        private readonly IndexingError OutOfRange;
        private readonly IndexingError NonIndexable;
        internal bool Reindex()
        {
            Update.StructureLock.EnterWriteLock();
            try
            {
                if (!(_CachedBaseValue is IIndexable baseIndexable))
                {
                    if (Head == null || Head.Value != NonIndexable || Value != NonIndexable)
                    {
                        Inputs[2] = NonIndexable;
                        if (NonIndexable.Equals(Value)) return false;
                        Value = NonIndexable;
                        return true;
                    }
                }                   
                else if (_CachedOrdinalValue is Error e)
                {
                    if (Value.Equals(e)) return false;
                    Value = e;
                    return true;
                }
                else if (baseIndexable.TryIndex(_CachedOrdinalValue, out IEvaluateable newHead))
                {
                    if (newHead.Equals(Head)) return false;
                    if (Head is IAsyncUpdater iau_old) iau_old.RemoveListener(this);
                    if (newHead is IAsyncUpdater iau_new) iau_new.AddListener(this);
                    Inputs[2] = newHead;
                    IEvaluateable newValue = Head.Value;
                    if (newValue.Equals(Value)) return false;
                    if (Helpers.TryFindDependency(Head, this, out IEnumerable<IEvaluateable> path))
                    {
                        newValue = new CircularityError(this, path);
                    }
                    Value = newValue;
                    return true;
                }
                else if (Head != OutOfRange || Value != OutOfRange)
                {
                    Inputs[2] = (Value = OutOfRange);
                    return true;
                }

                // The Value didn't change.
                return false;
            }
            finally { Update.StructureLock.ExitWriteLock(); }
        }
        ITrueSet<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild, ITrueSet<IEvaluateable> updatedDomain)
        {
            if (updatedChild == null)
            {
                _CachedBaseValue = Base.Value;
                _CachedOrdinalValue = Ordinal.Value;
            }

            if (Head == null || Head is Error)
                return Reindex() ? Update.UniversalSet : null;

            // Most common case - the head's value is the one that was changed.
            if (Head.Equals(updatedChild))
            {
                if (Head.Value.Equals(Value)) return null;
                Value = Head.Value;

                // If the Base controls re-indexing, just pass the change signal up the line.
                if (Base is IIndexable ii)
                    return Update.UniversalSet ;         

                return Reindex() ? Update.UniversalSet : null;
            }

            // Did the base's value change?
            else if (updatedChild == null || updatedChild.Equals(Base))
            {
                IEvaluateable newBaseValue = Base.Value;                  
                if (newBaseValue != null && newBaseValue.Equals(_CachedBaseValue)) return null;                
                _CachedBaseValue = newBaseValue;
                return Reindex() ? Update.UniversalSet : null;
            }

            // Otherwise, the updated child is the ordinal.  Is it a non-change?
            else if (Ordinal.Value.Equals(_CachedOrdinalValue))
                return null;

            // Finally, it must be an ordinal change.  Do a Reindex()
            return Reindex() ? Update.UniversalSet : null;
        }


        IEnumerable<IEvaluateable> IReference.GetComposers() => Inputs;



    }

}
