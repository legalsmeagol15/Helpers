using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataStructures;
using System.Diagnostics;

namespace Dependency
{
    

    public sealed class Parse
    {
        /// <summary>Readable left-to-right.</summary>
        public interface IExpression : IEvaluateable
        {
            
        }

        public static IEvaluateable FromString(string str, IFunctionFactory functions = null, IContext rootContext = null)
        {

            string[] splits = _Regex.Split(str);
            int splitIdx = 0;
            IContext ctxt = rootContext;

            try
            {
                Parenthetical p = new Parenthetical();
                _TokenizeAndParse(p);
                return p.Head;
            }
            catch (SyntaxException _)
            {
                // This is a normal exception occurring due to a bad input string.  It will only have a message, no splits data.
                throw;
            }
            catch (Exception e)
            {
                // Any other exception represents a bug.
                throw new SyntaxException(__ComposeLexed(), splits[splitIdx], "Unexpected exception: " + e.GetType().Name, e);
            }

            string __ComposeLexed() => string.Join("", splits.Take(splitIdx));

            IEvaluateable _TokenizeAndParse(IExpression exp)
            {
                DynamicLinkedList<IEvaluateable> inputs = new DynamicLinkedList<IEvaluateable>();
                Heap<Token> heap = new Heap<Token>();
                bool dot = false;                
                while (splitIdx < splits.Length)
                {
                    string token = splits[splitIdx++];
                    if (string.IsNullOrWhiteSpace(token)) continue;

                    // If a reference has been started, parse as a reference.
                    if (ctxt != rootContext)
                    {
                        if (token == ".")
                        {
                            if (dot) throw new ReferenceException(__ComposeLexed(), token, "Bad reference path.");
                            dot = true;
                            continue;
                        }
                        else if (!dot)
                            throw new ReferenceException(__ComposeLexed(), token, "Reference path items must be separated by dot '.' operators.");
                        else if (ctxt.TryGetSubcontext(token, out IContext next_ctxt)) ctxt = next_ctxt;
                        else if (ctxt.TryGetVariable(token, out IVariable var)) { inputs.AddLast(var); ctxt = rootContext; }
                        else if (ctxt is IRangeable ir && ir.TryGetImmobile(token, out Reference r)) { inputs.AddLast(r); ctxt = rootContext; }
                        else
                            throw new ReferenceException(__ComposeLexed(), token, "Reference path termination must be an evaluateable variable.");
                        dot = false;
                        continue;
                    }                    
                    Debug.Assert(!dot, "It should be impossible to see a dot at this point.");                    

                    // Handle literals
                    if (Number.TryParse(token, out Number n)) { inputs.AddLast(n); continue; }

                    else if (bool.TryParse(token, out bool b)) { inputs.AddLast(Dependency.Boolean.FromBool(b)); continue; }

                    else if (String.IsQuotedString(token)) { inputs.AddLast(new Dependency.String(token.Substring(1, token.Length - 2))); continue; }


                    // Handle nesting
                    switch (token)
                    {
                        case "(":
                            {
                                __CheckImpliedScalar();
                                if (inputs.Count > 0 && inputs.Last is NamedFunction prev)
                                    inputs.AddLast(_TokenizeAndParse(prev));
                                else
                                    inputs.AddLast(_TokenizeAndParse(new Parenthetical()));
                                continue;
                            }
                        case "[":
                            {
                                if (inputs.Count == 0) throw new NestingSyntaxException(__ComposeLexed(), token, "No base to index.");

                                // Create the indexing operator.
                                __EnlistAndEnqueue(new TokenIndex());

                                // Create the indexed clause.
                                Indexing brckt = new Indexing();
                                _TokenizeAndParse(brckt);
                                inputs.AddLast(brckt);
                                continue;
                            }
                        case "{":
                            {
                                inputs.AddLast(_TokenizeAndParse(new Curly()));
                                continue;
                            }
                        case ")":
                            {
                                IEvaluateable[] parsed = __Parse();
                                switch (exp)
                                {
                                    case NamedFunction nf:
                                        nf.Inputs = parsed;
                                        return nf;
                                    case Parenthetical p:
                                        if (parsed.Length != 1)
                                            throw new SyntaxException(__ComposeLexed(), token, "Invalid structure for a parenthetical clause.");
                                        p.Head = parsed[0];
                                        return p;
                                    default:
                                        throw new NestingSyntaxException(__ComposeLexed(), token, "Invalid closure ')' of parenthetical without matching '('.");
                                }
                            }
                        case "]":
                            {
                                if (exp is Indexing idxing)
                                {
                                    IEvaluateable[] parsed = __Parse();
                                    idxing.Inputs = new IEvaluateable[2];
                                    idxing.Base = null;  // This will be assigned at the operator parse.
                                    idxing.Ordinal = (parsed.Length == 1) ? parsed[0] : new Vector(parsed);
                                    return exp;
                                }
                                throw new NestingSyntaxException(__ComposeLexed(), token, "Invalid closure ']' of indexing bracket without matching '['.");
                            }
                        case "}":
                            {
                                if (exp is Curly curly)
                                {
                                    if (inputs.Count != 1)
                                        throw new NestingSyntaxException(__ComposeLexed(), token, "A curly-bracketed clause may contain only a single variable reference.");
                                    if (!(inputs.First is Reference r))
                                        throw new NestingSyntaxException(__ComposeLexed(), token, "A curly-bracketed clause may contain only a variable reference.");
                                    curly.Reference = r;
                                    return exp;
                                }
                                throw new NestingSyntaxException(__ComposeLexed(), token, "Invalid closure '}' of vector bracket without matching '{'.");
                            }
                    }

                    // An operator?
                    if (Token.FromString(token, splitIdx-1, out Token t))
                    {
                        __EnlistAndEnqueue(t);                        
                        continue;
                    }
                    
                    // A named function?
                    if (functions != null && functions.TryCreate(token, out NamedFunction f))
                    {
                        inputs.AddLast(f);
                        continue;
                    }

                    // A starting subcontext or a variable?
                    if (ctxt != null)
                    {
                        if (ctxt.TryGetSubcontext(token, out IContext next_ctxt)) { ctxt = next_ctxt; continue; }
                        else if (ctxt.TryGetVariable(token, out IVariable var)) { inputs.AddLast(new Reference(var)); ctxt = rootContext; continue; }
                        else if (ctxt is IRangeable ir && ir.TryGetImmobile(token, out Reference r)) { inputs.AddLast(r); ctxt = rootContext; continue; }
                    }
                    
                    bool __CheckImpliedScalar()
                    {
                        if (inputs.Count == 0) return false;
                        switch (inputs.Last)
                        {
                            case Number _:
                            case NamedFunction _:
                            case Parenthetical _: __EnlistAndEnqueue(new TokenStar()); return true;
                        }
                        return false;
                    }

                }

                __Parse();
                if (exp is Function function) { function.Inputs = inputs.ToArray(); }
                else if (inputs.Count == 1)
                {
                    if (exp is Parenthetical p) { p.Head = inputs.First; return p; }
                    else if (exp is Curly c && inputs.First is Reference r) { c.Reference = r; return c; }
                    return inputs.First;
                }
                throw new ParsingException(__ComposeLexed(), splits[splitIdx - 1], "Failed to parse " + exp.GetType().Name);

                void __EnlistAndEnqueue(Token token)
                {
                    heap.Enqueue(token);
                    token.Node = inputs.AddLast(token);
                }

                IEvaluateable[] __Parse()
                {
                    Token.Parse(heap);
                    return inputs.ToArray();                    
                }
            }


        }


