using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Contexts
{
    [Serializable]
    public class ContextDictionary : IEnumerable<IContext>
    {
        private readonly IContext _Context;
        public ContextDictionary(IContext context) { this._Context = context; }

        public bool Contains(string name) => _Context.TryGet(name, out IContext _);

        /// <exception cref="ContextNotFoundException">Thrown when no sub-context matching the given name exists.</exception>
        public IContext this[string name]
        {
            get
            {
                if (_Context.TryGet(name, out IContext ctxt)) return ctxt;
                throw new ContextNotFoundException(name, _Context.Name);
            }
        }

        public IEnumerator<IContext> GetEnumerator() => _Context.GetContexts.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}
