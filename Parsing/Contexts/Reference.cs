using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parsing.Contexts;

namespace Parsing
{
    public sealed class Reference : IEvaluateable, IEnumerable<IContext>
    {
        private readonly List<IContext> _ContextStack;

        public IEvaluateable Head { get; private set; } = null;

        public Variable Variable => Head as Variable;
        public Function Function => Head as Function;

        private Reference(List<IContext> stack, IEvaluateable head) { this._ContextStack = stack; this.Head = head; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="root"></param>
        /// <param name="reference"></param>
        /// <param name="addedContexts"></param>
        /// <param name="addedVariables"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when a function appears in the middle of the token list.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <seealso cref="IContext"/> <paramref name="root"/> is null.</exception>
        public static bool TryCreate(IEnumerable<string> tokens, IContext root, out Reference reference, ISet<IContext> addedContexts, ISet<Variable> addedVariables)
        {
            if (root == null)
                throw new ArgumentNullException("Root cannot be null.");

            reference = null;
            IContext ctxt = root;
            List<IContext> tempStack = new List<IContext>();
            IEvaluateable head = null;

            foreach (string token in tokens)
            {
                if (ctxt == null)
                    return false;

                // Add this context to the stack.
                tempStack.Add(ctxt);

                // Might be a context-specific function.
                if (ctxt.TryCreateFunction(token, out Function f))
                {
                    head = f;
                    ctxt = null;
                    continue;
                }

                // Try getting an existing context.
                if (ctxt.TryGet(token, out IContext c))
                {
                    // This context might also be a Variable
                    if (c is Variable v || ctxt.TryGet(token, out v))                    
                        head = v;                                            
                    ctxt = c;
                    continue;
                }

                // Try ending the loop with an existing variable.
                if (ctxt.TryGet(token, out Variable var))
                {
                    head = var;
                    ctxt = null;
                    continue;
                }

                // Try creating a new sub-context, which may or may not be a Variable as well.
                if (ctxt.TryAdd(token, out c))
                {
                    addedContexts.Add(c);
                    // This new context might also be a Variable
                    if (c is Variable v || ctxt.TryGet(token, out v))
                    {
                        head = v;
                        addedVariables.Add(v);
                    }
                    ctxt = c;                    
                    continue;
                }

                // Finally, try adding a simple variable.
                if (ctxt.TryAdd(token, out var))
                {
                    head = var;
                    addedVariables.Add(var);
                    ctxt = null;
                    continue;
                }


                // In all other cases, the next reference could not be reached either as a context or as a variable.
                else return false;
            }

            reference = new Reference(tempStack, head);
            return true;
        }


        public IEvaluateable Evaluate() => Head.Evaluate();


        public IEnumerator<IContext> GetEnumerator() => _ContextStack.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _ContextStack.GetEnumerator();



        public override string ToString() => (Head == null) ? ToString(_ContextStack[0]) : Head.ToString();

        public string ToString(IContext perspective)
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
