using DataStructures;
using Parsing.NamedFunctions;
using Parsing.Operators;
using Mathematics.Functions;
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
    public abstract class Formula : IDisposable //, ICacheValue
    {

        
        private object[] _Inputs = null;

        /// <summary>The arguments that determine how the formula is evaluated.  Starts out null.</summary>
        public object[] Inputs
        {
            get => _Inputs;
            protected  set
            {
                //Update
                _Inputs = value;

                //Combine variables.
                foreach (object input in value)
                {
                    if (input is Variable v) Variables.Add(v);
                    else if (input is Formula f) foreach (Variable v1 in f.Variables) Variables.Add(v1);
                }
            }
        }

        /// <summary>The Variables of which this Formula is a function.  Starts out instantiated.</summary>
        public ISet<Variable> Variables { get; private set; } = new HashSet<Variable>();

        /// <summary>The DataContext specifying the Variables and NamedFunctions for this Formula.  If null, no references to Variables 
        /// or NamedFunctions will be allowed.</summary>
        public DataContext Context { get; private set; }

        /// <summary>The parent of the Formula.  If this Formula is root, this will be null.</summary>
        public Formula Parent { get; protected set; }


        /// <summary>Returns whether this Formula is identical to the other given Formula.</summary>
        /// <remarks>Can be overridden in derived classes.  Default behave is to return strict identicality (meaning all inputs are 
        /// identical in identical order).</remarks>
        protected virtual bool IsIdenticalTo(Formula other)
        {
            //Shortcut - reference equality must be identical.
            if (object.ReferenceEquals(this, other)) return true;

            //Shortcut - different types cannot be identical.
            if (GetType() != other.GetType()) return false;

            //Shortcut - unequal input counts cannot be identical.
            if (Inputs.Length != other.Inputs.Length) return false;

            //Examine all inputs.
            for (int i = 0; i < other.Inputs.Length; i++)
            {

                //If both inputs are Formulas, recursively examine the inputs.
                if (Inputs[i] is Formula a && other.Inputs[i] is Formula b && !a.IsIdenticalTo(b))
                    return false;

                //Otherwise, the inputs can be equal only if Equals() is true.
                else if (!Inputs[i].Equals(other.Inputs[i]))
                    return false;
            }

            //All inputs have been examined and found equal, the types are equal, so the Formula must be equal.
            return true;
        }

        /// <summary>
        /// A helper method which returns whether two lists are identical in all respects except that their items are in different orders.
        /// </summary>
        protected static bool ArePermutations(IList<object> a, IList<object> b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a.Count != b.Count) return false;
            List<object> compare = new List<object>(b);
            foreach (object item in a)
                if (!compare.Remove(item)) return false;
            return true;
        }
        


        #region Formula value caching members

        /// <summary>The cached value of this Formula.</summary>
        public object Value { get; protected set; }

        /// <summary>Updates the cached value of the formula, and returns that value.</summary>        
        public object Update()
        {
            List<object> evaluatedInputs = new List<object>();
            foreach (object obj in Inputs)
            {
                if (obj is Formula f) evaluatedInputs.Add(f.Update());
                else if (obj is Variable v) evaluatedInputs.Add(v.Update());
                else evaluatedInputs.Add(obj);
            }
            return Value = Evaluate(evaluatedInputs);
        }



        /// <summary>The method called to find a Formula's value.</summary>
        /// <param name="evaluatedInputs">The inputs to evaluate.  For inputs which are ICacheValue, the cached value is supplied instead of the 
        /// caching object.</param>        
        protected abstract object Evaluate(IList<object> evaluatedInputs);


        /// <summary>Returns the cached value for the object, if there is one, or the object itself.</summary>
        protected static object GetValue(object obj)
        {
            if (obj is Formula f) return f.Value;
            else if (obj is Variable v) return v.Value;
            return obj;
        }

        

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
        protected internal abstract int ParsingPriority { get; }




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
            var pattern = (context == null) ? DataContext.StandardFormulaPattern : context.FormulaPattern;
            string[] rawTokens = pattern.Split(str);

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
            BlockNode result = Lex(rawTokens.ToList(), context);

            //Second, ensure that references to all the formula's variables are incremented.
            foreach (Variable var in result.Block.Variables) var.References++;

            //Third, parse.            
            result.Block.Parse(result.Inputs.FirstNode);            

            //Sanity check.
            if (result.Block.Inputs.Length != 1) throw new LexingException("Lexing structure error.  Not sure how this might even happen.");

            return result.Block.Inputs[0];

        }



        /// <summary>A structure used for building Formulas.</summary>
        /// <remarks>This data structure is used for building the Formulas because it allows for a dynamic linked list for its Inputs, 
        /// whereas a finished Block must have an array specifying its Inputs.  Once building the Formula is complete, all of these 
        /// structs are thrown away and garbage collected.</remarks>
        protected internal struct BlockNode
        {
            /// <summary>The Formula block associated with these inputs.</summary>
            public readonly Block Block;
            /// <summary>The inputs associated with this Block.</summary>
            public readonly DynamicLinkedList<object> Inputs;           
            /// <summary>Creates a new Block node.</summary>
            /// <param name="block">The Block associated with this node.</param>
            public BlockNode(Block block) { Block = block; Inputs = new DynamicLinkedList<object>(); }
        }

        /// <summary>Lexes the tokens, and returns the result within a parenthetical block (this block constitutes one extra 
        /// layer which may need to be removed).  For example, if the tokens are:  "COS" "(" "3.14159" ")", then a single parenthetical 
        /// block will be returned.  The inputs will be a named function COS, then another parenthetical block whose only contents is the 
        /// number 3.14159.</summary>
        private static BlockNode Lex(IList<string> rawTokens, DataContext context)
        {
            BlockNode focus = new BlockNode(Block.FromParenthetical(context));
            BlockNode headNode = focus;
            Stack<BlockNode> stack = new Stack<BlockNode>();

            for (int tokenIdx = 0; tokenIdx < rawTokens.Count; tokenIdx++)
            {

                string token = rawTokens[tokenIdx].Trim();

                try
                {
                    //Step #1 - skip whitespace.                
                    if (string.IsNullOrWhiteSpace(token) || string.IsNullOrEmpty(token)) continue;

                    //Step #2 - opening nesting structures?
                    if (Block.TryOpen(token, context, out Block b))
                    {
                        if (focus.Inputs.Count > 0 && focus.Inputs.Last is decimal)  //A scalar?  Then it's an implied multiplication.
                            focus.Inputs.AddLast(new Star(context));

                        //Create the new Block nested within the current focus.
                        BlockNode bnNew = new BlockNode(b);
                        focus.Inputs.AddLast(bnNew);
                        stack.Push(focus);
                        focus = bnNew;

                        continue;
                    }

                    //Step #3 - closing a nesting structure?
                    if (stack.Count > 0 && stack.Peek().Block.Closer == token)
                    {
                        //The parent's variable list must include everything relied upon by the child.
                        BlockNode parentBlockInputs = stack.Pop();
                        //parentBlockInputs.Block.CombineVariables(focus.Block);                        
                        focus = parentBlockInputs;
                        continue;
                    }

                    //Step #4 - a literal?
                    if (decimal.TryParse(token, out decimal number)) { focus.Inputs.AddLast(number); continue; }
                    if (bool.TryParse(token, out bool @bool)) { focus.Inputs.AddLast(@bool); continue; }
                    if (TryParseString(token, out string str)) { focus.Inputs.AddLast(str); continue; }

                    //Step #5 - an operator?
                    if (Operator.TryLex(token, focus.Inputs, context, out Operator op)) { focus.Inputs.AddLast(op); continue; }

                    //Step #6 - Context-dependent tokens include Variables and NamedFunctions
                    if (context != null)
                    {
                        object lexxed = null;
                        //Step #6a - a named function?
                        if (context.TryMakeFunction(token, out NamedFunction namedFunction)) lexxed = namedFunction;

                        //Step #6b - an existing variable?
                        else if (context.TryGetVariable(token, out Variable v)) focus.Block.Variables.Add((Variable)(lexxed = v));

                        //Step #6c - a new variable?
                        else if (context.IsVariableNameValid(token) && (lexxed = context.AddVariable(token)) != null) focus.Block.Variables.Add((Variable)lexxed);

                        if (lexxed != null)
                        {
                            //Step #6d - implied scalar?
                            if (focus.Inputs.Count > 0 && focus.Inputs.Last is decimal)
                                focus.Inputs.AddLast(new Star(context));
                            focus.Inputs.AddLast(lexxed);
                            continue;
                        }

                        //Step #6e - special handling provided by the context?
                        if (context.InterpretToken(token, out object specialToken)) { focus.Inputs.AddLast(specialToken); continue; }
                    }


                    //Step #7 - a divider between inputs?
                    if (token == ",") continue;

                    //Step #8 - a divider between sub-vectors?
                    if (token == ";") throw new NotImplementedException();

                    //Step #9 - some blocking exceptions
                    if (token == ")" || token == "]" || token == "}")
                        throw new LexingException("Closing bracket '" + token + "' without an opening bracket.");

                    //Step #10 - something else?  interpret as a string and parse later.
                    focus.Inputs.AddLast(token);
                }
                catch (LexingException l_ex)
                {
                    l_ex.TokenIndex = tokenIdx;
                    l_ex.Tokens = rawTokens.ToArray();
                    throw;
                }
                catch (Exception ex)
                {
                    throw new LexingException(ex.Message, ex, rawTokens.ToArray(), tokenIdx);
                }
            }            

            //Some final global error handling.
            if (stack.Count > 0) throw new FormatException("Unclosed brackets.");
            if (focus.Block != headNode.Block) throw new FormatException("Incomplete nesting.");
            if (headNode.Block.Opener != "(" || headNode.Block.Closer != ")") throw new FormatException("Undefined nesting error.");
            if (headNode.Inputs.Count < 1) throw new FormatException("No lexable tokens.");
            
            return headNode;



            
        }

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




        /// <summary>Parses the given Formula as situated within a list of inputs.</summary>        
        protected abstract void Parse(DynamicLinkedList<object>.Node node);


        /// <summary>Creates a Formula, with the given data context.</summary>  
        /// <param name="inputs">Optional.  The Inputs to set up this Formula with.</param>
        /// <param name="context">The data context from which Variables will be referenced or NamedFunctions created.</param>        
        protected Formula (DataContext context, IEnumerable<object> inputs)
        {
            Context = context;
            if (inputs != null) Inputs = inputs.ToArray();
        }


        

        /// <summary>
        /// Returns a copy of this formula.  All sub-formulae and literals will be copied, but references to Variables will be the same 
        /// as in the original, and the copy will have the same Context as the original.  The copy will not have its value cached yet, 
        /// and will require a separate call to Update().
        /// </summary>
        public virtual Formula Copy()
        {
            throw new NotImplementedException();
        }



        #endregion




        #region Formula calculus members
        

        /// <summary>Returns the derivative of this Formula.</summary>
        /// <param name="obj">The object to seek the derivative of.</param>
        /// <param name="v">The Variable, with respect to which the derivative is sought.</param>            
        public static object GetDerivative(object obj, Variable v)
        {
            if (obj is decimal) return 0m;
            if (obj is Variable) return (obj.Equals(v)) ? 1m : 0m;
            if (obj is Formula f)
            {
                object d = f.GetDerivativeRecursive(v);
                while (d is Formula)
                {
                    object simpler = ((Formula)d).GetSimplified();
                    if (simpler.Equals(d)) break;
                    d = simpler;
                }
                return d;
            }
            throw new InvalidOperationException("Cannot find derivative of object of type " + obj.GetType().Name + ".");
        }

        
        /// <summary>
        /// Returns the simplified version of this Formula.  If the Formula cannot be simplified, returns a copy of the Formula.
        /// </summary>        
        public object GetSimplified()
        {
            object[] simps = new object[_Inputs.Length];
            for (int i = 0; i < _Inputs.Length; i++)
                simps[i] = (_Inputs[i] is Formula f) ? f.GetSimplified() : _Inputs[i];
            return FromSimplified(simps);
        }

        /// <summary>Override to specify how a particular Formula type simplifies.</summary>
        /// <param name="simplifiedInputs">The inputs have already had GetSimplified() called on them, and returned here.</param>
        /// <returns></returns>
        protected abstract object FromSimplified(IList<object> simplifiedInputs);


        /// <summary>Gets the derivative of the object, with respect to the given Variable.</summary>
        protected abstract object GetDerivativeRecursive(Variable v);


        #endregion



        /// <summary>A block of a formula.</summary>
        protected internal sealed class Block : Formula
        {
            /// <summary>The symbol that opens this block.</summary>
            public readonly string Opener;

            /// <summary>The symbol that closes this block.</summary>
            public readonly string Closer;

            /// <summary>Blocks have very low (early) priority.</summary>
            protected internal override int ParsingPriority
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

            
            /// <summary>Parse the contents of this block.</summary>
            /// <param name="node">Usually this refers to the node as its contents will appear among its parent's inputs.  For purposes of 
            /// a block, since the blocking structure is aleady established, this refers to the first node in the block's inputs.</param>    
            protected override void Parse(DynamicLinkedList<object>.Node node)
            {
                var n = node;

                //Prioritize the DynamicLinkedList nodes containing Formulas, according to those Formulas' parsing priority.  At the same 
                //time, recursively parse any content Blocks.
                Heap<DynamicLinkedList<object>.Node> priority = new Heap<DynamicLinkedList<object>.Node>(_n => ((Formula)_n.Contents).ParsingPriority);
                while (n != null)
                {
                    if (n.Contents is BlockNode bn)
                    {
                        bn.Block.Parse(bn.Inputs.FirstNode);
                        n.Contents = bn.Block;
                    }
                    else if (n.Contents is Formula f) priority.Add((DynamicLinkedList<object>.Node)n);
                    n = n.Next;
                }
                
                //Now, parse the node's contents in order of priority.
                while (priority.Count > 0)
                {
                    n = priority.Pop();
                    if (n.Contents is Formula f) f.Parse((DynamicLinkedList<object>.Node)n);
                }

                //Now, set the Block's Inputs to be the contents of the node's list.  If there were no prioritized nodes, then 'n' will 
                //be null.
                Inputs = (n == null) ? node.List.ToArray() : n.List.ToArray();
                
            }



            private Block(string opener, string closer, DataContext context) : this(opener, closer, context, new object[0]) { }
            private Block(string opener, string closer, DataContext context, params object[] inputs) : base(context, inputs)
            {
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


            
            /// <summary>Brackets the inputs with the Opener and Closer.</summary>            
            public override string ToString()
            {
                return Opener + string.Join(", ", Inputs) + Closer;
            }

            /// <summary>A block can have only a single evaluated input in its content.</summary>
            protected override object Evaluate(IList<object> inputs)
            {
                if (inputs.Count != 1) throw new EvaluationException("Orphan data blocks can have only one input.");
                return inputs[0];
            }

            /// <summary>A block can have only a single input, so returns the derivative of that.</summary>
            protected override object GetDerivativeRecursive(Variable v)
            {
                if (Opener != "(" || Opener != ")") throw new InvalidOperationException("Can only find derivative of parenthetical Block.");
                if (Inputs.Length == 1) return GetDerivative(Inputs[0], v);
                throw new InvalidOperationException("Can only find derivative of Block with single input.");
            }

            /// <summary>
            /// If the block's simplified contents is a Formula, then this Block's existence is meaningful and so returns a copy of itself 
            /// with the copy's contents equal to the simplified contents.  Otherwise, returns the simplified contents themselves.
            /// </summary>
            protected override object FromSimplified(IList<object> simplifiedInputs)
            {
                if (simplifiedInputs.Count != 1) throw new InvalidOperationException("Sanity check.");
                return (simplifiedInputs[0] is Formula f) ? new Block(Opener, Closer, Context, f) : simplifiedInputs[0];
            }
        }




        /// <summary>Returns true if the other object is a Formula for which IsIdenticalTo is true.</summary>
        public sealed override bool Equals(object obj) => (obj is Formula other_f) ? IsIdenticalTo(other_f) : false;

        /// <summary>The hash code is the sum of all the inputs' hash codes.</summary>        
        /// <remarks>TODO:  since Formula is immutable, would it be useful if it cached its hash code?</remarks>
        public sealed override int GetHashCode() => Inputs.Sum(input => input.GetHashCode());



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
