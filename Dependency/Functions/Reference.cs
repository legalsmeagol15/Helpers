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
                // Making this static forces use to work through the traversed leg.
               
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
                =>Update(this) ? Dependency.Variables.Update.UniversalSet : null;

            //public override string ToString() => "..." + Value.ToString() + "...";
        }

    }
    //internal sealed class Reference : IFunction, IDisposable, IReference
    //{
    //    // Cannot implement Function directly because the count of Inputs can change.

    //    // This is not intended to be a parent of anything else.

    //    public readonly bool IsAbsolute;
    //    public ISyncUpdater Parent
    //    {
    //        get;
    //        set;
    //    }
    //    private readonly Step[] _Steps;

    //    public IEvaluateable Value { get; private set; } = Dependency.Null.Instance;

    //    private Reference(string[] steps, bool isAbsolute)
    //    {
    //        this._Steps = new Step[steps.Length + 1];
    //        this._Steps[0] = new Step(null); // for the origin
    //        for (int i = 0; i < steps.Length; i++) this._Steps[i + 1] = new Step(steps[i]);
    //        this.IsAbsolute = isAbsolute;
    //    }
    //    public static bool TryCreate(IContext defaultOrigin, string token, out Reference reference)
    //    {
    //        string[] splits = token.Split('.');
    //        for (int i = 1; i < splits.Length; i++)
    //            if (string.IsNullOrWhiteSpace(splits[i])) { reference = null; return false; }
    //        bool isAbsolute = string.IsNullOrWhiteSpace(splits[0]);
    //        reference = new Reference(splits, isAbsolute);
    //        reference._Steps[0].Context = defaultOrigin;
    //        reference._Steps[0].Input = null;
    //        return true;
    //    }
    //    public static bool TryCreate(IEvaluateable originInput, string token, out Reference reference)
    //    {
    //        string[] splits = token.Split('.');
    //        for (int i = 1; i < splits.Length; i++)
    //            if (string.IsNullOrWhiteSpace(splits[i])) { reference = null; return false; }
    //        bool isAbsolute = string.IsNullOrWhiteSpace(splits[0]);
    //        reference = new Reference(splits, isAbsolute);
    //        reference._Steps[0].Context = null;
    //        reference._Steps[0].Input = originInput;
    //        return true;
    //    }

    //    ITrueSet<IEvaluateable> ISyncUpdater.Update(Dependency.Variables.Update caller, ISyncUpdater updatedChild, ITrueSet<IEvaluateable> updatedDomain)

    //    {
    //        // Find the index of the child that changed.
    //        int stepIdx = 0;
    //        for (; stepIdx < _Steps.Length && !ReferenceEquals(_Steps[stepIdx].Input, updatedChild); stepIdx++) ;
    //        Debug.Assert(stepIdx < _Steps.Length);


    //        Variables.Update.StructureLock.EnterUpgradeableReadLock();
    //        IEvaluateable newValue = null;
    //        try
    //        {
    //            // Step through all the pre-head steps, check if a reference error occurred. 
    //            for (; stepIdx < _Steps.Length - 1; stepIdx++)
    //            {
    //                IContext ctxt = null;
    //                Step step = _Steps[stepIdx];
    //                Step nextStep = _Steps[stepIdx + 1];

    //                // What is the context at this step?  If there is variable input, the context will 
    //                // be the evaluation of the variable (if it can be made into a context).
    //                if (step.Input != null)
    //                {
    //                    IEvaluateable inputValue = step.Input.Value;

    //                    // If the input's value hasn't changed (and the input's value is a struct or literal) then no change 
    //                    // will occur later down the line.
    //                    if (inputValue.Equals(step.Context) && !inputValue.GetType().IsClass) return null;

    //                    // Is the input's new value a valid context?  If not, this evaluates to an error.
    //                    ctxt = inputValue as IContext;
    //                    if (ctxt == null)
    //                    {
    //                        string msg = (stepIdx == 0) ? "Origin context was invalid."
    //                                                    : "Evaluation of segment " + step.String + " is invalid context (" + inputValue.GetType().Name + ").";
    //                        newValue = new ReferenceError(msg, _Steps[0].Context, IsAbsolute, _Steps.Take(stepIdx + 1).Select(s => s.String).ToArray());
    //                        break;
    //                    }
    //                }
    //                // Since we have no variable input, if there's no context for this step, re-evaluate the whole thing.
    //                else if (step.Context == null)
    //                {
    //                    stepIdx = 0;
    //                    continue;
    //                }
    //                // Otherwise, just stand in the cached context for this step.
    //                else
    //                    ctxt = step.Context;

    //                // If this context has a matching subcontext, the next step's subcontext must be that.
    //                IContext newNextContext = null;
    //                IEvaluateable newNextInput = null;

    //                if (ctxt.TryGetSubcontext(nextStep.String, out IContext next_ctxt))
    //                {
    //                    newNextContext = next_ctxt;
    //                    newNextInput = null;
    //                }
    //                else if (!ctxt.TryGetProperty(nextStep.String, out IEvaluateable prop))
    //                {
    //                    newValue = new ReferenceError("No matching property for \"" + step.String + "\".",
    //                        _Steps[0].Context, IsAbsolute, _Steps.Take(stepIdx + 1).Select(s => s.String).ToArray());
    //                    break;
    //                }
    //                else if (prop.Equals(nextStep.Input))
    //                    return null;
    //                else if (prop is IAsyncUpdater)
    //                {
    //                    newNextContext = null;
    //                    newNextInput = prop;
    //                }
    //                else if (prop is ISyncUpdater idi)
    //                {
    //                    newValue = new ReferenceError("Cannot reference any dynamic property except variables",
    //                        _Steps[0].Context, IsAbsolute, _Steps.Take(stepIdx + 1).Select(s => s.String).ToArray());
    //                    break;
    //                }
    //                else if (prop.GetType().IsClass)
    //                {
    //                    newValue = new ReferenceError("References allowed only for literal values and variables",
    //                        _Steps[0].Context, IsAbsolute, _Steps.Take(stepIdx + 1).Select(s => s.String).ToArray());
    //                    break;
    //                }
    //                else
    //                {
    //                    newNextContext = null;
    //                    newNextInput = prop;

    //                }

    //                // Apply the new Input and Context
    //                if (newNextInput != null && newNextInput.Equals(nextStep.Input)) return null;
    //                if (newNextContext != null && newNextContext.Equals(nextStep.Context)) return null;
    //                if (nextStep.Input is IAsyncUpdater ivi)
    //                    ivi.RemoveListener(this);
    //                if (newNextInput is IAsyncUpdater new_ivi)
    //                    new_ivi.AddListener(this);
    //                _Steps[stepIdx + 1].Input = newNextInput;
    //                _Steps[stepIdx + 1].Context = newNextContext;
    //            }
    //        }
    //        finally { Variables.Update.StructureLock.ExitUpgradeableReadLock(); }


    //        // For the last step, the newValue will either be the Input's Value, or it will be a wrapped IContext.
    //        if (newValue == null)
    //        {
    //            Step lastStep = _Steps[stepIdx];
    //            if (lastStep.Input != null) newValue = lastStep.Input.Value;
    //            else newValue = new ContextWrapper(lastStep.Context);
    //        }

    //        if (newValue.Equals(Value)) return null;
    //        Value = newValue;
    //        return updatedDomain;
    //    }

    //    /// <summary>
    //    /// Refactors the reference to change the given item's name.  Returns whether a change was 
    //    /// made or not.
    //    /// </summary>        
    //    internal bool Refactor(IContext context, string newName)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    IList<IEvaluateable> IFunction.Inputs => Inputs.ToList();
    //    private IEnumerable<IEvaluateable> Inputs => _Steps.Where(s => s.Input != null).Select(s => s.Input);
    //    IEnumerable<IEvaluateable> IReference.GetComposers() => Inputs;

    //    private struct ContextWrapper : IEvaluateable, IContext
    //    {
    //        public readonly IContext Context;
    //        public ContextWrapper(IContext context) { this.Context = context; }

    //        IEvaluateable IEvaluateable.Value => this;
    //        bool IContext.TryGetProperty(string path, out IEvaluateable source) => Context.TryGetProperty(path, out source);
    //        bool IContext.TryGetSubcontext(string path, out IContext ctxt) => Context.TryGetSubcontext(path, out ctxt);
    //        public override bool Equals(object obj) => (obj is ContextWrapper other) && other.Context.Equals(Context);
    //        public override int GetHashCode() => throw new NotImplementedException();
    //    }

    //    private struct Step
    //    {
    //        public readonly string String;
    //        public IContext Context;
    //        public IEvaluateable Input;
    //        public Step(string str) { this.String = str; Context = default(IContext); Input = default(IEvaluateable); }
    //        public override string ToString() => String ?? "";
    //    }

    //    public override string ToString() => string.Join(".", _Steps);

    //    #region Reference IDisposable Support

    //    // This is implemented if the Reference's host changes its content, this Reference will be garbage 
    //    // collected because the HeadProperty variables maintain only weak references to this.  However, 
    //    // those weak references are maintained in a WeakReferenceSet, so to avoid leaving the weak 
    //    // reference as a memory leak, go ahead and use the fact of this Reference's disposal to keep all 
    //    // the sources clean.

    //    private bool disposedValue = false; // To detect redundant calls

    //    void Dispose(bool disposing)
    //    {
    //        if (!disposedValue)
    //        {
    //            foreach (Step step in _Steps) if (step.Input is IAsyncUpdater ivi) ivi.RemoveListener(this);
    //            disposedValue = true;
    //        }
    //    }

    //    // This code added to correctly implement the disposable pattern.
    //    void IDisposable.Dispose() => Dispose(true);

    //    #endregion

    //}

}
