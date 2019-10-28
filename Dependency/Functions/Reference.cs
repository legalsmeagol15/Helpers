using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Functions
{
        
    internal sealed class Reference : IFunction, IDisposable, IReference
    {
        // Cannot implement Function directly because the count of Inputs can change.
        
        public readonly bool IsAbsolute;
        public ISyncUpdater Parent { get; set; }
        private readonly Step[] _Steps;
        
        public IEvaluateable Value { get; private set; } = Dependency.Null.Instance;

        private Reference(string[] steps, bool isAbsolute)
        {
            this._Steps = new Step[steps.Length+1];
            this._Steps[0] = new Step(null); // for the origin
            for (int i = 0; i < steps.Length; i++) this._Steps[i+1] = new Step(steps[i]);
            this.IsAbsolute = isAbsolute;
        }
        public static bool TryCreate(IContext defaultOrigin, string token, out Reference reference)
        {
            string[] splits = token.Split('.');
            for (int i = 1; i < splits.Length; i++)
                if (string.IsNullOrWhiteSpace(splits[i])) { reference = null; return false; }
            bool isAbsolute = string.IsNullOrWhiteSpace(splits[0]);
            reference = new Reference(splits, isAbsolute);
            reference._Steps[0].Context = defaultOrigin;
            reference._Steps[0].Input = null;
            return true;
        }
        public static bool TryCreate(IEvaluateable originInput, string token, out Reference reference)
        {
            string[] splits = token.Split('.');
            for (int i = 1; i < splits.Length; i++)
                if (string.IsNullOrWhiteSpace(splits[i])) { reference = null; return false; }
            bool isAbsolute = string.IsNullOrWhiteSpace(splits[0]);
            reference = new Reference(splits, isAbsolute);
            reference._Steps[0].Context = null;
            reference._Steps[0].Input = originInput;
            return true;
        }
        
        bool ISyncUpdater.Update(ISyncUpdater updatedChild)
        {
            // Find the index of the child that changed.
            int stepIdx = 0;
            for (; stepIdx < _Steps.Length && !ReferenceEquals(_Steps[stepIdx].Input, updatedChild); stepIdx++) ;
            Debug.Assert(stepIdx < _Steps.Length);

            
            Variables.Update.StructureLock.EnterReadLock();
            IEvaluateable newValue = null;
            try
            {
                // Step through all the pre-head steps, check if a reference error occurred. 
                for (; stepIdx < _Steps.Length - 1; stepIdx++)
                {
                    IContext ctxt = null;
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
                    IContext newNextContext = null;
                    IEvaluateable newNextInput = null;

                    if (ctxt.TryGetSubcontext(nextStep.String, out IContext next_ctxt))
                    {
                        newNextContext = next_ctxt;
                        newNextInput = null;
                    }
                    else if (!ctxt.TryGetProperty(nextStep.String, out IEvaluateable prop))
                    {
                        newValue = new ReferenceError("No matching property for \"" + step.String + "\".",
                            _Steps[0].Context, IsAbsolute, _Steps.Take(stepIdx + 1).Select(s => s.String).ToArray());
                        break;
                    }
                    else if (prop.Equals(nextStep.Input))
                        return false;
                    else if (prop is IAsyncUpdater)
                    {
                        newNextContext = null;
                        newNextInput = prop;
                    }
                    else if (prop is ISyncUpdater idi)
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
                        newNextContext = null;
                        newNextInput = prop;
                        
                    }

                    // Apply the new Input and Context
                    if (newNextInput != null && newNextInput.Equals(nextStep.Input)) return false;
                    if (newNextContext != null && newNextContext.Equals(nextStep.Context)) return false;
                    if (nextStep.Input is IAsyncUpdater ivi) ivi.RemoveListener(this);
                    if (newNextInput is IAsyncUpdater new_ivi) new_ivi.AddListener(this);
                    _Steps[stepIdx + 1].Input = newNextInput;
                    _Steps[stepIdx + 1].Context = newNextContext;
                }
            } finally { Variables.Update.StructureLock.ExitReadLock(); }
            

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
        
        IList<IEvaluateable> IFunction.Inputs => Inputs.ToList();
        private IEnumerable<IEvaluateable> Inputs => _Steps.Where(s => s.Input != null).Select(s => s.Input);
        IEnumerable<IEvaluateable> IReference.GetComposers() => Inputs;
        
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
            public override string ToString() => String ?? "";
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
                foreach (Step step in _Steps) if (step.Input is IAsyncUpdater ivi) ivi.RemoveListener(this);
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose() => Dispose(true);
        
        #endregion

    }

}
