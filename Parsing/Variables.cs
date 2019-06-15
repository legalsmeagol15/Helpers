using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    public enum Mobility
    {
        None = 0,
        Column = 1,
        Row = 2,
        All = ~0
    }
    public class Variables : IVariable, ISource, IListener  // NOT an ITerms, otherwise this variable's terms will be added to an variable this is a term of.
    {
        public IContext Context { get; private set; }

        public IEvaluateable Contents { get; set; }

        public string Name { get; private set; }

        public IEvaluateable Value => Contents.Value;
        IEvaluateable IEvaluateable.UpdateValue() => Contents.UpdateValue();

        IEnumerable<IListener> ISource.Listeners => throw new NotImplementedException();

        IEnumerable<ISource> IListener.Sources => throw new NotImplementedException();

        internal string GetExpressionString(IContext perspective)
        {
            // Find the perspective's ancestry.
            List<IContext> perspectiveAncestry = new List<IContext>();  // Chose a list not a hashset because not expected to be huge.
            IContext p = perspective.Parent;
            while (p != null) { perspectiveAncestry.Add(p); p = p.Parent; }

            // Find the nearest ancestor for this variable that is shared by the perspective.
            List<IContext> uniqueAncestry = new List<IContext>();
            p = this.Context;            
            List<string> names = new List<string>();
            while (p != null && !perspectiveAncestry.Contains(p))
            {
                p = p.Parent;
                names.Add(p is INamed pn ? pn.Name : "..");
            }
            if (names.Count == 0) return this.Name;
            return string.Join(".", names) + "." + Name;
        }

        
    }
}
