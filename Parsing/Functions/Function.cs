using DataStructures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    public abstract class Function : Clause, IComparable<Function>
    {
        internal FunctionDomain Domain = null;
        public bool IsLimited { get => Domain != null; }

        protected Function(params IEvaluatable[] inputs) : base("", "", inputs) { }

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
        

        protected internal virtual void ParseNode(DynamicLinkedList<object>.Node node) => ParseNode(node, 0, 1);

        protected void ParseNode(DynamicLinkedList<object>.Node node, int preceding, int following)
        {
            IEvaluatable[] inputs = new IEvaluatable[preceding + following];
            following = preceding;
            while (following < inputs.Length) inputs[following++] = (IEvaluatable)node.Next.Remove();
            while (--preceding >= 0) inputs[preceding] = (IEvaluatable)node.Previous.Remove();
            this.Inputs = inputs;
        }

        /// <summary>
        /// Returns the simplified form
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        protected virtual IEvaluatable GetSimplified(Clause inputs) => inputs;

        /// <summary>
        /// For example, a division by any variable will be undefined if that variable is 0.
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        protected internal IDictionary<Variable, List<IEvaluatable>> GetUndefined(Clause inputs) => throw new NotImplementedException();

        
        public override string ToString() => Name + ((Opener != "") ? (Opener + " ") : "") + string.Join(", ", (IEnumerable<IEvaluatable>)Inputs) + ((Closer != "") ? (" " + Closer) : "");

        public class Factory
        {
            private readonly Dictionary<string, Func<Function>> Functions = new Dictionary<string, Func<Function>>();
            private readonly Dictionary<string, Functions.Constant> Constants = new Dictionary<string, Parsing.Functions.Constant>();
            public Function this[string name] { get => Functions[name]() ; }
            
           
            public Dictionary<string, Function> GetStandardDictionary()
            {
                throw new NotImplementedException();
            }

            public bool Contains(string functionName) => Functions.ContainsKey(functionName);

            public bool TryCreateFunction(string functionName, out Function f)
            {
                if (Functions.TryGetValue(functionName, out Func<Function> constructor))
                {
                    f = constructor();
                    return true;
                }
                f = null;
                return false;
            }


            // These references are expected to be used frequently, so direct functions are included for speed
            internal Functions.Addition CreateAddition() => new Functions.Addition();
            internal Functions.Subtraction CreateSubtraction() => new Functions.Subtraction();
            internal Functions.Multiplication CreateMultiplication() => new Functions.Multiplication();
            internal Functions.Division CreateDivision() => new Functions.Division();
            internal Functions.Exponentiation CreateExponentiation() => new Functions.Exponentiation();
            internal Functions.And CreateAnd() => new Functions.And();
            internal Functions.Or CreateOr() => new Functions.Or();
            internal Functions.Relation CreateRelation() => new Functions.Relation();
            internal Functions.Range CreateRange() => new Functions.Range();
            internal Functions.Negation CreateNegation() => new Functions.Negation();
            
            public static Factory StandardFactory()
            {
                Factory factory = new Factory();
                List<Function> types = new List<Function>();
                foreach (Type type in Assembly.GetAssembly(typeof(Function)).GetTypes().Where(myType => !myType.IsAbstract && myType.IsSubclassOf(typeof(Function))))
                {
                    if (typeof(Functions.Constant).IsAssignableFrom(type)) continue;           
                    Func<Function> caller  = delegate () { return (Function)Activator.CreateInstance(type); };
                    Function specimen = caller();
                    factory.Functions[specimen.Name] = caller;
                }

                foreach (FieldInfo fInfo in typeof(Functions.Constant).GetFields())
                {
                    if (!fInfo.IsStatic || !fInfo.IsPublic) continue;
                    Functions.Constant constant = (Functions.Constant)fInfo.GetValue(null);
                    factory.Constants[constant.Name] = constant;
                    factory.Functions[constant.Name] = delegate () { return constant; };                    
                }

                return factory;
            }
        }

        protected Error InputTypeError(IList<IEvaluatable> inputs, int index, params Type[] t)
        {
            Debug.Assert(index < inputs.Count);
            Debug.Assert(!t.Contains(inputs[index].GetType()));
            return new Error("Input " + index + " incorrect type for function " + Name + ".  Expected " + string.Join(", ", t.Select((tp) => tp.Name)) + ", but given " + inputs[index].GetType().Name + ".");
        }
        
        /// <summary>Returns an input type error with a suitable message.</summary>
        /// <param name="inputs">The inputs that caused the error.</param>
        /// <param name="expected">The input counts that are acceptable.  Overloaded functions may all different counts.</param>
        protected Error InputCountError(IList<IEvaluatable> inputs, params int[] expected)
        {
            Debug.Assert(!expected.Contains(inputs.Count));
            return new Error("Incorrect input count for function " + Name + ".  Expected " + string.Join(", ", expected) + ", but given " + inputs.Count + ".");
        }

        


        /// <summary>
        /// Presumed to be infinite, unless a limit applies.
        /// </summary>
        internal sealed class FunctionDomain
        {
            private readonly Dictionary<Variable, Functions.Comparison> Limits = new Dictionary<Variable, Functions.Comparison>();
            
            // TODO:  implement
            public bool IsDefined(Variable variable, IEvaluatable value) => true;


            public void NotEqual(Variable v, IEvaluatable value)            {
                
                // TODO:  further implement, along with stuff like "GreaterThan" and etc.
            }
            public void Limit(Functions.Comparison c) => throw new NotImplementedException();
        }
        
    }



}

