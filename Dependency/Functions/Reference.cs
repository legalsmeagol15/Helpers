using Dependency.Variables;
using Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Functions
{

    internal sealed class Reference : IReference, ISyncUpdater
    {
        // TODO:  the PathLegs could be iterated more quickly if they were in a List<PathLeg> instead of linked list.
        internal PathLeg Head;
        public IEvaluateable Tail;
        public IEvaluateable Value { get; private set; }
        public ISyncUpdater Parent
        {
            get;
            set;
        }

        private Reference(IContext anchor, IEnumerable<IEvaluateable> path)
        {
            List<PathLeg> legs = new List<PathLeg>();
            foreach (IEvaluateable iev in path)
                legs.Add(new PathLeg(anchor, this, iev, legs.Count == 0 && anchor == null));
            if (legs.Count == 0) throw new ArgumentException("Empty path", nameof(path));
            Head = legs[0];
            for (int i = 0; i < legs.Count - 1; i++)
                legs[i].Next = legs[i + 1];
        }

        public static Reference CreateAnchored(IContext anchor, params string[] path)
        {
            Debug.Assert(!path.Any(p => p.Contains(".")));
            return new Reference(anchor, path.Select(p => (IEvaluateable)new Dependency.String(p)));
        }
        public static Reference CreateDynamic(IEnumerable<IEvaluateable> path)
            => new Reference(null, path);


        internal bool Update()
        {
            if (Value == null)
            {
                if (Tail == null || Tail.Value == null) return false;
            }
            else if (Value.Equals(Tail.Value))
                return false;
            Value = Tail.Value;
            return true;
        }
        ITrueSet<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild, ITrueSet<IEvaluateable> indexDomain)
            => Update() ? Dependency.Variables.Update.UniversalSet : null;

        IEnumerable<IEvaluateable> IReference.GetComposers() => GetContents();
        IEnumerable<string> GetPath() => GetContents().Select(c => c.Value.ToString());
        internal IEnumerable<IEvaluateable> GetContents()
        {
            PathLeg leg = Head;
            while (leg != null) { yield return leg.Contents; leg = leg.Next; }
        }

        public override string ToString() => "->" + string.Join(".", GetPath());

        internal class PathLeg : ISyncUpdater
        {
            public IEvaluateable Contents { get; set; }
            public IEvaluateable Value { get; private set; }
            public IContext Context { get; private set; }
            public PathLeg Next { get; internal set; }
            public Reference Parent { get; private set; }
            ISyncUpdater ISyncUpdater.Parent { get => Parent; set => Parent = value as Reference; }
            public readonly bool IsDynamic;
            public PathLeg(IContext context, Reference parent, IEvaluateable contents, bool isDynamic)
            {
                this.Context = context;
                this.Parent = parent;
                this.IsDynamic = isDynamic;
                this.Contents = contents ?? throw new ArgumentNullException(nameof(Contents));
            }

            internal static bool Update(PathLeg leg)
            {
                // This should be a static method.  It prevents me from making some stupid mistakes.

                while (leg.Next != null)
                {
                    // For the pre-tail legs, identify the new leg value.
                    IEvaluateable newValue = leg.Contents.Value;
                    if (Helpers.TryFindDependency(leg.Value, leg.Parent, out var circ_path))
                        return _Invalidate(leg, new CircularityError(leg, circ_path));
                    if (newValue.Equals(leg.Value))
                        return false;
                    leg.Value = newValue;

                    IContext next_ctxt = null;
                    if (leg.Value is Dependency.String quote)
                    {
                        if (leg.IsDynamic)
                            next_ctxt = leg.Value as IContext;
                        else if (leg.Context.TryGetSubcontext(quote, out var sub_c))
                            next_ctxt = sub_c;
                        else if (leg.Context.TryGetProperty(quote, out var sub_i_c))
                            next_ctxt = sub_i_c as IContext;
                    }
                    if (next_ctxt == null)
                        return _Invalidate(leg, new ReferenceError(leg.Parent, "Invalid path \"" + string.Join(".", leg.Parent.GetPath()) + "\"."));
                    if (next_ctxt.Equals(leg.Next.Context))
                        return false;
                    leg = leg.Next;
                    leg.Context = next_ctxt;
                }
                // We're now at the last leg.  Identify the last value.
                IEvaluateable last_newValue = leg.Contents.Value;
                if (Helpers.TryFindDependency(leg.Value, leg.Parent, out var last_circ_path))
                    return _Invalidate(leg, new CircularityError(leg, last_circ_path));
                if (last_newValue.Equals(leg.Value))
                    return false;
                leg.Value = last_newValue;

                // We now know the last value changed, let's see if the tail changed.
                IEvaluateable newTail = null;
                if (leg.IsDynamic)
                    newTail = leg.Value;
                else if (leg.Value is Dependency.String q)
                {
                    if (leg.Context.TryGetSubcontext(q, out IContext c))
                        newTail = c as IEvaluateable;
                    else if (leg.Context.TryGetProperty(q, out IEvaluateable p))
                        newTail = p;
                }
                if (newTail == null)
                    return _Invalidate(leg, new ReferenceError(leg.Parent, "Invalid path \"" + string.Join(".", leg.Parent.GetPath()) + "\"."));
                if (Helpers.TryFindDependency(newTail, leg.Parent, out var tail_circ_path))
                    return _Invalidate(leg, new CircularityError(leg, tail_circ_path));
                if (newTail.Equals(leg.Parent.Tail))
                    return false;

                // The tail changed.  Update listeners and then update the tail.
                if (leg.Parent.Tail is IAsyncUpdater iau_old)
                    iau_old.RemoveListener(leg.Parent);
                if (newTail is IAsyncUpdater iau_new)
                    iau_new.AddListener(leg.Parent);
                leg.Parent.Tail = newTail;
                return true;

                bool _Invalidate(PathLeg pl, Error err)
                {
                    // Returns whether the last path was changed.
                    while (pl.Next != null)
                    {
                        if (err.Equals(pl.Value)) return false;
                        pl.Value = err;
                        pl = pl.Next;
                    }
                    if (err.Equals(pl.Parent.Tail)) return false;
                    pl.Parent.Tail = err;
                    return true;
                }
            }
            ITrueSet<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild, ITrueSet<IEvaluateable> indexDomain)
                => Update(this) ? Dependency.Variables.Update.UniversalSet : null;
        }

    }
}
