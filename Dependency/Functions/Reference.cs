using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    //internal struct EvaluateableContext : IContext, IEvaluateable, ITypeGuarantee, ILiteral<IContext>
    //{
    //    public readonly IContext Context;

    //    public EvaluateableContext(IContext context) { this.Context = context; }

    //    bool IContext.TryGetSubcontext(object path, out IContext ctxt) => Context.TryGetSubcontext(path, out ctxt);

    //    bool IContext.TryGetProperty(object path, out IEvaluateable source) => Context.TryGetProperty(path, out source);

    //    IEvaluateable IEvaluateable.Value => this;

    //    TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Context;

    //    IContext ILiteral<IContext>.CLRValue => Context;

    //    public override string ToString() => Context.ToString();

    //    public override bool Equals(object obj) => (obj is EvaluateableContext other) && Context.Equals(other.Context);

    //    public override int GetHashCode() => throw new InvalidOperationException();
        
    //}
    

    [NonVariadic(0, TypeFlags.Context)]
    internal sealed class Reference : IFunction, IDisposable
    {
        public IContext Origin { get; private set; }
        public readonly bool IsAbsolute;
        public IDynamicItem Parent { get; set; }
        private readonly Step[] _Steps;
        IList<IEvaluateable> IFunction.Inputs => _Steps.Select(s => s.Input).ToList();
        public IEvaluateable Value { get; private set; } = Dependency.Null.Instance;

        private Reference(IContext defaultOrigin, string[] steps, bool isAbsolute)
        {
            this.Origin = defaultOrigin ?? throw new ArgumentNullException("defaultOrigin");
            this._Steps = new Step[steps.Length+1];
            this._Steps[0] = new Step(null) { Context = defaultOrigin, Input = new ContextWrapper(defaultOrigin) }; // for the origin
            for (int i = 0; i < steps.Length; i++) this._Steps[i+1] = new Step(steps[i]);
            this.IsAbsolute = isAbsolute;
        }
        public bool TryCreate(IContext defaultOrigin, string token, out Reference reference)
        {
            string[] splits = token.Split('.');
            for (int i = 1; i < splits.Length; i++)
                if (string.IsNullOrWhiteSpace(splits[i])) { reference = null; return false; }
            bool isAbsolute = string.IsNullOrWhiteSpace(splits[0]);
            reference = new Reference(defaultOrigin, splits, isAbsolute);
            return true;
        }
        bool IDynamicItem.Update(IDynamicItem updatedChild)
        {
            // Find the index of the child that changed.
            int stepIdx = 0;
            for (; stepIdx < _Steps.Length && !ReferenceEquals(_Steps[stepIdx].Input, updatedChild); stepIdx++) ;
            Debug.Assert(stepIdx < _Steps.Length);

            // Step through all the pre-head steps, check if a reference error occurred.
            IEvaluateable newValue = null;
            IContext ctxt = null;
            for (; stepIdx < _Steps.Length - 1; stepIdx++)
            {
                Step step = _Steps[stepIdx];
                Step nextStep = _Steps[stepIdx + 1];

                // What is the context at this step?  If there is variable input, the context will 
                // be the evaluation of the variable (if it can be made into a context).
                if (step.Input != null)
                {
                    IEvaluateable inputValue = step.Input.Value;

                    // If the input's value hasn't changed (and the input's value is a struct or literal) then no change 
                    // will occur later down the line.
                    if (inputValue.Equals(step.Context) && !inputValue.GetType().IsClass) return false;

                    // Is the input's new value a valid context?  If not, this evaluates to an error.
                    ctxt = inputValue as IContext;
                    if (ctxt == null)
                    {
                        string msg = (stepIdx == 0) ? "Origin context was invalid."
                                                    : "Evaluation of segment " + step.String + " is invalid context (" + inputValue.GetType().Name + ").";
                        newValue = new ReferenceError(msg, _Steps[0].Context, IsAbsolute, _Steps.Take(stepIdx + 1).Select(s => s.String).ToArray());
                        break;
                    }
                }
                // Since we have no variable input, if there's no context for this step, re-evaluate the whole thing.
                else if (step.Context == null)
                {
                    stepIdx = 0;
                    continue;
                }
                // Otherwise, just stand in the cached context for this step.
                else
                    ctxt = step.Context;

                // If this context has a matching subcontext, the next step's subcontext must be that.
                if (ctxt.TryGetSubcontext(step.String, out IContext next_ctxt))
                {
                    ctxt = next_ctxt;
                    if (ctxt.Equals(nextStep.Context) && nextStep.Input == null) return false;
                    _Steps[stepIdx + 1].Context = ctxt;
                    _Steps[stepIdx + 1].Input = null;
                    continue;
                }
                else if (!ctxt.TryGetProperty(step.String, out IEvaluateable prop))
                {
                    newValue = new ReferenceError("No matching property for \"" + step.String + "\".",
                        _Steps[0].Context, IsAbsolute, _Steps.Take(stepIdx + 1).Select(s => s.String).ToArray());
                    break;
                }
                else if (prop.Equals(_Steps[stepIdx + 1].Input))
                    return false;
                else if (prop is IVariableInternal ivi)
                {
                    IEvaluateable oldInput = _Steps[stepIdx + 1].Input;
                    if (nextStep.Context == null && ivi.Equals(oldInput)) return false;
                    if (oldInput != null && oldInput is IVariableInternal old_ivi)
                        old_ivi.RemoveListener(this);
                    ivi.AddListener(this);
                }
                else if (prop is IDynamicItem idi)
                {
                    newValue = new ReferenceError("Cannot reference any dynamic property except variables",
                        _Steps[0].Context, IsAbsolute, _Steps.Take(stepIdx + 1).Select(s => s.String).ToArray());
                    break;
                }
                else if (prop.GetType().IsClass)
                {
                    newValue = new ReferenceError("References allowed only for literal values and variables",
                        _Steps[0].Context, IsAbsolute, _Steps.Take(stepIdx + 1).Select(s => s.String).ToArray());
                    break;
                }
                else
                {
                    if (nextStep.Context == null && prop.Equals(nextStep.Input)) return false;
                    _Steps[stepIdx + 1].Input = prop;
                    _Steps[stepIdx + 1].Context = null;
                }
                
                
                
            }

            // For the last step, the newValue will either be the Input's Value, or it will be a wrapped IContext.
            if (newValue == null)
            {
                Step lastStep = _Steps[stepIdx];
                if (lastStep.Input != null) newValue = lastStep.Input.Value;
                else newValue = new ContextWrapper(lastStep.Context);
            }

            if (newValue.Equals(Value)) return false;
            Value = newValue;
            return true;
        }

        private struct ContextWrapper : IEvaluateable, IContext
        {
            public readonly IContext Context;
            public ContextWrapper(IContext context) { this.Context = context; }

            IEvaluateable IEvaluateable.Value => this;
            bool IContext.TryGetProperty(object path, out IEvaluateable source) => Context.TryGetProperty(path, out source);
            bool IContext.TryGetSubcontext(object path, out IContext ctxt) => Context.TryGetSubcontext(path, out ctxt);
            public override bool Equals(object obj) => (obj is ContextWrapper other) && other.Context.Equals(Context);
            public override int GetHashCode() => throw new NotImplementedException();
        }

        private struct Step
        {
            public readonly string String;
            public IContext Context;
            public IEvaluateable Input;
            public Step (string str) { this.String = str; Context = default(IContext); Input = default(IEvaluateable); }
            public override string ToString() => String;
        }
        
        public override string ToString() => string.Join(".", _Steps);
        
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
                if (Head is IVariableInternal iv) iv.RemoveListener(this);
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
    [NonVariadic(0, TypeFlags.Indexable | TypeFlags.Context | TypeFlags.Vector | TypeFlags.Range, TypeFlags.Any)]
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
                if (newBase.TryGetProperty(newOrdinal, out IEvaluateable sub_prop)) return sub_prop.Value;
                else return new IndexingError(this, newBase, newOrdinal, "Base is not indexable by " + newOrdinal.ToString());
            }
            finally { Dependency.Variables.Update.StructureLock.ExitUpgradeableReadLock(); }
        }

        public override string ToString() => Inputs[0].ToString() + "[" + Inputs[1].ToString() + "]";
    }
}
