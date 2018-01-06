using System;
using System.Collections.Generic;
using Mathematics.Functions;
using System.Linq;

namespace Parsing
{



    /// <summary>
    /// An object which represents a variable whose value may change.  Variables have associated names and the update of their stored 
    /// values is handled by a Manager.
    /// </summary>
    public class Variable : IVariable<object> //, IDifferentiable<object, object> //, ICacheValue
    {
        /// <summary>
        /// The count of Formulas which reference this variable.  When the count of references is 0, the Variable will be removed from 
        /// its containing Manager.
        /// </summary>
        public uint References { get; set; } = 0;

        /// <summary>The name of the Variable, normalized according to the rules of its context.</summary>
        public readonly string Name;


        /// <summary>The contents of the Variable, which may be a Formula or some literal.</summary>
        public object Contents { get; set; } = null;

        /// <summary>The current cached value of this Variable.</summary>
        public object Value { get; private set; } = null;

        /// <summary>Updates and returns the cached value of this Variable.  If Contents is changed, the value of the variable will not 
        /// be updated until Update() is called.</summary>        
        public object Update() => (Contents is Formula f) ? Value = f.Update() : Value = Contents;

        ///// <summary>
        ///// The DataContext associated with this Variable.
        ///// </summary>
        //public DataContext Context { get; private set; }

        /// <summary>Creates a new Variable with the given name.</summary>
        /// <param name="name">The name of the Variable.  No normalization or validation is done for the given name.</param>
        /// <param name="context">The DataContext referenced by the Variable.</param>
        private Variable(string name, DataContext context)
        {
            Name = name;
            //Context = context;
        }





        /// <summary>
        /// Manages the Variables for a DataContext, keeping Variables accessible in O(1) time by their names.  Uses threading to manage 
        /// the update of Variables' values.
        /// </summary>
        /// <remarks>Note:  the Manager must be nested in Variable so it can access the private Variable constructor.</remarks>
        internal class Manager
        {
            private static Func<string, bool> _StandardNameValidator = (s) => true;
            private static Func<string, string> _StandardNameNormalizer = (s) => s.ToLower();
            internal const string StandardVariablePattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";


            /// <summary>The regular expression pattern that describes variable names for this manager.</summary>
            public readonly string VariablePattern = StandardVariablePattern;


            /// <summary>Returns the normalized form of the given variable name.</summary>
            public readonly Func<string, string> GetNormalized;


            /// <summary>Returns whether the given variable name is valid.  A name is valid if it is 
            /// normalizeable to a valid name as well.</summary>
            public readonly Func<string, bool> IsValid;


            /// <summary>
            /// A reference to the Context is maintained so the manager can pass that reference to new variables created.  TODO:  needed?
            /// </summary>
            public DataContext Context { get; private set; }
            



            /// <summary>The hash table that associates variables to their names.</summary>
            private Dictionary<string, Variable> _Table = new Dictionary<string, Variable>();


            /// <summary>
            /// Creates a new Variable Manager.
            /// </summary>
            /// <param name="nameValidator">Optional.  The function that specifies whether the given Variable name is valid.  Note that a 
            /// valid name may not be in normalized form.  If omitted or given null, uses the standard Variable name validator (which 
            /// always returns true).</param>
            /// <param name="nameNormalizer">Optional.  The function that returns the normalized form of the given Variable name.  If 
            /// omitted or given null, uses the standard Variable name normalizer (which simply converts Variable names to lower 
            /// case).</param>
            /// <param name="context"></param>
            public Manager(DataContext context, Func<string, bool> nameValidator = null, Func<string, string> nameNormalizer = null)
            {
                Context = context;
                GetNormalized = nameNormalizer ?? _StandardNameNormalizer;
                IsValid = nameValidator ?? _StandardNameValidator;
            }


            /// <summary>Gets or sets the variable associated with the given (normalized) name.</summary>
            public Variable this[string name]
            {
                get => _Table[GetNormalized(name)];
                set => _Table[GetNormalized(name)] = value;
            }


