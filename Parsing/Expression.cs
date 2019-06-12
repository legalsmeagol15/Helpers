using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataStructures;
using System.Diagnostics;
using Dependency.Functions;
using Dependency.Operators;

namespace Dependency
{
    public sealed class Parse
    {
        /// <summary>Readable left-to-right.</summary>
        internal interface IExpression : IEvaluateable
        {

        }

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
            catch (Exception e)
            {
                // Any other exception represents a bug.
                throw new SyntaxException(_ComposeParsed(), splits[splitIdx], "Unexpected exception: " + e.GetType().Name, e);
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
                    if (__TryStartSubExpression(token)) continue;
                    if (__TryFinish(token)) return exp;

                    // Handle literals
                    if (Number.TryParse(token, out Number n)) { inputs.AddLast(n); continue; }
                    if (bool.TryParse(token, out bool b)) { inputs.AddLast(Dependency.Boolean.FromBool(b)); continue; }
                    if (String.TryUnquote(token, out string s)) { inputs.AddLast(new Dependency.String(s)); continue; }
                    
                    // An operator?
                    if (Token.FromString(token, splitIdx - 1, out Token t))
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

                    if (__TryCreateReference(token, out Reference r)) { inputs.AddLast(r); continue; }

                    throw new UnrecognizedTokenException(_ComposeParsed(), token, "The token '" + token + "' is not recognized.");
                }
                
                // We're out of tokens.
                __TryFinish("");
                Expression e = (Expression)exp;
                var fullParsed = __Parse();
                if (fullParsed.Length == 0) e.Result = Null.Instance;
                else if (fullParsed.Length == 1) e.Result = fullParsed[0];
                else e.Result = new Vector(fullParsed);
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
                        if (node.Contents is TokenComma tc) throw new ParsingException(tc, "Arguments must succeed and follow every comment.");
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

                bool __TryCreateReference(string token, out Reference result)
                {
                    // If there is no context, then of course there can be no reference.
                    if (rootContext == null) { result = null; return false; }

                    // If the token refers to a variable then we've already nailed down a reference.
                    if (rootContext.TryGetVariable(token, out IVariable var, out Mobility m)) { result = new Reference(var, m); return true; }

                    // Once a context-to-subcontext chain starts, any failure will be an exception.
                    if (!rootContext.TryGetSubcontext(token, out IContext ctxt)) { result = null; return false; }

                    // Maintain a list for purposes of error report.
                    List<IContext> traversed = new List<IContext>();

                    // Traverse through the contexts till we either find a variable, or run out of subcontexts.
                    bool dot = false;
                    while (splitIdx < splits.Length)
                    {
                        traversed.Add(ctxt);
                        token = splits[splitIdx];
                        if (ctxt.TryGetVariable(token, out var, out m)) { result = new Reference(var, m); return true; }
                        else if (token == ".")
                        {
                            if (dot) throw new ReferenceException(_ComposeParsed(), ".", "Only a single dot may separate edges in a reference path.");
                            dot = true;
                            splitIdx++;
                        }
                        else if (ctxt.TryGetSubcontext(token, out IContext sub_ctxt)) { ctxt = sub_ctxt; splitIdx++; dot = false; }
                        else throw new ReferenceException(_ComposeParsed(), token, "Invalid token '" + token + "' in reference hierarchy.");
                    }

                    throw new ReferenceException(_ComposeParsed(), "", "Incomplete reference: " + string.Join(".", traversed.Select(c => c is INamed cn ? cn.Name : ".")));
                }

                bool __TryStartSubExpression(string token)
                {
                    switch (token)
                    {
                        case "(":
                            if (exp is NamedFunction nf)
                                return true;
                            else
                                inputs.AddLast(_TokenizeAndParse(new Parenthetical()));
                            return true;
                        case "{": inputs.AddLast(_TokenizeAndParse(new Curly())); return true;
                        case "[": inputs.AddLast(_TokenizeAndParse(new Indexing())); return true;
                    }
                    return false;
                }

