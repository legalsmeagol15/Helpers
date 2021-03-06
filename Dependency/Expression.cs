﻿using System;
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


        public static string GetRelativeString(IContext origin)
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
                case Dependency.String ds: return ds._Value;
                case Number n: return (n.IsInteger) ? (int)n : (decimal)n;
                default: return iev;
            }
        }

        //public static IEnumerable<IVariable> GetDependees(IEvaluateable obj)
        //{
        //    Stack<IEvaluateable> stack = new Stack<IEvaluateable>();
        //    HashSet<IEvaluateable> visited = new HashSet<IEvaluateable>();
        //    _Enstack_Subs(obj);

        //    while (stack.Count > 0)
        //    {
        //        IEvaluateable focus = stack.Pop();
        //        if (focus is IVariable v) yield return v;
        //        else _Enstack_Subs(focus);
        //    }

        //    void _Enstack_Subs(IEvaluateable iev)
        //    {
        //        if (!visited.Add(iev)) return;
        //        switch (iev)
        //        {
        //            case IReference r: foreach (var c in r.GetComposers()) stack.Push(c); break;
        //            case IFunction f: foreach (var i in f.Inputs) stack.Push(f); break;
        //            case IExpression e: stack.Push(e.Contents); break;
        //            case IVariable v: stack.Push(v.Contents); break;
        //        }
        //    }
        //}


        /// <summary>
        /// Returns whether a dependency exists from the given item, upon itself.  The path returned is 
        /// guaranteed to be the shortest possible dependency path.
        /// </summary>
        public static bool TryFindDependency(IEvaluateable item, out IEnumerable<IEvaluateable> path)
        {
            // This method works by going backwards, checking if the starter depends on its parents/listeners
            List<IEvaluateable> dependees = new List<IEvaluateable>();
            if (item is ISyncUpdater isu && isu.Parent != null)
                dependees.Add(isu.Parent);
            if (item is IAsyncUpdater iau)
                dependees.AddRange(iau.GetListeners());
            return TryFindDependency(new IEvaluateable[] { item }, dependees, out path);
        }
        /// <summary>
        /// Returns whether a dependency exists from the given item, upon the given 
        /// item.  The path returned is guaranteed to be the shortest possible path.
        /// </summary>
        public static bool TryFindDependency(IEvaluateable dependent, IEvaluateable dependee, out IEnumerable<IEvaluateable> circular_path)
            => TryFindDependency(new IEvaluateable[] { dependent }, new IEvaluateable[] { dependee }, out circular_path);
        /// <summary>
        /// Returns whether a dependency exists among the given dependents upon the given 
        /// dependees.  The returned path is guaranteed to be the shortest possible path from any 
        /// of the dependents to any of the dependees.
        /// </summary>
        public static bool TryFindDependency(IEnumerable<IEvaluateable> dependents,
                                             IEnumerable<IEvaluateable> dependees,
                                             out IEnumerable<IEvaluateable> path)
        {
            // This method will go listener-ward to look for dependencies.

            if (!dependents.Any()) { path = default; return false; }
            HashSet<IEvaluateable> realDependents
                = new HashSet<IEvaluateable>(dependents.Where(d => d is ISyncUpdater || d is IAsyncUpdater));

            // Must be a queue, not a stack, so depth will be the true minimal depth.
            HashSet<IEvaluateable> wasEnqueued = new HashSet<IEvaluateable>();
            Queue<DependencyPointer> queue = new Queue<DependencyPointer>();
            foreach (var dee in dependees.Where(d => d is ISyncUpdater || d is IAsyncUpdater))
                if (wasEnqueued.Add(dee))
                    queue.Enqueue(new DependencyPointer() { Dependent = dee, Dependee = null, Depth = 0 });

            while (queue.Count > 0)
            {
                var focus = queue.Dequeue();
                IEvaluateable focus_dent = focus.Dependent;

                // Did we find a route to the specified dependents?
                if (realDependents.Contains(focus_dent))
                {
                    List<IEvaluateable> resultPath = new List<IEvaluateable>();
                    while (focus != null) { resultPath.Add(focus.Dependent); focus = focus.Dependee; }
                    path = resultPath;
                    return true;
                }

                // Enqueue any parent/listeners who are dependent on the current focus, but 
                // haven't been enqueued yet.
                if (focus_dent is ISyncUpdater isu && isu.Parent != null)
                    if (wasEnqueued.Add(isu.Parent))
                        queue.Enqueue(new DependencyPointer() { Dependee = focus, Dependent = isu.Parent, Depth = focus.Depth + 1 });
                if (focus_dent is IAsyncUpdater iau)
                    foreach (var listener in iau.GetListeners())
                        if (wasEnqueued.Add(listener))
                            queue.Enqueue(new DependencyPointer() { Dependee = focus, Dependent = listener, Depth = focus.Depth + 1 });
            }

            // No dependency found.
            path = default;
            return false;
        }
        private class DependencyPointer
        {
            public IEvaluateable Dependent;
            public int Depth = 0;
            public DependencyPointer Dependee;
        }

        /// <summary>
        /// Recalculates everything below the given <seealso cref="IEvaluateable"/>, down to 
        /// (but not including) references to asynchronous <seealso cref="Reference"/> sources.
        /// <para/>This method should be used only on the creation of an <seealso cref="IEvaluateable"/>.  It is more 
        /// expensive than the bottom-update <seealso cref="Dependency.Variables.Update"/> process.
        /// </summary>
        /// <returns>Returns the new value of the given <seealso cref="IEvaluateable"/>.</returns>
        public static IEvaluateable Recalculate(IEvaluateable ieval)
        {
            const ISyncUpdater nullChild = null;
            var universalSet = Dependency.Variables.Update.UniversalSet;
            return _RecursiveRecalc(ieval);

            IEvaluateable _RecursiveRecalc(IEvaluateable focus)
            {   
                switch (focus)
                {
                    case IVariable iv:  // Variables are presumed to be up-to-date.
                        break;
                    case IFunction ifunc:
                        foreach (var input in ifunc.Inputs) _RecursiveRecalc(input);
                        ifunc.Update(null, nullChild, universalSet);
                        break;
                    case IExpression ie:
                        return _RecursiveRecalc(ie.Contents);
                    case Vector v:
                        foreach (var c in v.GetContents()) _RecursiveRecalc(c);
                        v.Update(universalSet);
                        break;
                    case Reference r:
                        _RecursiveRecalc(r.Base);
                        _RecursiveRecalc(r.Path);
                        r.Update(r.Base);
                        _RecursiveRecalc(r.Subject);
                        r.Update(r.Subject);
                        break;
                    case Indexing idxing:
                        _RecursiveRecalc(idxing.Base);
                        _RecursiveRecalc(idxing.Ordinal);
                        idxing.Update(idxing.Base);
                        _RecursiveRecalc(idxing.Subject);
                        idxing.Update(idxing.Subject);
                        break;
                    default:
                        Debug.Assert(!(focus is ISyncUpdater), "Haven't implemented " + nameof(Recalculate) + " for sync " + focus.GetType().Name);
                        Debug.Assert(!(focus is IAsyncUpdater), "Haven't implemented " + nameof(Recalculate) + " for async " + focus.GetType().Name);
                        break;
                }
                return focus.Value;
            }
        }

        internal static void SetParent(ISyncUpdater parent, object child, bool checkParent = true)
        {
            if (!(child is ISyncUpdater isu)) return;
            Debug.Assert(!checkParent || isu.Parent == null || isu.Parent == parent);
            isu.Parent = parent;
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
