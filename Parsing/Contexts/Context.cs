using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    
    /// <summary>The context in which Variables live, and from which functions can be created.  The DataContext manages access to things I don't 
    /// want to expose on other objects:  Variable dependency graph</summary>
    public partial class Context : IContext, IEvaluateable
    {
        /// <summary>The name of this context.</summary>
        public string Name { get; protected set; }

        /// <summary>The function which determines if a given variable name is valid.</summary>
        public readonly Func<string, bool> IsVariableNameValid;

        protected readonly Dictionary<string, Variable> Variables = new Dictionary<string, Variable>();
        protected readonly Dictionary<string, Context> SubContexts = new Dictionary<string, Context>();

        public Context(string name, Func<string, bool> variableNameValidator  = null)
        {
            IsVariableNameValid = variableNameValidator ?? (s => true);
            this.Name = name;
        }


        /// <summary>Tries to add the described Variable to this context.</summary>
        /// <param name="key">The Variable's name.</param>
        /// <param name="v">Out.  The new Variable returned.  If the Variable could not be added, this value will be null.</param>
        /// <returns>Returns true if add was successful, false if it was not.</returns>
        public virtual bool TryAdd(string name, out Variable v)
        {            
            if (!IsVariableNameValid(name))
            {
                v = null;
                return false;
            }
            else if (Variables.ContainsKey(name))
            {
                v = Variables[name];
                return false;
            }
            else
            {
                v = new Variable(this, name);
                Variables.Add(name, v);
                return true;
            }            
        }
        

        /// <summary>
        /// Tries to delete the given Variable, and returns whether the attempt was successful or not.  A variable can be deleted only 
        /// if its contents are null, and nothing is listening to it.
        /// </summary>
        public virtual bool TryDelete(Variable v)
        {
            // Delete only occurs when a Variable's contents are set to null, and it has no listeners left.
            if (v.Contents != null) return false;

            // If Contents is null, there will be no Sources left either.
            if (v._Listeners.Any()) return false;
            Variables.Remove(v.Name);
            return true;
        }

        /// <summary>Tries to retrieve the indicated property variable.</summary>
        /// <param name="key">The property variable's name.</param>
        /// <param name="v">Out.  The property variable to be returned.  If no variable matched the given name, this will be null.</param>
        /// <returns>Returns true if lookup was successful, false if it was not.</returns>
        public virtual bool TryGet(string name, out Variable v)
        {
            if (!Variables.ContainsKey(name)) { v = null; return false; }
            v = Variables[name];
            return true;
        }


        /// <summary>Attempts to get the named object and store in the out reference.</summary>
        /// <param name="name">The name of the object to retrieve.</param>
        /// <param name="subContext">The object retrieved.  If lookup was unsuccessful, this reference will be null.</param>
        /// <returns>Returns true if the object lookup was successful, or false if not.</returns>
        public virtual bool TryGet(string name, out Context subContext)
        {
            if (!SubContexts.ContainsKey(name)) { subContext = null; return false; }
            subContext = SubContexts[name];
            return true;
        }

        bool IContext.Delete(IEvaluateable var) => throw new NotImplementedException();


        public IEvaluateable Evaluate() => throw new InvalidOperationException("A context cannot be evaluated, but it can be stored alongside things which can.");



        public bool TryGet(string key, out IContext sub)
        {
            bool result = TryGet(key, out Context s);
            sub = s;
            return result;
        }

        bool IContext.TryGet(string key, out IEvaluateable var)
        {
            bool result = TryGet(key, out Variable v);
            var = v;
            return result;
        }

        IEnumerator<IContext> IEnumerable<IContext>.GetEnumerator() => this.SubContexts.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.SubContexts.Values.GetEnumerator();
    }
}
