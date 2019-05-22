﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataStructures;

namespace Dependency
{
    public interface IExpression : IEvaluateable
    {
        IEvaluateable[] Contents { get; }        
    }
    
    public sealed class Clause : IExpression
    {
        internal enum BracketType { None, Paren, Curly} // Square is reserved for indexing.
        internal readonly BracketType Brackets;
        internal string Closer
        {
            get
            {
                switch (Brackets)
                {
                    case BracketType.Paren: return ")";
                    case BracketType.Curly: return "}";
                    default:return "";
                }
            }
        }
        internal string Opener
        {
            get
            {
                switch (Brackets)
                {
                    case BracketType.Paren: return "(";
                    case BracketType.Curly: return "{";
                    default: return "";
                }
            }
        }

        private Clause _Value = null;
        public IEvaluateable Value => _Value ?? Evaluate();
        public IEvaluateable Evaluate() => _Value = new Clause(Brackets, Contents.Select(c => c.Evaluate()));

        public IEvaluateable[] Contents { get; internal set; }

        private Clause(BracketType brackets = BracketType.None) { this.Brackets = brackets; }
        private Clause(BracketType brackets, IEnumerable<IEvaluateable> contents) : this(brackets) { Contents = contents.ToArray(); }


        


        private class Prioritized
        {
            public readonly Operator.Priorities Priority;
            public readonly DynamicLinkedList<IEvaluateable>.Node Node;
            public Prioritized(Operator.Priorities priority, DynamicLinkedList<IEvaluateable>.Node node) { this.Priority = priority; this.Node = node; }
        }

