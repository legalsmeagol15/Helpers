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


    /// <summary>
    /// A square-bracket clause.  The only legal contents of a <see cref="Bracket"/> would be a 
    /// <seealso cref="Vector"/>.
    /// </summary>
    public sealed class Bracket : IExpression
    {
        /// <summary>The vector that this bracket object contains.</summary>
        internal IIndexable Vector { get; set; }

        private IEvaluateable _Value = null;
        IEvaluateable IEvaluateable.Value => _Value ?? (_Value = Vector.UpdateValue());
        IEvaluateable IEvaluateable.UpdateValue() => _Value = Vector.UpdateValue();        

        public override string ToString() => '[' + Vector.ToString() + ']';

    }


    /// <summary>
    /// A parenthetical clause.  Any operation or literal is the valid <seealso cref="Parenthetical.Head"/> of a 
    /// <see cref="Parenthetical"/>.</summary>
    public sealed class Parenthetical : IExpression
    {

        /// <summary>The head of the parsed evaluation tree.  If this is a function or operation, it is the last 
        /// operation performed in the tree.</summary>
        public IEvaluateable Head { get; private set; }

        private IEvaluateable _Value = null;
        IEvaluateable IEvaluateable.Value => _Value ?? (_Value = Head.UpdateValue());
        IEvaluateable IEvaluateable.UpdateValue() => _Value = Head.UpdateValue();


        public static IEvaluateable FromString(string str, IFunctionFactory functions = null, IContext rootContext = null)
        {

            string[] splits = _Regex.Split(str);
            int splitIdx = 0;
            IContext ctxt = rootContext;

            try
            {
                Parenthetical p = new Parenthetical();
                _Parse(p);
                return p.Head;
            }
            catch (NestingSyntaxException nme)
            {
                Array.Resize(ref splits, splitIdx + 1);
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
                        else if (!dot)
                            throw new ReferenceException("References path items must be separated by dot '.' operators.");
                        else if (ctxt.TryGetSubcontext(token, out IContext next_ctxt))
                            ctxt = next_ctxt;
                        else if (ctxt.TryGetVariable(token, out IVariable var))
                        {
                            inputs.AddLast(var);
                            ctxt = rootContext;
                        }
                        else
                            throw new ReferenceException("Reference path termination must be an evaluateable variable.");
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
                                inputs.AddLast(_Parse(prev));
                            else
                                inputs.AddLast(_Parse(new Parenthetical()));
                            continue;
                        case "{":
                            if (inputs.Count == 0) throw new NestingSyntaxException("No base to index.");
                            Indexing curly = new Indexing();
                            _Parse(curly);
                            __AddOperator(operatorNodes, inputs, curly);
                            continue;
                        case "[":
                            inputs.AddLast(_Parse(new Bracket()));
                            continue;
                        case ")":
                            IEvaluateable[] pushedDown = __Pushdown(operatorNodes, inputs);
                            switch (exp)
                            {
                                case NamedFunction nf:
                                    nf.Inputs = pushedDown;
                                    return nf;
                                case Parenthetical p:
                                    if (pushedDown.Length != 1) throw new SyntaxException("Invalid structure for a parenthetical clause.", str);
                                    p.Head = pushedDown[0];
                                    return p;
                                default:
                                    throw new NestingSyntaxException("Invalid closure ')' of parenthetical without matching '('.");
                            }
                        case "}":
                            if (exp is Indexing idxing)
                            {
                                pushedDown = __Pushdown(operatorNodes, inputs);
                                idxing.Inputs = new IEvaluateable[pushedDown.Length + 1];
                                idxing.Inputs[0] = null;
                                pushedDown.CopyTo(idxing.Inputs, 1);
                                return exp;
                            }
                            throw new NestingSyntaxException("Invalid closure '}' of indexing bracket without matching '{'.");
                        case "]":
                            if (exp is Bracket br)
                            {
                                br.Vector = new Vector(__Pushdown(operatorNodes, inputs));
                                return br;
                            }
                            throw new NestingSyntaxException("Invalid closure ']' of vector bracket without matching ']'.");
                    }

                    // An operator?
                    if (Operator.TryCreate(token, out Operator oper))
                    {
                        operatorNodes.Enqueue(new PrioritizedToken(inputs.AddLast(oper)));
                        inputs.AddLast(oper);
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
                        if (ctxt.TryGetSubcontext(token, out IContext next_ctxt))
                        {
                            ctxt = next_ctxt;
                            dot = false;
                            continue;
                        }

                        else if (ctxt.TryGetVariable(token, out IVariable var))
                        {
                            inputs.AddLast(var);
                            dot = false;
                            ctxt = rootContext;
                        }
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
                                Multiplication m = new Multiplication();
                                operatorNodes.Enqueue(new PrioritizedToken(inputs.AddLast(m)));
                                inputs.AddLast(m);
                                return true;
                        }
                        return false;
                    }

                }

                return exp;

                void __AddOperator(Heap<PrioritizedToken> heap, DynamicLinkedList<IEvaluateable> list, Operator oper)
                    => heap.Enqueue(new PrioritizedToken(list.AddLast(oper)));

                IEvaluateable[] __Pushdown(Heap<PrioritizedToken> heap, DynamicLinkedList<IEvaluateable> list)
                {
                    while (heap.Count > 0)
                    {
                        PrioritizedToken prioritized = heap.Dequeue();
                        Operator oper = (Operator)prioritized.Node.Contents;
                        if (!oper.Parse(prioritized.Node))
                            throw new OperatorException("Failed to parse operator " + oper.ToString());
                    }
                    return list.ToArray();
                }
            }
            
        }


        private struct PrioritizedToken
        {
            private static Dictionary<Type, int> _Priorities = new Dictionary<Type, int> {
                { typeof(Indexing), 100 },
                { typeof(Or), 200 },
                { typeof(And), 300 },
                { typeof(Exponentiation), 400 },
                { typeof(Negation), 500 },
                { typeof(Multiplication), 600 },
                { typeof(Division), 700 },
                { typeof(Addition), 800 },
                { typeof(Subtraction), 900 },
                { typeof(Range), 1000 }
            };

            public readonly DynamicLinkedList<IEvaluateable>.Node Node;
            public readonly int Priority;
            public PrioritizedToken(DynamicLinkedList<IEvaluateable>.Node node)
            {
                this.Node = node;
                if (!_Priorities.TryGetValue(node.Contents.GetType(), out int priority)) throw new NotImplementedException();
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

        #endregion


        public override string ToString() => '(' + Head.ToString() + ')';
    }




    public class SyntaxException : Exception
    {
        public readonly string Parsed;
        public SyntaxException(string message, string parsed, Exception inner = null) : base(message, inner) { this.Parsed = parsed; }
    }

    public class OperatorException : Exception
    {
        public OperatorException(string message = null) : base(message) { }
    }

    public class ReferenceException : Exception
    {
        public ReferenceException(string message) : base(message) { }
    }

    internal class NestingSyntaxException : Exception
    {
        public NestingSyntaxException() { }
        public NestingSyntaxException(string message) : base(message) { }
    }
}
