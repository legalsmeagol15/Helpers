using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    public class DependencyManager
    {
        internal bool TryGet(string token, out IContext ctxt)
        {
            throw new NotImplementedException();
        }

        internal bool TryGet(string token, out IVariable ctxt)
        {
            throw new NotImplementedException();
        }
    }
}
