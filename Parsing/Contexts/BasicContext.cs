using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Contexts
{
    public enum SearchPolicies
    {
        ANCESTORS_VARIABLES = 1,
        ANCESTORS_CONTEXTS = 2
    }
    

    /// <summary>
    /// The basic context allowing for addition and retrieval of variables and sub-contexts by name.
    /// <para/>The <see cref="BasicContext"/> is backed by O(1) dictionaries for variable and sub-context operations.  The user 
    /// is required to derive from this class to specify whether a sub-context or variable with a given name is allowed to be 
    /// created.  Note that this <seealso cref="IContext"/> class is heavy-weight, so if the desire is for a more lightweight 
    /// implementation, the user should implement IContext directly.
    /// </summary>
    [Serializable]
    public abstract class BasicContext : IContext
    {
        public SearchPolicies SearchPolicy = SearchPolicies.ANCESTORS_VARIABLES;

        public IContext Parent { get; }

        public string Name { get; }

        internal Dictionary<string, Variable> InternalVariables;
        internal Dictionary<string, IContext> InternalSubcontexts;
        /// <summary>Returns the indicated subcontext.  Call like this:  <para/><code>context.Subcontexts["contextName"]</code></summary>
        public readonly ContextDictionary Subcontexts;
        /// <summary>Returns the indicated variable.  Call like this:  <para/><code>context.Variables["variableName"]</code></summary>
        public readonly VariableDictionary Variables;

        public Expression.DeletionStatus DeletionStatus { get; set; } 
            = Expression.DeletionStatus.ALLOW_DELETION;


        public BasicContext(IContext parent, string name)
        {
            this.Parent = parent;
            this.Name = name;
            this.InternalVariables = new Dictionary<string, Variable>();
            this.InternalSubcontexts = new Dictionary<string, IContext>();
            this.Subcontexts = new ContextDictionary(this);
            this.Variables = new VariableDictionary(this);            
        }
        public Variable this[string varName] => InternalVariables[varName];

        IEnumerable<Variable> IContext.GetVariables => InternalVariables.Values;

        IEnumerable<IContext> IContext.GetContexts => InternalSubcontexts.Values;

        Variable IContext.this[string name] => InternalVariables[name];

        bool IContext.TryGet(string name, out IContext ctxt)
        {
            if (InternalSubcontexts.TryGetValue(name, out ctxt)) return true;
            if ((SearchPolicy & SearchPolicies.ANCESTORS_CONTEXTS) != 0)
            {
                IContext focus = this.Parent;
                while (focus != null)
                {
                    if (focus.TryGet(name, out ctxt)) return true;
                    focus = focus.Parent;
                }
            }
            return false;             
        }

        bool IContext.TryGet(string name, out Variable v)
        {
            if (InternalVariables.TryGetValue(name, out v)) return true;
            if ((SearchPolicy & SearchPolicies.ANCESTORS_VARIABLES) != 0)
            {
                IContext focus = this.Parent;
                while (focus != null)
                {
                    if (focus.TryGet(name, out v)) return true;
                    focus = focus.Parent;
                }
            }
            return false;
        }

        bool IContext.TryAdd(string name, out Variable variable)
        {
            if (InternalVariables.ContainsKey(name)) { variable = null; return false; }
            if (!TryCreateVariable(name, out variable)) return false;
            InternalVariables.Add(name, variable);
            return true;
        }

        protected abstract bool TryCreateVariable(string name, out Variable v);

        bool IContext.TryAdd(string name, out IContext context)
        {
            if (InternalSubcontexts.ContainsKey(name)) { context = null; return false; }
            if (!TryCreateSubcontext(name, out context)) return false;
            InternalSubcontexts.Add(name, context);
            return true;
        }
        protected abstract bool TryCreateSubcontext(string name, out IContext c);

        bool IContext.TryDelete(Variable variable)
        {
            // The given variable might not be the variable linked to the name according to this context.
            string name = variable.Name;
            if (!InternalVariables.TryGetValue(name, out Variable v)) return false;
            if (!ReferenceEquals(variable, v)) return false;
            if (variable.DeletionStatus != Expression.DeletionStatus.ALLOW_DELETION) return false;
            return InternalVariables.Remove(name);
        }

        bool IContext.TryDelete(IContext context)
        {
            // The given context might not be the subcontext associated with the name per this BasicContext.
            String name = context.Name;
            if (!InternalSubcontexts.TryGetValue(name, out IContext c)) return false;
            if (!ReferenceEquals(context, c)) return false;
            if (c.DeletionStatus != Expression.DeletionStatus.ALLOW_DELETION) return false;
            return InternalSubcontexts.Remove(name);
        }

        bool IContext.TryCreateFunction(string token, out Function f) { f = null; return false; }
    }
}
