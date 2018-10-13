using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    

    [Serializable]
    public abstract class Context 
    {
        internal Dictionary<string, Variable> Variables;
        internal Dictionary<string, Context> Subcontexts;
        internal Function.Factory Functions;

        public Context(Context parent, string name, Function.Factory functions)
        {
            this.Parent = parent;
            this.Name = name;
            this.Functions = (parent == null) ? (functions ?? Function.Factory.StandardFactory) : parent.Functions;
            this.Variables = new Dictionary<string, Variable>();
            this.Subcontexts = new Dictionary<string, Context>();
        }
        public Variable this[string varName] => Variables[varName];

        public Context Parent { get; }

        public string Name { get; }

        internal bool TryAddAsContext(string name, out Context c, out Variable v)
        {
            if (Variables.TryGetValue(name, out Variable _) || Subcontexts.TryGetValue(name, out Context _)) { c = null; v = null; return false; }
            if (!TryCreateVariableContext(name, out v)) { c = null; return false; }
            if (v == null)
                throw new InvalidOperationException(string.Format("Returned %s cannot be null.", typeof(Variable).Name));
            if (v.Context == null)
                throw new InvalidOperationException(string.Format("To create a %s which also functions as a %s, the %s Context property cannot be null.", typeof(Variable).Name, typeof(Context).Name, typeof(Variable).Name));
            if (v.Name != v.Context.Name)
                throw new InvalidOperationException(string.Format("To create a %s which also functions as a %s, the %s.Name must equal the %s.Name cannot be null.", typeof(Variable).Name, typeof(Context).Name, typeof(Variable).Name, typeof(Context).Name) );
            Variables.Add(name, v);
            Subcontexts.Add(name, c = v.Context);
            return true;
        }
        protected abstract bool TryCreateVariableContext(string name, out Variable v);

        internal bool TryAddWithinContext(string name, out Variable v)
        {
            if (Variables.TryGetValue(name, out v)) return false;
            if (!TryCreateVariable(name, out v)) return false;
            if (v == null)
                throw new InvalidOperationException(string.Format("Returned %s cannot be null.", typeof(Variable).Name));
            Variables.Add(name, v);
            return true;
        }
        protected abstract bool TryCreateVariable(string name, out Variable v);

        internal bool TryAddContext(string name, out Context sub_ctxt)
        {
            if (Subcontexts.TryGetValue(name, out sub_ctxt)) return false;
            if (!TryCreateContext(name, out sub_ctxt)) return false;
            if (sub_ctxt == null)
                throw new InvalidOperationException(string.Format("Returned %s cannot be null.", typeof(Context).Name));
            Subcontexts.Add(name, sub_ctxt);
            return true;
        }
        protected abstract bool TryCreateContext(string name, out Context sub_ctxt);

        internal bool  TryCreateFunction(string rawToken, out Function f) => Functions.TryCreateFunction(rawToken, out f);

        internal bool TryDelete(Variable v)
        {
            if (!Variables.TryGetValue(v.Name, out Variable vExisting))
                return false;
            if (!ReferenceEquals(v, vExisting))
                return false;      
            // Do NOT check that there are no listeners.  This is checked when orphans are identified.
            v.Context = null;
            v.Contents = null;
            return true;
        }
        internal bool TryDelete(Context ctxt)
        {
            throw new NotImplementedException();
        }


        internal bool TryGet(string name, out Variable v) => Variables.TryGetValue(name, out v);

        internal bool TryGet(string name, out Context sub_ctxt) => Subcontexts.TryGetValue(name, out sub_ctxt);
    }

    
    //    /// <summary>
    //    /// Deletes the given <see cref="Variable"/> from this <see cref="Context"/>, if the <see cref="Variable"/> exists on this 
    //    /// <see cref="Context"/> and has no listeners.
    //    /// </summary>
    //    public virtual bool TryDelete(Variable v)
    //    {
    //        if (!Variables.TryGetValue(v.Name, out Variable vExisting))
    //            return false;
    //        if (!ReferenceEquals(v, vExisting))
    //            return false;
    //        if (v.Listeners.Any())
    //            return false;
    //        v.Context = null;
    //        v.Contents = null;
    //        return true;
    //    }

    //    /// <summary>
    //    /// Deletes the given <see cref="Context"/> from this <see cref="Context"/>, if the sub-context (and all of its sub-contexts) has 
    //    /// no listeners.
    //    /// </summary>
    //    public virtual bool TryDelete(Context sub_ctxt)
    //    {
    //        if (!Subcontexts.TryGetValue(sub_ctxt.Name, out Context cExisting))
    //            return false;
    //        if (!ReferenceEquals(sub_ctxt, cExisting))
    //            return false;
    //        if (HasListeners(sub_ctxt))
    //            return false;
    //        return true;

    //        bool HasListeners(Context ctxt)
    //        {
    //            if (ctxt.Variables.Values.Any(v => v.Listeners.Any())) return true;
    //            if (ctxt.Subcontexts.Values.Any(s => HasListeners(s))) return true;
    //            return false;
    //        }
    //    }

    //    /// <summary>Default behavior is simply to fail to add a new sub-context.</summary>
    //    public virtual bool TryAdd(string name, out Context sub_ctxt)        {            sub_ctxt = null;            return false;        }

    //    /// <summary>
    //    /// Default behavior simply fails.
    //    /// </summary>
    //    public virtual bool TryAdd(string name, out Variable new_var)
    //    {
    //        new_var = null;
    //        return false;            
    //    }

    //    public Variable this[string  varName] => Variables[varName];

    //    public bool TryGet(string name, out Context sub_obj)
    //    {
    //        if (Subcontexts == null) { sub_obj = null; return false; }
    //        return Subcontexts.TryGetValue(name, out sub_obj);
    //    }

    //    public bool TryGet(string name, out Variable sub_val) => Variables.TryGetValue(name, out sub_val);

    //    /// <summary>Forces the Variables of this <see cref="Context"/>, and those of all sub-contexts, to update their values.</summary>
    //    public void Refresh()
    //    {
    //        foreach (Variable v in Variables.Values)
    //            if (v.Listeners.Count == 0)
    //                v.Update(out ISet<Variable> _);
    //        foreach (Context c in Subcontexts.Values)
    //            c.Refresh();
    //    }

    //    public override string ToString() => Name;
    //}
}
