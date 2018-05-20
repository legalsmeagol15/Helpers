using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    public class Variable : IEvaluatable
    {
        public readonly string Name;
        public readonly DataContext Context;

        private IEvaluatable _Contents = null;
        public IEvaluatable Contents { get => _Contents; set { _CachedValue = null; _Contents = value; } }
        public HashSet<Variable> Dependents = new HashSet<Variable>();
        private IEvaluatable _CachedValue;

        public Variable(DataContext context, string name, IEvaluatable contents = null)
        {
            this.Name = name;
            this.Contents = contents;
            this.Context = context;
            context.Add(this);
        }


        public IEvaluatable Evaluate()
        {
            if (_CachedValue != null) return _CachedValue;
            return _CachedValue = (Contents == null ? Contents : Contents.Evaluate());
        }

        public override int GetHashCode() => Name.GetHashCode();

        public override bool Equals(object obj) => ReferenceEquals(this, obj);

        public override string ToString() => Name;

        public class DataContext
        {

            private readonly Dictionary<string, Variable> _Variables = new Dictionary<string, Variable>();
            public Variable this[string name]
            {
                get => _Variables[name];
            }

            public bool Add(Variable v)
            {
                if (_Variables.ContainsKey(v.Name)) return false;
                _Variables.Add(v.Name, v);
                return true;
            }



            internal bool TryGetVariable(string name, out Variable v)
            {
                if (!_Variables.ContainsKey(name))
                {
                    v = null;
                    return false;
                }
                v = _Variables[name];
                return true;
            }

            /// <summary>
            /// Attempts the create the variable within this context.  If the variable does not 
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
            internal bool TryCreateVariable(string name, out Variable v)
            {
                if (_Variables.ContainsKey(name))
                {
                    v = _Variables[name];
                    return false;
                }
                if (!IsNameValid(name))
                {
                    v = null;
                    return false;
                }
                v = new Variable(this, name);
                _Variables.Add(name, v);
                return true;
            }

            public bool IsNameValid(string name) => true;
        }


    }
}
