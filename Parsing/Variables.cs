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
    public class Variable : IVariable, ISource, IListener  // NOT an ITerms, otherwise this variable's terms will be added to an variable this is a term of.
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


    public sealed class AutoContext : IContext
    {
        
        public IContext Parent { get; }

        private readonly IDictionary<string, IContext> _TypedContexts = new Dictionary<string, IContext>();
        private readonly IDictionary<string, ISource> _TypedSources = new Dictionary<string, ISource>();
        private readonly IDictionary<string, IListener> _TypedListeners = new Dictionary<string, IListener>();
        private readonly IDictionary<Type, AutoProfile> _Profiles = new Dictionary<Type, AutoProfile>();

        public bool Add(object obj, string name)
        {
            if (obj is IContext ic)
            {
                if (_TypedContexts.ContainsKey(name)) return false;
                _TypedContexts[name] = ic;
                return true;
            }
            else if (obj is ISource isrc)
            {
                if (_TypedSources.ContainsKey(name)) return false;
                _TypedSources[name] = isrc;
                return true;
            }
            else if (_Profiles.TryGetValue(obj.GetType(), out AutoProfile prof))
            {

            }
        }

        public AutoContext(IContext parent = null) { this.Parent = parent; }

        bool IContext.TryGetSource(string token, out ISource source, out Mobility mobility)
        {
            ExtractMobility(token, out token, out mobility);
            if (_TypedSources.TryGetValue(token, out source)) return true;

        }

        bool IContext.TryGetSubcontext(string token, out IContext ctxt) => this._TypedContexts.TryGetValue(token, out ctxt);

        private void ExtractMobility(string token, out string stripped, out Mobility mobility)
        {
            if (this.Parent is IAllowReferenceMobility iarm)
            {
                if (iarm.TryExtract(token, out stripped, out mobility)) return;
            }
            mobility = Mobility.All;
            stripped = token;
        }



        [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
        public sealed class SourceAttribute : Attribute { }

        [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
        public sealed class ListenerAttribute : Attribute { }

        [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
        public sealed class SubContextAttribute : Attribute { }

        internal sealed class AutoProfile : IContext
        {

        }
    }


}