        public static IEvaluateable FromLatex(string latex, IFunctionFactory function = null, IContext rootContent = null)
        {
            throw new NotImplementedException();
        }


        #region Parser Tokens parsing

        
        internal abstract class Token : IComparable<Token>, IEvaluateable
        {
            protected enum Priorities
            {
                Question = 50,
                Index = 70,
                And = 100,
                Or = 101,
                Xor = 102,
                Minus = 200,
                Negation = 201,
                NotEquals = 202,
                Range = 250,
                Exponentiation = 300,
                Multiplication = 400,
                Division = Multiplication,
                Addition = 500,
                Subtraction = Addition,                
                Ternary = 800,
                GreaterThan = 900,
                LessThan = 901,
                GreaterThanOrEquals = 902,
                LessThanOrEquals = 903,
                Equals = 1000,
                Semicolon = 5000,
                Comma = 10000
            }

            public const string BRACKET_STRING = "!bracket!";
            public static bool FromString (string str, int index, out Token token)
            {
                switch (str)
                {
                    case "-": token = new TokenMinus() { Index = index }; return true;
                    case "!": token = new TokenBang() { Index = index }; return true;
                    case "~": token = new TokenTilde() { Index = index }; return true;
                    case "+": token = new TokenPlus() { Index = index }; return true;
                    case "*": token = new TokenStar() { Index = index }; return true;
                    case "/": token = new TokenSlash() { Index = index }; return true;
                    case "^": token = new TokenHat() { Index = index }; return true;
                    case "&": token = new TokenAmpersand() { Index = index }; return true;
                    case "|": token = new TokenPipe() { Index = index }; return true;
                    case ":": token = new TokenColon() { Index = index }; return true;
                    case "$": token = new TokenDollar() { Index = index }; return true;
                    case "?": token = new TokenQuestion() { Index = index }; return true;
                    case ">": token = new TokenGreaterThan() { Index = index }; return true;
                    case "<": token = new TokenLessThan() { Index = index }; return true;
                    case "=": token = new TokenEquals() { Index = index }; return true;
                    default: token = null; return false;
                }
            }
            internal static void Parse (Heap<Token> prioritized)
            {
                while (prioritized.Count > 0)
                {
                    Token token = prioritized.Dequeue();
                    if (token.TryParse(out Token replacement)) continue;
                    replacement.Node = token.Node;
                    replacement.Node.Contents = replacement;
                    prioritized.Enqueue(replacement);
                }
            }
            /// <summary>The left-to-right index.</summary>
            protected internal int Index { get; internal set; }
            internal protected abstract bool TryParse(out Token replacement);
            protected abstract Priorities Priority { get; }
            
