using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace Dependency
{


    
    public static class Expression 
    {
        public static IEvaluateable FromString(string str, IContext context, Function.Factory functions) 
            => FromStringInternal(str, context, functions);

        public static IEvaluateable FromString(string str, IContext context)
            => FromStringInternal(str, context, (context == null) ? null : Function.Factory.StandardFactory);

        public static IEvaluateable FromString(string str) 
            => FromStringInternal(str, null, null);
       

        /// <summary>
        /// Creates and returns an evaluatable objects from the given string, or returns an error indicating which it cannot.
        /// <para/>Strong guarantee:  there will be no changes to the state of the given context if an exception is thrown.  If an 
        /// exception is thrown, all added variables will be deleted by calling their respective contexts' Delete method.
        /// </summary>        
        /// <exception cref="SyntaxException">Thrown if the given string cannot be converted into a valid IEvaluateable.</exception>
        /// <param name="str">The string to convert into an evaluatable object.</param>
        /// <param name="functions">The allowed functions for this expression.</param>
        /// <param name="context">The variable context in which variables are created or from which they are retrieved.</param>
        internal static IEvaluateable FromStringInternal(string str, IContext context, Function.Factory functions)
        {            
            // Step #1 - check for edge conditions that will result in errors rather than throwing exceptions.            
            //ISet<Variable> terms = new HashSet<Variable>();            
            if (str == null || str == "") return null;

            // Step #2a - from here, we'll be parsing the string.  Prep for parsing.            
            string[] rawTokens = _Regex.Split(str);
            Debug.Assert(rawTokens.Length > 0);

            // Step #2c - prep objects that will embody the nesting structure.
            Stack<TokenList> stack = new Stack<TokenList>();
            TokenList rootList = new TokenList("", 0);
            stack.Push(rootList);
            List<Reference> newReferences = new List<Reference>();

            // Step #3 - Parse into clause-by-clause tree structure containing tokenized objects
            int i = 0;
            try
            {                
                int position = 0;
                for (i = 0; i < rawTokens.Length; i++)
                {
                    // Step #3a - Set up the raw token.
                    string rawToken = rawTokens[i];
                    position += rawToken.Length;

                    // Step #3b - nesting, literals, and operators.  Enough to make an arithmetic expression with no variables, 
                    // functions, or context references.
                    switch (rawToken)
                    {
                        // Skip whitespace
                        case "":
                        case string _ when string.IsNullOrWhiteSpace(rawToken):
                            continue;

                        //Nesting?
                        case "(":
                        case "[":
                        case "{":
                            TokenList tl = new TokenList(rawToken, i);
                            stack.Peek().AddLast(tl);
                            stack.Push(tl);
                            continue;
                        case ")":
                        case "]":
                        case "}":
                            stack.Pop().Close(rawToken, i);
                            if (stack.Count == 0)
                                throw new SyntaxException("Invalid token: " + rawToken, string.Join("", rawTokens), string.Join("", rawTokens.Take(i)).Length, context);
                            continue;

                        //Sectioner?
                        case ",": continue;
                        case ";": continue;


                        //Unary operator?
                        case "-":
                        case "!":
                        case "~":
                            if (stack.Peek().Count == 0 || stack.Peek().Last() is Operator)
                                stack.Peek().AddLast(Function.Factory.CreateNegation());
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

                        // Literal?
                        case var s when s.StartsWith("\"") && s.EndsWith("\"") && s.Count((c) => c == '\"') == 2:
                            stack.Peek().AddLast(new String(s.Substring(1, s.Length - 2)));
                            continue;
                        case string _ when Number.TryParse(rawToken, out Number n):
                            stack.Peek().AddLast(n);
                            continue;
                        case string _ when bool.TryParse(rawToken, out bool b):
                            stack.Peek().AddLast(Boolean.FromBool(b));
                            continue;

                        // A function from the supplied dictionary?
                        case string _ when functions != null && functions.TryCreateFunction(rawToken, out Function f):
                            stack.Peek().AddLast(f);
                            continue;

                        // Context-specific?
                        case string _ when context != null && Reference.TryCreate( context, rawToken.Split('.'), out Reference rel):                            
                            stack.Peek().AddLast(rel);
                            continue;

                        // If we still don't know what the token is, it's definitely a syntax error.
                        default:
                            throw new SyntaxException("Invalid token: " + rawToken,
                             string.Join("", rawTokens), string.Join("", rawTokens.Take(i)).Length,
                                context);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is SyntaxException) throw ex;
                throw new SyntaxException("Syntax error", string.Join("", rawTokens), string.Join("", rawTokens.Take(i)).Length, context, ex);                
            }
            
            IEvaluateable contents = rootList.Parse();
            return contents;
        }
                

        private const string StringPattern = "(?<stringPattern>\".*\")";
        private const string OpenerPattern = @"(?<openerPattern>[\(\[{])";
        private const string CloserPattern = @"(?<closerPattern>[\)\]}])";
        private const string OperPattern = @"(?<operPattern>[+-/*&|^~!])";
        private const string VarPattern = @"(?<varPattern> \$? [a-zA-Z_][\w_]* (?:\.[a-zA-Z_][\w_]*)*)";
        private const string NumPattern = @"(?<numPattern>(?:-)? (?: \d+\.\d* | \d*\.\d+ | \d+ ))";
        private const string SpacePattern = @"(?<spacePattern>\s+)";
        private static string regExPattern = string.Format("{0} | {1} | {2} | {3} | {4} | {5} | {6}",
               StringPattern,        //0
               OpenerPattern,        //1
               CloserPattern,        //2
               OperPattern,          //3
               VarPattern,           //4
               NumPattern,           //5
               SpacePattern);        //6
        private static Regex _Regex = new Regex(regExPattern, RegexOptions.IgnorePatternWhitespace);



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
            public IEvaluateable Parse()
            {
                // Step #1 - Watch for malformed nestings.
                switch (Opener)
                {
                    case "": if (Closer != "") throw new Exception("Mismatched brackets: " + Opener + " vs. " + Closer); break;
                    case "(": if (Closer != ")") throw new Exception("Mismatched brackets: " + Opener + " vs. " + Closer); break;
                    case "[": if (Closer != "]") throw new Exception("Mismatched brackets: " + Opener + " vs. " + Closer); break;
                    case "{": if (Closer != "}") throw new Exception("Mismatched brackets: " + Opener + " vs. " + Closer); break;
                    default: throw new Exception("Unrecognized brackets: " + Opener + " vs. " + Closer);
                }


                // Step #2 - evaluate for pragmatics - things like scalars preceding functions or other nestings without a '*', etc.
                // The following ungodly heap is designed to sort nodes according to the priority levels of the Function objects they contain.
                StableHeap<DynamicLinkedList<object>.Node> prioritized
                    = new StableHeap<DynamicLinkedList<object>.Node>((a, b) => ((Function)a.Contents).Priority.CompareTo(((Function)b.Contents).Priority));

                DynamicLinkedList<object>.Node node = this.FirstNode;
                while (node != null)
                {
                    switch (node.Contents)
                    {
                        case TokenList tl: node.Contents = tl.Parse(); break;
                        case EvaluationError e: return e;
                        case Reference r when r.Function != null:
                        case Function f: prioritized.Enqueue(node); break;
                        case Number n when node.Next != null:
                            if (node.Next.Contents is Operator || node.Next.Contents is Constant) break;
                            if (node.Next.Contents is TokenList || node.Next.Contents is Reference || node.Next.Contents is Function || node.Next.Contents is Clause)
                                node.InsertAfter(Function.Factory.CreateMultiplication());
                            break;
                    }
                    node = node.Next;
                }

                // Step #3 - construct a within-this-clause tree according to operator precedence.
                while (prioritized.Count > 0)
                {
                    node = prioritized.Dequeue();
                    Function function = (node.Contents is Function f) ? f : ((Reference)node.Contents).Function;
                    function.ParseNode(node);
                    function.Terms = new HashSet<Variable>(function.Inputs.SelectMany(input => Variable.GetTermsOf(input)));
                }

                // Step #4 - Return the fully-parsed clause.  If there's just one naked sub-clause in the parsed clause, return that with 
                // this clause's opener/closer.                

                // No need to return a chain of naked single-input clauses
                if (Opener == "" && Closer == "")
                {
                    IEvaluateable result = (IEvaluateable)this.First;
                    while ((result is Clause nakedC) && (nakedC.Inputs.Count() == 1) && (nakedC.IsNaked))
                        result = nakedC.Inputs[0];
                    return result;
                }

                // In all other cases, just make a containing clause of the content.
                return new Clause(Opener, Closer, this.Select(item => (IEvaluateable)item).ToArray());
            }
        }
        

    }
}
