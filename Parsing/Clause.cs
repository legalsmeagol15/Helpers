using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{


    public class Clause : IEvaluatable
    {

        internal string Opener;
        internal string Closer;
        internal ISet<DataContext.Variable> Terms;

        public IEvaluatable[] Inputs { get; protected set; }

        internal Clause(string opener, string closer, params IEvaluatable[] inputs) { this.Opener = opener; this.Closer = closer; this.Inputs = inputs ?? new IEvaluatable[0]; }


        
        public static Clause Parenthetical(params IEvaluatable[] expressions) => new Clause("(", ")", expressions);
        public static Clause Bracketed(params IEvaluatable[] expressions) => new Clause("[", "]", expressions);
        public static Clause Braced(params IEvaluatable[] expressions) => new Clause("{", "}", expressions);
        public static Clause Naked(params IEvaluatable[] expressions) => new Clause("", "", expressions);
        

        public bool IsParenthetical => Opener == "(" && Closer == ")";
        public bool IsBracketed => Opener == "[" && Closer == "]";
        public bool IsBraced => Opener == "{" && Closer == "}";

        /// <summary>Call to evaluate this function or clause.</summary>
        public IEvaluatable Evaluate() => Evaluate(EvaluateInputs());


        /// <summary>In the base class, simply returns the evaluation of all the inputs.</summary>
        protected virtual IEvaluatable[] EvaluateInputs() => Inputs.Select(i => i.Evaluate()).ToArray();


        /// <summary>
        /// Override if a function evaluates according to the given evaluated inputs, in a manner distinct from simply being a new clause 
        /// containing those inputs.
        /// </summary>
        /// <param name="evaluatedInputs">The inputs, which have already been recursively evaluated.</param>
        /// <returns>The evaluation of this clause or function, given the pre-evaluated inputs.</returns>
        protected internal virtual IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length == 1) return evaluatedInputs[0];
            return new Clause(Opener, Closer, evaluatedInputs);
        }


        public IEvaluatable Evaluate(params object[] inputs)
        {
            IEvaluatable[] evaluated = new IEvaluatable[inputs.Length];
            for (int idx = 0; idx < inputs.Length; idx++)
            {
                switch (inputs[idx])
                {
                    case decimal m: evaluated[idx] = new Number(m); break;
                    case double d: evaluated[idx] = new Number(d); break;
                    case int i: evaluated[idx] = new Number((decimal)i); break;
                    case bool b: evaluated[idx] = Boolean.FromBool(b); break;
                    case string str: evaluated[idx] = new String(str);break;
                    case IEvaluatable ie: evaluated[idx] = ie; break;
                    default: throw new ArgumentException("Invalid type: " + inputs[idx].GetType().Name + ".");
                }
                evaluated[idx] = evaluated[idx].Evaluate();
            }
            return Evaluate(evaluated);
        }

        
        public override string ToString() => (Opener != "" ? Opener + " " : "") + string.Join(", ", (object[])Inputs) + (Closer != "" ? " " + Closer : "");


        public override bool Equals(object obj)
        {
            Clause other = obj as Clause;
            if (other == null) return false;
            if (!this.Opener.Equals(other.Opener)) return false;
            if (!this.Closer.Equals(other.Closer)) return false;
            if (this.Inputs.Length != other.Inputs.Length) return false;
            for (int i = 0; i < Inputs.Length; i++) if (!Inputs[i].Equals(other.Inputs[i])) return false;
            return true;
        }


        private int _CachedHashCode = -1;
        public override int GetHashCode()
        {
            if (_CachedHashCode != -1) return _CachedHashCode;
            int h = 0;
            foreach (IEvaluatable input in Inputs) h = unchecked(h + input.GetHashCode());
            if (_CachedHashCode == -1) _CachedHashCode++;
            return _CachedHashCode = h;
        }
        
    }
}
