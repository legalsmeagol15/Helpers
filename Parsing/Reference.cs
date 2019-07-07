using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    public sealed class Reference : IEvaluateable, IExpression
    {
        public readonly Mobility Mobility;
        private readonly Reference Prior;
        private readonly string Segment;
        public readonly IContext Context;
        public readonly IEvaluateable Source;

        public bool IsComplete => Source != null;

        IEvaluateable IEvaluateable.Value => Source.Value;
        IEvaluateable IEvaluateable.UpdateValue() => Source.Value;
        IEvaluateable IExpression.GetGuts() => Source;

        public object Contents
        {
            set
            {
                Variable v = Source as Variable;
                if (v == null) throw new NullReferenceException("Reference " + ToString() + " does not refer to a variable.");
                if (value is IEvaluateable e)
                    v.Contents = e;
                else
                    v.Contents = Helpers.Obj2Eval(value);
                v.UpdateValue();                
            }
        }

        public Reference this[string segment]
        {
            get
            {
                if (Context.TryGetSubcontext(segment, out IContext sub_ctxt))
                    return new Reference(this, sub_ctxt, segment);
                else if (Context.TryGetProperty(segment, out IEvaluateable src))
                    return new Reference(this, null, segment, src);
                throw new Parse.ReferenceException(this, "Invalid reference '" + segment + "' in reference path.");
            }
        }

        private Reference(Reference prior, IContext context, string segment, IEvaluateable head = null, Mobility mobility = Mobility.All)
        {
            this.Prior = prior;
            this.Context = context;
            this.Segment = segment;
            this.Source = head;
            this.Mobility = mobility;
        }

        /// <summary>
        /// Creates a path embodying the context or variable at its conclusion, starting at the given root.
        /// </summary>
        public static Reference FromPath(IContext root, params string[] segments)
        {
            Reference result = new Reference(null, root, "", (root as IEvaluateable));
            int i;
            for (i = 0; i < segments.Length; i++)
            {
                string segment = segments[i];
                result = result[segment];
                if (result.Context == null) break;
            }
            if (i < segments.Length - 1)
                throw new Parse.ReferenceException(result, "Premature completion of path at index " + i + ".");
            return result;
        }
        /// <summary>
        /// Creates a path embodying the context or variable at its conclusion, starting at the given root.  The given 
        /// root must either implement <seealso cref="IContext"/> or already be managed.
        /// </summary>
        public static Reference FromPath(object root, params string[] segments)
        {
            if (root is IContext ic) return FromPath(ic, segments);
            if (!ManagedContext.HostContexts.TryGetValue(root, out WeakReference<IContext> weakRef) || !weakRef.TryGetTarget(out ic))
                throw new Parse.ReferenceException(null, "Given root is an invalid context.");
            return FromPath(ic, segments);
        }


        public string ToString(IContext perspective)
        {
            if (perspective == null) return this.ToString();
            throw new NotImplementedException();
        }
        public override string ToString()
        {
            Deque<string> steps = new Deque<string>();
            Reference root = this;
            while (root != null) { steps.AddFirst(root.Segment); root = root.Prior; }
            return string.Join(".", steps);
        }


    }

}
