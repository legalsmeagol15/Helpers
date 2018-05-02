using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing
{
    public class Variable : IExpression
    {
        public readonly string Name;
        public readonly DataContext Context;
        public IExpression Value;

        public Variable(DataContext context, string name)
        {
            context.Add(this);
            this.Name = name;
            this.Value = new Number(0m);
        }
        public static int SortCompare(Variable a, Variable b)
        {
            return a.Name.CompareTo(b.Name);
        }

        public override bool Equals(object obj)
        {
            Variable other = obj as Variable;
            if (other == null) return false;
            return other.Context == this.Context && other.Name == this.Name;
        }

        public IExpression Evaluate()
        {
            return Value.Evaluate();
        }

        public override int GetHashCode() => Name.GetHashCode();


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
