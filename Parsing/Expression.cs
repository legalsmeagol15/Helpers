using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using DataStructures;
using Parsing;
using Parsing.Functions;
using static Parsing.DataContext;

namespace Parsing
{
    /// <summary>IEvaluteable objects can evaluate to another IEvaluatable value.</summary>
    public interface IEvaluateable
    {
        IEvaluateable Evaluate();
    }

    /// <summary>ISubContext objects support sub-contexts and property variables.</summary>
    public interface IContext : IEvaluateable,  IEnumerable<IContext>
    {
        string Name { get; }

        /// <summary>Tries to retrieve the indicated property variable.</summary>
        /// <param name="key">The property variable's name.</param>
        /// <param name="v">Out.  The property variable to be returned.  If no variable matched the given name, this will be null.</param>
        /// <returns>Returns true if lookup was successful, false if it was not.</returns>
        bool TryGet(string key, out Variable v);

        /// <summary>Tries to retrieve the indicated subContext.</summary>
        /// <param name="key">The sub-context's name.</param>
        /// <param name="v">Out.  The The sub-context to be returned.  If no sub-context matched the given name, this will be null.</param>
        /// <returns>Returns true if lookup was successful, false if it was not.</returns>
        bool TryGet(string key, out IContext subContext);

        /// <summary>Tries to add the described Variable to this sub-context.</summary>
        /// <param name="key">The Variable's name.</param>
        /// <param name="v">Out.  The new Variable returned.  If the Variable could not be added, this value will be null.</param>
        /// <returns>Returns true if add was successful, false if it was not.</returns>
        bool TryAdd(string name, out Variable v);
        
    }

    public interface IIndexable<T>
    {
        T this[int index] { get; }
    }

    public interface IEvaluateable<T> : IEvaluateable where T : IEvaluateable
    {
        new T Evaluate();
    }



    /// <summary>
    /// Contains methods for returning objects which can be evaluated according to associated functions.
    /// </summary>
    public static class Expression
    {
        /// <summary>
        /// A data structure that mirrors a Clause, but does not require that its contents be IEvaluatable.  This structure is designed to 
        /// function as an intermediary between a simple list of tokens and an actual IEvaluatable Clause, and contains a method Parse() 
        /// which will return a Clause based on this structure.
        /// </summary>
        private class TokenList : DynamicLinkedList<object>
        {
            public string Opener = "";
            public string Closer = "";
            public int Start;
            public int End;
            public TokenList(string opener, int startIndex) : base() { this.Opener = opener; this.Start = startIndex; }
            public TokenList(IEnumerable<object> items, string opener, int startIndex) : base(items) { this.Opener = opener; this.Start = startIndex; }
            public void Close(String closer, int endIndex) { this.Closer = closer; this.End = endIndex; }



