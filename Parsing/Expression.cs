using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using DataStructures;
using Helpers.Parsing;
using Helpers.Parsing.Functions;

namespace Helpers.Parsing
{

    public interface IEvaluatable
    {
        IEvaluatable Evaluate();
    }


    /// <summary>
    /// Contains methods for returning objects which can be evaluated according to associated functions.
    /// </summary>
    public static class Expression
    {
        /// <summary>
        /// A data structure that mirrors a Clause, but does not require that its contents be IEvaluatable.  This structure is designed to 
        /// function as an intermediary between a token list and an actual IEvaluatable Clause, and contains a method Parse() which will 
        /// return a Clause based on this structure.
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
            public IEvaluatable Parse()
            {
                //TODO:  contain all within a try/catch block to catch parsing errors?
                
                // Step #1 - Watch for malformed nestings.
                switch (Opener)
                {
                    case "": if (Closer != "") throw new Exception("Mismatched brackets."); break;
                    case "(": if (Closer != ")") throw new Exception("Mismatched brackets."); break;
                    case "[": if (Closer != "]") throw new Exception("Mismatched brackets."); break;
                    case "{": if (Closer != "}") throw new Exception("Mismatched brackets."); break;
                    //default: return new Error("Unrecognized brackets: \"" + Opener + "\", \"" + Closer + "\".", this.Start, this.End);
                    default:  Debug.Assert(false); break;   //Sanity check.
                }

                // Step #2 - evaluate for pragmatics - things like scalars preceding functions or other nestings without a '*', etc.

                // The following ungodly heap is designed to sort nodes according to the priority levels of the Function objects they contain.
                Heap<DynamicLinkedList<object>.Node> priorities = new Heap<DynamicLinkedList<object>.Node>((a, b) => ((Function)a.Contents).Priority.CompareTo(((Function)b.Contents).Priority));

                DynamicLinkedList<object>.Node node = this.FirstNode;
                while (node != null)
                {
                    switch (node.Contents)
                    {
                        case TokenList tl: node.Contents = tl.Parse(); continue;
                        case Clause c: break;
                        case Error e: return e;
                        case Function f: priorities.Add(node); break;
                        case Number n when node.Next != null:
                            if (node.Next.Contents is TokenList || node.Next.Contents is Variable || node.Next.Contents is Function || node.Next.Contents is Clause)
                                node.InsertAfter(Function.Multiplication);
                            break;
                        //default: return new Error("Unrecognized token type: " + node.Contents.GetType().Name + ".", this.Start, this.End);
                        default: Debug.Assert(false); break;    //Sanity check.
                    }
                    node = node.Next;
                }

                // Step #3 - construct a within-this-clause tree according to operator precedence.
                while (priorities.Count > 0)
                {
                    node = priorities.Pop();
                    Function function = (Function)node.Contents;
                    function.Parse(node);
                }

                // Step #4 - finally, put the contents into IEvaluatable form.
                int i = 0;
                IEvaluatable[] contents = new IEvaluatable[this.Count];
                foreach (object obj in this) contents[i++] = (IEvaluatable)obj;

                // Return the fully-parsed clause.
                return new Clause(Opener[0], Closer[0], contents);
            }
        }

        public static IEvaluatable FromLaTeX(string latex, IDictionary<string, Function> functions, out ISet<Variable> dependees, Variable.DataContext context = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>Creates and returns an evaluatable objects from the given string, or returns an error indicating which it cannot.</summary>
        /// <param name="str">The string to convert into an evaluatable object.</param>
        /// <param name="functions">The allowed functions for this expression.</param>
        /// <param name="dependees">The variables on which this expression depends, if any.</param>
        /// <param name="context">The variable context in which variables are created or from which they are retrieved.</param>
        public static IEvaluatable FromString(string str, IDictionary<string, Function> functions, out ISet<Variable> dependees, Variable.DataContext context = null)
        {
            // Step #1 - setup
            dependees = new HashSet<Variable>();
            if (str == null) return new Error("Expression string cannot be null.");
            string[] rawTokens = _Regex.Split(str);
            Debug.Assert(rawTokens.Length > 0);

            // Step #2 - Parse into clause-by-clause tree structure containing tokenized objects
            Stack<TokenList> stack = new Stack<TokenList>();
            TokenList root = new TokenList("", 0);
            stack.Push(root);
            for (int i = 0; i < rawTokens.Length; i++)
            {
                // Step #2a - skip whitespace
                string rawToken = rawTokens[i].Trim();
                if (rawToken == "" || string.IsNullOrWhiteSpace(rawToken)) continue;

                // Step #2b - simple tokens
                switch (rawToken)
                {

                    //Nesting?
                    case "(":
                    case "[":
                    case "{": stack.Push(new TokenList(rawToken, i)); continue;
                    case ")":
                    case "]":
                    case "}": stack.Pop().Close(rawToken, i); if (stack.Count == 0) return new Error("Too many closing brackets.", 0, i); continue;

                    //Sectioner?
                    case ",": continue;
                    case ";": throw new NotImplementedException();// A sub-vector.                    
                    case "-":

                    //Operator?
                    case "!":
                    case "~":
                        if (stack.Peek().Count == 0 || stack.Peek().Last() is Operator) stack.Peek().AddLast(Function.Negation);
                        else stack.Peek().AddLast(Function.Subtraction);
                        continue;
                    case "+": stack.Peek().AddLast(Function.Addition); continue;
                    case "*": stack.Peek().AddLast(Function.Multiplication); continue;
                    case "/": stack.Peek().AddLast(Function.Division); continue;
                    case "^": stack.Peek().AddLast(Function.Exponentiation); continue;
                    case "|": stack.Peek().AddLast(Function.Or); continue;
                    case "&": stack.Peek().AddLast(Function.And); continue;
                    case ".": stack.Peek().AddLast(Function.Relation); continue;
                    case ":": stack.Peek().AddLast(Function.Range); continue;

                    //Literal?
                    case var s when s.StartsWith("\"") && s.EndsWith("\"") && s.Count((c) => c == '\"') == 2: stack.Peek().AddLast(new String(s.Substring(1, s.Length - 2))); continue;
                    case string _ when decimal.TryParse(rawToken, out decimal m): stack.Peek().AddLast(new Number(m)); continue;
                    case string _ when bool.TryParse(rawToken, out bool b): stack.Peek().AddLast(new Boolean(b)); continue;

                    //Function?
                    case string _ when functions.TryGetValue(rawToken, out Function f): stack.Peek().AddLast(f); continue;

                    //Variable?
                    case string _ when context != null && context.TryGetVariable(rawToken, out Variable old_var): stack.Peek().AddLast(old_var); dependees.Add(old_var); continue;
                    case string _ when context != null && context.TryCreateVariable(rawToken, out Variable new_var): stack.Peek().AddLast(new_var); dependees.Add(new_var); continue;

                    default: return new Error("Unrecognized token: " + rawToken + ".", 0, i);
                }
            }

            return root.Parse();
        }



        private static Regex _Regex = new Regex(regExPattern, RegexOptions.IgnorePatternWhitespace);
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
    }
}
