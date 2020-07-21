using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;
using Dependency.Variables;

namespace Dependency.Values
{
    public class Range : IEvaluateable, ITypeGuarantee, IContext, IIndexable, ISyncUpdater
    {
        public readonly IEvaluateable From;
        public readonly IEvaluateable To;
        public bool IsNumeric => From is Number && To is Number;
        public bool IsInteger => From is Number na && na.IsInteger && To is Number nb && nb.IsInteger;
        public Range(IEvaluateable @from, IEvaluateable @to) { this.From = from; this.To = to; }

        public bool Contains(IEvaluateable item)
        {
            if (item is Number ni && From is Number na && To is Number nb && na <= ni && ni <= nb) return true;
            return false;
        }

        IEvaluateable IEvaluateable.Value => this;

        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Range;

        ISyncUpdater ISyncUpdater.Parent { get; set; } = null;

        bool IIndexable.ControlsReindex => false;

        bool IContext.TryGetProperty(string path, out IEvaluateable source)
        {
            switch (path.ToLower())
            {
                case "size":
                case "count":
                case "length":
                    if (From.Value is Number na && To.Value is Number nb)
                        source = new Number(nb - na);
                    else
                        source = new RangeError(this, "Range cannot be sized.");

                    return true;
                default:
                    source = null;
                    return false;
            }
        }

        bool IContext.TryGetSubcontext(string path, out IContext ctxt) { ctxt = null; return false; }

        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
        {
            if (IsNumeric && ordinal is Number nv)
            { val = ((Number)From) + nv; return true; }
            val = default(IEvaluateable);
            return false;
        }

        ICollection<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild, ICollection<IEvaluateable> updatedDomain)
            => updatedDomain;
    }
}
