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
        
        public Context(Context parent, string name)
        {
            this.Parent = parent;
            this.Name = name;            
            this.Variables = new Dictionary<string, Variable>();
            this.Subcontexts = new Dictionary<string, Context>();
        }
        public Variable this[string varName] => Variables[varName];

        public Context Parent { get; }

        public string Name { get; }

        public Variable Declare(string name)
        {
            lock (Variable.ModifyLock)
            {
                if (Variables.ContainsKey(name)) throw new DuplicateVariableException(this, Variables[name], name);
                Variable v = new Variable(this, name);
                Variables.Add(name, v);
                return v;
            }
        }

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

        /// <summary>Returns whether the given child is a descendant of (or identical to) the given parent.</summary>        
        public static bool IsDescendant(Context parent, Context child)
        {
            while (child != null)
            {
                if (child.Equals(parent)) return true;
                child = child.Parent;
            }
            return false;
        }

        protected abstract bool TryCreateContext(string name, out Context sub_ctxt);

        protected internal virtual bool  TryCreateFunction(string rawToken, out Function f) { f = null; return false; }

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

        public class DuplicateVariableException : Exception
        {
            public readonly Context Context;
            public readonly Variable Existing;
            public readonly string Name;
            public DuplicateVariableException(Context context, Variable existing, string name)
                : base("Duplicate variable name: \"" + name + "\".")
            {
                this.Context = context;
                this.Existing = existing;
                this.Name = name;
            }
        }
    }

    
}