                bool __TryFinish(string token)
                {
                    switch (token)
                    {
                        case ")":
                            var parsed = __Parse();
                            if (exp is Parenthetical p) { p.Head = (parsed.Length != 1) ? new Vector(parsed) : parsed[0]; return true; }
                            else if (exp is NamedFunction nf) { nf.Inputs = (parsed.Length == 1 && parsed[0] is Vector v) ? v.Inputs : parsed; return true; }
                            throw new NestingSyntaxException(_ComposeParsed(), token, exp.GetType().Name + " cannot be closed by ')'.");
                        case "]":
                            if (exp is Indexing) return true;
                            throw new NestingSyntaxException(_ComposeParsed(), token, exp.GetType().Name + " cannot be closed by ']'.");
                        case "}":
                            if (exp is Curly) return true;
                            throw new NestingSyntaxException(_ComposeParsed(), token, exp.GetType().Name + " cannot be closed by '}'.");
                        case "":
                            if (exp is Expression) return true;
                            throw new NestingSyntaxException(_ComposeParsed(), token, "Only general " + typeof(Expression).Name + "s can be closed without a token.");
                    }
                    return false;
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
            protected internal enum Priorities
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
                Comma = 10000,
                Semicolon = 10000
            }
            
            /// <summary>The left-to-right index.</summary>
            protected internal int Index { get; internal set; }
            internal protected abstract bool TryParse(out Token replacement);
            protected internal abstract Priorities Priority { get; }
            public static bool FromString(string str, int index, out Token token)
            {
                switch (str)
                {
                    case "-": token = new TokenMinus() { Index = index }; return true;
                    case "!": token = new TokenBang() { Index = index }; return true;
                    case "~": token = new TokenTilde() { Index = index }; return true;
                    case "+": token = new TokenPlus() { Index = index }; return true;
                    case "*": token = new TokenStar() { Index = index}; return true;
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
            

            internal DynamicLinkedList<IEvaluateable>.Node Node;

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

                Deque<IEvaluateable> deque = new Deque<IEvaluateable>();
                while (true)
                {
                    if (Node.Previous == null) throw new ParsingException(this, "Failed to parse " + typeof(TResult).Name);
                    deque.AddFirst(Node.Previous.Remove());
                    if (Node.Previous == null || !(Node.Previous.Contents is TToken)) break;
                    Node.Previous.Remove();
                }
                while (true)
                {
                    if (Node.Next == null) throw new ParsingException(this, "Failed to parse " + typeof(TResult).Name);
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
                if (Node.Next == null) throw new ParsingException(this, "Failed to parse " + typeof(T).Name);
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


        internal class TokenAmpersand : Token
        {
            protected internal override Priorities Priority => Priorities.And;

            protected internal override bool TryParse(out Token _) => ParseSeries<And, TokenAmpersand>(out _);
        }

        internal class TokenBang : Token
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

        internal class TokenColon : Token
        {
            protected internal override Priorities Priority => Priorities.Range;

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
            protected internal override Priorities Priority => Priorities.Comma;
            protected internal override bool TryParse(out Token keepIt)
            {
                if (Node.Previous == null) throw new ParsingException(this,"A comma ',' must follow an expression.");
                if (Node.Next == null) throw new ParsingException(this, "A comma ',' must precede an expression.");                
                keepIt = this;
                return false;
            }
            public override string ToString() => ", ";
        }

        internal class TokenSemicolon : Token
        {
            protected internal override Priorities Priority => Priorities.Semicolon;
            protected internal override bool TryParse(out Token keepIt)
            {
                if (Node.Previous == null) throw new ParsingException(this, "A semicolon ';' must follow an expression.");
                if (Node.Next == null) throw new ParsingException(this, "A semicolon ';' must precede an expression.");
                keepIt = this;
                return false;
            }
        }

        internal class TokenDollar : Token
        {
            protected internal override Priorities Priority => throw new NotImplementedException();

            protected internal override bool TryParse(out Token substituted) { throw new NotImplementedException(); }
        }

        internal class TokenEquals : Token
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

        internal class TokenGreaterThan : Token
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

        internal class TokenGreaterThanOrEquals : Token
        {
            protected internal override Priorities Priority => Priorities.GreaterThanOrEquals;

            protected internal override bool TryParse(out Token _) => ParseBinary<GreaterThanOrEquals>(out _);
        }

        internal class TokenHat : Token
        {
            protected internal override Priorities Priority => Priorities.Exponentiation;

            protected internal override bool TryParse(out Token _) => ParseBinary<Exponentiation>(out _);
        }

        internal class TokenNotEquals : Token
        {
            protected internal override Priorities Priority => Priorities.NotEquals;

            protected internal override bool TryParse(out Token _) => ParseBinary<NotEquality>(out _);
        }

        internal class TokenIndex : Token
        {
            protected internal override Priorities Priority => Priorities.Index;

            protected internal override bool TryParse(out Token _) => ParseBinary<Indexing>(out _);
        }

        internal class TokenLessThan : Token
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

        internal class TokenLessThanOrEquals : Token
        {
            protected internal override Priorities Priority => Priorities.LessThanOrEquals;

            protected internal override bool TryParse(out Token _) => ParseBinary<LessThanOrEquals>(out _);
        }

        internal class TokenPipe : Token
        {
            protected internal override Priorities Priority => Priorities.Or;

            protected internal override bool TryParse(out Token _) => ParseBinary<Or>(out _);
        }

        internal class TokenPlus : Token
        {
            protected internal override Priorities Priority => Priorities.Addition;

            protected internal override bool TryParse(out Token _) => ParseBinary<Addition>(out _);
        }

        internal class TokenQuestion : Token
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

        internal class TokenSlash : Token
        {
            protected internal override Priorities Priority => Priorities.Division;

            protected internal override bool TryParse(out Token _) => ParseBinary<Division>(out _);
        }

        internal class TokenStar : Token
        {
            protected internal override Priorities Priority => Priorities.Multiplication;

            protected internal override bool TryParse(out Token _) => ParseBinary<Multiplication>(out _);
        }

        internal class TokenSubtraction : Token
        {
            protected internal override Priorities Priority => Priorities.Subtraction;
            protected internal override bool TryParse(out Token _) => ParseBinary<Subtraction>(out _);
        }

        internal class TokenTernary : Token
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

        internal class TokenTilde : Token
        {
            protected internal override Priorities Priority => Priorities.Negation;
            protected internal override bool TryParse(out Token _) => ParseNext<Negation>(out _);
        }

        internal class TokenMinus : Token
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
            public string Parsed { get; protected set; }
            public string Token { get; protected set; }
            public SyntaxException(string parsed, string failedToken, string message, Exception inner = null) : base(message, inner)
            {
                this.Parsed = parsed;
                this.Token = failedToken;
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

        public class ReferenceException : SyntaxException
        {
            internal ReferenceException(string parsed, string failedToken, string message) : base(parsed, failedToken, message) { }
        }

        public class UnrecognizedTokenException : SyntaxException
        {
            internal UnrecognizedTokenException(string parsed, string failedToken, string message) : base(parsed, failedToken, message) { }
        }
        
        #endregion



        #region Parser LR expressions

        internal struct Immobiles
        {

        }


        /// <summary>
        /// A curly-bracket clause.  The only legal contents of a <see cref="Curly"/> would be a 
        /// <seealso cref="Reference"/>.  The value of a <see cref="Curly"/> is the contents of the 
        /// <seealso cref="IVariable"/> indicated by the <seealso cref="Reference"/>.
        /// </summary>
        internal sealed class Curly : IExpression
        {
            /// <summary>The vector that this bracket object contains.</summary>
            internal Reference Reference { get; private set; }

            private IEvaluateable _Value = null;
            IEvaluateable IEvaluateable.Value => _Value;
            IEvaluateable IEvaluateable.UpdateValue() => _Value = Reference.Variable.Contents;

            public override string ToString() => '{' + Reference.ToString() + '}';
        }


        public sealed class Expression : IExpression
        {
            internal IEvaluateable Result { get; set; }
            IEvaluateable IEvaluateable.Value => Result.Value;
            
            IEvaluateable IEvaluateable.UpdateValue() => Result.UpdateValue();

            public override string ToString() => Result.ToString();
        }

        internal sealed class Reference : IEvaluateable
        {

            public IVariable Variable { get; internal set; }
            public Mobility Mobility { get; internal set; }
            
            private IEvaluateable _Value = null;
            IEvaluateable IEvaluateable.Value => _Value;
            IEvaluateable IEvaluateable.UpdateValue() => _Value = Variable.Value;
            
            public Reference() { }
            public Reference(IVariable head, Mobility immobiles) { Variable = head; Mobility = immobiles; }
            
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