        public static Clause FromString(string str, FunctionFactory functions, DependencyManager depMngr)
        {
            Console.WriteLine("Pattern = " + Pattern);
            Console.WriteLine("Input = " + str);
            
            string[] splits = Regex.Split(str);
            HashSet<Reference> refs = new HashSet<Reference>();
            int splitIdx = 0;
            
            try
            {
                Clause e = new Clause(BracketType.None);
                _Parse(e);
                return e;
            }catch (NestingMismatchException nme)
            {
                Array.Resize(ref splits, splitIdx+1);
                string msg = string.Join("", splits);
                throw new SyntaxException("Nesting mismatch.", msg, nme);
            }
            
            

            IExpression _Parse(IExpression exp)
            {
                DynamicLinkedList<IEvaluateable> inputs = new DynamicLinkedList<IEvaluateable>();
                Heap<Prioritized> operatorNodes 
                    = new Heap<Prioritized>((a, b) => a.Priority.CompareTo(b.Priority));
                while (splitIdx < splits.Length)
                {
                    string token = splits[splitIdx];
                    if (string.IsNullOrWhiteSpace(token)) continue;
                    switch (token)
                    {
                        case string _ when Number.TryParse(token, out Number n):
                            inputs.AddLast(n); continue;
                        case string _ when bool.TryParse(token, out bool b):
                            inputs.AddLast(Dependency.Boolean.FromBool(b)); continue;
                        case string _ when token.StartsWith("\"") && token.EndsWith("\"") && token.Count((c) => c == '\"') == 2:
                            inputs.AddLast(new Dependency.String(token)); continue;

                        case "(": inputs.AddLast(_Parse(new Clause(BracketType.Paren))); continue;                        
                        case "{": inputs.AddLast(_Parse(new Clause(BracketType.Curly))); continue;
                        case "[":
                            operatorNodes.Enqueue(new Prioritized(Operator.Priorities.INDEXING, inputs.AddLast(_Parse(new Indexing())))); continue;                        
                        case ")":
                        case "}":
                            if (!(exp is Clause c)) throw new NestingMismatchException();
                            if (c.Closer != token) throw new NestingMismatchException();
                            c.Contents = __InputsToTree();
                            return exp;
                        case "]":
                            if (!(exp is Indexing i)) throw new NestingMismatchException();
                            i.Contents = __InputsToTree();
                            return exp;
                        case ",": continue;
                        case ";": throw new NotImplementedException();

                        case "-":
                            Operator o;
                            if (inputs.Any() && !(inputs.Last is Operator))
                                operatorNodes.Enqueue(new Prioritized(Operator.Priorities.SUBTRACTION, inputs.AddLast(new Subtraction())));
                            else
                                operatorNodes.Enqueue(new Prioritized(Operator.Priorities.SUBTRACTION, inputs.AddLast(new Negation())));
                            continue;
                        case "!":
                        case "~":
                            operatorNodes.Enqueue(new Prioritized(Operator.Priorities.SUBTRACTION, inputs.AddLast(new Negation()))); continue;
                        case "+":
                            operatorNodes.Enqueue(new Prioritized(Operator.Priorities.SUBTRACTION, inputs.AddLast(new Addition()))); continue;
                        case "*":
                            operatorNodes.Enqueue(new Prioritized(Operator.Priorities.SUBTRACTION, inputs.AddLast(new Multiplication()))); continue;
                        case "/":
                            operatorNodes.Enqueue(new Prioritized(Operator.Priorities.SUBTRACTION, inputs.AddLast(new Division()))); continue;
                        case "^":
                            operatorNodes.Enqueue(new Prioritized(Operator.Priorities.SUBTRACTION, inputs.AddLast(new Exponentiation()))); continue;
                        case ".":
                            operatorNodes.Enqueue(new Prioritized(Operator.Priorities.SUBTRACTION, inputs.AddLast(new Reference()))); continue;
                        case "&":
                            operatorNodes.Enqueue(new Prioritized(Operator.Priorities.SUBTRACTION, inputs.AddLast(new And()))); continue;
                        case "|":
                            operatorNodes.Enqueue(new Prioritized(Operator.Priorities.SUBTRACTION, inputs.AddLast(new Or()))); continue;
                        case ":":
                            operatorNodes.Enqueue(new Prioritized(Operator.Priorities.RANGE, inputs.AddLast(new Range()))); continue;

                        case string _ when functions != null && functions.TryCreate(token, out Function f):
                            inputs.AddLast(_Parse(f)); continue;

                        case string _ when depMngr != null && depMngr.TryGet(token, out IContext ctxt):
                            inputs.AddLast(ctxt); continue;

                        case string _ when depMngr != null && depMngr.TryGet(token, out IVariable v):
                            inputs.AddLast(v); continue;

                        default: throw new SyntaxException("Unrecognized token: ", token, null);
                    }
                    if (inputs.Count >=2)
                    {
                        switch (inputs.LastNode.Previous.Contents)
                        {
                            case Number n:
                                if (inputs.Last is Function fLast) inputs.LastNode.InsertBefore(new Multiplication(n, fLast));
                                else if (inputs.Last is Clause cLast) inputs.LastNode.InsertBefore(new Multiplication(n, cLast));                                
                                break;
                            case Function fPrev:
                                if (inputs.Last is Clause inputClause && inputClause.Brackets == BracketType.Paren)
                                {
                                    fPrev.Contents = inputClause.Contents;
                                    inputs.RemoveLast();
                                    break;
                                }
                                throw new SyntaxException("Function " + fPrev.Name + " must be followed by parenthetical input expression.");
                            
                        }
                        
                    }
                    
                    
                    splitIdx++;
                }
                

                IEvaluateable[] __InputsToTree()
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


        public override string ToString()
        {
            switch (Brackets)
            {
                case BracketType.Paren: return "(" + string.Join(",", (IEnumerable<IEvaluateable>)Contents) + ")";
                case BracketType.Curly: return "{" + string.Join(",", (IEnumerable<IEvaluateable>)Contents) + "}";
                default:return string.Join(",", (IEnumerable<IEvaluateable>)Contents);
            }
        }

        IEvaluateable IEvaluateable.Evaluate()
        {
            throw new NotImplementedException();
        }

        #region Expression RegEx members
        private const string StringPattern = "(?<stringPattern>\".*\")";
        private const string OpenerPattern = @"(?<openerPattern>[\(\[{])";
        private const string CloserPattern = @"(?<closerPattern>[\)\]}])";
        private const string OperPattern = @"(?<operPattern>[+-/*&|^~!])";
        private const string VarPattern = @"(?<varPattern> \$? [a-zA-Z_][\w_]* (?:\.[a-zA-Z_][\w_]*)*)";
        private const string NumPattern = @"(?<numPattern>(?:-)? (?: \d+\.\d* | \d*\.\d+ | \d+ ))";
        private const string SpacePattern = @"(?<spacePattern>\s+)";

        private static string Pattern = string.Join(" | ", StringPattern, OpenerPattern, CloserPattern, OperPattern, VarPattern, NumPattern, SpacePattern);
        private static Regex Regex = new Regex(Pattern, RegexOptions.IgnorePatternWhitespace);

        public class SyntaxException : Exception
        {
            public readonly string Parsed;
            public SyntaxException(string message, string parsed, Exception inner) : base(message, inner) { this.Parsed = parsed; }
        }

        public class OperatorException: Exception 
        {
            
        }

        internal class NestingMismatchException : Exception
        {
            public NestingMismatchException()
            {
            }            
        }
        #endregion
    }
}