            /// <exception cref="InvalidOperationException">Thrown for a mismatch.</exception>
            public IEvaluateable Parse(DataContext.Function.Factory functions)
            {
                // Step #1 - Watch for malformed nestings.
                switch (Opener)
                {
                    case "": if (Closer != "") throw new Exception("Mismatched brackets."); break;
                    case "(": if (Closer != ")") throw new Exception("Mismatched brackets."); break;
                    case "[": if (Closer != "]") throw new Exception("Mismatched brackets."); break;
                    case "{": if (Closer != "}") throw new Exception("Mismatched brackets."); break;
                    //default: return new Error("Unrecognized brackets: \"" + Opener + "\", \"" + Closer + "\".", this.Start, this.End);
                    default: Debug.Assert(false); break;   //Sanity check.
                }


                // Step #2 - evaluate for pragmatics - things like scalars preceding functions or other nestings without a '*', etc.

                // The following ungodly heap is designed to sort nodes according to the priority levels of the Function objects they contain.
                StableHeap<DynamicLinkedList<object>.Node> priorities = new StableHeap<DynamicLinkedList<object>.Node>((a, b) => ((DataContext.Function)a.Contents).Priority.CompareTo(((DataContext.Function)b.Contents).Priority));

                DynamicLinkedList<object>.Node node = this.FirstNode;
                while (node != null)
                {
                    switch (node.Contents)
                    {
                        case TokenList tl: node.Contents = tl.Parse(functions); break;
                        case Error e: return e;
                        case DataContext.Function f: priorities.Add(node); break;
                        case Number n when node.Next != null:
                            if (node.Next.Contents is Operator || node.Next.Contents is Constant) break;
                            if (node.Next.Contents is TokenList || node.Next.Contents is DataContext.Variable || node.Next.Contents is DataContext.Function || node.Next.Contents is Clause)
                                node.InsertAfter(functions.CreateMultiplication());
                            break;
                    }
                    node = node.Next;
                }

                // Step #3 - construct a within-this-clause tree according to operator precedence.
                while (priorities.Count > 0)
                {
                    node = priorities.Pop();
                    DataContext.Function function = (DataContext.Function)node.Contents;
                    function.ParseNode(node);
                    function.Terms = new HashSet<Variable>();
                    foreach (IEvaluateable input in function.Inputs)
                    {
                        if (input is Variable v) function.Terms.Add(v);
                        else if (input is Clause sub_c) foreach (Variable sub_v in sub_c.Terms) function.Terms.Add(sub_v);
                    }
                }

                // Step #4 - Return the fully-parsed clause.  If there's just one naked sub-clause in the parsed clause, return that with 
                // this clause's opener/closer.
                if (this.Count() == 1 && this.First is Clause result && result.Opener == "" && result.Closer == "")
                {
                    result.Opener = Opener;
                    result.Closer = Closer;
                    return result;
                }

                // Step #5 - catch non-parseable situations.
                try
                {
                    IEvaluateable[] contents = this.Select(obj => (IEvaluateable)obj).ToArray();
                    Clause rv = new Clause(Opener, Closer, contents);
                    rv.Terms = new HashSet<Variable>();
                    foreach (IEvaluateable input in contents)
                    {
                        if (input is Variable v) rv.Terms.Add(v);
                        else if (input is Clause sub_c) foreach (Variable sub_v in sub_c.Terms) rv.Terms.Add(sub_v);
                    }
                    return rv;
                }
                catch
                {
                    return new Error("Invalid objects in parsed token list.");
                }
            }
        }


