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

        public IEvaluateable Head { get; private set; } = null;

        public bool IsVariable => Head is Variable;
        public bool IsFunction => Head is Function;

        private Reference(List<Context> stack, IEvaluateable head) { this._ContextStack = stack; this.Head = head; }

        public static bool TryCreate(IEnumerable<string> tokens, Context root, out Reference reference, ISet<Context> addedContexts, ISet<Variable> addedVariables)
        {
            reference = null;
            Context ctxt = root;
            List<Context> tempStack = new List<Context>();
            IEvaluateable head = null;

            foreach (string token in tokens)
            {
                // r.Variable should not be null until the last token.
                if (head != null) return false;

                // Add this context to the stack.
                tempStack.Add(ctxt);

                // Might be a context-specific function.
                if (ctxt.TryCreateFunction(token, out Function f)) head = f;

                // Try getting an existing context.
                else if (ctxt.TryGet(token, out Context c)) ctxt = c;

                // Try ending the loop with an existing variable.
                else if (ctxt.TryGet(token, out Variable var)) head = var;

                // Try creating a vanilla context (one that does not also function as a variable).
                else if (ctxt.TryAddContext(token, out c)) addedContexts.Add(ctxt = c);

                // Try creating a dual context/variable
                else if (ctxt.TryAddAsContext(token, out c, out Variable newVar)) { addedContexts.Add(ctxt = c); addedVariables.Add(newVar); }

                // Try creating a loop-ending variable.
                else if (ctxt.TryAddWithinContext(token, out newVar)) head = var;



                // In all other cases, the next reference could not be reached either as a context or as a variable.
                else return false;
            }

            reference = new Reference(tempStack, head);
            return true;
        }


        public IEvaluateable Evaluate() => Head.Evaluate();


        public IEnumerator<Context> GetEnumerator() => _ContextStack.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _ContextStack.GetEnumerator();



        public override string ToString() => (Head == null) ? ToString(_ContextStack[0]) : Head.ToString();

        public string ToString(Context perspective)
        {
            perspective = _ContextStack.LastOrDefault(c => Context.IsDescendant(c, perspective));
            if (perspective == null) return ToString(_ContextStack[0]);
            int idx = _ContextStack.IndexOf(perspective);
            if (idx < 0) return ToString(_ContextStack[0]);
            return string.Join(".", _ContextStack.Skip(_ContextStack.IndexOf(perspective)).Select(c => c.Name))
                   + ((Head is Variable v) ? v.Name : (Head is Function f) ? f.Name : "");
        }

        public override bool Equals(object obj) => (Head == null) ? obj == null : Head.Equals(obj);

        public override int GetHashCode() => Head.GetHashCode();
    }
}
