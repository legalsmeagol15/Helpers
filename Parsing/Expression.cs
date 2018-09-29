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
using System.Threading;

namespace Parsing
{




    /// <summary>
    /// An object of this class will contain a newly-created <see cref="IEvaluateable"/> object created by parsing or some similar 
    /// operation.  Creating an <see cref="IEvaluateable"/> through an <see cref="Expression"/> object is always a thread-safe operation.
    /// <para/>
    /// An <see cref="Expression"/> will contain all the necessary locking information to 
    /// release locks on all referenced objects.  For every <see cref="IContext"/> where a <see cref="IVariable"/> is either referenced or 
    /// added, the Expression will maintain a lock on that <see cref="IContext"/>.  The locks are released by calling the 
    /// <see cref="Expression.Commit"/> method, which will also return the content <see cref="IEvaluateable"/>.  Also, the locks will be 
    /// released by garbage collection, but since this is an unreliable method it would be better to call <see cref="Expression.Commit"/>
    /// directly.
    /// </summary>
    // This method should NOT be serializable, because it is designed as an intermediary data holder that helps manage concurrency.
    public class Expression : IDisposable
    {
        internal IEvaluateable Contents;
        private object _ChangeLock = null;
        public static readonly IEvaluateable Null = new Null();
        private ISet<Variable> AddedVariables;
        private ISet<Context> AddedContexts;

        private Expression(IEvaluateable contents = null) { this.Contents = contents; }

        public static Expression FromLaTeX(string latex, Context context = null) => throw new NotImplementedException();

        


        /// <summary>
        /// Creates and returns an evaluatable objects from the given string, or returns an error indicating which it cannot.
        /// <para/>Strong guarantee:  there will be no changes to the state of the given context if an exception is thrown.  If an 
        /// exception is thrown, all added variables will be deleted by calling their respective contexts' Delete method.
        /// </summary>        /// 
        /// <param name="str">The string to convert into an evaluatable object.</param>
        /// <param name="functions">The allowed functions for this expression.</param>
        /// <param name="context">The variable context in which variables are created or from which they are retrieved.</param>
        public static Expression FromString(string str, Context context = null, Function.Factory functions = null)
        {
            // Step #1 - check for edge conditions that will result in errors rather than throwing exceptions.
            functions = functions ?? Function.Factory.StandardFactory;
            ISet<Variable> terms = new HashSet<Variable>();
            if (str == null)
                return new Expression(new EvaluationError("Expression string cannot be null."));

            // Step #2a - from here, we'll be parsing the string.  Prep for parsing.
            Expression result = new Expression();
            ISet<Variable> addedVariables = (result.AddedVariables = new HashSet<Variable>());
            ISet<Context> addedContexts = (result.AddedContexts = new HashSet<Context>());
            Context currentContext = context;

            // Step #2b - split the inputs according to the Regex.
            string[] rawTokens = _Regex.Split(str);
            Debug.Assert(rawTokens.Length > 0);

            // Step #2c - prep objects that will embody the nesting structure.
            Stack<TokenList> stack = new Stack<TokenList>();
            TokenList rootList = new TokenList("", 0);
            stack.Push(rootList);

            // Step #2d - if there is a context which can grab variables, lock on the Variable.LockObject.
            if (context != null)
                Monitor.Enter(result._ChangeLock = Variable.ModifyLock);
            

            // Step #3 - Parse into clause-by-clause tree structure containing tokenized objects
            try
            {
                int position = 0;
                for (int i = 0; i < rawTokens.Length; i++)
                {
                    // Step #3a - Set up the raw token.
                    string rawToken = rawTokens[i];
                    position += rawToken.Length;

                    // Step #3b - nesting, literals, functions, and operators.
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
                            currentContext = context;
                            continue;
                        case ")":
                        case "]":
                        case "}":                            
                            stack.Pop().Close(rawToken, i);
                            if (stack.Count == 0)
                                throw new SyntaxException("Invalid token: " + rawToken, string.Join("", rawTokens), string.Join("", rawTokens.Take(i)).Length, currentContext, addedVariables);
                            currentContext = context;
                            continue;

                        //Sectioner?
                        case ",": currentContext = context; continue;
                        case ";": currentContext = context; continue;                       


                        //Unary operator?
                        case "-":
                        case "!":
                        case "~":                            
                            if (stack.Peek().Count == 0 || stack.Peek().Last() is Operator)
                                stack.Peek().AddLast(Function.Factory.CreateNegation());
                            else stack.Peek().AddLast(Function.Factory.CreateSubtraction());
                            currentContext = context;
                            continue;                        

                        // Binary operator?
                        case "+":  stack.Peek().AddLast(Function.Factory.CreateAddition()); currentContext = context; continue;
                        case "*":  stack.Peek().AddLast(Function.Factory.CreateMultiplication()); currentContext = context; continue;
                        case "/":  stack.Peek().AddLast(Function.Factory.CreateDivision()); currentContext = context; continue;
                        case "^":  stack.Peek().AddLast(Function.Factory.CreateExponentiation()); currentContext = context; continue;
                        case "|":  stack.Peek().AddLast(Function.Factory.CreateOr()); currentContext = context; continue;
                        case "&":  stack.Peek().AddLast(Function.Factory.CreateAnd()); currentContext = context; continue;
                        case ":":  stack.Peek().AddLast(Function.Factory.CreateRange()); currentContext = context; continue;
                        
                        

                        // Literal?
                        case var s when s.StartsWith("\"") && s.EndsWith("\"") && s.Count((c) => c == '\"') == 2:                            
                            stack.Peek().AddLast(new String(s.Substring(1, s.Length - 2)));
                            currentContext = context;
                            continue;
                        case string _ when decimal.TryParse(rawToken, out decimal m):                            
                            stack.Peek().AddLast(new Number(m));
                            currentContext = context;
                            continue;
                        case string _ when bool.TryParse(rawToken, out bool b):                            
                            stack.Peek().AddLast(Boolean.FromBool(b));
                            currentContext = context;
                            continue;

                        // Function?
                        case string _ when functions != null && functions.TryCreateFunction(rawToken, out Function f):                            
                            stack.Peek().AddLast(f);
                            currentContext = context;
                            continue;

                        // Context-dependent?
                        case ".":
                            if (stack.Peek().Count == 0 || !(stack.Peek().Last is Context))
                                throw new SyntaxException("Relation operator must follow a Context: " + rawToken, string.Join("", rawTokens), string.Join("", rawTokens.Take(i)).Length, currentContext, addedVariables);
                            currentContext = (Context)stack.Peek().Last;
                            stack.Peek().AddLast(Function.Factory.CreateRelation());                            
                            continue;
                        case string _ when currentContext != null:

                            //A variable?
                            Variable v = null;
                            Context varCtxt = currentContext;
                            while (varCtxt != null && !varCtxt.TryGet(rawToken, out v)) varCtxt = varCtxt.Parent;
                            if (v != null || currentContext.TryAdd(rawToken, out v))
                            {
                                stack.Peek().AddLast(v);
                                terms.Add(v);
                                currentContext = context;
                                continue;
                            }

                            //A sub-context?
                            if (currentContext.TryGet(rawToken, out Context subCtxt) || currentContext.TryAdd(rawToken, out subCtxt))
                            {
                                if (currentContext != context && stack.Peek().Count > 0 && !(stack.Peek().Last is Relation))
                                    throw new SyntaxException("Designated context \"" + rawToken + "\" cannot follow a " + stack.Peek().Last.GetType().Name + ".", string.Join("", rawTokens), string.Join("", rawTokens.Take(i)).Length, currentContext, addedVariables);
                                stack.Peek().AddLast(subCtxt);
                                currentContext = subCtxt;
                                continue;
                            }
                            break;
                    }

                    // Step #3e - we still don't know what the token is, so it's definitely a syntax error.
                    throw new SyntaxException("Invalid token: " + rawToken, string.Join("", rawTokens), string.Join("", rawTokens.Take(i)).Length, currentContext, addedVariables);


                }
            }
            catch (Exception ex)
            {
                // An exception means that changes cannot be made to the states of the variables and the contexts.  Unwind any changes 
                // made to state, ie, remove added variables and contexts.  Then, release the editing lock.  Finally, ensure the exception 
                // make its way up the stack.                
                result.Cancel();
                if (ex is SyntaxException) throw;
                throw new SyntaxException("Syntax error.", str, 0, currentContext, addedVariables, ex);
            }


