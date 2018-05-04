using DataStructures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing
{
    public abstract class Function : IComparable<Function>
    {
        /// <summary>
        /// The priority for parsing this function.  Functions with higher priority will bind more tightly (meaning, first) than 
        /// functions with a lower priority.
        /// </summary>
        protected internal enum ParsingPriority
        {
            None = 0,
            Range = 1000000,
            Addition = 2000000, Subtraction = 2000000,
            Multiplication = 3000000, Division = 3000000,
            Exponentiation = 4000000,
            Concatenation = 5000000, And = 5000000, Or = 5000000,
            Negation = 6000000,
            Function = 7000000, Relation = 7000000
        }

        int IComparable<Function>.CompareTo(Function other) => -(this.Priority.CompareTo(other.Priority));

        protected internal virtual ParsingPriority Priority => ParsingPriority.Function;

        public virtual string Name => this.GetType().Name;        
        
        protected internal abstract IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs);

        /// <summary>Check whether the inputs given is of the right number, type, etc.</summary>
        public virtual bool ValidateInputs(IList<IEvaluatable> unEvaluatedInputs) => true;

        protected internal virtual IEvaluatable Parse(DynamicLinkedList<object>.Node node) => Parse(node, 0, 1);

        protected Clause Parse(DynamicLinkedList<object>.Node node, int preceding, int following)
        {
            Clause[] clauses = new Clause[preceding + following];
            while (following++ < clauses.Length) clauses[following] = (Clause)node.Next.Remove();
            while (--preceding >= 0) clauses[preceding] = (Clause)node.Previous.Remove();
            Clause result = Clause.Parenthetical((Function)node.Contents, clauses);
            node.Contents = result;
            return result;
        }


        /// <summary>Returns the string representation of this function from the given clause.</summary>
        /// <param name="clause">The parenthetical clause for this function to render into a string.</param>
        protected internal virtual string ToString(Clause clause) => Name + clause.Opener + " " + string.Join(", ", (IEnumerable<IEvaluatable>)clause.Inputs) + " " + clause.Closer;


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
        internal static Functions.Constant Pi = new Functions.Constant("Pi", Number.Pi);
        internal static Functions.Constant E = new Functions.Constant("E", Number.E);

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

