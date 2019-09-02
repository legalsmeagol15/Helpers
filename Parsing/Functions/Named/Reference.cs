﻿using System;
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
        public IContext Context;

        public EvaluateableContext(IContext context) { this.Context = context; }

        bool IContext.TryGetSubcontext(object path, out IContext ctxt) => Context.TryGetSubcontext(path, out ctxt);

        bool IContext.TryGetProperty(object path, out IEvaluateable source) => Context.TryGetProperty(path, out source);

        IEvaluateable IEvaluateable.Value => this;

        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Reference;

        IContext ILiteral<IContext>.CLRValue => Context;

        public override string ToString() => Context.ToString();
    }

    [TypeControl.NonVariadic(0, TypeFlags.Reference)]
    internal sealed class Reference : Function, IDisposable
    {
        public readonly string[] Paths;

        private object _Head;
        internal object Head
        {
            get => _Head;
            set
            {
                if (_Head == null)
                {
                    if (value == null) return;
                    else if (_Head is IVariable iv) iv.RemoveListener(this);
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
            
            int i;
            for (i = 0; i < Paths.Length - 1; i++)
            {
                if (ctxt == null) return new ReferenceError(evaluatedInputs[0], Paths, i, "Invalid context.");
                if (!ctxt.TryGetSubcontext(Paths[i], out ctxt)) return new ReferenceError(evaluatedInputs[0], Paths, i, "Invalid subcontext.");
            }

            // At the end of the path.  Is it a subcontext or a property?
            if (ctxt.TryGetSubcontext(Paths[i], out IContext head_ctxt))
            {
                Head = head_ctxt;
                return new EvaluateableContext(head_ctxt);
            }
            if (ctxt.TryGetProperty(Paths[i], out IEvaluateable head_source))
            {
                Head = head_source;
                return head_source.Value;
            }

            Head = null;
            return new ReferenceError(evaluatedInputs[0], Paths, i, "Invalid subcontext or property.");
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
    [TypeControl.NonVariadic(0, TypeFlags.Indexable | TypeFlags.Reference | TypeFlags.Vector | TypeFlags.Range, TypeFlags.Any)]
    internal sealed class Indexing : Function
    {
        

        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            // The base might be a context.  Try that first.
            if (evaluatedInputs[0] is IContext ctxt)
            {
                if (ctxt.TryGetSubcontext(evaluatedInputs[1], out IContext sub_ctxt)) return new EvaluateableContext(sub_ctxt);
                if (ctxt.TryGetProperty(evaluatedInputs[1], out IEvaluateable sub_prop)) return sub_prop.Value;
            }

            // Otherwise, the base MUST be indexable.
            IIndexable indexable = evaluatedInputs[0] as IIndexable;
            if (indexable == null) return new IndexingError(this, evaluatedInputs[0], evaluatedInputs[1], "Base is not indexable.");
            return indexable[evaluatedInputs[1]];
        }

        public override string ToString() => Inputs[0].ToString() + "[" + Inputs[1].ToString() + "]";
    }
}