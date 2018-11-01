using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Contexts
{
    /// <summary>
    /// Allows the organization of variable-related operations into a Variables object of type VariableDictionary.
    /// </summary>
    [Serializable]
    public class VariableDictionary : IEnumerable<Variable>
    {
        private readonly IContext _Context;
        public VariableDictionary(IContext context) { this._Context = context; }

        public bool Contains(string name) => _Context.TryGet(name, out Variable _);

        public Variable this[string name] => _Context[name];

        public IEnumerator<Variable> GetEnumerator() => _Context.GetVariables.GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
