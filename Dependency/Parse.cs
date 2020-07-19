using DataStructures;
using Dependency.Functions;
using Dependency.Operators;
using Dependency.Variables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dependency
{
    public static class Parse
    {
        public static Expression FromString(string str, IFunctionFactory functions = null, IContext rootContext = null)
        {

            string[] splits = _Regex.Split(str);
            int splitIdx = 0;

            try
            {
                Expression e = new Expression();
                _TokenizeAndParse(e);
                return e;
            }
            catch (SyntaxException s)
            {
                // This is a normal exception occurring due to a bad input string.
                // Nobody else is allowed to throw a non-child SyntaxException.
                throw new SyntaxException(s.Parsed ?? _ComposeParsed(), s.Token, s.Message, s);
            }
            catch (Exception) // e)
            {
                // Any other exception represents a bug.
                //throw new SyntaxException(_ComposeParsed(), splits[splitIdx], "Unexpected exception: " + e.GetType().Name, e);
                throw;
            }

            string _ComposeParsed() => string.Join("", splits.Take(splitIdx));

            IEvaluateable _TokenizeAndParse(IExpression exp)
            {
                DynamicLinkedList<IEvaluateable> inputs = new DynamicLinkedList<IEvaluateable>();
                Heap<Token> heap = new Heap<Token>();
                while (splitIdx < splits.Length)
                {
                    string token = splits[splitIdx++];
                    if (string.IsNullOrWhiteSpace(token)) continue;

                    // See if a sub-expression can be started or finished.
                    Expression.ExpressionType exp_type;
                    if ((exp_type = __TryStartSubExpression(token)) != Expression.ExpressionType.NONE) continue;
                    if ((exp_type = __TryFinish(token)) != Expression.ExpressionType.NONE) return exp;

                    // Handle literals
                    if (Number.TryParse(token, out Number n)) { inputs.AddLast(n); continue; }
                    if (bool.TryParse(token, out bool b)) { inputs.AddLast(Dependency.Boolean.FromBool(b)); continue; }
                    if (String.TryUnquote(token, out string s)) { inputs.AddLast(new Dependency.String(s)); continue; }

                    // An operator?
                    if (__TryCreateToken(token, out Token t))
                    {
                        heap.Enqueue(t);
                        t.Node = inputs.AddLast(t);
                        continue;
                    }

                    // A named function?
                    if (functions != null && functions.TryCreate(token, out NamedFunction f))
                    {
                        inputs.AddLast(_TokenizeAndParse(f));
                        continue;
                    }

                    // At last, just add the token as a string.
                    if (rootContext != null && Regex.IsMatch(token, RefPattern, RegexOptions.IgnorePatternWhitespace))
                    {
                        TokenReference tr = new TokenReference(rootContext, token) { Index = splitIdx - 1 };
                        heap.Enqueue(tr);
                        tr.Node = inputs.AddLast(tr);
                        continue;
                    }

                    throw new UnrecognizedTokenException(_ComposeParsed(), token);
                }

                // We're out of tokens.  Parse, and stuff the result into the given expression.
                __TryFinish("");
                Expression e = (Expression)exp;
                var fullParsed = __Parse();
                if (fullParsed.Length == 0) e.Contents = Null.Instance;
                else if (fullParsed.Length == 1) e.Contents = fullParsed[0];
                else e.Contents = new Vector(fullParsed);
                return exp;

                IEvaluateable[] __Parse()
                {
                    while (heap.Count > 0)
                    {
                        Token token = heap.Dequeue();
                        if (token.TryParse(out Token replacement))
                            continue;
                        replacement.Node = token.Node;
                        replacement.Node.Contents = replacement;
                        if (token != replacement)
                            heap.Enqueue(replacement);
                    }

                    Debug.Assert(inputs.Count > 0);

                    // Look for implied multiplications and break as vectors.
                    List<TokenSemicolon> semiColons = new List<TokenSemicolon>();
                    var node = inputs.FirstNode;
                    List<List<IEvaluateable>> allLegs = new List<List<IEvaluateable>>();
                    List<IEvaluateable> thisLeg = new List<IEvaluateable>();
                    while (node != null)
                    {
                        if (node.Contents is TokenComma tc) throw new ParsingException(tc, "Arguments must succeed and follow every comma.");
                        if (node.Contents is TokenSemicolon ts0) throw new ParsingException(ts0, "Arguments must succeed and follow every semicolon.");

                        thisLeg.Add(node.Contents);
                        if (node == inputs.LastNode) break;

                        var after = node.Next;
                        if (after.Contents is TokenComma) { after.Remove(); node = node.Next; continue; }
                        else if (after.Contents is TokenSemicolon ts1) { after.Remove(); node = node.Next; allLegs.Add(thisLeg); thisLeg = new List<IEvaluateable>(); continue; }
                        else if (node.Contents is Dependency.String || after.Contents is Dependency.String) continue;
                        node.Contents = new ImpliedMultiplication() { Inputs = new IEvaluateable[] { node.Contents, after.Remove() } };
                    }
                    if (allLegs.Count == 0)
                        return thisLeg.ToArray();
                    allLegs.Add(thisLeg);
                    return allLegs.Select(leg => leg.Count == 1 ? leg[0] : new Vector(leg.ToArray())).ToArray();
                }

                bool __TryCreateToken(string tokenString, out Token resultToken)
                {
                    switch (tokenString)
                    {
                        case "-": resultToken = new TokenMinus() { Index = splitIdx - 1 }; return true;
                        case "!": resultToken = new TokenBang() { Index = splitIdx - 1 }; return true;
                        case "~": resultToken = new TokenTilde() { Index = splitIdx - 1 }; return true;
                        case "+": resultToken = new TokenPlus() { Index = splitIdx - 1 }; return true;
                        case "*": resultToken = new TokenStar() { Index = splitIdx - 1 }; return true;
                        case "/": resultToken = new TokenSlash() { Index = splitIdx - 1 }; return true;
                        case "^": resultToken = new TokenHat() { Index = splitIdx - 1 }; return true;
                        case "&": resultToken = new TokenAmpersand() { Index = splitIdx - 1 }; return true;
                        case "|": resultToken = new TokenPipe() { Index = splitIdx - 1 }; return true;
                        case ":": resultToken = new TokenColon() { Index = splitIdx - 1 }; return true;
                        case "$": resultToken = new TokenDollar() { Index = splitIdx - 1 }; return true;
                        case "?": resultToken = new TokenQuestion() { Index = splitIdx - 1 }; return true;
                        case ">": resultToken = new TokenGreaterThan() { Index = splitIdx - 1 }; return true;
                        case "<": resultToken = new TokenLessThan() { Index = splitIdx - 1 }; return true;
                        case "=": resultToken = new TokenEquals() { Index = splitIdx - 1 }; return true;
                        case ",": resultToken = new TokenComma() { Index = splitIdx - 1 }; return true;
                    }
                    resultToken = null;
                    return false;
                }

                Expression.ExpressionType __TryStartSubExpression(string token)
                {
                    switch (token)
                    {
                        case "(":
                            if (!(exp is NamedFunction)) inputs.AddLast(_TokenizeAndParse(new Parenthetical()));
                            return Expression.ExpressionType.PAREN;
                        case "{":
                            inputs.AddLast(_TokenizeAndParse(new Curly()));
                            return Expression.ExpressionType.CURLY;
                        case "[":
                            // Add the contents, then add a token for LR parsing
                            TokenIndexing tr = new TokenIndexing() { Index = splitIdx - 1 };
                            heap.Enqueue(tr);
                            tr.Node = inputs.AddLast(tr);
                            inputs.AddLast(_TokenizeAndParse(new Bracket()));
                            return Expression.ExpressionType.BRACKET;
                    }
                    return Expression.ExpressionType.NONE;
                }

                Expression.ExpressionType __TryFinish(string token)
                {
                    IEvaluateable[] parsed;
                    switch (token)
                    {
                        case ")":
                            parsed = __Parse();
                            if (exp is Parenthetical p) { p.Contents = (parsed.Length != 1) ? new Vector(parsed) : parsed[0]; return Expression.ExpressionType.PAREN; }
                            else if (exp is NamedFunction nf) { nf.Inputs = (parsed.Length == 1 && parsed[0] is Vector v) ? v.Inputs.ToArray() : parsed; return Expression.ExpressionType.PAREN; }
                            throw new NestingSyntaxException(_ComposeParsed(), token, exp.GetType().Name + " cannot be closed by ')'.");
                        case "]":
                            parsed = __Parse();
                            if (exp is Bracket b) { b.Contents = (parsed.Length != 1) ? new Vector(parsed) : parsed[0]; return Expression.ExpressionType.BRACKET; }
                            throw new NestingSyntaxException(_ComposeParsed(), token, exp.GetType().Name + " cannot be closed by ']'.");
                        case "}":
                            if (exp is Curly) return Expression.ExpressionType.CURLY;
                            throw new NestingSyntaxException(_ComposeParsed(), token, exp.GetType().Name + " cannot be closed by '}'.");
                        case "":
                            if (exp is Expression) return Expression.ExpressionType.NAKED;
                            throw new NestingSyntaxException(_ComposeParsed(), token, "Only general " + typeof(Expression).Name + "s can be closed without a token.");
                    }
                    return Expression.ExpressionType.NONE;
                }
            }


        }


        public static IEvaluateable FromLatex(string latex, IFunctionFactory function = null, IContext rootContent = null)
        {
            throw new NotImplementedException();
        }






        #region Parser priority and LR parsing (Tokens)


        internal abstract class Token : IComparable<Token>, IEvaluateable
        {
            // Implements IEvaluateable solely so it can lived on an inputs list temporarily.

            protected internal enum Priorities
            {
                Refer = 30,
                Index = 40,
                Question = 50,
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
                Comma = 10000,
                Semicolon = 10100
            }

            /// <summary>The left-to-right index.</summary>
            protected internal int Index { get; internal set; }
            protected internal abstract Priorities Priority { get; }
            internal DynamicLinkedList<IEvaluateable>.Node Node;
            internal protected abstract bool TryParse(out Token replacement);

            /// <summary>Parse tokens in the form of "5 / 7".</summary>
            protected bool ParseBinary<T>(out Token _) where T : Function, new()
            {
                Debug.Assert(Node != null);
                Debug.Assert(Node.List != null);
                if (Node.Previous == null || Node.Next == null) throw new ParsingException(this, "Failed to parse " + typeof(T).Name);
                Function newFunc = new T { Inputs = new IEvaluateable[] { Node.Previous.Remove(), Node.Next.Remove() } };
                Node.Contents = newFunc;
                _ = null;
                return true;
            }

            protected bool ParseSeries<TResult, TToken>(out Token _) where TResult : Function, new() where TToken : Token
            {
                Deque<IEvaluateable> deque = GatherSeries<TToken>();
                Function newFunc = new TResult { Inputs = deque.ToArray() };
                Node.Contents = newFunc;
                _ = null;
                return true;
            }

            protected Deque<IEvaluateable> GatherSeries<TToken>() where TToken : IEvaluateable
            {
                Deque<IEvaluateable> deque = new Deque<IEvaluateable>();
                while (true)
                {
                    if (Node.Previous == null) throw new ParsingException(this, "Failed to parse prior nodes in series.");
                    deque.AddFirst(Node.Previous.Remove());
                    if (Node.Previous == null || !(Node.Previous.Contents is TToken)) break;
                    Node.Previous.Remove();
                }
                while (true)
                {
                    if (Node.Next == null) throw new ParsingException(this, "Failed to parse following nodes in series.");
                    deque.AddLast(Node.Next.Remove());
                    if (Node.Next == null || !(Node.Next.Contents is TToken)) break;
                    Node.Next.Remove();
                }
                return deque;
            }

            protected bool ParseNext<T>(out Token _) where T : Function, new()
            {
                Debug.Assert(Node != null);
                Debug.Assert(Node.List != null);
                if (Node.Next == null) throw new ParsingException(this, "Failed to parse " + typeof(T).Name);
                Function newFunc = new T { Inputs = new IEvaluateable[] { Node.Next.Remove() } };
                Node.Contents = newFunc;
                _ = null;
                return true;
            }

            protected bool ParseVector()
            {
                Deque<IEvaluateable> deque = new Deque<IEvaluateable>();
                while (true)
                {
                    if (Node.Previous == null) throw new ParsingException(this, "Failed to parse " + typeof(Vector).Name);
                    deque.AddFirst(Node.Previous.Remove());
                    if (Node.Previous == null || !(Node.Previous.Contents is TokenComma)) break;
                    Node.Previous.Remove();
                }
                while (true)
                {
                    if (Node.Next == null) throw new ParsingException(this, "Failed to parse " + typeof(Vector).Name);
                    deque.AddLast(Node.Next.Remove());
                    if (Node.Next == null || !(Node.Next.Contents is TokenComma)) break;
                    Node.Next.Remove();
                }
                Node.Contents = new Vector { Inputs = deque.ToArray() };
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
            IEvaluateable IEvaluateable.Value
                => throw new InvalidOperationException("No value should be obtained from a "
                                                        + this.GetType().Name + ".  It is solely a temporary class.");

            public override string ToString()
            {
                // This is solely to help with debugging.
                StringBuilder sb = new StringBuilder();
                if (Node.Previous != null) sb.Append(Node.Previous.Contents.GetType().Name + ".");
                sb.Append(GetType().Name);
                if (Node.Next != null) sb.Append(".." + Node.Next.Contents.GetType().Name);
                return sb.ToString();
            }
        }


        internal sealed class TokenAmpersand : Token
        {
            protected internal override Priorities Priority => Priorities.And;

            protected internal override bool TryParse(out Token _) => ParseSeries<And, TokenAmpersand>(out _);
        }

        internal sealed class TokenBang : Token
        {
            protected internal override Priorities Priority => Priorities.Negation;

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

        internal sealed class TokenColon : Token
        {
            protected internal override Priorities Priority => Priorities.Range;

            protected internal override bool TryParse(out Token substituted)
            {
                //Debug.Assert(Node.Previous == null || Node.Previous.Previous == null || !(Node.Previous.Previous.Contents is TokenQuestion),
                //    "Question marks '?' should be parsed before colons ':'");
                //Node.Contents = new Range() { Inputs = new IEvaluateable[] { Node.Previous.Remove(), Node.Next.Remove() } };
                //substituted = null;
                //return true;
                throw new NotImplementedException();
            }
        }

        internal sealed class TokenComma : Token
        {
            protected internal override Priorities Priority => Priorities.Comma;
            protected internal override bool TryParse(out Token _) { _ = null; return ParseVector(); }
        }

        internal sealed class TokenDollar : Token
        {
            protected internal override Priorities Priority => throw new NotImplementedException();
            protected internal override bool TryParse(out Token substituted) { throw new NotImplementedException(); }
        }

        internal sealed class TokenEquals : Token
        {
            protected internal override Priorities Priority => Priorities.Equals;

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

        internal sealed class TokenGreaterThan : Token
        {
            protected internal override Priorities Priority => Priorities.GreaterThan;
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

        internal sealed class TokenGreaterThanOrEquals : Token
        {
            protected internal override Priorities Priority => Priorities.GreaterThanOrEquals;
            protected internal override bool TryParse(out Token _) => ParseBinary<GreaterThanOrEquals>(out _);
        }

        internal sealed class TokenHat : Token
        {
            protected internal override Priorities Priority => Priorities.Exponentiation;
            protected internal override bool TryParse(out Token _) => ParseBinary<Exponentiation>(out _);
        }

        internal sealed class TokenIndexing : Token
        {
            protected internal override Priorities Priority => Priorities.Index;
            protected internal override bool TryParse(out Token _)
            {
                Debug.Assert(Node != null);
                Debug.Assert(Node.List != null);
                if (Node.Previous == null || Node.Next == null) throw new ParsingException(this, "Failed to parse " + typeof(Indexing).Name);
                IEvaluateable @base = Node.Previous.Remove();
                IEvaluateable ordinal = Node.Next.Remove();
                if (ordinal is Bracket b) ordinal = b.Contents;
                else throw new ParsingException(this, "Second input for " + typeof(Indexing).Name + " must be a bracketed clause.");
                Indexing idxing = new Indexing(@base, ordinal);
                Node.Contents = idxing;
                _ = null;
                return true;
            }
        }

        internal sealed class TokenNotEquals : Token
        {
            protected internal override Priorities Priority => Priorities.NotEquals;
            protected internal override bool TryParse(out Token _) => ParseBinary<NotEquality>(out _);
        }

        internal sealed class TokenLessThan : Token
        {
            protected internal override Priorities Priority => Priorities.LessThan;

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

        internal sealed class TokenLessThanOrEquals : Token
        {
            protected internal override Priorities Priority => Priorities.LessThanOrEquals;

            protected internal override bool TryParse(out Token _) => ParseBinary<LessThanOrEquals>(out _);
        }

        internal sealed class TokenMinus : Token
        {
            protected internal override Priorities Priority => Priorities.Minus;

            protected internal override bool TryParse(out Token substituted)
            {
                if (Node.Previous == null || Node.Previous.Contents is Token)
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
                int c = Priority.CompareTo(other.Priority);
                if (c != 0) return c;
                return -Index.CompareTo(other.Index);
            }
        }

        internal sealed class TokenPipe : Token
        {
            protected internal override Priorities Priority => Priorities.Or;

            protected internal override bool TryParse(out Token _) => ParseBinary<Or>(out _);
        }

        internal sealed class TokenPlus : Token
        {
            protected internal override Priorities Priority => Priorities.Addition;

            protected internal override bool TryParse(out Token _) => ParseBinary<Addition>(out _);
        }

        internal sealed class TokenQuestion : Token
        {
            protected internal override Priorities Priority => Priorities.Question;

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

        internal sealed class TokenReference : Token
        {
            protected internal override Priorities Priority => Priorities.Refer;
            private readonly IContext Root;
            private readonly string Path;
            public TokenReference(IContext root, string path) { this.Root = root; this.Path = path; }
            protected internal override bool TryParse(out Token _)
            {
                Debug.Assert(Node != null);
                Debug.Assert(Node.List != null);

                Reference r;
                if (Node.Previous == null)
                {
                    if (!Reference.TryCreate(Root, Path, out r))
                        throw new SyntaxException("", Path, "Failed to parse reference \"" + Path + "\" from origin.");
                }
                else
                {
                    if (!Reference.TryCreate(Node.Previous.Remove(), Path, out r))
                        throw new SyntaxException("", Path, "Failed to parse reference \"" + Path + "\".");
                }

                Node.Contents = r;
                _ = null;
                return true;
            }
        }

        internal sealed class TokenSemicolon : Token
        {
            protected internal override Priorities Priority => Priorities.Semicolon;
            protected internal override bool TryParse(out Token _) { _ = null; return ParseVector(); }
        }

        internal sealed class TokenSlash : Token
        {
            protected internal override Priorities Priority => Priorities.Division;
            protected internal override bool TryParse(out Token _) => ParseBinary<Division>(out _);
        }

        internal sealed class TokenStar : Token
        {
            protected internal override Priorities Priority => Priorities.Multiplication;

            protected internal override bool TryParse(out Token _) => ParseBinary<Multiplication>(out _);
        }

        internal sealed class TokenSubtraction : Token
        {
            protected internal override Priorities Priority => Priorities.Subtraction;
            protected internal override bool TryParse(out Token _) => ParseBinary<Subtraction>(out _);
        }

        internal sealed class TokenTernary : Token
        {
            protected internal override Priorities Priority => Priorities.Ternary;

            protected internal override bool TryParse(out Token substituted)
            {
                if (Node.Previous == null || Node.Next == null || Node.Next == null) throw new SyntaxException("", "", "Failure to parse Ternary.");
                Node.Contents = new Ternary() { Inputs = new IEvaluateable[] { Node.Previous.Remove(), Node.Next.Remove(), Node.Next.Remove() } };
                substituted = null;
                return true;
            }
        }

        internal sealed class TokenTilde : Token
        {
            protected internal override Priorities Priority => Priorities.Negation;
            protected internal override bool TryParse(out Token _) => ParseNext<Negation>(out _);
        }

        #endregion



        #region Parser RegEx members

        private const string OpenerPattern = @"(?<openerPattern>[\(\[{])";
        private const string CloserPattern = @"(?<closerPattern>[\)\]}])";
        private const string OperPattern = @"(?<operPattern>[+-/*&|^~!><=])";
        private const string NumPattern = @"(?<numPattern>(?:-)? (?: \d+\.\d* | \d*\.\d+ | \d+ ))";
        private const string SpacePattern = @"(?<spacePattern>\s+)";
        //private const string RefPattern = @"(?<referencePattern>\.)";
        private const string RefPattern = @"(?<referencePattern>\.?[_a-zA-Z]\w*(?:\.[_a-zA-Z]\w*)*)";

        private static string _Pattern = string.Join(" | ", String.PARSE_PATTERN, OpenerPattern, CloserPattern, OperPattern, NumPattern, SpacePattern, RefPattern);
        private static Regex _Regex = new Regex(_Pattern, RegexOptions.IgnorePatternWhitespace);

        #endregion



        #region Parser Exceptions

        public class SyntaxException : Exception
        {
            public readonly string Parsed;
            public readonly string Token;
            public SyntaxException(string parsed, string failedToken, string message, Exception inner = null) : base(message, inner)
            {
                this.Parsed = parsed;
                this.Token = failedToken;
            }
            public SyntaxException(string message, Exception inner = null) : base(message, inner)
            {
                if (inner is SyntaxException se)
                {
                    Parsed = se.Parsed;
                    Token = se.Token;
                }
            }
        }

        public class EmptySyntaxException : SyntaxException
        {
            public EmptySyntaxException(string parsed, string message = "Empty contents.") : base(parsed, "", message) { }
        }

        internal class NestingSyntaxException : SyntaxException
        {
            internal NestingSyntaxException(string parsed, string failedToken, string message) : base(parsed, failedToken, message) { }
        }

        public class ParsingException : SyntaxException
        {
            internal readonly Token Complainant;
            internal ParsingException(Token complainant, string message) : base("", "", message)
            {
                this.Complainant = complainant;
            }
        }

        internal class ReferenceException : SyntaxException
        {
            public readonly Reference Incomplete;
            internal ReferenceException(Reference incomplete, string message) : base(message) { this.Incomplete = incomplete; }
        }

        internal class UnrecognizedTokenException : SyntaxException
        {
            internal UnrecognizedTokenException(string parsed, string failedToken, string message = null)
                : base(parsed, failedToken, message ?? "The token " + failedToken + " is not recognized.") { }
        }

        #endregion



        #region Parser LR expressions

        /// <summary>
        /// Used to build a LR clause, but this will not actually be stored in the dependency tree (an 
        /// <seealso cref="Indexing"/> will take its place.
        /// </summary>
        internal sealed class Bracket : IExpression, IEvaluateable
        {
            // IEvaluateable is implemented solely to make this class fit on an IEnumerable<IEvaluateable>
            public IEvaluateable Contents { get; internal set; }
            IEvaluateable IEvaluateable.Value => throw new NotImplementedException();
        }

        /// <summary>
        /// A curly-bracket clause.  The only legal contents of a <see cref="Curly"/> would be a 
        /// <seealso cref="Contents"/>.  The value of a <see cref="Curly"/> is the contents of the 
        /// <seealso cref="IAsyncUpdater"/> indicated by the <seealso cref="Contents"/>.
        /// </summary>
        internal sealed class Curly : IExpression, IContext, ISyncUpdater
        {
            /// <summary>The vector that this bracket object contains.</summary>
            public Vector Contents { get; }
            IEvaluateable IExpression.Contents => this.Contents;

            IEvaluateable IEvaluateable.Value => Contents;

            ISyncUpdater ISyncUpdater.Parent { get; set; }

            public override string ToString() => '{' + Contents.ToString() + '}';

            bool IContext.TryGetSubcontext(string path, out IContext ctxt) => ((IContext)Contents).TryGetSubcontext(path, out ctxt);

            bool IContext.TryGetProperty(string path, out IEvaluateable source) => Contents.TryGetProperty(path, out source);

            ICollection<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild, ICollection<IEvaluateable> updatedDomain)
                => updatedDomain;
        }


        /// <summary>
        /// A parenthetical clause.  Any operation or literal is the valid <seealso cref="Parenthetical.Contents"/> of a 
        /// <see cref="Parenthetical"/>.</summary>
        internal sealed class Parenthetical : IExpression, ISyncUpdater
        {
            public ISyncUpdater Parent { get; set; }
            /// <summary>The head of the parsed evaluation tree.  If this is a function or operation, it is the last 
            /// operation performed in the tree.</summary>
            public IEvaluateable Contents { get => _Contents; internal set { _Contents = value; if (_Contents is ISyncUpdater ide) ide.Parent = this; } }
            private IEvaluateable _Contents;

            IEvaluateable IEvaluateable.Value => _Contents.Value;

            ISyncUpdater ISyncUpdater.Parent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public override string ToString() => "( " + Contents.ToString() + " )";

            ICollection<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild, ICollection<IEvaluateable> updatedDomain)
                => updatedDomain;
        }




        #endregion
    }
}
