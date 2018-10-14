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
        internal Dictionary<string, Variable> InternalVariables;
        internal Dictionary<string, Context> InternalSubcontexts;
        
        public Context(Context parent, string name)
        {
            this.Parent = parent;
            this.Name = name;            
            this.InternalVariables = new Dictionary<string, Variable>();
            this.InternalSubcontexts = new Dictionary<string, Context>();
            this.Subcontexts = new SubcontextDictionary(this);            
        }
        public Variable this[string varName] => InternalVariables[varName];

        /// <summary>Returns the indicated subcontext.  Call like this:  <para/><code>context.Subcontext["contextName"]</code></summary>
        public readonly SubcontextDictionary Subcontexts; // Don't publicly expose the real Subcontexts dictionary.

        public Context Parent { get; }

        public string Name { get; }

        public Variable Declare(string name, string contents = "")
        {
            lock (Variable.ModifyLock)
            {
                if (InternalVariables.ContainsKey(name)) throw new DuplicateVariableException(this, InternalVariables[name], name);
                Variable v = new Variable(this, name);
                if (contents != "") v.Contents = contents;
                InternalVariables.Add(name, v);
                return v;
            }
        }

        internal bool TryAddAsContext(string name, out Context c, out Variable v)
        {
            if (InternalVariables.TryGetValue(name, out Variable _) || InternalSubcontexts.TryGetValue(name, out Context _)) { c = null; v = null; return false; }
            if (!TryCreateVariableContext(name, out v)) { c = null; return false; }
            if (v == null)
                throw new InvalidOperationException(string.Format("Returned %s cannot be null.", typeof(Variable).Name));
            if (v.Context == null)
                throw new InvalidOperationException(string.Format("To create a %s which also functions as a %s, the %s Context property cannot be null.", typeof(Variable).Name, typeof(Context).Name, typeof(Variable).Name));
            if (v.Name != v.Context.Name)
                throw new InvalidOperationException(string.Format("To create a %s which also functions as a %s, the %s.Name must equal the %s.Name cannot be null.", typeof(Variable).Name, typeof(Context).Name, typeof(Variable).Name, typeof(Context).Name) );
            InternalVariables.Add(name, v);
            InternalSubcontexts.Add(name, c = v.Context);
            return true;
        }
        protected abstract bool TryCreateVariableContext(string name, out Variable v);

        internal bool TryAddWithinContext(string name, out Variable v)
        {
            if (InternalVariables.TryGetValue(name, out v)) return false;
            if (!TryCreateVariable(name, out v)) return false;
            if (v == null)
                throw new InvalidOperationException(string.Format("Returned %s cannot be null.", typeof(Variable).Name));
            InternalVariables.Add(name, v);
            return true;
        }
        protected abstract bool TryCreateVariable(string name, out Variable v);

        internal bool TryAddContext(string name, out Context sub_ctxt)
        {
            if (InternalSubcontexts.TryGetValue(name, out sub_ctxt)) return false;
            if (!TryCreateContext(name, out sub_ctxt)) return false;
            if (sub_ctxt == null)
                throw new InvalidOperationException(string.Format("Returned %s cannot be null.", typeof(Context).Name));
            InternalSubcontexts.Add(name, sub_ctxt);
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
            if (!InternalVariables.TryGetValue(v.Name, out Variable vExisting))
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


        internal bool TryGet(string name, out Variable v) => InternalVariables.TryGetValue(name, out v);

        internal bool TryGet(string name, out Context sub_ctxt) => InternalSubcontexts.TryGetValue(name, out sub_ctxt);

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

        /// <summary>The dictionary of subcontexts within a context, keyed by context name.  Subcontexts can be get, but not set.</summary>
        [Serializable]
        public class SubcontextDictionary : IEnumerable<Context>
        {
            private readonly Context _Context;
            public SubcontextDictionary(Context context) { this._Context = context; }

            public Context this[string key] { get => _Context.InternalSubcontexts[key]; }

            public IEnumerator<Context> GetEnumerator() => _Context.InternalSubcontexts.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    
}
