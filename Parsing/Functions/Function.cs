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
    [Serializable]
    public abstract partial class Context
    {
        public static Function.Factory Functions = Function.Factory.StandardFactory;

        [Serializable]
        public abstract class Function : Clause
        {
            protected internal VariableDomain Domain { get; private set; }
            protected internal VariableDomain CoDomain { get; private set; }

            public virtual string Name => this.GetType().Name;
            


            protected Function(params IEvaluateable[] inputs) : base("", "", inputs)
            {
                if (inputs == null) return;
                IEnumerable<Variable> vars = inputs.OfType<Variable>();
                if (!vars.Any()) return;
                Terms = new HashSet<Variable>(vars);
                foreach (Function f in inputs.OfType<Function>()) foreach (Variable v in f.Terms) Terms.Add(v);
            }

            public override IEvaluateable Evaluate() => Evaluate(GetEvaluatedInputs());

            public abstract IEvaluateable Evaluate(params IEvaluateable[] inputs);

            #region Function calculus

            protected internal static IEvaluateable Differentiate(IEvaluateable function, Variable v)
            {
                switch (function)
                {
                    case Function f: return (f.Terms.Contains(v)) ? f.GetDerivative(v) : Number.Zero;
                    case Clause c:
                        if (c.Inputs.Length != 1) return new Error("Cannot differentiate multi-input clause: " + c.ToString());
                        return Differentiate(c.Inputs[0], v);
                    case Number n: return Number.Zero;
                    case Variable var: return (var == v) ? Number.One : Number.Zero;
                    case Error e: return e;
                    default: return new Error("Cannot find derivative of: " + function.ToString());
                }
            }


            protected virtual IEvaluateable GetDerivative(Variable v) => NonDifferentiableFunctionError();

            protected IEvaluateable ApplyChainRule(Variable v, IEvaluateable fPrime, IEvaluateable g)
            {
                Functions.Multiplication result = new Functions.Multiplication(fPrime, g);
                return result.GetSimplified();
            }

            protected internal virtual IEvaluateable GetSimplified() => this;

            protected Error NonDifferentiableFunctionError() => new Error("Nondifferentiable function: " + this.ToString());

            #endregion





            #region Function parsing

            /// <summary>
            /// The priority for parsing this function.  Functions with higher priority will bind more loosely (meaning, after) than 
            /// functions with a lower priority.
            /// </summary>
            protected internal enum ParsingPriority
            {
                None = 100000,
                Span = 90000,
                Addition = 80000, Subtraction = 80000,
                Multiplication = 70000, Division = 70000,
                Exponentiation = 60000,
                Concatenation = 50000, And = 50000, Or = 50000,
                Negation = 40000,
                Function = 30000, Relation = 30000
            }

            protected internal virtual ParsingPriority Priority => ParsingPriority.Function;

            /// <summary>
            /// Override to determine how a function is parsed in its token list.  The default behavior is to call ParseNode(node, 0, 1).
            /// </summary>
            protected internal virtual void ParseNode(DynamicLinkedList<object>.Node node) => ParseNode(node, 0, 1);

            /// <summary>
            /// Parses this node in its token list, with the indicate number of preceding and following tokens.  For example, parsing the 
            /// Sin function will be called with 0 preceding and 1 following, because no token relevant to the Sin function is expected 
            /// to precede it and only 1 token (the Sin function's contents) is expected to follow it.
            /// </summary>
            /// <param name="node"></param>
            /// <param name="preceding"></param>
            /// <param name="following"></param>
            protected void ParseNode(DynamicLinkedList<object>.Node node, int preceding, int following)
            {
                IEvaluateable[] inputs = new IEvaluateable[preceding + following];
                following = preceding;
                while (following < inputs.Length) inputs[following++] = (IEvaluateable)node.Next.Remove();
                while (--preceding >= 0) inputs[preceding] = (IEvaluateable)node.Previous.Remove();
                this.Inputs = inputs;
            }

            #endregion




            public override string ToString() => Name + ((Opener != "") ? (Opener + " ") : "") + string.Join(", ", (IEnumerable<IEvaluateable>)Inputs) + ((Closer != "") ? (" " + Closer) : "");

            public sealed class Factory
            {

                private readonly static Type[] IEvaluatableArray = new Type[] { typeof(IEvaluateable[]) };

                private readonly static object[] EmptyIEvaluatable = new object[] { new IEvaluateable[0] };

                /// <summary>
                /// The function used to standardize function names.  Default behavior is to return the upper-case conversion of the function 
                /// name.
                /// </summary>
                public Func<string, string> Standardize = (s) => s.ToUpper();

                private readonly Dictionary<string, Func<Function>> Functions = new Dictionary<string, Func<Function>>();
                private readonly Dictionary<string, Functions.Constant> Constants = new Dictionary<string, Parsing.Functions.Constant>();


                public Function this[string name] { get => Functions[Standardize(name)](); }



                public bool Contains(string functionName) => Functions.ContainsKey(Standardize(functionName));

                public bool TryCreateFunction(string functionName, out Function f)
                {
                    if (Functions.TryGetValue(Standardize(functionName), out Func<Function> constructor))
                    {
                        f = constructor();
                        return true;
                    }
                    f = null;
                    return false;
                }


                public bool AddRecipe(string functionName, Func<Function> recipe)
                {
                    functionName = Standardize(functionName);
                    if (Functions.ContainsKey(functionName)) return false;
                    Functions[functionName] = recipe;
                    return true;
                }

                internal bool AddConstant(Functions.Constant constant)
                {
                    string constantName = Standardize(constant.Name);
                    if (Constants.ContainsKey(constantName) || Functions.ContainsKey(constantName)) return false;
                    Constants[constantName] = constant;
                    Functions[constantName] = () => constant;
                    return true;
                }


                #region Factory universal creators
                /// It is expected that ALL function factories will be able to interpret operators, so direct creation methods 
                /// are included for operators.

                internal static Functions.Addition CreateAddition() => new Functions.Addition();
                internal static Functions.Subtraction CreateSubtraction() => new Functions.Subtraction();
                internal static Functions.Multiplication CreateMultiplication() => new Functions.Multiplication();
                internal static Functions.Division CreateDivision() => new Functions.Division();
                internal static Functions.Exponentiation CreateExponentiation() => new Functions.Exponentiation();
                internal static Functions.And CreateAnd() => new Functions.And();
                internal static Functions.Or CreateOr() => new Functions.Or();
                internal static Functions.Relation CreateRelation() => new Functions.Relation();
                internal static Functions.Span CreateRange() => new Functions.Span();
                internal static Functions.Negation CreateNegation() => new Functions.Negation();

                #endregion

                public static readonly Factory StandardFactory = GetStandardFactory();

                private static Factory GetStandardFactory()
                {

                    Factory factory = new Factory();
                    List<Function> types = new List<Function>();
                    foreach (Type type in Assembly.GetAssembly(typeof(Function)).GetTypes().Where(myType => !myType.IsAbstract && myType.IsSubclassOf(typeof(Function))))
                    {
                        // No instances of constant functions are wasteful.  Don't create a dynamic caller for them.  Instead, callers will be
                        // created later for each of the defined constant functions (one for 'pi', one for 'e', etc.).
                        if (typeof(Functions.Constant).IsAssignableFrom(type)) continue;

                        // The caller works by invoking an appropriate constructor for the type, and returning the resulting Function.  Public 
                        // constructors are preferred over non-public, and IEvaluatable[] parameters are preferred over no-params ctors.
                        Func<Function> caller = delegate ()
                        {

                            /// The standard factory must be able to call a constructor on each type which exists in the following chart:
                            /// 
                            ///             No parameter    IEvaluate[] as only parameter
                            ///             ----------------------------------------------
                            ///     Public |      2       |            1                 |
                            ///            -----------------------------------------------
                            ///  NonPublic |      4       |            3                 |
                            ///            -----------------------------------------------



                            // Case #1 - public ctor, IEvaluatable[] parameter
                            ConstructorInfo c = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, IEvaluatableArray, null);
                            if (c != null) return (Function)c.Invoke(EmptyIEvaluatable);

                            // Case #2 - public ctor, no parameter
                            c = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                            if (c != null) return (Function)c.Invoke(null);

                            // Case #3 - non-public ctor, IEvaluatable[] parameter
                            c = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, IEvaluatableArray, null);
                            if (c != null) return (Function)c.Invoke(EmptyIEvaluatable);


                            // Case #4 - non-public ctor, no parameter
                            c = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                            if (c != null) return (Function)c.Invoke(null);

                            // Case #5 - No matching ctor found, so throw an exception.
                            throw new FactoryException("Function of type " + type.Name + " has been set up incorrectly.  Must contain a no-argument constructor for parsing purposes.");
                        };
                        Function specimen = caller();
                        factory.AddRecipe(specimen.Name, caller);
                    }

                    // Here is where the callers that return the single object for each defined constant function is created.
                    foreach (FieldInfo fInfo in typeof(Functions.Constant).GetFields())
                    {
                        if (!fInfo.IsStatic || !fInfo.IsPublic) continue;
                        Functions.Constant constant = (Functions.Constant)fInfo.GetValue(null);
                        factory.AddConstant(constant);
                    }

                    return factory;
                }


                public class FactoryException : Exception
                {
                    public FactoryException(string message) : base(message) { }
                }
            }



            #region Function error helpers
            protected Error InputTypeError(IList<IEvaluateable> inputs, int index, params Type[] t)
            {
                Debug.Assert(index < inputs.Count);
                Debug.Assert(!t.Contains(inputs[index].GetType()));
                return new Error("Input " + index + " incorrect type for function " + Name + ".  Expected " + string.Join(", ", t.Select((tp) => tp.Name)) + ", but given " + inputs[index].GetType().Name + ".");
            }

            /// <summary>Returns an input type error with a suitable message.</summary>
            /// <param name="inputs">The inputs that caused the error.</param>
            /// <param name="expected">The input counts that are acceptable.  Overloaded functions may all different counts.</param>
            protected Error InputCountError(IList<IEvaluateable> inputs, params int[] expected)
            {
                Debug.Assert(!expected.Contains(inputs.Count));
                return new Error("Incorrect input count for function " + Name + ".  Expected {" + string.Join(", ", expected) + "}, but given " + inputs.Count + ".");
            }

            #endregion

            [Serializable]
            protected internal class VariableDomain
            {

            }

        }


    }


}

