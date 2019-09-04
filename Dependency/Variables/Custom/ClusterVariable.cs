using DataStructures;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dependency.Variables.Custom
{

    public abstract class ClusterVariable : Variable, IContext
    {
        
        protected virtual bool TryClusterProperty(object path, out IEvaluateable property) { property = null; return false; }
        protected virtual bool TryClusterSubcontext(object path, out IContext context) { context = null; return false; }

        bool IContext.TryGetSubcontext(object path, out IContext ctxt)
        {
            if (!TryClusterSubcontext(path, out ctxt)) return false;
            if (!(ctxt is IDynamicItem idi)) return false;
            idi.Parent = this;
            return true;
        }

        bool IContext.TryGetProperty(object path, out IEvaluateable source)
        {
            // Setting the parent to "this" will establish a hard link keeping this object alive.
            if (!TryClusterProperty(path, out source)) return false;
            if (source is IDynamicItem idi) idi.Parent = this;
            return true;
        }
    }
}