            /// <summary>
            /// Returns the Variable associated with the normalization of the given '<paramref name="name"/>', if it exists in this 
            /// Manager.  If it does, a reference to the variable will be returned in the '<paramref name="v"/>' out argument.  If not, 
            /// the '<paramref name="v"/>' argument will be null.
            /// </summary>
            /// <param name="name">The name (which will be normalized) of the variable to get.</param>
            /// <param name="v">Out.  A reference to the Variable, if it exists in this Manager.  If it does not exist, returns null.
            /// </param>
            /// <returns>Returns true if the Variable could be found by the given <paramref name="name"/>, false if not.</returns>
            public bool TryGet(string name, out Variable v) => _Table.TryGetValue(GetNormalized(name), out v);
            

            /// <summary>
            /// Adds a Variable with the normalized given name to this manager.  If the Variable 
            /// already exists, returns null.  Otherwise, returns a reference to the Variable.
            /// </summary>
            /// <param name="name">The name of the Variable.  The name will be normalized before 
            /// adding.</param>
            /// <returns>Returns a reference to the added Variable, if one is added, or null if not.</returns>
            /// <exception cref="System.ArgumentException">Thrown when a variable with the normalization of the given name already 
            /// exists in this Manager.</exception>
            public Variable Add(string name)
            {                
                name = GetNormalized(name);
                if (_Table.ContainsKey(name)) return null;
                Variable v = new Variable(name, Context);
                _Table[name] = v;
                return v;
            }


            /// <summary>Adds a reference to the Variable associated with the given name.</summary>
            /// <param name="name">The name of the Variable to be anchored.  The name will be normalized.</param>
            /// <returns>Returns true if the anchoring was successful.  Anchoring can fail if no variable associated with the given 
            /// (normalized) name exists in this Manager.</returns>
            public bool Anchor(string name)
            {
                if (!_Table.TryGetValue(GetNormalized(name), out Variable v)) return false;
                v.References++;                
                return true;
            }

            /// <summary>Releases a reference to the Variable associated with the given name.</summary>
            /// <param name="name">The name of the Variable to be released.  The name will be normalized.</param>
            /// <returns>Returns true if the release was successful.  Release can fail if no Variable associated with the given 
            /// (normalized) name exists in this Manager.  If there are no references to the Variable, throws an exception.</returns>
            /// <exception cref="System.InvalidOperationException">Thrown when a Variable that has no references to it is 
            /// released.</exception>
            public bool Release(string name)
            {
                name = GetNormalized(name);
                if (!_Table.TryGetValue(name, out Variable v)) return false;
                if (--v.References < 0) throw new InvalidOperationException("Variable " + name + " with no references cannot be released.");
                return true;
            }


            /// <summary>Removes the Variable with the given (normalized) name from the Manager.</summary>
            /// <param name="name">The name of the Variable to removed.  The name will be normalized before removal.</param>
            /// <returns>Returns true if the removal was successful; otherwise, returns false.  Removal can fail if no Variable exists 
            /// with the given (normalized) name, or if the Variable still has Formula references to it, or the Variable's contents are 
            /// not null.</returns>
            public bool Remove(string name)
            {
                name = GetNormalized(name);
                if (!_Table.TryGetValue(name, out Variable v)) return false;
                if (v.References > 0) return false;
                if (v.Contents != null) return false;
                return _Table.Remove(name);
            }


            /// <summary>Removes the given Variable from the Manager.</summary>
            /// <param name="v">The Variable to remove.</param>
            /// <returns>Returns true if the removal was successful; otherwise, returns false.  Removal can fail if the Variable does not 
            /// exist on the Manager, or if the Variable that is associated with the (normalized) name is not the same Variable.  Removal 
            /// can also fail if the Variable has Formula references to it remaining, or if the contents of the Variable are not null.
            /// </returns>
            /// <exception cref="System.ArgumentException">Thrown when the Variable on the Manager listed under the Variable's name is 
            /// the same as the Variable given.</exception>
            public bool Remove(Variable v)
            {
                if (!_Table.TryGetValue(v.Name, out Variable existingV)) return false;
                if (!v.Equals(existingV))
                    throw new ArgumentException("The Variable \"" + v.Name + "\" to remove is not the same as that existing on the " 
                                                + "Manager, though they share the same name.");
                if (v.References > 0) return false;
                if (v.Contents != null) return false;
                return _Table.Remove(v.Name);
            }
        }


        

        /// <summary>Returns the Name of the variable.</summary>        
        public override string ToString() => Name;
    }
}
