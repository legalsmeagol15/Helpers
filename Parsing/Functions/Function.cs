using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing
{
    internal abstract class Function
    {                
        public virtual string Name => this.GetType().Name;        
        
        protected internal abstract IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs);

        /// <summary>Check whether the inputs given is of the right number, type, etc.</summary>
        public virtual bool ValidateInputs(IList<IEvaluatable> unEvaluatedInputs) => true;
        
        /// <summary>Returns the string representation of this function from the given clause.</summary>
        /// <param name="clause">The parenthetical clause for this function to render into a string.</param>
        protected internal virtual string ToString(Clause clause)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Name);
            sb.Append(clause.Opener + " ");
            if (clause.Inputs.Count > 0) sb.Append(clause.Inputs[0].ToString());
            for (int i = 1; i < clause.Inputs.Count; i++) sb.Append(", " + clause.Inputs[i].ToString());
            sb.Append(" " + clause.Closer);
            return sb.ToString();
        }


        public static Function Addition = new Functions.Addition();
        public static Function Subtraction = new Functions.Subtraction();
        public static Function Division = new Functions.Division();
        public static Function Multiplication = new Functions.Multiplication();
        public static Function Negation = new Functions.Negation();
        public static Function Exponentiation = new Functions.Exponentiation();
        public static Function And = new Functions.And();
        public static Function Or = new Functions.Or();
        public static Function Relation = new Functions.Relation();
        public static Function Range = new Functions.Range();
        public static Function Concatenation = new Functions.Concatenation();
        public static Functions.Constant Pi = new Functions.Constant("Pi", Number.Pi);
        public static Functions.Constant E = new Functions.Constant("E", Number.E);

        public class Factory
        {
            private static Dictionary<string, Function> _Dictionary = new Dictionary<string, Function>();
            public Function this[string name] { get => _Dictionary[name]; }

            public Factory()
            {
                _Dictionary.Add("Addition", Addition);
                _Dictionary.Add("Subtraction", Subtraction);
                _Dictionary.Add("Division", Division);
                _Dictionary.Add("Multiplication", Multiplication);
                _Dictionary.Add("Exponentiation", Exponentiation);
                _Dictionary.Add("And", And);
                _Dictionary.Add("Or", Or);
                _Dictionary.Add("Relation", Relation);
                _Dictionary.Add("Range", Range);
                _Dictionary.Add("Concatenation", Concatenation);
                _Dictionary.Add("PI", Pi);
                _Dictionary.Add("E", E);
            }

            internal bool TryGetFunction(string rawToken, out Function f) => _Dictionary.TryGetValue(rawToken, out f);
        }

    }





}


namespace Helpers.Parsing.Functions
{
    internal class Constant : Function, IEvaluatable
    {
        private readonly string _ConstantName;
        public override string Name => _ConstantName;
        public readonly Number Value;
        public Constant(string name, Number number) { this._ConstantName = name; Value = number; }
        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs) => throw new Exception("Sanity check.");
        IEvaluatable IEvaluatable.Evaluate() => Value;
    }

    /// <summary>
    /// A special type of function typically written with a special relationship to its inputs.  For 
    /// example, addition could be written "add(a,b)", instead we use "a + b" with a special symbol 
    /// in between its two inputs.
    /// </summary>
    internal abstract class Operator : Function, IComparable<Operator>
    {
        
        public override string Name => base.Name + "&Operator";

        
    }


    internal sealed class Addition : Operator
    {      
        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            decimal m = 0.0m;
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs)
            {
                if (input is Number n) m += n.Value;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return new Number(m);
        }
        protected internal override string ToString(Clause n) => string.Join(" + ", n.Inputs);
    }


    internal class And : Operator
    {   
        protected internal  override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            bool b = true;
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs)
            {
                if (input is Boolean i) b &= i;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return new  Boolean(b);
        }
        protected internal override string ToString(Clause n) => string.Join(" & ", n.Inputs);
    }

    internal sealed class Concatenation : Operator
    {
        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            StringBuilder sb = new StringBuilder();
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs) sb.Append(input.ToString());                            
            if (expressions.Count > 0) throw new NotImplementedException();
            return new String(sb.ToString());
        }
        protected internal override string ToString(Clause n) => string.Join(" & ", n.Inputs);
    }

    internal class Division : Operator
    {       
        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            if (evaluatedInputs.Count != 2) return new Error("Division must have exactly two inputs.");
            if (evaluatedInputs[0] is Number a && evaluatedInputs[1] is Number b) return a / b;
            throw new NotImplementedException();
        }
        protected internal override string ToString(Clause n) => string.Join(" / ", n.Inputs);
    }


    internal class Exponentiation : Operator
    {
        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            decimal m = 0.0m;
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs)
            {
                if (input is Number n) m^=n;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return new Number(m);
        }
        protected internal override string ToString(Clause n) => string.Join(" ^ ", n.Inputs);
    }


    internal class Multiplication : Operator
    {
        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            decimal m = 0.0m;
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs)
            {
                if (input is Number n) m *= n;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return new Number(m);
        }
        protected internal override string ToString(Clause n) => string.Join(" * ", n.Inputs);
    }


    internal class Negation : Operator
    {
        
        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            if (evaluatedInputs.Count != 1) return new Error("Negation must have exactly one input.");
            switch (evaluatedInputs[0])
            {
                case Number n: return -n;
                case Boolean b: return !b;
            }
            throw new NotImplementedException();
        }


        protected internal override string ToString(Clause clause)
        {
            switch (clause.Inputs[0])
            {
                case Number n: return "-" + n.ToString();
                case Boolean b: return "!" + b.ToString();
            }
            return base.ToString();
        }
    }



    internal class Or : Operator
    {
        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            bool b =false;
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs)
            {
                if (input is Boolean i) b |= i;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return new Boolean(b);
        }
        protected internal override string ToString(Clause n) => string.Join(" & ", n.Inputs);
    }

    internal class Range : Function
    {
        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            throw new NotImplementedException();
        }
    }

    internal class Relation : Function
    {
        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            throw new NotImplementedException();
        }
    }

    internal class Subtraction : Operator
    {
        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            if (evaluatedInputs.Count != 2) return new Error("Subtraction must have exactly two inputs.");
            if (evaluatedInputs[0] is Number a && evaluatedInputs[1] is Number b) return a - b;
            throw new NotImplementedException();
        }
        protected internal override string ToString(Clause n) => string.Join(" - ", n.Inputs);
    }
}
