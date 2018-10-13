using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    public sealed class Reference : IEvaluateable, IEnumerable<Context>
    {
        private readonly List<Context> _ContextStack = new List<Context>();
        public Variable Variable { get; private set; }

        private Reference() { }

        internal static Reference FromTokens(IEnumerable<string> tokens, Context root, out IEnumerable<Context> addedContexts, out IEnumerable<Variable> addedVariables)
        {
            Context ctxt = root;
            Reference r = new Reference();
            List<Context> tempAddedCtxt = new List<Context>();
            List<Variable> tempAddedVars = new List<Variable>();

            try
            {
                foreach (string token in tokens)
                {                    
                    // r.Variable should not be null until the last token.
                    if (r.Variable != null)
                        throw new InvalidOperationException("Reference to non-contextual variable " + r.Variable.Name + " as a sub-context of context " + ctxt.Name + ".");

                    // Add this context to the stack.
                    r._ContextStack.Add(ctxt);

                    // Try getting an existing context.
                    if (ctxt.TryGet(token, out ctxt)) continue;

                    // Try ending the loop with an existing variable.
                    else if (ctxt.TryGet(token, out Variable var)) r.Variable = var;

                    // Try creating a vanilla context (one that does not also function as a variable).
                    else if (ctxt.TryAddContext(token, out ctxt)) tempAddedCtxt.Add(ctxt);

                    // Try creating a dual context/variable
                    else if (ctxt.TryAddAsContext(token, out ctxt, out Variable newVar)) { tempAddedCtxt.Add(ctxt); tempAddedVars.Add(newVar); }

                    // Try creating a loop-ending variable.
                    else if (ctxt.TryAddWithinContext(token, out newVar)) r.Variable = var;

                    // In all other cases, the next reference could not be reached either as a context or as a variable.
                    else throw new InvalidOperationException("Reference string could not proceed to the next token \"" + token + "\".");
                }
            }
            catch
            {
                foreach (Variable v in tempAddedVars) if (v.Context != null) v.Context.TryDelete(v);
                foreach (Context c in tempAddedCtxt) if (c.Parent != null) c.Parent.TryDelete(c);
                throw;
            }

            addedContexts = tempAddedCtxt;
            addedVariables = tempAddedVars;
            return r;
        }

        public IEvaluateable Evaluate() => Variable.Evaluate();

        public IEnumerator<Context> GetEnumerator()
        {
            return ((IEnumerable<Context>)_ContextStack).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => _ContextStack.GetEnumerator();
    }
}
