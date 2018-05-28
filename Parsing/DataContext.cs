using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    /// <summary>
    /// The context in which Variables live, and from which functions can be created.  The DataContext manages access to things I don't 
    /// want to expose on other objects:  Variable dependency graph
    /// <para/>It is expected that multiple threads will be accessing the context.  Changing or accessing the variable contents is 
    /// protected by mutexes.
    /// </summary>
    public partial class DataContext
    {

        private readonly Dictionary<string, Variable> _Variables = new Dictionary<string, Variable>();
        public Variable this[string name] { get { lock (this) { return _Variables[name]; } } }
        

        
        /// <summary>
        /// Tries to delete the given Variable, and returns whether the attempt was successful or not.  A variable can be delted only 
        /// if its contents are null, and nothing is listening to it.
        /// </summary>
        public bool TryDelete(Variable v)
        {
            // Delete only occurs when a Variable's contents are set to null, and it has no listeners left.
            if (v.Contents != null) return false;
            lock (this)
            {
                // If Contents is null, there will be no Sources left either.
                if (v._Listeners.Any()) return false;
                _Variables.Remove(v.Name);
                return true;
            }            
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
                return true;
            }            
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


            lock (this)
            {
                if (_Variables.ContainsKey(name))
                {
                    v = _Variables[name];
                    return false;
                }

                v = new Variable(this, name);
                _Variables.Add(name, v);
                return true;
            }
            
           
        }

        /// <summary>The function which determines if a given variable name is valid.</summary>
        public readonly Func<string, bool> IsNameValid = (s) => true;



        
    }


}
