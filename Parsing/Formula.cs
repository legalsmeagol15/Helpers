using DataStructures;
using Parsing.NamedFunctions;
using Parsing.Operators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Parsing
{


    /// <summary>
    /// A Formula is a parse tree that interprets strings for evaluation.  The resulting tree should be as near as possible to the 
    /// structure of the original string, with no flattening or simplification.
    /// </summary>
    public abstract class Formula : IDisposable, ICacheValue
    {
        /// <summary>The arguments that determine how the formula is evaluated.  Starts out null.</summary>
        public List<object> Inputs { get; protected internal set; } = null;

        /// <summary>The Variables of which this Formula is a function.</summary>
        public ISet<Variable> Variables { get; private set; } = new HashSet<Variable>();

        /// <summary>The DataContext specifying the Variables and NamedFunctions for this Formula.  If null, no references to Variables 
        /// or NamedFunctions will be allowed.</summary>
        public DataContext Context { get; private set; }




        #region Formula value caching members

        /// <summary>The cached value of this Formula.</summary>
        public object Value { get; protected set; }

        /// <summary>Updates the cached value of the formula, and returns that value.</summary>        
        public object Update()
        {
            object[] inputs = Inputs.Select(i => (i is ICacheValue icf) ? icf.Update() : i).ToArray();
            return Value = Evaluate(inputs);
        }

        /// <summary>The method called to find a Formula's value.</summary>
        /// <param name="inputs">The inputs to evaluate.  For inputs which are ICacheValue, the cached value is supplied instead of the 
        /// caching object.</param>        
        protected abstract object Evaluate(params object[] inputs);

        /// <summary>Returns the cached value for the object, if there is one, or the object itself.</summary>
        protected static object GetValue(object obj) => obj is ICacheValue icv ? icv.Value : obj;


        ///// <summary>Returns the cached values for the objects, if they exist, or the objects themselves.</summary>
        //protected static object[] GetValues(IEnumerable<object> inputs) => 
        //                                    (inputs.Select((input) => (input is ICacheValue icf) ? icf.Value() : input)).ToArray();

        #endregion




        #region Formula constructors and parsers



        #region Formula parsing priorities

        /// <summary></summary>
        protected const int PRIORITY_PLUS = 11;
        /// <summary></summary>
        protected const int PRIORITY_MINUS = 10;
        /// <summary></summary>
        protected const int PRIORITY_STAR = 8;
        /// <summary></summary>
        protected const int PRIORITY_DIVIDE = 9;
        /// <summary></summary>
        protected const int PRIORITY_HAT = 5;
        /// <summary></summary>
        protected const int PRIORITY_NOT = 3;
        /// <summary></summary>
        protected const int PRIORITY_AMPERSAND = 13;
        /// <summary></summary>
        protected const int PRIORITY_PIPE = 14;
        /// <summary></summary>
        protected const int PRIORITY_COLON = 2;
        /// <summary></summary>
        protected const int PRIORITY_QUESTION = 1;
        /// <summary></summary>
        protected const int PRIORITY_DOT = 0;
        /// <summary></summary>
        protected const int PRIORITY_NAMED_FUNCTION = -1;
        /// <summary></summary>
        protected const int PRIORITY_BLOCK_PARENTHESES = -2;
        /// <summary></summary>
        protected const int PRIORITY_BLOCK_CURLY = -2;
        /// <summary></summary>
        protected const int PRIORITY_BLOCK_BRACKETS = -2;

        #endregion


        /// <summary></summary>
        internal abstract int ParsingPriority { get; }




        /// <summary>Creates a Formula from the given LaTex string.</summary>        
        public static object FromLaTex() { throw new NotImplementedException(); }

        /// <summary>
        /// Creates a Formula from the given string, within the given data context.
        /// </summary>
        /// <param name="str">The string to interpret into a Formula.</param>
        /// <param name="context">Optional.  The DataContext for the Formula.  If omitted or given null, no DataContext will be 
        /// established, and so the Formula will be unable to interpret any Variables or NamedFunctions.</param>        
        public static object FromString(string str, DataContext context = null)
        {
            string[] rawTokens = (context == null) ? DataContext.StandardFormulaPattern.Split(str) : context.FormulaPattern.Split(str);

            if (rawTokens.Length == 0) throw new LexingException("No tokens found.", rawTokens, -1);

            return FromTokens(rawTokens, context);
        }


        /// <summary>
        /// Parses the given string tokens into a parse tree, and returns it with minimal correction.
        /// </summary>
        internal static object FromTokens(IEnumerable<string> rawTokens, DataContext context = null)
        {
            if (rawTokens == null) throw new ArgumentException("Token list cannot be null.");

            //First, lex the string tokens into sub-formulae and nesting structure.
            Formula result = Lex(rawTokens.ToList(), context);

            //Second, ensure that references to all the formula's variables are incremented.
            foreach (Variable var in result.Variables) var.References++;

            //Third, parse.            
            result.Parse(null);


            if (result.Inputs.Count != 1) throw new LexingException("Lexing structure error.  Not sure how this might even happen.");

            return result.Inputs[0];

        }


        /// <summary>Tries to parse the token into a string literal.</summary>
        private static bool TryParseString(string token, out string result)
        {
            if (token[0] == '\"'                                //Starts with quotes?
                    && token[token.Length - 1] == '\"'          //Ends with quotes?
                    && token.Count((c) => c == '\"') == 2)      //Only two quotes?
            {
                result = token.Substring(1, token.Length - 2);
                return true;
            }

            result = "";
            return false;
        }






        /// <summary>Lexes the tokens, and returns the result within a parenthetical block (this block constitutes one extra 
        /// layer which may need to be removed).  For example, if the tokens are:  "COS" "(" "3.14159" ")", then a single parenthetical 
        /// block will be returned.  The inputs will be a named function COS, then another parenthetical block whose only contents is the 
        /// number 3.14159.</summary>
        private static Formula Lex(IList<string> rawTokens, DataContext context)
        {
            Formula.Block focus = Formula.Block.FromParenthetical(context);
            Formula.Block head = focus;
            Stack<Formula.Block> stack = new Stack<Formula.Block>();

            for (int tokenIdx = 0; tokenIdx < rawTokens.Count; tokenIdx++)
            {

                string token = rawTokens[tokenIdx].Trim();

                try
                {
                    //Step #1 - skip whitespace.                
                    if (string.IsNullOrWhiteSpace(token) || string.IsNullOrEmpty(token)) continue;

                    //Step #2 - opening nesting structures?
                    if (Formula.Block.TryOpen(token, context, out Formula.Block nfNew))
                    {
                        if (focus.Inputs.Count > 0 && focus.Inputs.Last() is decimal)  //A scalar?  Then it's an implied multiplication.
                            focus.Inputs.Add(new Star(context));
                        focus.Inputs.Add(nfNew);
                        stack.Push(focus);
                        focus = nfNew;
                        continue;
                    }

                    //Step #3 - closing a nesting structure?
                    if (stack.Count > 0 && stack.Peek().Closer == token)
                    {
                        //The parent's variable list must include everything relied on by the child.
                        Formula.Block parent = stack.Pop();
                        foreach (Variable childVar in focus.Variables) parent.Variables.Add(childVar);
                        focus = parent;
                        continue;
                    }

                    //Step #4 - a literal?
                    if (decimal.TryParse(token, out decimal number)) { focus.Inputs.Add(number); continue; }
                    if (bool.TryParse(token, out bool @bool)) { focus.Inputs.Add(@bool); continue; }
                    if (TryParseString(token, out string str)) { focus.Inputs.Add(str); continue; }

                    //Step #5 - an operator?
                    if (Operator.TryLex(token, focus.Inputs, context, out Operator op)) { focus.Inputs.Add(op); continue; }

                    //Step #6 - Context-dependent tokens include Variables and NamedFunctions
                    if (context != null)
                    {
                        object lexxed = null;
                        //Step #6a - a named function?
                        if (context.TryMakeFunction(token, out NamedFunction namedFunction)) lexxed = namedFunction;

                        //Step #6b - an existing variable?
                        else if (context.TryGetVariable(token, out Variable v)) focus.Variables.Add((Variable)(lexxed = v));

                        //Step #6c - a new variable?
                        else if (context.IsVariableNameValid(token) && (lexxed = context.AddVariable(token)) != null) focus.Variables.Add((Variable)lexxed);

                        if (lexxed != null)
                        {
                            //Step #6d - implied scalar?
                            if (focus.Inputs.Count > 0 && focus.Inputs[focus.Inputs.Count - 1] is decimal)
                                focus.Inputs.Add(new Star(context));
                            focus.Inputs.Add(lexxed);
                            continue;
                        }

                        //Step #6e - special handling provided by the context?
                        if (context.InterpretToken(token, out object specialToken)) { focus.Inputs.Add(specialToken); continue; }
                    }


                    //Step #7 - a divider between inputs?
                    if (token == ",") continue;

                    //Step #8 - a divider between sub-vectors?
                    if (token == ";") throw new NotImplementedException();
                }
                catch (Exception ex)
                {
                    if (ex is LexingException fe) { fe.TokenIndex = tokenIdx; fe.Tokens = rawTokens.ToArray(); throw; }
                    else throw new LexingException(ex.Message, ex, rawTokens.ToArray(), tokenIdx);
                }

                //Error handling.
                {
                    //Invalid nest closure?
                    if (token == ")" || token == "]" || token == "}")
                        throw new LexingException("Closing bracket '" + token + "' without opening bracket.", rawTokens, tokenIdx);

                    //Something else?
                    throw new LexingException("Unrecognized token: " + token + ".", rawTokens, tokenIdx);
                }

            }

            //Some final global error handling.
            if (stack.Count > 0) throw new FormatException("Unclosed brackets.");
            if (focus != head) throw new FormatException("Incomplete nesting.");
            if (head.Opener != "(" || head.Closer != ")") throw new FormatException("Undefined nesting error.");
            if (head.Inputs.Count < 1) throw new FormatException("No lexable tokens.");

            return head;
        }






        /// <summary>Parses the given Formula as situated within a list of inputs.</summary>
        /// <param name="node">The node containing this formula.  The next input will be in node.Next.Contents, and previous will be 
        /// in node.Previous.Contents, and so forth.</param>
        protected abstract void Parse(DynamicLinkedList<object>.Node node);


        /// <summary>Creates a Formula, with the given data context.</summary>        
        protected Formula(DataContext context) { Context = context; }



        /// <summary>Adds all the Variables from the given <paramref name="other"/> Formula to the this Formula's Variables.</summary>        
        protected internal void CombineVariables(object other)
        {
            if (other is Variable v) Variables.Add(v);
            else if (other is Formula f) foreach (Variable v1 in f.Variables) Variables.Add(v1);
        }


        /// <summary>
        /// Returns a copy of this formula.  All sub-formulae and literals will be copied, but references to Variables will be the same 
        /// as in the original, and the copy will have the same Context as the original.  The copy will not have its value cached yet, 
        /// and will require a separate call to Update().
        /// </summary>
        public virtual Formula Copy()
        {
            Formula copy = (Formula)Activator.CreateInstance(this.GetType(), new object[1] { Context });
            copy.Inputs = new List<object>();
            foreach (object existing in Inputs)
            {
                if (existing is Formula existing_f) copy.Inputs.Add(existing_f.Copy());
                else if (existing is Variable v) copy.Inputs.Add(v);
                else copy.Inputs.Add(existing);
            }
            copy.Variables = new HashSet<Variable>(Variables);
            return copy;
        }


        ///// <summary>Idealizes the formula in terms of the variables given, in the order given.</summary>        
        //public void Idealize(IEnumerable<Variable> variables)
        //{

        //}

        #endregion



        #region Formula calculus members
        //Because formulas are also functions in terms of the variables in the Variables member.

        public static object GetDerivative()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the derivative of the given object, with respect to the given variables.
        /// </summary>
        /// <param name="variables"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public static object GetDerivative(ISet<Variable> variables, object function)
        {
            if (function is decimal)
                return 0m;
            else if (function is Variable v)
            {
                if (variables.Contains(v)) return 1;
                throw new DerivationException("Cannot find derivative of a variable that does not appear in the variable set.");
            }            
            else if (function is Formula f)
            {
                if (f.TryDerive(variables, out object result)) return result;
            }
            throw new DerivationException("Cannot find derivative of type " + function.GetType().Name + ".");
        }

        protected virtual bool TryDerive(ISet<Variable> variables, out object derivative)
        {

            derivative = null;
            return false;
        }

        #endregion




        /// <summary>A block of a formula.</summary>
        internal sealed class Block : Formula
        {
            /// <summary>The symbol that opens this block.</summary>
            public readonly string Opener;

            /// <summary>The symbol that closes this block.</summary>
            public readonly string Closer;

            internal override int ParsingPriority
            {
                get
                {
                    switch (Opener)
                    {
                        case "(": return PRIORITY_BLOCK_PARENTHESES;
                        case "[": return PRIORITY_BLOCK_BRACKETS;
                        case "{": return PRIORITY_BLOCK_CURLY;
                    }
                    throw new NotImplementedException();
                }
            }


            private struct NodeOrder : IComparable<NodeOrder>
            {
                public readonly DynamicLinkedList<object>.Node Node;
                public readonly int Order;
                public NodeOrder(DynamicLinkedList<object>.Node node, int order) { Node = node; Order = order; }

                int IComparable<NodeOrder>.CompareTo(NodeOrder other)
                {
                    Formula f_this = (Formula)Node.Contents;
                    Formula f_other = (Formula)other.Node.Contents;
                    int c = f_this.ParsingPriority.CompareTo(f_other.ParsingPriority);
                    if (c != 0) return c;
                    return this.Order.CompareTo(other.Order);
                }
            }


            protected override void Parse(DynamicLinkedList<object>.Node node)
            {
                //There is no in-place parsing for a Block.  Just parse the children.

                //Create a dynamic list of all the children.


                //Put all the formula with parsing priority on a heap.
                Heap<NodeOrder> priority = new Heap<NodeOrder>();
                DynamicLinkedList<object> list = new DynamicLinkedList<object>();
                for (int i = 0; i < Inputs.Count; i++)
                {
                    var n = list.AddLast(Inputs[i]);
                    if (Inputs[i] is Formula f) priority.Add(new NodeOrder(n, i));
                }

                //Parse the items on the heap in order of priority.
                while (priority.Count > 0)
                {
                    var n = priority.Pop().Node;
                    if (n.Contents is Formula f) f.Parse(n);
                }

                Inputs = list.ToList();
            }



            private Block(string opener, string closer, DataContext context) : base(context)
            {
                Inputs = new List<object>();
                Opener = opener;
                Closer = closer;
            }

            /// <summary>
            /// Returns whether a new Block can be opened with the given symbol.  If it can, the new block will be contained in 
            /// '<paramref name="result"/>'.  If, for some reason, an error occurs in the Block creation, a message indicating the error 
            /// will be returned in '<paramref name="result"/>'.  If a Block cannot be created, the '<paramref name="result"/>' will 
            /// return null.
            /// </summary>
            /// <param name="opener">The symbol, such as '(' or '[', that would open a block.</param>
            /// <param name="context">The data context for the block.</param>
            /// <param name="result">Out.  The block created from the given opener.  If no block could be created, returns null.</param>
            /// <returns>Returns true if a new block could be created with the given opener, or if an error occurred during the creation; 
            /// otherwise, returns false.</returns>
            public static bool TryOpen(string opener, DataContext context, out Block result)
            {
                switch (opener)
                {
                    case "(": result = FromParenthetical(context); return true;
                    case "[": result = FromSquare(context); return true;
                    case "{": result = FromCurly(context); return true;
                }
                result = null;
                return false;
            }


            /// <summary>Creates a new parenthetical block.</summary>
            public static Block FromParenthetical(DataContext context) { return new Block("(", ")", context); }

            /// <summary>Creates a new square-bracket block.</summary>
            public static Block FromSquare(DataContext context) { return new Block("[", "]", context); }

            /// <summary>Creates a new curly-bracket block.</summary>
            public static Block FromCurly(DataContext context) { return new Block("{", "}", context); }



            public override string ToString()
            {
                return Opener + string.Join(", ", Inputs) + Closer;
            }

            protected override object Evaluate(params object[] inputs)
            {
                if (inputs.Length != 1) throw new EvaluationException("Orphan data blocks can have only one input.");
                return inputs[0];
            }

            protected override bool TryDerive(ISet<Variable> variables, out object derivative)
            {
                if (Inputs.Count == 1 && Inputs[0] is Formula f) return f.TryDerive(variables, out derivative);
                return base.TryDerive(variables, out derivative);
            }
        }






        #region Formula IDisposable support

        private bool _disposedValue = false; // To detect redundant calls

        /// <summary>
        /// The method called upon garbage collection of this Formula.  For all Variables that are referenced in the Formula, the count 
        /// of references is decremented.  If no references remain, the Variable is removed from the associated Context's Variable table.
        /// </summary>        
        protected virtual void Dispose(bool disposing)
        {
            //TODO:  must this method really be made virtual or protected?  How about private and non-virtual?

            if (!_disposedValue)
            {
                if (disposing)
                {
                    //The only objects necessarily managed here are variables existing on the 
                    //context.  If the Formula is disposed and no reference remains, then 
                    //destroy the variable.
                    foreach (Variable v in Variables)
                    {
                        if (--v.References < 1) Context.RemoveVariable(v);
                    }
                }
                //No unmanaged resources
                _disposedValue = true;
            }
        }


        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }


        #endregion


    }

}
