using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataStructures;

namespace Dependency
{
    /// <summary>Readable left-to-right.</summary>
    public interface IExpression : IEvaluateable
    {
        IEvaluateable[] Contents { get; }
    }
    
    


    public sealed class Clause : IExpression
    {
        private IEvaluateable _Value = null;
        internal readonly char Closer;

        public IEvaluateable[] Contents { get; internal set; }


        public IEvaluateable Value => _Value ?? (_Value = Contents[0].Value);

        private Clause (bool isParen, char closer = ')') { this.Closer = closer; }
        
        
        public static Clause FromString(string str, IFunctionFactory functions = null, IContext rootContext = null)
        {
            
            string[] splits = _Regex.Split(str);
            int splitIdx = 0;
            IContext ctxt = rootContext;            

            try
            {                
                Clause e = new Clause(false);  // Start parsing an open Clause.
                _Parse(e);
                return (e.Contents[0] is Clause eSub) ? eSub : e;
            }catch (NestingSyntaxException nme)
            {
                Array.Resize(ref splits, splitIdx+1);
                string msg = string.Join("", splits);
                throw new SyntaxException("Nesting mismatch.", msg, nme);
            }
            
            

            IExpression _Parse(IExpression exp)
            {
                DynamicLinkedList<IEvaluateable> inputs = new DynamicLinkedList<IEvaluateable>();
                Heap<PrioritizedToken> operatorNodes 
                    = new Heap<PrioritizedToken>((a, b) => a.Priority.CompareTo(b.Priority));
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
                            if (dot) throw new ReferenceException("Bad reference path.");
                            dot = true;
                            continue;
                        }
                        else if (ctxt.TryGetSubcontext(token, out ctxt)) continue;
                        else if (ctxt.TryGetVariable(token, out IVariable var))
                        {
                            inputs.AddLast(var);
                            ctxt = rootContext;
                        }
                        dot = false;
                        continue;
                    }

                    // Handle literals
                    else if (Number.TryParse(token, out Number n)) { inputs.AddLast(n); continue; }                    

                    else if (bool.TryParse(token, out bool b)) { inputs.AddLast(Dependency.Boolean.FromBool(b)); continue; }

                    else if (String.IsQuotedString(token)) { inputs.AddLast(new Dependency.String(token.Substring(1, token.Length - 2))); continue; }


                    // Handle nesting
                    switch (token)
                    {
                        case "(":
                            __ImpliedScalar();
                            if (inputs.Count==0 || !(inputs.Last is NamedFunction))
                                inputs.AddLast(_Parse(new Clause(true)));
                            else
                                throw new NotImplementedException
                            continue;
                        case "{":
                            if (inputs.Count == 0) throw new NestingSyntaxException("No base to index.");
                            Indexing idxing = new Indexing();
                            operatorNodes.Enqueue(new PrioritizedToken(inputs.AddLast(idxing)));
                            inputs.AddLast(_Parse(new Clause(false, '}')));
                            continue;
                        case "[":
                            inputs.AddLast(_Parse(new Clause(false, ']')));
                            throw new NotImplementedException();
                        //continue;
                        case ")":

                            if (clause.Closer != token[0]) throw new NestingSyntaxException("Clause closer '" + token[0] + "' does not match expected: '" + clause.Closer + "'. ");
                            clause.Contents = __Pushdown();
                            if (clause.Contents.Length != 1) throw new NestingSyntaxException("Parenthetical clause cannot contain a vector.");
                            return clause;
                        case "}":
                        case "]":
                            if (clause.Closer != token[0]) throw new NestingSyntaxException("Clause closer '" + token[0] + "' does not match expected: '" + clause.Closer + "'. ");
                            clause.Contents = __Pushdown();
                            return clause;
                    }

                    // An operator?
                    if (Operator.TryCreate(token, out Operator oper))
                    {
                        operatorNodes.Enqueue(new PrioritizedToken(inputs.AddLast(oper)));
                        inputs.AddLast(oper);
                        continue;
                    }

                    if (functions.TryCreate(token, out NamedFunction f))
                    {
                        inputs.AddLast(f);
                    }

                    splitIdx++;

                    bool __ImpliedScalar()
                    {
                        if (inputs.Count == 0) return false;
                        switch (inputs.Last) 
                        {
                            case Number _:
                            case NamedFunction _:
                            case Clause _:
                                Multiplication m = new Multiplication();
                                operatorNodes.Enqueue(new PrioritizedToken(m));
                                inputs.AddLast(m);
                                return true;
                        }
                        return false;
                    }
                    
                }

                return clause;

                IEvaluateable[] __Pushdown()
                {
                    while (operatorNodes.Count > 0)
                    {
                        var prioritized = operatorNodes.Dequeue();
                        Operator oper = (Operator)prioritized.Node.Contents;
                        if (!oper.Parse(prioritized.Node))
                            throw new OperatorException();
                    }
                    return inputs.ToArray();
                }
            }

            
            
        }


        private string Opener()
        {
            switch (Closer)
            {
                case ')': return "(";
                case ']': return "[";
                case '}': return "{";
                default: return "";
            }
        }
        public override string ToString() => Opener() + string.Join(",", (IEnumerable<IEvaluateable>)Contents) + Closer;




        private struct PrioritizedToken 
        {
            private static Dictionary<Type, int> _Priorities = new Dictionary<Type, int> {
                { typeof(Or), 200 },
                { typeof(And), 300 },
                { typeof(Exponentiation), 400 },
                { typeof(Negation), 500 },
                { typeof(Multiplication), 600 },
                { typeof(Division), 700 },
                { typeof(Addition), 800 },
                { typeof(Subtraction), 900 },
                { typeof(Range), 1000 },
                { typeof(Indexing), 1100 }
            };

            public readonly DynamicLinkedList<IEvaluateable>.Node Node;
            public readonly int Priority;
            public PrioritizedToken(DynamicLinkedList<IEvaluateable>.Node node)
            {
                this.Node = node;
                if (!_Priorities.TryGetValue( node.Contents.GetType(), out int priority)) throw new NotImplementedException();
                this.Priority = priority;
            }
        }



        #region Expression RegEx members
        private const string OpenerPattern = @"(?<openerPattern>[\(\[{])";
        private const string CloserPattern = @"(?<closerPattern>[\)\]}])";
        private const string OperPattern = @"(?<operPattern>[+-/*&|^~!])";
        private const string VarPattern = @"(?<varPattern> \$? [a-zA-Z_][\w_]* (?:\.[a-zA-Z_][\w_]*)*)";
        private const string NumPattern = @"(?<numPattern>(?:-)? (?: \d+\.\d* | \d*\.\d+ | \d+ ))";
        private const string SpacePattern = @"(?<spacePattern>\s+)";

        private static string _Pattern = string.Join(" | ", String.PARSE_PATTERN, OpenerPattern, CloserPattern, OperPattern, VarPattern, NumPattern, SpacePattern);
        private static Regex _Regex = new Regex(_Pattern, RegexOptions.IgnorePatternWhitespace);

        public class SyntaxException : Exception
        {
            public readonly string Parsed;
            public SyntaxException(string message, string parsed, Exception inner = null) : base(message, inner) { this.Parsed = parsed; }
        }

        public class OperatorException: Exception 
        {
            
        }

        public class ReferenceException : Exception
        {
            public ReferenceException(string message) : base(message) { }
        }

        internal class NestingSyntaxException : Exception
        {
            public NestingSyntaxException() { }
            public NestingSyntaxException(string message) : base(message)            {            }            
        }
        #endregion
    }
}
