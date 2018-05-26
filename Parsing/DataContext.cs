using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    /// <summary>
    /// The context in which Variables live, and from which functions can be created.
    /// <para/>It is expected that multiple threads will be accessing the context, so access is controlled by mutexes.
    /// </summary>
    public partial class DataContext
    {

        private readonly Dictionary<string, Variable> _Variables = new Dictionary<string, Variable>();
        public Variable this[string name] { get { lock (this) { return _Variables[name]; } } }

        /// <summary>Returns whether a direct dependency relationship exists between the given source and listener.</summary>
        public bool DependencyExists(Variable source, Variable listener)
        {
            //TODO:  add functionality to return non-direct dependency?
            lock (this) { return Sources.ContainsKey(listener) || Listeners.ContainsKey(source); }
        }

        /// <summary>Adds the given Variable to this data context.  Returns whether the variable was successful or not.  Add can fail 
        /// if a Variable by the same name already exists, or if the Variable's name is invalid.</summary>
        public bool Add(Variable v)
        {
            lock (this)
            {
                if (_Variables.ContainsKey(v.Name)) return false;
                if (!IsNameValid(v.Name)) return false;
                _Variables.Add(v.Name, v);
                Sources[v] = DetectSources(v);
                Listeners[v] = new HashSet<Variable>();
            }
            return true;
        }

        /// <summary>
        /// Tries to delete the given Variable, and returns whether the attempt was successful or not.  A variable can be delted only 
        /// if its contents are null, and nothing is listening to it.
        /// </summary>
        public bool TryDelete(Variable v)
        {
            // Delete only occurs when a Variable's contents are set to null, and it has no listeners left.
            if (v._Contents != null) return false;
            lock (this)
            {
                // If Contents is null, there will be no Sources left either.
                ISet<Variable> listeners = Listeners[v];
                if (listeners.Count != 0) return false;
                _Variables.Remove(v.Name);
            }
            return true;
        }

        /// <summary>
        /// Attempts the get the named variable and store in the out reference.
        /// </summary>
        /// <param name="name">The name of the variable to retrieve from this data context.</param>
        /// <param name="v">The variable retrieved from this data context.  If no variable matched the given name, returns null.
        /// </param>
        /// <returns>Returns true if a variable matching the given name existed; otherwise, returns false.</returns>
        public bool TryGet(string name, out Variable v)
        {
            lock (this)
            {
                if (!_Variables.ContainsKey(name))
                {
                    v = null;
                    return false;
                }
                v = _Variables[name];
            }
            return true;
        }

        /// <summary>
        /// Attempts to create the variable within this context.  If the variable does not 
        /// already exist and its name is valid, returns true, with the 'out' variable being 
        /// the new variable added to this context.  If the variable already exists, the 'out' 
        /// variable will be the existing variable, and returns false.  If the variable cannot 
        /// be added, the 'out' variable will be null, and returns false.
        /// </summary>
        /// <param name="name">The name of the variable to create.</param>
        /// <param name="v">If the variable does not exist, returns the variable newly created 
        /// and added to this context.  If the variable does exist, returns the variable.  If 
        /// the variable does not exist and cannot be created with the given name, returns null.
        /// </param>
        /// <returns>True if the variable is created and added, otherwise false.</returns>
        public bool TryAdd(string name, out Variable v)
        {
            if (!IsNameValid(name))
            {
                v = null;
                return false;
            }

            v = new Variable(this, name);

            lock (this)
            {
                if (_Variables.ContainsKey(name))
                {
                    v = _Variables[name];
                    return false;
                }
                _Variables.Add(name, v);
                Sources[v] = new HashSet<Variable>();
                Listeners[v] = new HashSet<Variable>();
            }




            return true;
        }

        /// <summary>The function which determines if a given variable name is valid.</summary>
        public readonly Func<string, bool> IsNameValid = (s) => true;



        
    }


}
