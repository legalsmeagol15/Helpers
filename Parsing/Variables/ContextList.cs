using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    public class ContextList : IContext
    {
        bool IContext.TryGetProperty(object path, out IEvaluateable source)
        {
            throw new NotImplementedException();
        }

        bool IContext.TryGetSubcontext(object path, out IContext ctxt)
        {
            throw new NotImplementedException();
        }
    }
}
