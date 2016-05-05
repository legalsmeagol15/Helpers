using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Arithmetic.Text
{
    /// <summary>
    /// Override this class to create new named functions.
    /// </summary>    
    public abstract class NamedFunction
    {
        /// <summary>
        /// Override to return a distinct name for this function.  The base will return the name
        /// of the function's type.
        /// </summary>
        public virtual string Name { get { return this.GetType().Name; } }

        /// <summary>
        /// Override this propery to return the minimum number of arguments upon which this 
        /// function may operate.
        /// </summary>
        public abstract int MinimumArgs { get; }
        /// <summary>
        /// Override this property to return the maximum number of arguments upon which this 
        /// function may operate.
        /// </summary>
        public abstract int MaximumArgs { get; }

        /// <summary>
        /// Override this method to return the results of this function's operation.  Note that the 
        /// arguments will already be checked for null value or incorrect numbers.
        /// </summary>
        /// <param name="args">The arguments upon which this method will operate.</param>
        /// <returns></returns>
        protected abstract object Operate(IList<object> args);

        /// <summary>
        /// Applies the given arguments to this function.  If an incorrect number of arguments is 
        /// provided, throws an exception.
        /// </summary>
        /// <param name="args">The arguments upon which this function will operate, in the form of 
        /// an array.</param>
        /// <returns></returns>
        public object Evaluate(IList<object> args)
        {
            //If args is null, it would still work if the function needs 0 arguments.  Otherwise, 
            //this will throw an exception.
            if (args == null)
            {
                if (MinimumArgs != 0)
                    throw new NullReferenceException("Cannot evaluate function named " + Name + " without arguments.");
                return Operate(new object[0]);
            }

            //Look for incorrect number of args.
            if (args.Count < MinimumArgs)
                throw new ArgumentException("Function " + ToString() + " must have at least " + MinimumArgs
                    + " arguments.");
            if (args.Count > MaximumArgs)
                throw new ArgumentException("Function " + ToString() + " must have no more than "
                    + MaximumArgs + " arguments.");
            if (MinimumArgs > MaximumArgs)
                throw new ArgumentException("Function error - maximum and minimum argument count are reversed.");

            //Arguments appear okay, go ahead and evaluate.
            return Operate(args);
        }

        /// <summary>
        /// Override this method to return the name of the function.  In the base 
        /// method, will return the name of the function.
        /// </summary>        
        public override string ToString()
        {
            if (MaximumArgs < 1) return Name;
            string str = Name + "(";
            int i = 0;
            for (i = 0; i < MinimumArgs; i++)
            {
                str += "arg";
                if (i < MaximumArgs - 1) str += ",";
            }

            for (int j = i; j < MaximumArgs; j++)
            {
                str += "[arg]";
                if (j < (MaximumArgs - 1)) str += ",";
            }
            str += ")";
            return str;
        }

        /// <summary>
        /// Returns true if the other given function has the same name; false if not.
        /// </summary>
        public override bool Equals(object obj)
        {
            NamedFunction other = obj as NamedFunction;
            if (other == null) return false;

            return other.Name == this.Name;
        }

        /// <summary>
        /// Returns the hash code of the function's name.
        /// </summary>        
        public sealed override int GetHashCode()
        {
            return Name.GetHashCode();
        }


        public static IDictionary<string, NamedFunction> GetStandardDictionary()
        {
            Dictionary<string, NamedFunction> returnDict
                = new Dictionary<string, NamedFunction>();
            foreach (Type t in GetTypesInNamespace(Assembly.GetExecutingAssembly(),
                                                    "Arithmetic.Text.NamedFunctions"))
            {
                if (typeof(NamedFunction).IsAssignableFrom(t))
                {
                    NamedFunction func = (NamedFunction)Activator.CreateInstance(t);
                    returnDict.Add(func.Name, func);
                }

            }
            return returnDict;
        }

        private static IEnumerable<Type> GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return assembly.GetTypes().Where(t =>
                    String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal));
        }
    }
}
