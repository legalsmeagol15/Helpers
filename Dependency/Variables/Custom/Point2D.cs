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

        internal IDynamicItem Parent { get; set; }
        IDynamicItem IDynamicItem.Parent { get => Parent; set => Parent = value; }

        bool IContext.TryGetProperty(object path, out IEvaluateable source)
        {
            string path_str = path.ToString().ToLower();
            switch (path_str)
            {
                
                default:source = null; return false;
            }
        }

        bool IContext.TryGetSubcontext(object path, out IContext ctxt) { ctxt = null; return false; }

        bool IDynamicItem.Update() => true;
    }
}
