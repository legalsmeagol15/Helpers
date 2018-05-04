using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing
{


    public sealed class Clause : IEvaluatable
    {

        internal readonly char Opener;
        internal readonly char Closer;
        public readonly IEvaluatable[] Inputs;        
        internal Function Function { get; private set; }
        

        //private Clause(IEnumerable<IEvaluatable> inputs) { this.Inputs = inputs.ToList(); this.Function = null; }
        //private Clause(Function function, IEnumerable<IEvaluatable> expressions) { this.Inputs = Inputs.ToList(); this.Function = function; }
        //internal Clause(IEnumerable<IEvaluatable> expressions, char opener, char closer) : this(expressions) { this.Opener = opener; this.Closer = closer; }
        internal Clause(char opener, char closer, params IEvaluatable[] inputs) { this.Opener = opener; this.Closer = closer; this.Inputs = inputs; }
        internal Clause(char opener, char closer, Function f, params IEvaluatable[] inputs) : this(opener, closer, inputs) { this.Function = f; }

        public static Clause Parenthetical(Function function, params IEvaluatable[] expressions) => new Clause('(', ')', function, expressions ?? new IEvaluatable[0]);
        public static Clause Parenthetical(params IEvaluatable[] expressions) => new Clause('(', ')', expressions ?? new IEvaluatable[0]);
        public static Clause Bracketed(params IEvaluatable[] expressions) => new Clause('[', ']', expressions ?? new IEvaluatable[0]);
        public static Clause Braced(params IEvaluatable[] expressions) => new Clause('{', '}', expressions ?? new IEvaluatable[0]);
        
        //public static Clause FromFunction(Function function, IEnumerable<IEvaluatable> expressions = null) => new Clause(function, expressions ?? new List<IEvaluatable>());
        public static Clause FromSymbol(string symbol)
        {
            switch (symbol)
            {
                case "(": return Parenthetical();
                case "[": return Bracketed();
                case "{": return Braced();
                default: throw new NotImplementedException("Have not implemented nesting with symbol \"" + symbol + "\".");
            }
        }

        internal void Lex()
        {
            throw new NotImplementedException();
        }
        

        public bool IsParenthetical => Opener == '(' && Closer == ')';
        public bool IsBracketed => Opener == '[' && Closer == ']';
        public bool IsBraced => Opener == '{' && Closer == '}';


        public IEvaluatable Evaluate()
        {
            IEvaluatable[] evaluated = new IEvaluatable[Inputs.Length];
            for (int i = 0; i < Inputs.Length; i++) evaluated[i] = Inputs[i].Evaluate();
            return (Function != null) ? Function.EvaluateFunction(evaluated) : new Clause(Opener, Closer, evaluated);
        }

        public ISet<Variable> FindVariables()
        {
            HashSet<Variable> v = new HashSet<Variable>();
            FindVariables(v);
            return v;
        }
        private void FindVariables(HashSet<Variable> variables)
        {
            foreach (IEvaluatable i in Inputs)
            {
                if (i is Variable v) variables.Add(v);                
                else if (i is Clause n) n.FindVariables(variables);
            }
        }

        public override string ToString()
        {
            if (Function != null) return Function.ToString();
            StringBuilder sb = new StringBuilder();
            sb.Append(Opener + " ");
            if (Inputs.Length > 0) sb.Append(Inputs[0].ToString());
            for (int i = 1; i < Inputs.Length; i++) sb.Append(", " + Inputs[i].ToString());
            sb.Append(" " + Closer);
            return sb.ToString();
        }

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
