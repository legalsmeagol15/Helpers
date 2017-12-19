using System;
using System.Collections.Generic;

namespace Mathematics.Parsing
{
    internal abstract class NamedFunction : Formula
    {
        protected NamedFunction(DataContext context) : base(context) { }

        internal override int ParsingPriority => PRIORITY_NAMED_FUNCTION;

        public class Factory
        {
            private static readonly Func<string, string> _StandardNameNormalizer = (s) => s.ToUpper();
            private static readonly Func<string, bool> _StandardNameValidator = (s) => true;

            private Dictionary<string, Type> _Catalogue = new Dictionary<string, Type>();

            /// <summary>Returns the function name in normalized form for this factory.</summary>
            public readonly Func<string, string> GetNormalized;

            /// <summary>Returns whether the function name given is valid for this factory.</summary>
            public readonly Func<string, bool> IsValid;


            /// <summary>
            /// Creates a named function factory using the NamedFunction types in this assembly, with 
            /// the standard name validation and normalization methods.
            /// </summary>            
            public static Factory FromStandard()
            {
                return new Factory(System.Reflection.Assembly.GetExecutingAssembly().GetTypes());
            }
            private Factory(IEnumerable<Type> types, Func<string, bool> nameValidator = null, Func<string, string> nameNormalizer = null)
            {
                IsValid = nameValidator ?? _StandardNameValidator;
                GetNormalized = nameNormalizer ?? _StandardNameNormalizer;
                foreach (Type t in types)
                    Add(t);
            }
            private bool Add(Type t)
            {
                //Check that the class is okay to make an instance out of.
                if (!t.IsClass || !typeof(NamedFunction).IsAssignableFrom(t) || t.IsAbstract)
                    return false;

                //Check that the name is okay and hasn't been added yet.
                string name = GetNormalized(t.Name);
                if (_Catalogue.ContainsKey(name)) return false;
                if (!IsValid(name)) return false;

                //Add the function.
                _Catalogue[name] = t;
                return true;
            }

            /// <summary>
            /// Creates a new instance of a function with the given name.  If no such name exists on 
            /// this factory, returns null.
            /// </summary>
            public NamedFunction Make(string name)
            {
                return Make(name, new object[0]);
            }

            /// <summary>
            /// Creates a new instance of a function with the given name.  If no such name exists on 
            /// this factory, returns null.  The arguments after the function name will be fed to the 
            /// constructor in the order specified.
            /// </summary>
            public NamedFunction Make(string name, params object[] args)
            {
                name = GetNormalized(name);
                if (_Catalogue.TryGetValue(name, out Type t))
                    return (NamedFunction)Activator.CreateInstance(t, args);
                return null;
            }
        }


        
    }
}
