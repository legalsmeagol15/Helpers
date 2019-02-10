using Parsing.Dependency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{

    /// <summary>IEvaluteable objects can evaluate to another IEvaluatable value.</summary>
    public interface IEvaluateable
    {
        IEvaluateable Evaluate();
    }


    public interface IIndexable
    {
        object this[IEvaluateable index] { get; }

        int MaxIndex { get; }
        int MinIndex { get; }
    }
    
    public static class Methods
    {
        public static IEnumerable<Reference> GetReferences(this IEvaluateable iev)
        {
            List<Reference> result = new List<Reference>();
            ListReferencesInternal(iev, result);
            return result;

            void ListReferencesInternal(IEvaluateable focus, List<Reference> refs)
            {
                switch (focus)
                {
                    case Reference r: refs.Add(r); return;
                    case Clause c: foreach (IEvaluateable input in c.Inputs) ListReferencesInternal(input, refs); return;
                }
            }
        }


        public static double ToDouble(this IEvaluateable iev)
        {
            switch (iev)
            {
                case Number n: return (double)n.Value;
                default: throw new NotImplementedException();
            }
        }
        public static int ToInt(this IEvaluateable iev)
        {
            switch (iev)
            {
                case Number n: return (int)n.Value;
                default: throw new NotImplementedException();
            }
        }
        public static string ToString(this IEvaluateable iev)
        {
            switch (iev)
            {
                case Number n: return n.Value.ToString();
                case String s: return s.Value;
                default: throw new NotImplementedException();
            }
        }
    }
}
