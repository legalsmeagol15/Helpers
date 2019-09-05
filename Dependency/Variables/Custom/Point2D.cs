using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Variables.Custom
{
    public sealed class Point2D : IDynamicItem, IContext
    {
        private readonly LiteralDouble _XRef = new LiteralDouble();
        private readonly LiteralDouble _YRef = new LiteralDouble();
        
        internal IDynamicItem Parent { get; set; }
        IDynamicItem IDynamicItem.Parent { get => Parent; set => Parent = value; }

        bool IContext.TryGetProperty(object path, out IEvaluateable source)
        {
            string path_str = path.ToString().ToLower();
            switch (path_str)
            {
                case "x": source = _XRef.Source;return true;
                case "y": source = _YRef.Source; return true;
                default:source = null; return false;
            }
        }

        bool IContext.TryGetSubcontext(object path, out IContext ctxt) { ctxt = null; return false; }

        bool IDynamicItem.Update()
        {
            bool update = false;
            if (_XRef.TryGetSource(out Variable v)) update |= Variable.UpdateValue(v);
            if (_YRef.TryGetSource(out v)) update |= Variable.UpdateValue(v);
            return update;
        }
    }
}