            internal DynamicLinkedList<IEvaluateable>.Node Node;            
            
            /// <summary>Parse tokens in the form of "5 / 7".</summary>
            protected bool ParseBinary<T>(out Token _) where T : Function, new()
            {
                Debug.Assert(Node != null);
                Debug.Assert(Node.List != null);
                if (Node.Previous == null || Node.Next == null) throw new ParsingException("", "", "Failed to parse " + typeof(T).Name);
                Function newFunc = new T { Inputs = new IEvaluateable[] { Node.Previous.Remove(), Node.Next.Remove() } };
                Node.Contents = newFunc;
                _ = null;
                return true;
            }

            protected bool ParseSeries<TResult, TToken> (out Token _) where TResult:Function, new() where TToken : Token
            {
                
                Deque<IEvaluateable> deque = new Deque<IEvaluateable>();
                while (true)
                {
                    if (Node.Previous == null) throw new ParsingException("", "", "Failed to parse " + typeof(TResult).Name);
                    deque.AddFirst(Node.Previous.Remove());
                    if (Node.Previous == null || !(Node.Previous.Contents is TToken)) break;
                    Node.Previous.Remove();
                }
                while (true)
                {
                    if (Node.Next == null) throw new ParsingException("", "", "Failed to parse " + typeof(TResult).Name);
                    deque.AddLast(Node.Next.Remove());
                    if (Node.Next == null || !(Node.Next.Contents is TToken)) break;
                    Node.Next.Remove();
                }
                Function newFunc = new TResult { Inputs = deque.ToArray() };
                Node.Contents = newFunc;
                _ = null;
                return true;
            }

            protected bool ParseNext<T>(out Token _) where T : Function, new()
            {
                Debug.Assert(Node != null);
                Debug.Assert(Node.List != null);
                if (Node.Next == null) throw new ParsingException("", "", "Failed to parse " + typeof(T).Name);
                Function newFunc = new T { Inputs = new IEvaluateable[] { Node.Next.Remove() } };
                Node.Contents = newFunc;
                _ = null;
                return true;
            }

