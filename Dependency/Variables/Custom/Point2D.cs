using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Variables.Custom
{
    public sealed class Point2D : IDynamicItem, IContext
    {
        private readonly WeakReference<Variable> _XRef = new WeakReference<Variable>(null);
        private readonly WeakReference<Variable> _YRef = new WeakReference<Variable>(null);
        bool IContext.TryGetProperty(object path, out IEvaluateable source)
        {
            string p;
            throw new NotImplementedException();
        }

        bool IContext.TryGetSubcontext(object path, out IContext ctxt) { ctxt = null; return false; }
    }
}
