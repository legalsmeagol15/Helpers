using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Helpers;

namespace Dependency
{
    public sealed class Reference : IDynamicEvaluateable
    {
        internal readonly IContext RootContext;
        internal readonly IContext InvariantContext;
        internal int DynamicIndex = 0;
        public IVariable Source = null;
        public IVariable HostVariable { get; internal set; }
        private readonly List<Segment> Segments = new List<Segment>();
        
        internal IDynamicEvaluateable Parent { get; set; }

        private IEvaluateable _Value = Dependency.Null.Instance;
        public IEvaluateable Value
        {
            get => _Value;
            private set
            {
                _Value = value;
                var p = Parent;
                while (p != null) { p.Update(); p = p.Parent; }
                if (HostVariable != null) HostVariable.Update();
            }
        }

        IDynamicEvaluateable IDynamicEvaluateable.Parent { get => Parent; set => Parent = value; }

        internal Reference(IContext root)
        {
            this.RootContext = root;
            this.DynamicIndex = 1;
            Segments.Add(new Segment(this, 0, null));
            Segments[0].CachedContext = root;
        }

        internal void Refresh(int index = 1)
        {
            // No index prior to the dynamic index can be refreshed.
            if (index < DynamicIndex) index = DynamicIndex;
            
            // Find the first dynamic path index whose path actually changed.
            for (; index < Segments.Count; index++)
            {
                Segment seg = Segments[index];
                IEvaluateable newPathValue = seg.Path.Value;
                if (!newPathValue.Equals(seg.Value)) break;
            }

            // Traverse the contexts based on the re-evaluated path values.
            for (; index < Segments.Count -1 ; index++)
            {
                Segment seg = Segments[index];
                IEvaluateable newPathValue = seg.Path.Value;
                object path = Dependency.Helpers.Eval2Obj(seg.Value = newPathValue);
                
                IContext prevContext = Segments[index - 1].CachedContext;
                if (!prevContext.TryGetSubcontext(path, out IContext newContext))
                {
                    for (; index < Segments.Count; index++)
                    {
                        seg.Value = Dependency.Null.Instance;
                        seg.CachedContext = null;
                        Value = new ReferenceError(this, Segments.Take(index).Select(s => s.Value).ToList(), prevContext, "Failed to locate sub-context.");
                        Source = null;
                    }
                } else
                    seg.CachedContext = newContext;
            }

            // We should now be at the point of the property at the end of the reference.
            {
                Segment seg = Segments[index];
                IEvaluateable newPathValue = seg.Path.Value;
                object path = Dependency.Helpers.Eval2Obj(seg.Value = newPathValue);
                IContext prevContext = Segments[index - 1].CachedContext;
                if (!prevContext.TryGetProperty(path, out IEvaluateable source))
                {
                    seg.Value = Dependency.Null.Instance;
                    Value = new ReferenceError(this, Segments.Select(s => s.Value).ToList(), prevContext, "Failed to locate source.");
                    Source = null;
                } else if (source is IVariable iv)
                {
                    Source = iv;
                    iv.AddListener(this);
                    Value = iv.Value;
                }
            }
        }

        internal void AddSegment(IEvaluateable path, Mobility m = Mobility.All)
        {
            Segment seg = new Segment(this, Segments.Count, path, m);
            Segments.Add(seg);
        }

        internal class Segment : IDynamicEvaluateable
        {
            public readonly IEvaluateable Path;
            public readonly int Index;
            public readonly Mobility Mobility;
            public readonly Reference HostReference;
            public IContext CachedContext = null;

            IDynamicEvaluateable IDynamicEvaluateable.Parent { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }

            public IEvaluateable Value { get; internal set; }

            public Segment(Reference r, int index, IEvaluateable path, Mobility m = Mobility.All)
            {
                this.HostReference = r;
                this.Index = index;
                this.Mobility = m;
                this.Path = path;
                if (path !=  null)
                {
                    this.Value = path.Value;
                    if (path is IDynamicEvaluateable ide) ide.Parent = this;
                }
            }

            IEvaluateable IDynamicEvaluateable.Update()
            {
                HostReference.Refresh(Index); return null;
            }
        }



        public IEvaluateable Update()
            => Value = (Source == null) ? Dependency.Null.Instance : Source.Value;
        
        private void On_Source_Value_Changed(object sender, ValueChangedArgs<IEvaluateable> e)
            => this.Value = ((IVariable)sender).Value;

    }

}
