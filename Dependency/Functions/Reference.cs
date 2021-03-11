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

    internal sealed class Reference : IReference, ISyncUpdater
    {
        public readonly IEvaluateable Base;
        public readonly IEvaluateable Path;
        public IEvaluateable Subject;   //Contents

        public bool IsStatic =>
            !(this.Base is IAsyncUpdater || this.Base is ISyncUpdater
              || this.Path is IAsyncUpdater || this.Path is ISyncUpdater);

        private Reference(IEvaluateable @base, IEvaluateable path)
        {
            this.Base = @base;
            this.Path = path;
            Helpers.SetParent(this, this.Base, false);
            Helpers.SetParent(this, this.Parent, false);
        }
        private Reference(IContext root, IEvaluateable path)
        {
            this.Base = new IncompleteReference(this, root);
            this.Path = path;
            Helpers.SetParent(this, this.Base, false);
            Helpers.SetParent(this, this.Parent, false);
        }
        
        public static Reference FromPath(IEvaluateable @base, IEvaluateable path)
            => new Reference(@base, path);
        public static Reference FromRoot(IContext root, IEvaluateable path)
            => new Reference(root, path);


        public ISyncUpdater Parent { get; set; }

        public IEvaluateable Value { get; private set; }

        internal bool Update(IEvaluateable updatedChild)
        {
            if (ReferenceEquals(updatedChild, Base) || ReferenceEquals(updatedChild, Path))
            {
                IEvaluateable newSubject;
                if (!(Base.Value is IContext ctxt))
                    newSubject = new ReferenceError(this, "Base value of type " + Base.Value.GetType().Name + " is not indexable.");
                else if (!(Path.Value is Dependency.String q))
                    newSubject = new ReferenceError(this, "Path value of type " + Path.Value.GetType().Name + " is an invalid reference path.");
                else if (ctxt.TryGetSubcontext(q.ToString(), out IContext subj_ctxt))
                {
                    if (subj_ctxt is IEvaluateable already_ieval)
                        newSubject = already_ieval;
                    else 
                        newSubject = new IncompleteReference(this, subj_ctxt);
                }                    
                else if (!ctxt.TryGetProperty(q.ToString(), out newSubject))
                    newSubject = new ReferenceError(this, "Invalid reference path: " + q.ToString());
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
        ITrueSet<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild, ITrueSet<IEvaluateable> indexDomain)
            => Update(updatedChild) ? Dependency.Variables.Update.UniversalSet : null;

        IEnumerable<IEvaluateable> IReference.GetComposers()
        {
            yield return Base;
            yield return Path;
            yield return Subject;
        }

        public override string ToString()
        {
            if (Base is IncompleteReference) return "<root>." + Path.ToString() ;
            return Base.ToString() + "." + Path.ToString();
        }

        private sealed class IncompleteReference : IEvaluateable, IContext, ITypeGuarantee, IReference
        {
            private readonly IContext _Root;
            private readonly Reference _Host;
            internal IncompleteReference(Reference host, IContext ctxt)
            {
                Debug.Assert(host != null);
                Debug.Assert(ctxt != null);
                this._Host = host;
                this._Root = ctxt;
            }

            IEvaluateable IEvaluateable.Value => this;

            TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.None;

            public bool TryGetProperty(string path, out IEvaluateable property)
                => _Root.TryGetProperty(path, out property);

            public bool TryGetSubcontext(string path, out IContext ctxt)
                => _Root.TryGetSubcontext(path, out ctxt);

            public override bool Equals(object obj)
                => obj is IncompleteReference other && other._Root.Equals(_Root) && other._Host.Equals(_Host);
            public override int GetHashCode() => _Root.GetHashCode();

            IEnumerable<IEvaluateable> IReference.GetComposers() => _Root is IEvaluateable iev ? new IEvaluateable[1] { iev } : new IEvaluateable [0];
        }
    }
}
