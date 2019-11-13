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
        public static string  GetRelativeString(IContext origin)
        {
            throw new NotImplementedException();
        }

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

        public static IEnumerable<IVariable> GetDependees(object obj)
        {
            HashSet<IReference> visited = new HashSet<IReference>();
            HashSet<IVariable> returned = new HashSet<IVariable>();
            foreach (IReference r in GetReferences(obj))
            {
                if (!visited.Add(r)) continue;
                foreach (IEvaluateable c in r.GetComposers())
                    foreach (IUpdatedVariable sub_var in GetDependees(c))
                        if (returned.Add(sub_var))
                            yield return sub_var;
            }
        }

        /// <summary>
        /// Returns the collection of references below the given object.  Duplicate items are possible.
        /// </summary>
        internal static IEnumerable<IReference> GetReferences(object obj)
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
                    case IReference r: yield return r; break;
                    case IFunction f: foreach (object input in f.Inputs) stack.Push(input); break;
                    case IExpression e: stack.Push(e.Contents); break;
                    case IVariable v: stack.Push(v.Contents); break;
                    default: yield break;
                }
            }
        }

        /// <summary>Returns whether the given object is part of a dependency circularity.</summary>
        /// <param name="start">The object at which to start searching.  If a route back to this object is discovered, 
        /// a circularity is found.</param>
        internal static bool TryFindCircularity(IVariable start)
        {
            // If the starting object depends on nothing, circularity would be impossible.
            if (!GetReferences(start.Contents).Any())
                return false;

            Stack<object> stack = new Stack<object>();
            HashSet<object> visited = new HashSet<object>();
            _AppendListenersOf(start);
            
            while (stack.Count > 0)
            {
                object focus = stack.Pop();
                
                // If this item has already been visited, no need to follow it.
                if (!visited.Add(focus)) continue;

                // If we've found our way back to the start, that is a circularity.
                if (focus.Equals(start)) return true;

                // Append the focus's listeners.
                _AppendListenersOf(focus);
            }

            // No circularity found.
            return false;

            void _AppendListenersOf(object obj)
            {
                if (obj is ISyncUpdater isu && isu.Parent != null) stack.Push(isu.Parent);
                if (obj is IAsyncUpdater iau) foreach (var l in iau.GetListeners()) stack.Push(l);
            }
        }
        
        /// <summary>
        /// Recalculates everything below the given <seealso cref="IEvaluateable"/>, down to to the point of 
        /// asynchronous <seealso cref="Reference"/> sources.
        /// <para/>This method should be used only on the creation of an <seealso cref="IEvaluateable"/>.  It is more 
        /// expensive than the bottom-update <seealso cref="Dependency.Variables.Update"/> process.
        /// </summary>
        /// <returns>Returns the new value of the given <seealso cref="IEvaluateable"/>.</returns>
        public static IEvaluateable Recalculate(IEvaluateable ieval)
        {
            const ISyncUpdater nullChild = null;
            return _RecursiveRecalc(ieval);

            IEvaluateable _RecursiveRecalc(IEvaluateable focus)
            {
                switch (focus)
                {
                    case IVariable iv:  // Variables are presumed to be up-to-date.
                        break;
                    case IFunction ifunc:
                        foreach (var input in ifunc.Inputs) _RecursiveRecalc(input);
                        ifunc.Update(null, nullChild);
                        break;
                    case IExpression ie:
                        return _RecursiveRecalc(ie.Contents);
                }
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
