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
using static Parsing.Context;

namespace Parsing
{
    /// <summary>IEvaluteable objects can evaluate to another IEvaluatable value.</summary>
    public interface IEvaluateable
    {
        IEvaluateable Evaluate();
    }

    
    public interface IIndexable
    {
        IEvaluateable this[IEvaluateable index] { get; }

        int MaxIndex { get; }
        int MinIndex { get; }
    }

    public interface IEvaluateable<T> : IEvaluateable where T : IEvaluateable
    {
        new T Evaluate();
    }

    public class SyntaxException : Exception
    {
        public readonly string Entry;
        public readonly int Position;
        public SyntaxException(string message, string entry, int position) : base(message) { this.Entry = entry; this.Position = position; }
        public SyntaxException(string message, string entry, int position, Exception inner) : base(message, inner) { this.Entry = entry; this.Position = position; }
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
                StableHeap<DynamicLinkedList<object>.Node> prioritized
                    = new StableHeap<DynamicLinkedList<object>.Node>((a, b) => ((DataContext.Function)a.Contents).Priority.CompareTo(((DataContext.Function)b.Contents).Priority));

                DynamicLinkedList<object>.Node node = this.FirstNode;
                while (node != null)
                {
                    switch (node.Contents)
                    {
                        case TokenList tl: node.Contents = tl.Parse(functions); break;
                        case EvaluationError e: return e;
                        case DataContext.Function f: prioritized.Add(node); break;
                        case Number n when node.Next != null:
                            if (node.Next.Contents is Operator || node.Next.Contents is Constant) break;
                            if (node.Next.Contents is TokenList || node.Next.Contents is DataContext.Variable || node.Next.Contents is DataContext.Function || node.Next.Contents is Clause)
                                node.InsertAfter(Function.Factory.CreateMultiplication());
                            break;
                    }
                    node = node.Next;
                }

