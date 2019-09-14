using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Functions
{
    public enum Mobility
    {
        // TODO:  more info will probably be needed
        None = 0,
        Column = 1,
        Row = 2,
        All = ~0
    }

    internal struct EvaluateableContext : IContext, IEvaluateable, ITypeGuarantee, ILiteral<IContext>
    {
        public readonly IContext Context;

        public EvaluateableContext(IContext context) { this.Context = context; }

        bool IContext.TryGetSubcontext(object path, out IContext ctxt) => Context.TryGetSubcontext(path, out ctxt);

        bool IContext.TryGetProperty(object path, out IEvaluateable source) => Context.TryGetProperty(path, out source);

        IEvaluateable IEvaluateable.Value => this;

        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Reference;

        IContext ILiteral<IContext>.CLRValue => Context;

        public override string ToString() => Context.ToString();

        public override bool Equals(object obj)
        {
            if (obj is EvaluateableContext other) return Context.Equals(other.Context);
            return Context.Equals(obj);
        }

        public override int GetHashCode() => Context.GetHashCode();
    }

    [NonVariadic(0, TypeFlags.Reference)]
    internal sealed class Reference : Function, IDisposable
    {
        public readonly string[] Paths;

        private IContext _Origin;
        private IEvaluateable _Head;
        /// <summary>
        /// The object at the head of this reference.  The head can be an <seealso cref="IContext"/>, an 
        /// <seealso cref="IVariable"/>, or some kind of <seealso cref="IEvaluateable"/>.
        /// </summary>
        internal IEvaluateable Head
        {
            get => _Head;
            set
            {
                if (_Head == null)
                {
                    if (value == null) return;
                }
                else if (_Head.Equals(value))
                    return;
                else if (_Head is IVariable iv_old)
                    iv_old.RemoveListener(this);
                _Head = value;
                if (_Head is IVariable iv_new) iv_new.AddListener(this);
            }
        }

        public Reference(IEvaluateable origin, params string[] paths) { this.Paths = paths; this.Inputs = new IEvaluateable[] { origin };  }

        

        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            IContext ctxt = evaluatedInputs[0] as IContext;
            IContext newOrigin = ctxt;
            if (newOrigin == null)
                return new ReferenceError(evaluatedInputs[0], Paths, -1, "Origin is not a valid " + typeof(IContext).Name + ".");
            else if (newOrigin.Equals(_Origin))
                return Head.Value;

            int i;
            Dependency.Variables.Update.StructureLock.EnterUpgradeableReadLock();
            try
            {
                
                for (i = 0; i < Paths.Length - 1; i++)
                {
                    if (ctxt == null)
                        return (Head = new ReferenceError(evaluatedInputs[0], Paths, i, "Invalid context."));
                    if (!ctxt.TryGetSubcontext(Paths[i], out ctxt))
                        return (Head = new ReferenceError(evaluatedInputs[0], Paths, i, "Invalid subcontext."));
                }

                // At the end of the path.  Is it a subcontext or a property?
                if (ctxt.TryGetSubcontext(Paths[i], out IContext head_ctxt))
                {
                    Dependency.Variables.Update.StructureLock.EnterWriteLock();
                    Head = new EvaluateableContext(head_ctxt);
                    _Origin = newOrigin;
                    Dependency.Variables.Update.StructureLock.ExitWriteLock();
                }
                else if (ctxt.TryGetProperty(Paths[i], out IEvaluateable head_source))
                {
                    //if (ReferenceEquals(head_source, Head)) return head_source.Value;

                    if (head_source is IVariable v && Helpers.TryFindCircularity(this, v))
                        head_source = new ReferenceError(evaluatedInputs[0], Paths, i, "Circular reference.");
                    Dependency.Variables.Update.StructureLock.EnterWriteLock();
                    Head = head_source;
                    _Origin = newOrigin;
                    Dependency.Variables.Update.StructureLock.ExitWriteLock();
                }
                else
                    Head = new ReferenceError(evaluatedInputs[0], Paths, i, "Invalid subcontext or property.");
            }
            finally { Dependency.Variables.Update.StructureLock.ExitUpgradeableReadLock(); }
            return Head.Value;
        }
        
        public override string ToString() => "=>" + Inputs[0].ToString() + "." + string.Join(".", Paths);

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
                if (Head is IVariable iv) iv.RemoveListener(this);
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
    [NonVariadic(0, TypeFlags.Indexable | TypeFlags.Reference | TypeFlags.Vector | TypeFlags.Range, TypeFlags.Any)]
    internal sealed class Indexing : Function
    {
        private IContext _Base;
        private IEvaluateable _Ordinal;

        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            IContext oldBase = _Base;
            IEvaluateable oldOrdinal = _Ordinal;

            Dependency.Variables.Update.StructureLock.EnterUpgradeableReadLock();
            try
            {
                IContext newBase = evaluatedInputs[0] as IContext;
                if (newBase == null)
                    return new IndexingError(this, _Base = newBase, _Ordinal = evaluatedInputs[1], "Invalid base.");
                IEvaluateable newOrdinal = evaluatedInputs[1];
                if (newOrdinal == null)
                    return new IndexingError(this, _Base = newBase, _Ordinal = evaluatedInputs[1], "Invalid ordinal (" + newOrdinal.ToString() + ").");

                if (newBase.Equals(_Base) && newOrdinal.Equals(_Ordinal))
                    return Value;

                Dependency.Variables.Update.StructureLock.EnterWriteLock();
                _Base = newBase;
                _Ordinal = newOrdinal;
                Dependency.Variables.Update.StructureLock.ExitWriteLock();

                if (newBase.TryGetSubcontext(newOrdinal, out IContext sub_ctxt)) return new EvaluateableContext(sub_ctxt);
                if (newBase.TryGetProperty(newOrdinal, out IEvaluateable sub_prop))
                {
                    if (sub_prop is IVariable ivar && Helpers.TryFindCircularity(this, ivar))
                        return new IndexingError(this, newBase, newOrdinal, "Circular reference.");
                    return sub_prop.Value;
                }
                else return new IndexingError(this, newBase, newOrdinal, "Base is not indexable by " + newOrdinal.ToString());
            }
            finally { Dependency.Variables.Update.StructureLock.ExitUpgradeableReadLock(); }
        }

        public override string ToString() => Inputs[0].ToString() + "[" + Inputs[1].ToString() + "]";
    }
}
