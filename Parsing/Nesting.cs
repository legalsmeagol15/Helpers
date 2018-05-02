using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing
{


    public class Nesting : IExpression
    {
        public readonly List<IExpression> Inputs;
        
        internal Nesting(IEnumerable<IExpression> inputs) => this.Inputs = inputs.ToList();

        internal readonly char Opener;
        internal readonly char Closer;

        private Nesting(IEnumerable<IExpression> expressions, char opener, char closer) { Inputs = expressions.ToList(); this.Opener = opener; this.Closer = closer; }        
        public static Nesting Parenthetical(IEnumerable<IExpression> expressions = null) => new Nesting(expressions ?? new List<IExpression>(), '(', ')');
        public static Nesting Bracketed(IEnumerable<IExpression> expressions = null) => new Nesting(expressions ?? new List<IExpression>(), '[', ']');
        public static Nesting Braced(IEnumerable<IExpression> expressions = null) => new Nesting(expressions ?? new List<IExpression>(), '{', '}');
        public static Nesting FromSymbol(string symbol)
        {
            switch (symbol)
            {
                case "(": return Parenthetical();
                case "[": return Bracketed();
                case "{": return Braced();
                default: throw new NotImplementedException("Have not implemented nesting with symbol \"" + symbol + "\".");
            }
        }

        public bool IsParenthetical => Opener == '(' && Closer == ')';
        public bool IsBracketed => Opener == '[' && Closer == ']';
        public bool IsBraced => Opener == '{' && Closer == '}';


        public IExpression Evaluate()
        {
            IExpression[] evaluated = new IExpression[Inputs.Count];
            for (int i = 0; i < Inputs.Count; i++) evaluated[i] = Inputs[i].Evaluate();
            return new Nesting(evaluated, Opener, Closer);
        }

        internal void FindVariables(HashSet<Variable> variables)
        {
            foreach (IExpression i in Inputs)
            {
                if (i is Variable v) variables.Add(v);
                else if (i is Function f) f.FindVariables(variables);
                else if (i is Nesting n) n.FindVariables(variables);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Opener + " ");
            if (Inputs.Count > 0) sb.Append(Inputs[0].ToString());
            for (int i = 1; i < Inputs.Count; i++) sb.Append(", " + Inputs[i].ToString());
            sb.Append(" " + Closer);
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            Nesting other = obj as Nesting;
            if (other == null) return false;
            if (!this.Opener.Equals(other.Opener)) return false;
            if (!this.Closer.Equals(other.Closer)) return false;
            if (this.Inputs.Count != other.Inputs.Count) return false;
            for (int i = 0; i < Inputs.Count; i++) if (!Inputs[i].Equals(other.Inputs[i])) return false;
            return true;
        }

        private int _CachedHashCode = -1;
        public override int GetHashCode()
        {
            if (_CachedHashCode != -1) return _CachedHashCode;
            int h = 0;
            foreach (IExpression input in Inputs) h = unchecked(h + input.GetHashCode());
            if (_CachedHashCode == -1) _CachedHashCode++;
            return _CachedHashCode = h;
        }
    }
}