            IEvaluateable contents = rootList.Parse(functions);            
            if (contents is Clause clause) clause.Terms = terms;
            while (contents is Clause c && c.Inputs.Count() == 1 && c.IsNaked)
                contents = c.Inputs[0];
            result.Contents = contents;           
            return result;
        }



        public IEvaluateable Commit()
        {
            IEvaluateable ret = this.Contents;
            if (_ChangeLock != null) { Monitor.Exit(_ChangeLock); _ChangeLock = null; }
            return ret;
        }

        public void Cancel()
        {
            foreach (Variable v in AddedVariables) v.Context.TryDelete(v);
            foreach (Context c in AddedContexts) c.Parent.TryDelete(c);
            if (_ChangeLock != null) { Monitor.Exit(_ChangeLock); _ChangeLock = null; }
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
        private const string OperatorPattern = @"[+-/*&|^~!\.]"; //@"\+\-*/&\|^~!\.;";
        private const string WordPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
        private const string NumberPattern = @"(-)? (?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?"; //Includes support for scientific notation!
        private const string SpacePattern = @"?=\s+";
        private const string VariablePattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
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
            public IEvaluateable Parse(Function.Factory functions)
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
                    = new StableHeap<DynamicLinkedList<object>.Node>((a, b) => ((Function)a.Contents).Priority.CompareTo(((Function)b.Contents).Priority));

                DynamicLinkedList<object>.Node node = this.FirstNode;
                while (node != null)
                {
                    switch (node.Contents)
                    {
                        case TokenList tl: node.Contents = tl.Parse(functions); break;
                        case EvaluationError e: return e;
                        case Function f: prioritized.Enqueue(node); break;
                        case Number n when node.Next != null:
                            if (node.Next.Contents is Operator || node.Next.Contents is Constant) break;
                            if (node.Next.Contents is TokenList || node.Next.Contents is Variable || node.Next.Contents is Function || node.Next.Contents is Clause)
                                node.InsertAfter(Function.Factory.CreateMultiplication());
                            break;
                    }
                    node = node.Next;
                }

                // Step #3 - construct a within-this-clause tree according to operator precedence.
                while (prioritized.Count > 0)
                {
                    node = prioritized.Dequeue();
                    Function function = (Function)node.Contents;
                    function.ParseNode(node);
                    function.Terms = new HashSet<Variable>();
                    //foreach (IEvaluateable input in function.Inputs)                    
                    //    foreach (Variable v in Variable.GetTermsOf(input)) function.Terms.Add(v);                        
                    
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

        #region IDisposable Support

        // A disposed Expression must release all its held locks.

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Cancel();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose() => Dispose(true);

        #endregion


    }
}
