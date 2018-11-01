using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Contexts
{
    public interface IContext
    {
        string Name { get; }

        IContext Parent { get; }

        Expression.DeletionStatus DeletionStatus { get; set; }

        Variable this[string name] { get; }
        bool TryGet(string name, out IContext ctxt);
        bool TryGet(string name, out Variable v);        

        bool TryAdd(string name, out Variable variable);
        bool TryAdd(string name, out IContext context);

        bool TryDelete(Variable variable);
        bool TryDelete(IContext context);        

        IEnumerable<Variable> GetVariables { get; }
        IEnumerable<IContext> GetContexts { get; }

        bool TryCreateFunction(string token, out Function f);

    }
    
}