            internal virtual int CompareTo(Token other)
            {
                int c = this.Priority.CompareTo(other.Priority);
                if (c == 0) c = this.Index.CompareTo(other.Index);
                return c;
            }
            int IComparable<Token>.CompareTo(Token other) => CompareTo(other);

            // The Token is made IEvaluateable only so it can be put into the DynamicLinkedList as a place holder.
            IEvaluateable IEvaluateable.Value => this;
            IEvaluateable IEvaluateable.UpdateValue() => this;

            public sealed override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                if (Node.Previous != null) sb.Append(Node.Previous.Contents.GetType().Name + "..");
                sb.Append(GetType().Name);
                if (Node.Next != null) sb.Append(".." + Node.Next.Contents.GetType().Name);
                return sb.ToString();
            }
        }


        internal class TokenAmpersand : Token
        {
            protected override Priorities Priority => Priorities.And;

            protected internal override bool TryParse(out Token _) => ParseSeries<And, TokenAmpersand>(out _);
        }

        internal class TokenBang : Token
        {
            protected override Priorities Priority => Priorities.Negation;

            protected internal override bool TryParse(out Token substituted)
            {
                if (Node.Next != null && Node.Next.Contents is TokenEquals)
                {
                    substituted = new TokenNotEquals() { Node = Node, Index = Index };
                    Node.Contents = substituted;
                    return false;
                }
                return ParseBinary<Negation>(out substituted);
            }
        }

        internal class TokenColon : Token
        {
            protected override Priorities Priority => Priorities.Range;

            protected internal override bool TryParse(out Token substituted)
            {
                Debug.Assert(Node.Previous == null || Node.Previous.Previous == null || !(Node.Previous.Previous.Contents is TokenQuestion),
                    "Question marks '?' should be parsed before colons ':'");
                Node.Contents = new Range() { Inputs = new IEvaluateable[] { Node.Previous.Remove(), Node.Next.Remove() } };
                substituted = null;
                return true;
            }
        }

        internal class TokenComma : Token
        {
            protected override Priorities Priority => Priorities.Comma;
            protected internal override bool TryParse(out Token _) => ParseSeries<Vector, TokenComma>(out _);
        }

        internal class TokenSemicolon : Token
        {
            protected override Priorities Priority => Priorities.Semicolon;
            protected internal override bool TryParse(out Token _) => ParseSeries<Vector, TokenSemicolon>(out _);
        }

        internal class TokenDollar : Token
        {
            protected override Priorities Priority => throw new NotImplementedException();

            protected internal override bool TryParse(out Token substituted) { throw new NotImplementedException(); }
        }
        
        internal class TokenEquals : Token
        {
            protected override Priorities Priority => Priorities.Equals;

            protected internal override bool TryParse(out Token substituted)
            {
                if (Node.Previous != null)
                {
                    if (Node.Previous.Contents is TokenLessThan)
                    {
                        Node.Previous.Remove();
                        substituted = new TokenLessThanOrEquals { Node = Node, Index = Index };
                        Node.Contents = substituted;
                        return false;
                    }
                    else if (Node.Previous.Contents is TokenGreaterThan)
                    {
                        Node.Previous.Remove();
                        substituted = new TokenGreaterThanOrEquals { Node = Node, Index = Index };
                        Node.Contents = substituted;
                        return false;
                    }                   
                }
                return ParseBinary<Equality>(out substituted);                
            }

        }

        internal class TokenGreaterThan : Token
        {
            protected override Priorities Priority => Priorities.GreaterThan;

            protected internal override bool TryParse(out Token substituted)
            {
                if (Node.Next != null && Node.Next.Contents is TokenEquals)
                {
                    Node.Next.Remove();
                    substituted = new TokenGreaterThanOrEquals();
                    Node.Contents = substituted;
                    return false;
                }
                return ParseBinary<GreaterThan>(out substituted);
            }
        }

        internal class TokenGreaterThanOrEquals : Token
        {
            protected override Priorities Priority => Priorities.GreaterThanOrEquals;

            protected internal override bool TryParse(out Token _) => ParseBinary<GreaterThanOrEquals>(out _);
        }
        
