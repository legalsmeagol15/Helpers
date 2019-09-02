using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Helpers;

namespace Dependency
{
    public enum Mobility
    {
        // TODO:  more info will probably be needed
        None = 0,
        Column = 1,
        Row = 2,
        All = ~0
    }

    internal sealed class Reference : IDynamicItem, IEvaluateable, IDisposable
    {
        public readonly string[] Paths;
        private IEvaluateable _HeadProperty = null;

        /// <summary>In the sequence, "line.brush.color.red", this would refer to the property "red" at the end.  If it is 
        /// null then there is no associated property to be found at the conclusion of the reference.
        /// </summary>
        internal IEvaluateable HeadProperty
        {
            get => _HeadProperty;
            private set
            {
                if (ReferenceEquals(_HeadProperty, value)) return;
                if (_HeadProperty is IVariable old_var) old_var.RemoveListener(this);
                _HeadProperty = value;
                if (_HeadProperty is IVariable new_var) new_var.AddListener(this);

                IEvaluateable newValue = (_HeadProperty == null) ? Dependency.Null.Instance : _HeadProperty.Value;
                if (newValue.Equals(Value)) return;
                Value = newValue;
            }
        }

        /// <summary>
        /// In the sequence, "line.brush.color", this would refer to the context "color" at the end.  The context has 
        /// no value in itself, but may host more sub-contexts or properties.  If this is null, then there is no sub-
        /// context at the conclusion of the reference.
        /// </summary>
        internal IContext HeadContext { get; private set; } = null;

        /// <summary>
        /// The current context from which this <see cref="Reference"/> progresses.  If this <see cref="Reference"/> 
        /// is mathematically a vector, the base context represents an origin point. 
        /// </summary>
        internal IContext BaseContext { get; private set; } = null;

        /// <summary>
        /// The object supplying the original <seealso cref="IContext"/> for this <see cref="Reference"/>.  This may 
        /// be a static <seealso cref="IContext"/> itself, or it may be a dynamic object like an 
        /// <seealso cref="Indexing"/>.
        /// </summary>
        internal object Origin
        {
            get => _Origin;
            set
            {
                if (_Origin is IDynamicItem idi_before && ReferenceEquals(idi_before.Parent, this))
                    idi_before.Parent = null;
                _Origin = value;
                if (_Origin is IDynamicItem idi_after)
                    idi_after.Parent = this;
                Update();
            }
        }
        private object _Origin;

        /// <summary>The parent object in the evaluation tree.</summary>
        public IDynamicItem Parent { get; set; }

        /// <summary>
        /// The cached value of this <see cref="Reference"/>.  This value will represent the value of the 
        /// HeadProperty, if one exists.  If it does not, the stored value will be 
        /// <seealso cref="Dependency.Null.Instance"/>.
        /// </summary>
        public IEvaluateable Value { get; private set; }

        public Reference(IContext root, params string[] paths) { this.Paths = paths; this.Origin = root; }
        public Reference(IEvaluateable origin, params string[] paths) { this.Paths = paths; this.Origin = origin; }

        /// <summary>Refreshes the reference structure from root to head.  Returns the index at which refresh failed, 
        /// or -1 if it succeeded.
        /// </summary>
        internal int RefreshStructure()
        {
            // Figure out what the root is.
            IContext prior;
            if (Origin is IContext ic)
                prior = ic;
            else if (Origin is IEvaluateable iev && iev.Value is IContext evalCtxt)
                prior = evalCtxt;
            else
                return 0;

            //  If the root hasn't changed, and the value of the property hasn't changed, no need to refresh.
            if (ReferenceEquals(BaseContext, prior))
                return -1;
            BaseContext = prior;

            // Traverse the contexts that go between root and the head.  If a subcontext cannot be found along the 
            // way, make sure the value reflects an error.
            for (int i = 0; i < this.Paths.Length - 1; i++)
            {
                if (!prior.TryGetSubcontext(Paths[i].ToLower(), out IContext ctxt))
                {
                    HeadContext = null; HeadProperty = null;
                    return i;
                }
                prior = ctxt;
            }

            // If we've followed the paths to end at a subcontext, that's okay because this last reference might still 
            // be indexed.  However, the value should reflect null.
            if (prior.TryGetSubcontext(Paths[Paths.Length - 1].ToLower(), out IContext newContext))
            {
                if (newContext == null) throw new InvalidOperationException("Reference context cannot be null.");
                IContext oldCtxt = HeadContext;
                if (ReferenceEquals(oldCtxt, newContext)) return -1;
                HeadContext = newContext;
                HeadProperty = null;
                return -1;
            }
            // If we've landed at a property, the value is equal to the property's value.
            else if (prior.TryGetProperty(Paths[Paths.Length - 1].ToLower(), out IEvaluateable newProperty))
            {
                if (newProperty == null) throw new InvalidOperationException("Reference property cannot be null.");
                IEvaluateable oldProp = HeadProperty;
                if (ReferenceEquals(oldProp, newProperty)) return -1;
                HeadContext = null;
                HeadProperty = newProperty;
                return -1;
            }

            // This shouldn't ever happen.
            throw new InvalidOperationException("Only IEvaluatables and IContexts can function as the head of a reference.");
        }

