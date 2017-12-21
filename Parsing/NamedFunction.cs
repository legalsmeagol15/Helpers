using System;
using System.Collections.Generic;
using DataStructures;

namespace Parsing
{
    internal abstract class NamedFunction : Formula
    {
        protected NamedFunction(DataContext context) : base(context) { }


        internal override int ParsingPriority => PRIORITY_NAMED_FUNCTION;


        public class Factory
        {
            
            private static readonly Func<string, string> _StandardNameNormalizer = (s) => s.ToUpper();
            private static readonly Func<string, bool> _StandardNameValidator = (s) => true;

            private Dictionary<string, Func<DataContext, NamedFunction>> _Catalogue = 
                new Dictionary<string, Func<DataContext, NamedFunction>>();
            

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


            private Factory(IEnumerable<Type> types, Func<string, bool> nameValidator = null, 
                            Func<string, string> nameNormalizer = null)
            {
                //Add pre-defined static constants
                _Catalogue.Add(Pi.Name, (context) => Pi);

                //Add func-creators
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
                _Catalogue[name] = (context) => (NamedFunction)Activator.CreateInstance(t, new object[1] { context });
                return true;
            }
                        

            /// <summary>
            /// Creates a new instance of a function with the given name.  If no such name exists on 
            /// this factory, returns null.  The arguments after the function name will be fed to the 
            /// constructor in the order specified.
            /// </summary>
            public NamedFunction Make(string name, DataContext context)
            {
                name = GetNormalized(name);
                if (_Catalogue.TryGetValue(name, out Func<DataContext, NamedFunction> creatorFunc))
                    return creatorFunc(context);
                return null;
            }
        }


        /// <summary>Associates the following parenthetical block as inputs for the NamedFunction.</summary>        
        protected override void Parse(DynamicLinkedList<object>.Node node)
        {
            if (node.Next == null) throw new FormatException(ToString() + " must be followed by inputs.");
            if (node.Next.Contents is Block b)
            {
                if (b.Opener != "(" || b.Closer != ")")
                    throw new FormatException(ToString() + " must be followed by parenthetical expression containing inputs.");
                Inputs = b.Inputs;
                node.Next.Remove();                
            }
            else
                throw new FormatException(ToString() + " must be followed by block containing inputs.");            
        }


        public override string ToString() => GetType().Name;

        
        #region NamedFunction predefined constants


        private class Constant : NamedFunction
        {
            public readonly string Name;
            public Constant(decimal constantValue, string name) : base(null)
            {
                Name = name;
                Inputs = new List<object>() { constantValue };
            }

            protected override object Evaluate(params object[] inputs) => Inputs[0];

            protected override void Parse(DynamicLinkedList<object>.Node node)
            {
                if (node.Next == null) return;
                else if (node.Next.Contents is Block b)
                {
                    if (b.Opener != "(" || b.Closer != ")" || b.Inputs.Count != 0)
                        throw new FormatException(Name + " can only be followed by empty parenthetical, or nothing.");
                    node.Next.Remove();
                }
            }

            public override string ToString() { return Name; }
        }

        private static Constant Pi = new Constant((decimal)Math.PI, "PI");
        #endregion
    }


    internal sealed class COS : NamedFunction
    {
        public COS(DataContext context) : base(context) { }

        protected override object Evaluate(params object[] inputs)
        {
            if (inputs.Length != 1) throw new EvaluationException(ToString() + " has wrong number of inputs.");
            return (decimal)Math.Cos((double)inputs[0]);
        }
    }

}