                // Step #3 - construct a within-this-clause tree according to operator precedence.
                while (prioritized.Count > 0)
                {
                    node = prioritized.Pop();
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
                    return new EvaluationError("Invalid objects in parsed token list.");
                }
            }
        }


        public static IEvaluateable FromLaTeX(string latex, out ISet<Variable> dependees, Variable context = null)
        {
            throw new NotImplementedException();
        }


        

        /// <summary>Creates and returns an evaluatable objects from the given string, or returns an error indicating which it cannot.</summary>
        /// <param name="str">The string to convert into an evaluatable object.</param>
        /// <param name="functions">The allowed functions for this expression.</param>
        /// <param name="context">The variable context in which variables are created or from which they are retrieved.</param>       

        public static IEvaluateable FromString(string str, Context context = null)
        {
            // Step #1 - setup
            Function.Factory functions = (context == null) ? null : Function.Factory.StandardFactory;
            ISet<Variable> terms = new HashSet<Variable>();
            if (str == null) return new EvaluationError("Expression string cannot be null.");
            string[] rawTokens = _Regex.Split(str);
            Debug.Assert(rawTokens.Length > 0);
            IContext obj = context;
            Stack<TokenList> stack = new Stack<TokenList>();
            TokenList rootList = new TokenList("", 0);
            stack.Push(rootList);
            Dictionary<Context, List<Variable>> addedVariables
                = new Dictionary<Context, List<Variable>>();  // Used in case of exception so new variables can be unwound.

            // Step #2 - Parse into clause-by-clause tree structure containing tokenized objects
            int position = 0;
            for (int i = 0; i < rawTokens.Length; i++)
            {
                string rawToken = rawTokens[i];
                position += rawToken.Length;
                try
                {
                    // Step #2a - skip whitespace                    
                    if (rawToken.Length==0 || rawToken == "" || string.IsNullOrWhiteSpace(rawToken)) continue;

                    // Step #2b - nesting, literals, functions, and operators.
                    switch (rawToken)
                    {
                        case string _ when context != null && context.TryGet(rawToken, out Variable old_var): stack.Peek().AddLast(old_var); terms.Add(old_var); continue;

                        //Nesting?
                        case "(":
                        case "[":
                        case "{": TokenList tl = new TokenList(rawToken, i); stack.Peek().AddLast(tl); stack.Push(tl); continue;
                        case ")":
                        case "]":
                        case "}": stack.Pop().Close(rawToken, i); if (stack.Count == 0) return new EvaluationError("Too many closing brackets.", 0, i); continue;

                        //Sectioner?
                        case ",": continue;
                        case ";": continue;

                        //Unary operator?
                        case "-":
                        case "!":
                        case "~":
                            if (stack.Peek().Count == 0 || stack.Peek().Last() is Operator) stack.Peek().AddLast(Function.Factory.CreateNegation());
                            else stack.Peek().AddLast(Function.Factory.CreateSubtraction());
                            continue;

                        // Binary operator?
                        case "+": stack.Peek().AddLast(Function.Factory.CreateAddition()); continue;
                        case "*": stack.Peek().AddLast(Function.Factory.CreateMultiplication()); continue;
                        case "/": stack.Peek().AddLast(Function.Factory.CreateDivision()); continue;
                        case "^": stack.Peek().AddLast(Function.Factory.CreateExponentiation()); continue;
                        case "|": stack.Peek().AddLast(Function.Factory.CreateOr()); continue;
                        case "&": stack.Peek().AddLast(Function.Factory.CreateAnd()); continue;
                        case ":": stack.Peek().AddLast(Function.Factory.CreateRange()); continue;
                        case ".": stack.Peek().AddLast(Function.Factory.CreateRelation()); continue;

                        //Literal?
                        case var s when s.StartsWith("\"") && s.EndsWith("\"") && s.Count((c) => c == '\"') == 2: stack.Peek().AddLast(new String(s.Substring(1, s.Length - 2))); continue;
                        case string _ when decimal.TryParse(rawToken, out decimal m): stack.Peek().AddLast(new Number(m)); continue;
                        case string _ when bool.TryParse(rawToken, out bool b): stack.Peek().AddLast(Boolean.FromBool(b)); continue;

                        //Function?
                        case string _ when functions != null && functions.TryCreateFunction(rawToken, out Function f): stack.Peek().AddLast(f); continue;
                    }

                    // Step #2c - sub-contexting and properties/variables
                    if (obj != null)
                    {
                        

                        // Does this context have a matching variable?
                        if (obj.TryGet(rawToken, out IEvaluateable sub_val))
                        {
                            stack.Peek().AddLast(sub_val);
                            if (sub_val is Variable v) terms.Add(v);
                            obj = context;
                            continue;
                        }

                        // Does this context have a matching sub-context?                        
                        else if (obj.TryGet(rawToken, out IContext sub_obj))
                        {
                            stack.Peek().AddLast(obj);
                            obj = sub_obj;
                            continue;
                        }

                        // Can the variable be added to this sub-context?
                        else if (obj is Context ctxt && ctxt.TryAdd(rawToken, out Variable new_var)) 
                        {
                            stack.Peek().AddLast(new_var);
                            terms.Add(new_var); obj = context;
                            if (!addedVariables.TryGetValue(ctxt, out List<Variable> addedList))
                                addedVariables[ctxt] = (addedList = new List<Variable>());
                            addedList.Add(new_var);
                            continue;
                        }

                        // Otherwise, snap back to the root context.
                        else
                        {
                            obj = context;
                            continue;
                        }
                    }

                }
                catch (Exception ex)
                {
                    // Since an exception was caught but state might have changed by adding variables, unwind the expression creation by 
                    // deleting all newly-added variables.
                    foreach (Context ctxt in addedVariables.Keys)
                        foreach (Variable v in addedVariables[ctxt])
                            if (!ctxt.TryDelete(v))
                                throw new InvalidOperationException("Could not unwind added variables.  Context variable list may be corrupted.");

                    // A sanity check in case a later implementation accidentally throws SyntaxExceptions.
                    if (ex is SyntaxException)
                        throw new InvalidOperationException("SyntaxExceptions can be built only in the expression tokenizer method (i.e., in the FromString method).");

                    // Throw an exception that gives clues where the syntax error occurred.
                    throw new SyntaxException(ex.Message, string.Join("", rawTokens), string.Join("", rawTokens.Take(i)).Length, ex);
                }
                

                // In all other cases, throw a syntax exception.
                throw new SyntaxException("Invalid token: " + rawToken, string.Join("", rawTokens), string.Join("", rawTokens.Take(i)).Length);                
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