        public static IEvaluateable FromLaTeX(string latex, IDictionary<string, Function> functions, out ISet<Variable> dependees, Variable context = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>Creates and returns an evaluatable objects from the given string, or returns an error indicating which it cannot.</summary>
        /// <param name="str">The string to convert into an evaluatable object.</param>
        /// <param name="functions">The allowed functions for this expression.</param>
        /// <param name="context">The variable context in which variables are created or from which they are retrieved.</param>
        public static IEvaluateable FromString(string str, Function.Factory functions, DataContext context = null)
        {
            // Step #1 - setup
            ISet<Variable> terms = new HashSet<Variable>();
            if (str == null) return new Error("Expression string cannot be null.");
            string[] rawTokens = _Regex.Split(str);
            Debug.Assert(rawTokens.Length > 0);
            IContext obj = context;
            Stack<TokenList> stack = new Stack<TokenList>();            
            TokenList rootList = new TokenList("", 0);
            stack.Push(rootList);

            // Step #2 - Parse into clause-by-clause tree structure containing tokenized objects
            for (int i = 0; i < rawTokens.Length; i++)
            {
                // Step #2a - skip whitespace
                string rawToken = rawTokens[i];
                if (rawToken == "" || string.IsNullOrWhiteSpace(rawToken)) continue;

                // Step #2b - simple tokens
                switch (rawToken)
                {
                    case string _ when context != null && context.TryGet(rawToken, out Variable old_var): stack.Peek().AddLast(old_var); terms.Add(old_var); continue;

                    //Nesting?
                    case "(":
                    case "[":
                    case "{": TokenList tl = new TokenList(rawToken, i); stack.Peek().AddLast(tl); stack.Push(tl); continue;
                    case ")":
                    case "]":
                    case "}": stack.Pop().Close(rawToken, i); if (stack.Count == 0) return new Error("Too many closing brackets.", 0, i); continue;

                    //Sectioner?
                    case ",": continue;
                    case ";": throw new NotImplementedException();// A sub-vector.    

                    //Unary operator?
                    case "-":
                    case "!":
                    case "~":
                        if (stack.Peek().Count == 0 || stack.Peek().Last() is Operator) stack.Peek().AddLast(functions.CreateNegation());
                        else stack.Peek().AddLast(functions.CreateSubtraction());
                        continue;

                    // Binary operator?
                    case "+": stack.Peek().AddLast(functions.CreateAddition()); continue;
                    case "*": stack.Peek().AddLast(functions.CreateMultiplication()); continue;
                    case "/": stack.Peek().AddLast(functions.CreateDivision()); continue;
                    case "^": stack.Peek().AddLast(functions.CreateExponentiation()); continue;
                    case "|": stack.Peek().AddLast(functions.CreateOr()); continue;
                    case "&": stack.Peek().AddLast(functions.CreateAnd()); continue;
                    case ":": stack.Peek().AddLast(functions.CreateRange()); continue;
                    case ".": stack.Peek().AddLast(functions.CreateRelation()); continue;

                    //Literal?
                    case var s when s.StartsWith("\"") && s.EndsWith("\"") && s.Count((c) => c == '\"') == 2: stack.Peek().AddLast(new String(s.Substring(1, s.Length - 2))); continue;
                    case string _ when decimal.TryParse(rawToken, out decimal m): stack.Peek().AddLast(new Number(m)); continue;
                    case string _ when bool.TryParse(rawToken, out bool b): stack.Peek().AddLast(Boolean.FromBool(b)); continue;

                    //Function?
                    case string _ when functions.TryCreateFunction(rawToken, out Function f): stack.Peek().AddLast(f); continue;
                }

                // Step #2c - handle sub-contexting and properties/variables
                if (obj != null)
                {
                    // Does this context have a matching variable?
                    if (obj.TryGet(rawToken, out Variable old_var)) { stack.Peek().AddLast(old_var); terms.Add(old_var); obj = context; continue; }

                    // Does this context have a matching sub-context?
                    else if (obj.TryGet(rawToken, out IContext sub_obj)) { stack.Peek().AddLast(obj); obj = sub_obj; continue; }

                    // Can the variable be added to this sub-context?
                    else if (obj.TryAdd(rawToken, out Variable new_var)) { stack.Peek().AddLast(new_var); terms.Add(new_var); obj = context; continue; }

                    // Otherwise, snap back to the root context.
                    else { obj = context; continue; }
                }

                // In all other cases, return an interpretation error.
                return new Error("Invalid token: " + rawToken);
            }

            IEvaluateable result = rootList.Parse(functions);
            if (result is Clause clause) clause.Terms = terms;
            return result;
        }




        private static string regExPattern = string.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5}) | ({6}) | ({7})",
               StringPattern,                          //0
               LeftNestPattern,                        //1
               RightNestPattern,                       //2
               OperatorPattern,                        //3
               WordPattern,                            //4
               NumberPattern,                          //5
               SpacePattern,                           //6
               VariablePattern);                       //7
        private const string StringPattern = "\\\"[^\\\"]+\\\"";
        private const string LeftNestPattern = @"[([{]";
        private const string RightNestPattern = @"[)\]}]";
        private const string OperatorPattern = @"[+-/*&|^~!.]"; //@"\+\-*/&\|^~!\.;";
        private const string WordPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
        private const string NumberPattern = @"(-)? (?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?"; //Includes support for scientific notation!
        private const string SpacePattern = @"?=\s+";
        private const string VariablePattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
        private static Regex _Regex = new Regex(regExPattern, RegexOptions.IgnorePatternWhitespace);
    }
}
