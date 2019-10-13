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

    public static class Helpers
    {
        /// <summary>
        /// Converts a non-<seealso cref="IEvaluateable"/> object into an <seealso cref="ILiteral"/> object.
        /// </summary>
        public static ILiteral Obj2Eval(object obj)
        {
            switch (obj)
            {
                case ILiteral iev: return iev;
                case null: return Null.Instance;
                case double d: return new Number(d);
                case int i: return new Number(i);
                case decimal m: return new Number(m);
                case bool b: return Boolean.FromBool(b);
                default: return new Dependency.String(obj.ToString());
            }
        }
        public static ILiteral Obj2Eval<T>(T obj) => Obj2Eval((object)obj);

        public static object Eval2Obj(IEvaluateable iev)
        {
            switch (iev)
            {
                case Dependency.String ds: return ds.Value;
                case Number n: return (n.IsInteger) ? (int)n : (decimal)n;
                default: return iev;
            }
        }

        public static IEnumerable<Variables.Variable> GetDependees(object obj)
        {
            foreach (Reference r in GetReferences(obj)) if (r.Head is Variables.Variable v) yield return v;
        }

        internal static IEnumerable<Reference> GetReferences(object obj)
        {
            Stack<object> stack = new Stack<object>();
            HashSet<object> visited = new HashSet<object>();
            stack.Push(obj);
            while (stack.Count > 0)
            {
                object focus = stack.Pop();
                if (!visited.Add(focus)) continue;
                switch (focus)
                {
                    case Reference r: yield return r; break;
                    case IExpression e: stack.Push(e.Contents); break;
                    case IFunction f: foreach (object input in f.Inputs) stack.Push(input); break;
                    case IVariableInternal v: stack.Push(v.Contents); break;
                }
            }
        }


        internal static bool TryFindCircularity(object target, object start)
        {
            Stack<object> stack = new Stack<object>();
            HashSet<object> visited = new HashSet<object>();
            stack.Push(start);
            while (stack.Count > 0)
            {
                object focus = stack.Pop();
                if (ReferenceEquals(focus, target)) return true;
                if (!visited.Add(focus)) continue;
                switch (focus)
                {
                    case Reference r: stack.Push(r.Head); break;
                    case IExpression e: stack.Push(e.Contents); break;
                    case IFunction f: foreach (object input in f.Inputs) stack.Push(input); break;
                    case IVariableInternal v: stack.Push(v.Contents); break;
                }

            }
            return false;
        }
        
        public static IEvaluateable Recalculate(IEvaluateable ieval)
        {
            return _RecursiveRecalc(ieval);

            IEvaluateable _RecursiveRecalc(IEvaluateable focus)
            {
                if (focus is ILiteral) return focus;
                if (focus is IFunction ifunc) { foreach (var input in ifunc.Inputs) _RecursiveRecalc(input); ifunc.Update(); }
                else if (focus is IExpression iexp) return _RecursiveRecalc(iexp.Contents);
                else if (focus is IDynamicItem idi) idi.Update();
                return focus.Value;
            }
        }
    }


    public sealed class Expression : IExpression
    {
        internal enum ExpressionType { NONE, PAREN, BRACKET, CURLY, NAKED }

        public IEvaluateable Contents { get; set; }
        IEvaluateable IEvaluateable.Value => Contents.Value;

        public override string ToString() => Contents.ToString();

    }

}
