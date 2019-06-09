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
    /// <summary>Readable left-to-right.</summary>
    public interface IExpression : IEvaluateable
    {
        // Not sure if this is useful.  Just in case I end up adding members, I'm keeping this for now.
    }

    public sealed class Parse
    {
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

            IExpression _TokenizeAndParse(IExpression exp)
            {
                DynamicLinkedList<IEvaluateable> inputs = new DynamicLinkedList<IEvaluateable>();
                Heap<Token> operatorNodes = new Heap<Token>();
                bool dot = false;
                while (splitIdx < splits.Length)
                {
                    string token = splits[splitIdx];
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

                    if (dot) throw new Exception("This should be impossible.");

                    // Handle literals
                    else if (Number.TryParse(token, out Number n)) { inputs.AddLast(n); continue; }

                    else if (bool.TryParse(token, out bool b)) { inputs.AddLast(Dependency.Boolean.FromBool(b)); continue; }

                    else if (String.IsQuotedString(token)) { inputs.AddLast(new Dependency.String(token.Substring(1, token.Length - 2))); continue; }


                    // Handle nesting
                    switch (token)
                    {
                        case "(":
                            __CheckImpliedScalar();
                            if (inputs.Count > 0 && inputs.Last is NamedFunction prev)
                                inputs.AddLast(_TokenizeAndParse(prev));
                            else
                                inputs.AddLast(_TokenizeAndParse(new Parenthetical()));
                            continue;
                        case "[":
                            if (inputs.Count == 0) throw new NestingSyntaxException(__ComposeLexed(), token, "No base to index.");
                            
                            // Create the indexing operator.
                            Token implied = new TokenIndex();
                            operatorNodes.Enqueue(implied);
                            implied.Node = inputs.AddLast(implied);

                            // Create the indexed clause.
                            Indexing brckt = new Indexing();
                            _TokenizeAndParse(brckt);
                            inputs.AddLast(brckt);
                            continue;
                        case "{":
                            inputs.AddLast(_TokenizeAndParse(new Curly()));
                            continue;
                        case ")":
                            IEvaluateable[] pushedDown = __Parse(operatorNodes, inputs);
                            switch (exp)
                            {
                                case NamedFunction nf:
                                    nf.Inputs = pushedDown;
                                    return nf;
                                case Parenthetical p:
                                    if (pushedDown.Length != 1)
                                        throw new SyntaxException(__ComposeLexed(), token, "Invalid structure for a parenthetical clause.");
                                    p.Head = pushedDown[0];
                                    return p;
                                default:
                                    throw new NestingSyntaxException(__ComposeLexed(), token, "Invalid closure ')' of parenthetical without matching '('.");
                            }
                        case "]":
                            if (exp is Indexing idxing)
                            {
                                pushedDown = __Parse(operatorNodes, inputs);
                                idxing.Inputs = new IEvaluateable[2];
                                idxing.Base = null;  // This will be assigned at the operator parse.
                                idxing.Ordinal = (pushedDown.Length == 1) ? pushedDown[0] : new Vector(pushedDown);
                                return exp;
                            }
                            throw new NestingSyntaxException(__ComposeLexed(), token, "Invalid closure ']' of indexing bracket without matching '['.");
                        case "}":
                            if (exp is Curly curly)
                            {
                                if (inputs.Count != 1)
                                    throw new NestingSyntaxException(__ComposeLexed(), token, "A curly-bracketed clause may contain only a single variable reference.");
                                if (!(inputs.First is Reference r))
                                    throw new NestingSyntaxException(__ComposeLexed(), token, "A curly-bracketed clause may contain only a variable reference.");
                                curly.Reference = r;
                                return exp;
                            }
                            throw new NestingSyntaxException( __ComposeLexed(), token, "Invalid closure '}' of vector bracket without matching '{'.");
                    }

                    // An operator?
                    if (Token.FromString(token, out Token t))
                    {
                        operatorNodes.Enqueue(t);
                        inputs.AddLast(t.Node);
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

                    splitIdx++;
                    
                    bool __CheckImpliedScalar()
                    {
                        if (inputs.Count == 0) return false;
                        switch (inputs.Last)
                        {
                            case Number _:
                            case NamedFunction _:
                            case Parenthetical _:
                                Token impliedMult = Token.FromString("*");
                                operatorNodes.Enqueue(impliedMult);
                                inputs.AddLast(impliedMult.Node);
                                return true;
                        }
                        return false;
                    }

                }

                return exp;

                IEvaluateable[] __Parse(Heap<Token> heap, DynamicLinkedList<IEvaluateable> list)
                {
                    Token.Parse(heap);
                    return list.ToArray();                    
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
                Negation = 200,
                NotEquals = 201,
                Range = 250,
                Exponentiation = 300,
                Addition = 500,
                Subtraction = 501,
                Multiplication = 700,
                Division = 701,
                Ternary = 800,
                GreaterThan = 900,
                LessThan = 901,
                GreaterThanOrEquals = 902,
                LessThanOrEquals = 903,
                Equals = 1000
            }

            public const string BRACKET_STRING = "!bracket!";
            public static bool FromString (string str, out Token token)
            {
                switch (str)
                {
                    case "-": token = new TokenMinus(); return true;
                    case "!": token = new TokenBang(); return true;
                    case "~": token = new TokenTilde(); return true;
                    case "+": token = new TokenPlus(); return true;
                    case "*": token = new TokenStar(); return true;
                    case "/": token = new TokenSlash(); return true;
                    case "^": token = new TokenHat();return true;
                    case "&": token = new TokenAmpersand(); return true;
                    case "|": token = new TokenPipe(); return true;
                    case ":": token = new TokenColon(); return true;
                    case "$": token = new TokenDollar(); return true;
                    case "?": token = new TokenQuestion(); return true;
                    case ">": token = new TokenGreaterThan(); return true;
                    case "<": token = new TokenLessThan(); return true;
                    case "=": token = new TokenEquals(); return true;
                    default: token = null; return false;
                }
            }
            internal static void Parse (Heap<Token> prioritized)
            {
                while (prioritized.Count > 0)
                {   
                    Token token = prioritized.Dequeue();
                    if (!token.TryParse(out Token substitute)) { prioritized.Enqueue(substitute);continue; }
                }
            }
            internal protected abstract bool TryParse(out Token substituted);
            protected abstract Priorities Priority { get; }
            
            internal DynamicLinkedList<IEvaluateable>.Node Node;            
            
            /// <summary>Parse tokens in the form of "5 / 7".</summary>
            protected bool ParseBinary<T>(out Token _) where T : Operator, new()
            {
                Debug.Assert(Node != null);
                Debug.Assert(Node.List != null);
                if (Node.Previous == null || Node.Next == null) throw new ParsingException("", "", "Failed to parse " + typeof(T).Name);
                Operator newOperator = new T { Inputs = new IEvaluateable[] { Node.Previous.Remove(), Node.Next.Remove() } };
                Node.Contents = newOperator;
                _ = null;
                return true;
            }

            protected bool ParsePrevious<T>(out Token _) where T : Operator, new()
            {
                Debug.Assert(Node != null);
                Debug.Assert(Node.List != null);
                if (Node.Previous == null) throw new ParsingException("", "", "Failed to parse " + typeof(T).Name);
                Operator newOper = new T { Inputs = new IEvaluateable[] { Node.Previous.Remove() } };
                Node.Contents = newOper;
                _ = null;
                return true;
            }

            int IComparable<Token>.CompareTo(Token other) => Priority.CompareTo(other.Priority);

            // The Token is made IEvaluateable only so it can be put into the DynamicLinkedList as a place holder.
            IEvaluateable IEvaluateable.Value => this;
            IEvaluateable IEvaluateable.UpdateValue() => this;
        }


        internal class TokenAmpersand : Token
        {
            protected override Priorities Priority => Priorities.And;

            protected internal override bool TryParse(out Token _) => ParseBinary<And>(out _);
        }

        internal class TokenBang : Token
        {
            protected override Priorities Priority => Priorities.Negation;

            protected internal override bool TryParse(out Token substituted)
            {
                if (Node.Next != null && Node.Next.Contents is TokenEquals)
                {
                    substituted = new TokenNotEquals() { Node = Node };
                    Node.Contents = substituted;
                    return false;

                }
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
                        substituted = new TokenLessThanOrEquals { Node = Node };
                        Node.Contents = substituted;
                        return false;
                    }
                    else if (Node.Previous.Contents is TokenGreaterThan)
                    {
                        Node.Previous.Remove();
                        substituted = new TokenGreaterThanOrEquals { Node = Node };
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

            protected internal override bool TryParse(out Token _) => ParseBinary<NotEquals>(out _);
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
                    substituted = new TokenTernary() { Node = Node };
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

        internal class TokenTernary : Token
        {
            protected override Priorities Priority => Priorities.Ternary;

            protected internal override bool TryParse(out Token substituted)
            {
                if (Node.Previous == null || Node.Next == null || Node.Next == null) throw new SyntaxException("","","Failure to parse Ternary.");
                Debug.Assert(!(Node.Previous.Contents is Operator));
                Debug.Assert(!(Node.Next.Contents is Operator));
                Debug.Assert(!(Node.Next.Next.Contents is Operator));
                Node.Contents = new Ternary(Inputs = new IEvaluateable[] { Node.Previous.Remove(), Node.Next.Remove(), Node.Next.Remove() });
                substituted = null;
                return true;
            }
        }
        
        internal class TokenTilde : Token
        {
            protected override Priorities Priority => Priorities.Negation;

            protected internal override bool TryParse(out Token substituted) => ParsePrevious<Negation>(out _);
        }
        
        internal class TokenMinus : Token
        {
            protected override Priorities Priority => throw new NotImplementedException();

            protected internal override bool TryParse(out Token substituted)
            {
                if (Node.Previous == null || Node.Previous.Contents is Operator)
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

        internal sealed class Reference : IExpression
        {
            internal IVariable Variable { get; set; }

            /// <summary>
            /// TODO:  implement an immobiles structure.
            /// </summary>
            public object Immobiles_TODO;

            private IEvaluateable _Value = null;
            IEvaluateable IEvaluateable.Value => _Value ?? (_Value = Variable.Value);
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
            IEvaluateable IEvaluateable.Value => _Value ?? (_Value = Reference.Variable.Contents);
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
            IEvaluateable IEvaluateable.Value => _Value ?? (_Value = Head.UpdateValue());
            IEvaluateable IEvaluateable.UpdateValue() => _Value = Head.UpdateValue();


            public override string ToString() => '(' + Head.ToString() + ')';
        }




        #endregion
    }

}
