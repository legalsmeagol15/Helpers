using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing
{
    public abstract class Function : IExpression
    {
        /// <summary>Invariant rule:  should never change.  A Function should be an immutable object.</summary>
        protected internal readonly IList<IExpression> Inputs;

        public virtual string Name => this.GetType().Name;

        protected internal Function(IEnumerable<IExpression> expressions = null)
        {
            if (expressions == null) this.Inputs = new IExpression[0];
            else this.Inputs = expressions.ToArray();
        }
        protected Function() { }


        public IExpression Evaluate(int iterations = 1)
        {
            IExpression[] evaluated = Inputs.ToArray();
            while (iterations-- > 0)
            {
                bool complete = true;
                for (int j = 0; j < evaluated.Length; j++)
                {
                    IExpression newExpression = evaluated[j].Evaluate();
                    if (newExpression != evaluated[j] && iterations > 0) complete = false;
                    evaluated[j] = newExpression;
                }
                if (complete) break;
            }
            return EvaluateFunction(evaluated);
        }

        IExpression IExpression.Evaluate() => Evaluate(1);

        protected abstract IExpression EvaluateFunction(IList<IExpression> evaluatedInputs);

        /// <summary> Uses recursion to identify all the variables in this function.</summary>
        public HashSet<Variable> GetContainedVariables()
        {
            HashSet<Variable> set = new HashSet<Variable>();
            FindVariables(set);
            return set;
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


        protected class TermSorter : IComparer<IExpression>
        {
            int IComparer<IExpression>.Compare(IExpression a, IExpression b)
            {
                if (a is Error || b is Error) return 0;
                if (a is Number) return (b is Number) ? 0 : -1;
                if (b is Number) return 1;

                //TODO:  implement further
                return 0;
            }
        }
        protected readonly TermSorter FormSorter = new TermSorter();

        public override string ToString() => GetString();
        protected abstract string GetString();

        public override bool Equals(object obj)
        {
            Function f = obj as Function;
            if (f == null) return false;
            throw new NotImplementedException();
        }


        private int _CachedHashCode = -1;
        public sealed override int GetHashCode()
        {
            /// Cache the hashcode, or else getting the hash code will be an absurd iterative process.
            if (_CachedHashCode != -1) return _CachedHashCode;
            int h = 0;
            foreach (IExpression input in Inputs) h = unchecked(h + input.GetHashCode());
            if (h == -1) h++;
            return _CachedHashCode = h;
        }


        public class Factory
        {
            private static Dictionary<string, Function> _Dictionary = new Dictionary<string, Function>();
            public bool TryMakeFunction(string rawToken, out Function nf)
            {
                throw new NotImplementedException();
            }
        }

    }





}


namespace Helpers.Parsing.Functions
{
    /// <summary>
    /// A special type of function typically written with a special relationship to its inputs.  For 
    /// example, addition could be written "add(a,b)", instead we use "a + b" with a special symbol 
    /// in between its two inputs.
    /// </summary>
    public abstract class Operator : Function
    {
        protected Operator(IEnumerable<IExpression> expressions) : base(expressions) { }
        protected Operator(params IExpression[] expressions) : base(expressions) { }
    }


    public class Addition : Operator
    {
        internal Addition(IExpression a, IExpression b) : base(a, b) { }
        internal Addition(IEnumerable<IExpression> expressions) : base(expressions) { }
        internal Addition() : base(null, null) { }
        protected override IExpression EvaluateFunction(IList<IExpression> evaluatedInputs)
        {
            decimal m = 0.0m;
            List<IExpression> expressions = new List<IExpression>();
            foreach (IExpression input in evaluatedInputs)
            {
                if (input is Number n) m += n.Value;
                else expressions.Add(input);
            }
            if (expressions.Count == 0)
                return new Number(m);
            else
            {
                expressions.Add(new Number(m));
                expressions.Sort(FormSorter);
                return new Addition(expressions);
            }
        }


        protected override string GetString() => string.Join(" + ", Inputs);
    }


    public class And : Operator
    {
        internal And(IExpression a, IExpression b) : base(a, b) { }
        internal And(IEnumerable<IExpression> expressions) : base(expressions) { }
        internal And() : base(null, null) { }
        protected override IExpression EvaluateFunction(IList<IExpression> evaluatedInputs)
        {
            if (evaluatedInputs.Count != 2) return new Error("'And' must have exactly two inputs.");
            if (evaluatedInputs[0] is Boolean a && evaluatedInputs[1] is Boolean b) return a | b;
            return new Subtraction(evaluatedInputs[0], evaluatedInputs[1]);
        }

        protected override string GetString() => Inputs[0] + " & " + Inputs[1];
    }

    public class Division : Operator
    {
        internal Division(IExpression a, IExpression b) : base(a, b) { }
        internal Division(IEnumerable<IExpression> expressions) : base(expressions) { }
        internal Division() : base(null, null) { }
        protected override IExpression EvaluateFunction(IList<IExpression> evaluatedInputs)
        {
            if (evaluatedInputs.Count != 2) return new Error("Division must have exactly two inputs.");
            if (evaluatedInputs[0] is Number a && evaluatedInputs[1] is Number b) return a / b;
            return new Subtraction(evaluatedInputs[0], evaluatedInputs[1]);
        }

        protected override string GetString() => Inputs[0] + " / " + Inputs[1];
    }


    public class Exponentiation : Operator
    {
        internal Exponentiation(IExpression a, IExpression b) : base(a, b) { }
        internal Exponentiation(IEnumerable<IExpression> expressions) : base(expressions) { }
        internal Exponentiation() : base(null, null) { }
        protected override IExpression EvaluateFunction(IList<IExpression> evaluatedInputs)
        {
            if (evaluatedInputs.Count != 2) return new Error("Exponentiation must have exactly two inputs.");
            if (evaluatedInputs[0] is Number a && evaluatedInputs[1] is Number b) return a ^ b;
            return new Subtraction(evaluatedInputs[0], evaluatedInputs[1]);
        }

        protected override string GetString() => Inputs[0] + " ^ " + Inputs[1];
    }


    public class Multiplication : Operator
    {
        internal Multiplication() : base(null, null) { }
        public Multiplication(IExpression a, IExpression b) : base(a, b) { }
        internal Multiplication(IEnumerable<IExpression> expressions) : base(expressions) { }
        protected override IExpression EvaluateFunction(IList<IExpression> evaluatedInputs)
        {
            decimal m = 0.0m;
            List<IExpression> expressions = new List<IExpression>();
            foreach (IExpression input in evaluatedInputs)
            {
                if (input is Number n) m *= n.Value;
                else expressions.Add(input);
            }
            if (expressions.Count == 0)
                return new Number(m);
            else
            {
                expressions.Add(new Number(m));
                expressions.Sort(FormSorter);
                return new Addition(expressions);
            }
        }
        protected override string GetString() => string.Join(" * ", Inputs);
    }


    public class Negation : Operator
    {
        internal Negation(IExpression a) : base(a) { }
        internal Negation() : base((IExpression)null) { }
        protected override IExpression EvaluateFunction(IList<IExpression> evaluatedInputs)
        {
            if (evaluatedInputs.Count != 1) return new Error("Negation must have exactly one input.");
            switch (evaluatedInputs[0])
            {
                case Number n: return -n;
            }
            return new Negation(evaluatedInputs[0]);
        }


        protected override string GetString()
        {
            switch (Inputs[0])
            {
                case Number n: return "-" + n.ToString();
                case Boolean b: return "!" + b.ToString();
            }
            return base.ToString();
        }
    }



    public class Or : Operator
    {
        internal Or(IExpression a, IExpression b) : base(a, b) { }
        internal Or(IEnumerable<IExpression> expressions) : base(expressions) { }
        internal Or() : base(null, null) { }
        protected override IExpression EvaluateFunction(IList<IExpression> evaluatedInputs)
        {
            if (evaluatedInputs.Count != 2) return new Error("'Or' must have exactly two inputs.");
            if (evaluatedInputs[0] is Boolean a && evaluatedInputs[1] is Boolean b) return a | b;
            return new Subtraction(evaluatedInputs[0], evaluatedInputs[1]);
        }

        protected override string GetString() => Inputs[0] + " | " + Inputs[1];
    }



    public class Subtraction : Operator
    {
        internal Subtraction(IExpression a, IExpression b) : base(a, b) { }
        internal Subtraction() : base(null, null) { }
        protected override IExpression EvaluateFunction(IList<IExpression> evaluatedInputs)
        {
            if (evaluatedInputs.Count != 2) return new Error("Subtraction must have exactly two inputs.");
            if (evaluatedInputs[0] is Number a && evaluatedInputs[1] is Number b) return a - b;
            return new Subtraction(evaluatedInputs[0], evaluatedInputs[1]);
        }

        protected override string GetString() => Inputs[0].ToString() + " - " + Inputs[1].ToString();
    }
}
