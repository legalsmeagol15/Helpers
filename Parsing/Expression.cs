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
        public static ILiteral Obj2Eval<T>(T obj) => Obj2Eval(obj);

        public static object Eval2Obj(IEvaluateable iev)
        {
            switch (iev)
            {
                case Dependency.String ds: return ds.Value;
                case Number n: return (n.IsInteger) ? (int)n : (decimal)n;
                default: return iev;
            }
        }

        public static IEnumerable<Variables.Variable> GetDependees(object e)
        {
            if (e is IVariable iv) e = iv.Contents;
            foreach (Reference r in GetReferences(e)) if (r.Head is Variables.Variable v) yield return v;
        }
        internal static IEnumerable<IVariable> GetTerms(object e)
        {
            Stack<object> stack = new Stack<object>();
            stack.Push(e);
            while (stack.Count > 0)
            {
                object focus = stack.Pop();
                switch (focus)
                {
                    case Reference r: if (r.Head is IVariable iv) yield return iv; continue;
                    case IFunction f: foreach (var input in f.Inputs) stack.Push(input); continue;
                    case IExpression x: stack.Push(x.Contents); continue;
                    case ILiteral l: continue;
                    case IVariable v: yield return v; continue;
                    default: throw new NotImplementedException();
                }
            }
        }

        internal static IEnumerable<Reference> GetReferences(object e)
        {
            Stack<object> stack = new Stack<object>();
            stack.Push(e);
            while (stack.Count > 0)
            {
                object focus = stack.Pop();
                switch (focus)
                {
                    case Reference r: yield return r; break;
                    case IExpression exp: stack.Push(exp.Contents); break;
                    case IFunction f: foreach (object input in f.Inputs) stack.Push(input); break;
                    case IVariable v: throw new InvalidOperationException();
                }
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
