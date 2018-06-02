using System;
using System.Collections;
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
    public partial class DataContext : IContext
    {
        public string Name => "Root";

        string IContext.Name => throw new NotImplementedException();

        // TODO:  Function Factory should be a member of a DataContext.

        private readonly Dictionary<string, IContext> _Objects = new Dictionary<string, IContext>();
        private readonly Dictionary<string, Variable> _Variables = new Dictionary<string, Variable>();
        public Variable this[string name] { get { lock (this) { return _Variables[name]; } } }
        

        
        /// <summary>
        /// Tries to delete the given Variable, and returns whether the attempt was successful or not.  A variable can be deleted only 
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

        

        public bool TryDelete(IContext obj)
        {
            // If any variables within the to-be-deleted subcontext have listeners, this method should return false.
            throw new NotImplementedException();
        }

        /// <summary>Attempts the get the named variable and store in the out reference.</summary>
        /// <param name="name">The name of the variable to retrieve from this data context.</param>
        /// <param name="v">The variable retrieved from this data context.  If no variable matched the given name, returns null.
        /// </param>
        /// <returns>Returns true if a variable matching the given name existed; otherwise, returns false.</returns>
        public bool TryGet(string name, out Variable v)
        {
            lock (this)
            {
                if (!_Variables.ContainsKey(name)) { v = null; return false; }
                v = _Variables[name];
                return true;
            }
        }

        /// <summary>Attempts to get the named object and store in the out reference.</summary>
        /// <param name="name">The name of the object to retrieve.</param>
        /// <param name="subContext">The object retrieved.  If lookup was unsuccessful, this reference will be null.</param>
        /// <returns>Returns true if the object lookup was successful, or false if not.</returns>
        public bool TryGet(string name, out IContext subContext)
        {
            lock (this)
            {
                if (!_Objects.ContainsKey(name)) { subContext = null; return false; }
                subContext = _Objects[name];
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



        IEnumerator<IContext> IEnumerable<IContext>.GetEnumerator() => this._Objects.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this._Objects.Values.GetEnumerator();

        bool IContext.TryGet(string key, out Variable v)
        {
            throw new NotImplementedException();
        }

        bool IContext.TryGet(string key, out IContext subContext)
        {
            throw new NotImplementedException();
        }

        bool IContext.TryAdd(string name, out Variable v)
        {
            throw new NotImplementedException();
        }

        IEvaluateable IEvaluateable.Evaluate() => throw new InvalidOperationException("The root DataContext cannot be evaluated.");

        /// <summary>The function which determines if a given variable name is valid.</summary>
        public readonly Func<string, bool> IsNameValid = (s) => true;



        
    }


}
