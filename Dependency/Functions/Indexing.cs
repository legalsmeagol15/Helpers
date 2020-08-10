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
    internal sealed class Indexing : IReference, ISyncUpdater
    {
        /// <summary>
        /// The base value.  For example, in "Points[A1]", this would be the Points object.  The 
        /// value of the Points object can change.  If the base changes, the 
        /// <see cref="Indexing"/> will have to be re-indexed.
        /// </summary>
        internal readonly IEvaluateable Base;
        /// <summary>
        /// The ordinal value.  For example, in "Points[A1]", this would be the A1 variable 
        /// object.  If the ordinal changes, the <see cref="Indexing"/> will have to be re-indexed.
        /// </summary>
        internal readonly IEvaluateable Ordinal;

        /// <summary>The indexed value.  For example, the <see cref="Head"/> in "Points[A1]" would 
        /// be the point found at the A1 spot within Points.</summary>
        internal IEvaluateable Subject { get; private set; }

        public ISyncUpdater Parent { get; set; }

        public IEvaluateable Value { get; private set; }

        public Indexing(IEvaluateable @base, IEvaluateable ordinal)
        {
            this.Base = @base;
            this.Ordinal = ordinal;
            Helpers.SetParent(this, this.Base);
            Helpers.SetParent(this, this.Ordinal);
        }

        internal bool Update(IEvaluateable updatedChild)
        {
            if (ReferenceEquals(updatedChild, this.Base) || ReferenceEquals(updatedChild, this.Ordinal))
            {
                IEvaluateable newSubject;
                if (!(Base.Value is IIndexable idxable))
                    newSubject = new IndexingError(this, "Base value of type " + Base.Value.GetType().Name + " is not indexable.");
                else if (!idxable.TryIndex(Ordinal.Value, out newSubject))
                    newSubject = new IndexingError(this, "Invalid indexing ordinal \"" + Ordinal.Value.ToString() + "\" on indexable type " + Base.Value.GetType().ToString());
                Debug.Assert(newSubject != null);
                if (newSubject.Equals(Subject)) return false;
                if (Subject is IAsyncUpdater iau_old) iau_old.RemoveListener(this);
                Subject = newSubject;
                if (Subject is IAsyncUpdater iau_new) iau_new.AddListener(this);
            }
            IEvaluateable newValue = Subject.Value;
            if (newValue.Equals(Value)) return false;
            Value = newValue;
            return true;
        }
        ITrueSet<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild, ITrueSet<IEvaluateable> updatedDomain)
            => Update(updatedChild) ? Dependency.Variables.Update.UniversalSet : null;


        IEnumerable<IEvaluateable> IReference.GetComposers()
        {
            yield return Base;
            yield return Ordinal;
            yield return Subject;
        }

        public override string ToString() => this.Base.ToString() + "[" + this.Ordinal.ToString() + "]";

    }

}