        /// <summary>
        /// Updates the base context, and follows the reference progression to the terminal object (be that a 
        /// <seealso cref="IContext"/> or a <seealso cref="IVariable"/> property).  If the <see cref="Reference"/> is 
        /// found to point to a property, caches the value of that property.
        /// <para/>From there, the evaluation tree is updated until a <seealso cref="IVariable"/> is hit, or until 
        /// no value changes occur, whichever is earlier.  If an <seealso cref="IVariable"/> is hit, i.e., notified to 
        /// send notice of a value change to all listeners, the <seealso cref="IVariable"/> will do so asynchronously.
        /// </summary>
        /// <returns></returns>
        public bool Update()
        {
            // Try a full refresh first.
            IContext oldCtxt = HeadContext;
            IEvaluateable oldProp = HeadProperty;
            int failure = RefreshStructure();
            if (failure >= 0)
            {
                if (Value is ReferenceError re && re.Index == failure) return false;
                Value = new ReferenceError(Origin, Paths, failure);
                return true;
            }
            if (oldCtxt != null) return !ReferenceEquals(oldCtxt, HeadContext);
            if (HeadContext != null) return true;
            if (HeadProperty == null)
            {
                if (Value is ReferenceError re && re.Index == -1) return false;
                Value = new ReferenceError(Origin, Paths, -1, "Failure to determine reference structure.");
                return true;
            }
            IEvaluateable newValue = HeadProperty.Value;
            if (newValue.Equals(Value)) return false;
            Value = newValue;

            // This is the ONLY object that walks up the tree to update parents.
            IDynamicItem idi = this.Parent;
            while (idi != null && idi.Update()) idi = idi.Parent;

            return true;
        }

        #region Reference IDisposable Support

        // This is implemented if the Reference's host changes its content, this Reference will be garbage 
        // collected because the HeadProperty variables maintain only weak references to this.  However, 
        // those weak references are maintained in a WeakReferenceSet, so to avoid leaving the weak 
        // reference as a memory leak, go ahead and use the fact of this Reference's disposal to keep all 
        // the sources clean.

        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing && HeadProperty is IVariable iv) iv.RemoveListener(this);
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose() => Dispose(true);

        #endregion

    }

    /// <summary>
    /// A function which takes a base indexable object (like a <seealso cref="Vector"/>, or a 
    /// <seealso cref="Reference"/> which points to a property <seealso cref="Variables.Variable"/>), and an ordinal 
    /// value 'n', and returns the 'nth' item associated with that base object.
    /// </summary>
    [TypeControl.NonVariadic(0, TypeFlags.RealAny | TypeFlags.String)]
    internal sealed class Indexing : Functions.Function
    {
        /// <summary>
        /// Base is not stored with <seealso cref="Functions.Function.Inputs"/> because it might not be an 
        /// <seealso cref="IEvaluateable"/>.  It might be a <seealso cref="Reference"/>.
        /// </summary>
        internal readonly object Base; // Base is not part of Inputs because it could be an IContext
        internal Indexing(IEvaluateable @base, IEvaluateable ordinal)
        {
            this.Base = @base;
            if (this.Base is IDynamicItem idi_b) idi_b.Parent = this;
            if (ordinal is Parse.Bracket b) ordinal = b.Contents;
            this.Inputs = new IEvaluateable[] { ordinal };
        }
        internal Indexing(Reference @base, IEvaluateable ordinal)
        {
            this.Base = @base;
            if (this.Base is IDynamicItem idi_b) idi_b.Parent = this;
            if (ordinal is Parse.Bracket b) ordinal = b.Contents;
            this.Inputs = new IEvaluateable[] { ordinal };
        }

        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            // The base might itself be indexable (like a Vector), or it might be a variable whose 
            // value is indexable.
            IEvaluateable eval_ordinal = evaluatedInputs[0];

            IIndexable ii;

            // Is the base something like a Vector?
            if (Base is IIndexable ii2) ii = ii2;

            // Is the base some expression (like a sum of Vectors) whose value happens to be indexable?
            // Or, a Reference pointing at a Variable whose value happens to be indexable?
            else if (Base is IEvaluateable iev && iev.Value is IIndexable ii3) ii = ii3;

            // Is the base a Reference 
            else if (Base is Reference r && r.HeadContext != null && r.HeadContext.TryGetProperty(eval_ordinal, out IEvaluateable iv))
                return iv.Value;
            else return new IndexingError(this, Base, evaluatedInputs[0]);

            return ii[eval_ordinal];
        }
    }
}
