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
        }
        private Reference(IContext root, IEvaluateable path)
        {
            this.Base = new IEvalCtxt(this, root);
            this.Path = path;
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
                    newSubject = new ReferenceError(this, "Base of type " + Base.Value.GetType().Name + " is not indexable.");
                else if (!(Path.Value is Dependency.String q))
                    newSubject = new ReferenceError(this, "Path of type " + Path.Value.GetType().Name + " is an invalid reference path.");
                else if (ctxt.TryGetSubcontext(q.ToString(), out IContext subj_ctxt))
                    newSubject = new IEvalCtxt(this, subj_ctxt);
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
            if (Base is Reference) return Path.ToString() + ".";
            if (Base is IEvalCtxt) return ".";
            return Base.ToString() + "." + Path.ToString();
        }

        private sealed class IEvalCtxt : IEvaluateable, IContext
        {
            private readonly IContext _Root;
            private readonly Reference _Host;
            internal IEvalCtxt(Reference host, IContext ctxt)
            {
                Debug.Assert(host != null);
                Debug.Assert(ctxt != null);
                this._Host = host;
                this._Root = ctxt;
            }

            IEvaluateable IEvaluateable.Value => this;

            public bool TryGetProperty(string path, out IEvaluateable property)
                => _Root.TryGetProperty(path, out property);

            public bool TryGetSubcontext(string path, out IContext ctxt)
                => _Root.TryGetSubcontext(path, out ctxt);

            public override bool Equals(object obj)
                => obj is IEvalCtxt other && other._Root.Equals(_Root) && other._Host.Equals(_Host);
            public override int GetHashCode() => _Root.GetHashCode();
        }
    }
}
