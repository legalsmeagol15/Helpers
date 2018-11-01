using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Contexts
{
    public class SoftContext : IContext
    {
        private Dictionary<string, WeakReference<Variable>> _WeakVariables = new Dictionary<string, WeakReference<Variable>>();
        private Dictionary<string, WeakReference<IContext>> _WeakContexts = new Dictionary<string, WeakReference<IContext>>();

        Variable IContext.this[string name] => throw new NotImplementedException();

        string IContext.Name => throw new NotImplementedException();

        IContext IContext.Parent => throw new NotImplementedException();

        Expression.DeletionStatus IContext.DeletionStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        IEnumerable<Variable> IContext.GetVariables => throw new NotImplementedException();

        IEnumerable<IContext> IContext.GetContexts => throw new NotImplementedException();

        bool IContext.TryAdd(string name, out Variable variable)
        {
            throw new NotImplementedException();
        }

        bool IContext.TryAdd(string name, out IContext context)
        {
            throw new NotImplementedException();
        }

        bool IContext.TryCreateFunction(string token, out Function f)
        {
            throw new NotImplementedException();
        }

        bool IContext.TryDelete(Variable variable)
        {
            throw new NotImplementedException();
        }

        bool IContext.TryDelete(IContext context)
        {
            throw new NotImplementedException();
        }

        bool IContext.TryGet(string name, out IContext ctxt)
        {
            throw new NotImplementedException();
        }

        bool IContext.TryGet(string name, out Variable v)
        {
            throw new NotImplementedException();
        }
    }
}
