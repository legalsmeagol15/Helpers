﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    
    /// <summary>The context in which Variables live, and from which functions can be created.  The DataContext manages access to things I don't 
    /// want to expose on other objects:  Variable dependency graph</summary>
    public abstract class Context 
    {
        
        public readonly Context Parent;
        public readonly string Name;
        protected readonly Dictionary<string, Variable> Variables;
        protected readonly Dictionary<string, Context> Subcontexts;

        
        protected Context(Context parent, string name)
        {
            this.Parent = parent;
            this.Name = name;            
        }

        /// <summary>
        /// Deletes the given <see cref="Variable"/> from this <see cref="Context"/>, if the <see cref="Variable"/> exists on this 
        /// <see cref="Context"/> and has no listeners.
        /// </summary>
        public virtual bool TryDelete(Variable v)
        {
            if (!Variables.TryGetValue(v.Name, out Variable vExisting))
                return false;
            if (!ReferenceEquals(v, vExisting))
                return false;
            if (v.Listeners.Any())
                return false;
            v.Context = null;
            v.Contents = null;
            return true;
        }

        /// <summary>
        /// Deletes the given <see cref="Context"/> from this <see cref="Context"/>, if the sub-context (and all of its sub-contexts) has 
        /// no listeners.
        /// </summary>
        public virtual bool TryDelete(Context sub_ctxt)
        {
            if (!Subcontexts.TryGetValue(sub_ctxt.Name, out Context cExisting))
                return false;
            if (!ReferenceEquals(sub_ctxt, cExisting))
                return false;
            if (HasListeners(sub_ctxt))
                return false;
            return true;

            bool HasListeners(Context ctxt)
            {
                if (ctxt.Variables.Values.Any(v => v.Listeners.Any())) return true;
                if (ctxt.Subcontexts.Values.Any(s => HasListeners(s))) return true;
                return false;
            }
        }

        /// <summary>Default behavior is simply to fail to add a new sub-context.</summary>
        public virtual bool TryAdd(string name, out Context sub_ctxt)        {            sub_ctxt = null;            return false;        }

        /// <summary>
        /// Default behavior is simply to add a new variable with the given name (and to fail if a variable with a matching name already 
        /// exists).
        /// </summary>
        public virtual bool TryAdd(string name, out Variable new_var)
        {
            if (Variables.ContainsKey(name)) { new_var = null; return false; }
            Variables.Add(name, new_var = new Variable(this, name));
            return true;
        }

        public Variable this[string  varName] => Variables[varName];
        
        public bool TryGet(string name, out Context sub_obj) => Subcontexts.TryGetValue(name, out sub_obj);

        public bool TryGet(string name, out Variable sub_val) =>Variables.TryGetValue(name, out sub_val);
        
    }
}