        internal class TokenHat : Token
        {
            protected override Priorities Priority => Priorities.Exponentiation;

            protected internal override bool TryParse(out Token _) => ParseBinary<Exponentiation>(out _);
        }

        internal class TokenNotEquals : Token
        {
            protected override Priorities Priority => Priorities.NotEquals;

            protected internal override bool TryParse(out Token _) => ParseBinary<NotEquality>(out _);
        }

        internal class TokenIndex : Token
        {
            protected override Priorities Priority => Priorities.Index;

            protected internal override bool TryParse(out Token _) => ParseBinary<Indexing>(out _);
        }
        
        internal class TokenLessThan : Token
        {
            protected override Priorities Priority => Priorities.LessThan;

            protected internal override bool TryParse(out Token substituted)
            {
                if (Node.Next != null && Node.Next.Contents is TokenEquals)
                {
                    Node.Next.Remove();
                    substituted = new TokenLessThanOrEquals();
                    Node.Contents = substituted;
                    return false;
                }
                return ParseBinary<LessThan>(out substituted);                
            }
        }

        internal class TokenLessThanOrEquals : Token
        {
            protected override Priorities Priority => Priorities.LessThanOrEquals;

            protected internal override bool TryParse(out Token _) => ParseBinary<LessThanOrEquals>(out _);
        }
        
        internal class TokenPipe : Token
        {
            protected override Priorities Priority => Priorities.Or;

            protected internal override bool TryParse(out Token _) => ParseBinary<Or>(out _);
        }
        
        internal class TokenPlus : Token
        {
            protected override Priorities Priority => Priorities.Addition;

            protected internal override bool TryParse(out Token _) => ParseBinary<Addition>(out _);
        }

        internal class TokenQuestion : Token
        {
            protected override Priorities Priority => Priorities.Question;

            protected internal override bool TryParse(out Token substituted)
            {
                if (Node.Next != null && Node.Next.Next != null && Node.Next.Next.Contents is TokenColon)
                {
                    Node.Next.Next.Remove();
                    substituted = new TokenTernary() { Node = Node, Index = Index };
                    Node.Contents = substituted;
                    return false;
                }
                Node.Contents = new Evaluation() { Inputs = new IEvaluateable[] { Node.Previous.Remove() } };
                substituted = null;
                return true;
            }
        }
        
        internal class TokenSlash : Token
        {
            protected override Priorities Priority => Priorities.Division;

            protected internal override bool TryParse(out Token _) => ParseBinary<Division>(out _);
        }

        internal class TokenStar : Token
        {
            protected override Priorities Priority => Priorities.Multiplication;

            protected internal override bool TryParse(out Token _) => ParseBinary<Multiplication>(out _);
        }

        internal class TokenSubtraction : Token
        {
            protected override Priorities Priority => Priorities.Subtraction;
            protected internal override bool TryParse(out Token _) => ParseBinary<Subtraction>(out _);
        }

        internal class TokenTernary : Token
        {
            protected override Priorities Priority => Priorities.Ternary;

            protected internal override bool TryParse(out Token substituted)
            {
                if (Node.Previous == null || Node.Next == null || Node.Next == null) throw new SyntaxException("","","Failure to parse Ternary.");
                Node.Contents = new Ternary() { Inputs = new IEvaluateable[] { Node.Previous.Remove(), Node.Next.Remove(), Node.Next.Remove() } };
                substituted = null;
                return true;
            }
        }
        
        internal class TokenTilde : Token
        {
            protected override Priorities Priority => Priorities.Negation;
            protected internal override bool TryParse(out Token _) => ParseNext<Negation>(out _);
        }
        
        internal class TokenMinus : Token
        {
            protected override Priorities Priority => Priorities.Minus;

            protected internal override bool TryParse(out Token substituted)
            {
                if (Node.Previous == null ||  Node.Previous.Contents is Token)
                    return ParseNext<Negation>(out substituted);
                else if (Node.Next != null && Node.Next.Contents is TokenMinus nextMinus)
                    return nextMinus.TryParse(out substituted);
                substituted = new TokenSubtraction();
                return false;
            }

