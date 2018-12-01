using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parsing.Dependency;
using System.Threading;

namespace Parsing
{
    public sealed class Reference : IEvaluateable, IEnumerable<object>
    {
        internal readonly List<Context> Subcontexts = new List<Context>();
        internal readonly List<string> Names = new List<string>();
        internal readonly Context Root;

        public Function Function;
        public Variable Variable;
        
        public bool IsComplete => Variable != null || Function != null;
        
        internal Reference(Context root) { this.Root = root; }


        

        internal static bool TryCreate(Context root, IEnumerable<string> tokens, out Reference reference)
        {
            reference = Create(root, tokens);
            return (reference == null) ? false : true;
        }
        internal static Reference Create(Context root, IEnumerable<string> tokens)
        {
            Reference r = new Reference(root);
            return CreateInternal(root);

            Reference CreateInternal(Context context)
            {
                string name = tokens.FirstOrDefault();
                if (name == null) throw new ReferenceTooShortException(r);
                r.Variable = null;
                r.Function = null;
                r.Names.Add(name);
                tokens = tokens.Skip(1);
                if (context.TryCreateFunction(name, out Function f))
                {
                    r.Function = f;
                    if (tokens.Any()) throw new ReferenceTooLongException(r);
                    return r;
                }
                else if (context.TryGetSubcontext(name, out Context subcontext))
                {
                    r.Subcontexts.Add(subcontext);
                    return CreateInternal(subcontext);
                }
                else if (context.TryGetVariable(name, out Variable variable))
                {
                    r.Variable = variable;
                    if (tokens.Any()) throw new ReferenceTooLongException(r);
                    return r;
                }
                else if (context.TryAddSubcontext(name, out subcontext))
                {
                    if (context.TryAddVariable(name, out variable))
                    {
                        r.Variable = variable;
                        return tokens.Any() ? CreateInternal(subcontext) : r;
                    }
                    else
                    {
                        r.Subcontexts.Add(subcontext);
                        return CreateInternal(subcontext);
                    }
                }
                else if (context.TryAddVariable(name, out variable))
                {
                    r.Variable = variable;
                    if (tokens.Any()) throw new ReferenceTooLongException(r);
                    return r;
                }
                else
                    throw new ReferenceUnmatchedException(r);
            }
        }



        public IEvaluateable Evaluate() => Variable != null ? Variable.Evaluate() : Function.Evaluate();

        public IEnumerator<object> GetEnumerator() => Subcontexts.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Subcontexts.GetEnumerator();

        public override string ToString() => string.Join(".", Names);

        public string ToString(Context perspective)
        {
            int pIdx = Subcontexts.FindLastIndex(c => c.IsAncestorOf(perspective));
            if (pIdx < 0) pIdx = 0;
            return string.Join(".", Names.Skip(pIdx));            
        }

        public override bool Equals(object obj) => obj is Reference other
                                                    && ((Variable == null) ? other.Variable == null : Variable.Equals(other.Variable))
                                                    && ((Function == null) ? other.Function == null : Function.Equals(other.Function));

        public override int GetHashCode() => Variable != null ? Variable.GetHashCode() : Function.GetHashCode();
    }
}
