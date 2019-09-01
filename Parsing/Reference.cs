﻿using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Helpers;

namespace Dependency
{
    internal sealed class Reference : IDynamicItem, IEvaluateable, IDisposable
    {
        public readonly string[] Paths;
        internal IEvaluateable HeadProperty = null;
        internal IContext HeadContext = null;
        internal IContext BaseContext = null;
        private object _Origin;
        internal object Origin
        {
            get => _Origin;
            set
            {
                if (value is IDynamicItem idi) idi.Parent = this;
                _Origin = value;
                Update();
            }
        }
        public IDynamicItem Parent { get; set; }

        public IEvaluateable Value { get; private set; }

        public Reference(IContext root, params string[] paths)        {            this.Paths = paths;            this.Origin = root;        }
        public Reference (IEvaluateable origin, params string[] paths)        {            this.Paths = paths;            this.Origin = origin;        }

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

            // If we've followed the paths to end at a subcontext, the value must be a reference error.
            if (prior.TryGetSubcontext(Paths[Paths.Length - 1].ToLower(), out IContext newContext))
            {
                if (newContext == null) throw new InvalidOperationException("Reference context cannot be null.");
                IContext oldCtxt = HeadContext;
                if (ReferenceEquals(oldCtxt, newContext)) return -1;
                HeadContext = newContext; HeadProperty = null;
                return Paths.Length - 1;
            }
            // If we've landed at a property, the value is equal to the property's value.  Remember that if the 
            // property is in fact a variable, to remove and add the listeners.
            else if (prior.TryGetProperty(Paths[Paths.Length - 1].ToLower(), out IEvaluateable newProperty))
            {
                if (newProperty == null) throw new InvalidOperationException("Reference property cannot be null.");
                IEvaluateable oldProp = HeadProperty;
                if (ReferenceEquals(oldProp, newProperty)) return -1;
                if (oldProp is IVariable old_iv) old_iv.RemoveListener(this);
                HeadContext = null; HeadProperty = newProperty;
                if (newProperty is IVariable new_iv) new_iv.AddListener(this);
                IEvaluateable newValue = newProperty.Value;
                if (newValue.Equals(Value)) return -1;
                Value = newValue;
                return -1;
            }
            throw new InvalidOperationException("Only IEvaluatables and IContexts can function as the head of a reference.");
        }

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

    
    [TypeControl.NonVariadic(0, TypeFlags.RealAny | TypeFlags.String)]
    internal sealed class Indexing : Functions.Function
    {
        internal readonly object Base; // Base is not part of Inputs because it could be an IContext
        internal Indexing (IEvaluateable @base, IEvaluateable ordinal)
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
            IIndexable ii;
            if (Base is IIndexable ii2) ii = ii2;            
            else if (Base is IEvaluateable iev && iev.Value is IIndexable ii3) ii = ii3;
            else return new IndexingError(this, Base, evaluatedInputs[0]);

            return ii[evaluatedInputs[0]];
        }
    }
}