            internal override int CompareTo(Token other)
            {
                // Minus is the one case where repeated instances should be read right-to-left.
                // For example, 7- -1, if it were read left
                return base.CompareTo(other);
            }
        }


        #endregion



        #region Parser RegEx members

        private const string OpenerPattern = @"(?<openerPattern>[\(\[{])";
        private const string CloserPattern = @"(?<closerPattern>[\)\]}])";
        private const string OperPattern = @"(?<operPattern>[+-/*&|^~!><=])";
        private const string RefPattern = @"(?<refPattern> \$? _? (?:_* \$? \w _* (?:_*\$? \d_*)*)*)";
        private const string NumPattern = @"(?<numPattern>(?:-)? (?: \d+\.\d* | \d*\.\d+ | \d+ ))";
        private const string SpacePattern = @"(?<spacePattern>\s+)";

        private static string _Pattern = string.Join(" | ", String.PARSE_PATTERN, OpenerPattern, CloserPattern, OperPattern, RefPattern, NumPattern, SpacePattern);
        private static Regex _Regex = new Regex(_Pattern, RegexOptions.IgnorePatternWhitespace);

        #endregion



        #region Parser Exceptions


        public class SyntaxException : Exception
        {
            public readonly string Parsed;
            public readonly string Failed;
            public SyntaxException(string parsed, string failedToken, string message, Exception inner = null) : base(message, inner) { this.Parsed = parsed; this.Failed = failedToken; }
        }

        public class ParsingException : SyntaxException
        {
            internal ParsingException(string parsed, string failedToken, string message) : base(parsed, failedToken, message) { }
        }

        public class ReferenceException : SyntaxException
        {
            internal ReferenceException(string parsed, string failedToken, string message) : base(parsed, failedToken, message) { }
        }

        internal class NestingSyntaxException : SyntaxException
        {   
            internal NestingSyntaxException(string parsed, string failedToken, string message) : base(parsed, failedToken, message) { }
        }

        #endregion



        #region Parser LR expressions

        internal sealed class Reference : IEvaluateable
        {
            internal IVariable Variable { get; set; }

            /// <summary>
            /// TODO:  implement an immobiles structure.
            /// </summary>
            public object Immobiles_TODO;

            private IEvaluateable _Value = null;
            IEvaluateable IEvaluateable.Value => _Value;
            IEvaluateable IEvaluateable.UpdateValue() => _Value = Variable.Value;

            public Reference(IVariable var, object immobiles = null) { this.Variable = var; this.Immobiles_TODO = immobiles; }

            public override string ToString() => Variable.Name;
        }

        /// <summary>
        /// A curly-bracket clause.  The only legal contents of a <see cref="Curly"/> would be a 
        /// <seealso cref="Reference"/>.  The value of a <see cref="Curly"/> is the contents of the 
        /// <seealso cref="IVariable"/> indicated by the <seealso cref="Reference"/>.
        /// </summary>
        internal sealed class Curly : IExpression
        {
            /// <summary>The vector that this bracket object contains.</summary>
            internal Reference Reference { get; set; }

            private IEvaluateable _Value = null;
            IEvaluateable IEvaluateable.Value => _Value;
            IEvaluateable IEvaluateable.UpdateValue() => _Value = Reference.Variable.Contents;

            public override string ToString() => '{' + Reference.ToString() + '}';

        }


        /// <summary>
        /// A parenthetical clause.  Any operation or literal is the valid <seealso cref="Parenthetical.Head"/> of a 
        /// <see cref="Parenthetical"/>.</summary>
        internal sealed class Parenthetical : IExpression
        {

            /// <summary>The head of the parsed evaluation tree.  If this is a function or operation, it is the last 
            /// operation performed in the tree.</summary>
            public IEvaluateable Head { get; internal set; }

            private IEvaluateable _Value = null;
            IEvaluateable IEvaluateable.Value => _Value;
            IEvaluateable IEvaluateable.UpdateValue() => _Value = Head.UpdateValue();


            public override string ToString() => "( " + Head.ToString() + " )";
        }




        #endregion
    }

}
