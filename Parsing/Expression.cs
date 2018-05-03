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

    

    public static class Expression
    {
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
            public IEvaluatable ToClause()
            {
                // Watch for malformed nestings.
                switch (Opener)
                {
                    case "":  if (Closer != "") throw new Exception("Mismatched brackets."); break;
                    case "(": if (Closer != ")") throw new Exception("Mismatched brackets."); break;
                    case "[": if (Closer != "]") throw new Exception("Mismatched brackets."); break;
                    case "{": if (Closer != "}") throw new Exception("Mismatched brackets."); break;
                    default: throw new Exception("Unrecognized bracket: " + Opener);
                }

                // Evaluate for pragmatics - things like scalars preceding functions or other nestings without a '*', etc.
                List<IEvaluatable> evaluatables = new List<IEvaluatable>();
                DynamicHeap<Operator> operators = new DynamicHeap<Operator>();
                DynamicLinkedList<object>.Node node = this.FirstNode;
                while (node != null)
                {
                    switch (node.Contents)
                    {
                        case TokenList tl: node.Contents = tl.ToClause(); continue;
                        case Clause c:
                            if (node.Previous != null && node.Previous.Contents is Function precedingFunction)
                            {
                                if (!precedingFunction.ValidateInputs(c.Inputs))
                                    return new Error("Invalid inputs for function " + precedingFunction.Name, this.Start, this.End);
                                c.Function = (Function)node.Previous.Remove();
                            }
                            evaluatables.Add(c);
                            break;
                        case Error e: return e;
                        case Operator op:
                            break;
                        case Constant k:
                        case Number n:
                            if (node.Next == null) break;
                            if (node.Next.Contents is TokenList || node.Next.Contents is Variable || node.Next.Contents is Function)
                                node.InsertAfter(Function.Multiplication);
                            evaluatables.Add((IEvaluatable) node.Contents);
                            break;
                        case Function f:
                            if (node.Next == null || !(node.Next.Contents is TokenList)) return new Error("No inputs for given function.", this.Start, this.End);
                            break;
                        default: throw new NotImplementedException();
                    }
                    node = node.Next;
                }

                // Finally, construct trees according to operator precedence within this clause.
                throw new NotImplementedException();

                // Return the fully-parsed clause.
                return new Clause(evaluatables, Opener[0], Closer[0]);                
            }
        }


        public static IEvaluatable FromString(string str, out ISet<Variable> dependees, Variable.DataContext context = null, Function.Factory factory = null)
        {
            // Step #1 setup
            dependees = new HashSet<Variable>();
            if (str == null) return new Error("Expression string cannot be null.");
            string[] rawTokens = _Regex.Split(str);            
            Debug.Assert(rawTokens.Length > 0);
            
            // Step #2 - Parse into tree structure containing tokenized objects
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
                    case string _ when factory.TryGetFunction(rawToken, out Function f): stack.Peek().AddLast(f); continue;

                    //Variable?
                    case string _ when context.TryGetVariable(rawToken, out Variable old_var): stack.Peek().AddLast(old_var); continue;
                    case string _ when context.TryCreateVariable(rawToken, out Variable new_var): stack.Peek().AddLast(new_var); continue;

                    default: return new Error("Unrecognized token: " + rawToken + ".", 0, i);
                }
            }

            return root.ToClause();
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
