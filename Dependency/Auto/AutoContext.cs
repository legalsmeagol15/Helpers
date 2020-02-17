using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Auto
{
    internal sealed class AutoContext : IContext
    {
        private readonly static System.Runtime.CompilerServices.ConditionalWeakTable<object, AutoContext> _Catalogue
            = new System.Runtime.CompilerServices.ConditionalWeakTable<object, AutoContext>();
        private readonly WeakReference Host;

        public AutoContext(object host)
        {
            this.Host = new WeakReference(host);
        }

        public static AutoContext GetOrCreate(object host)
        {
            if (_Catalogue.TryGetValue(host, out AutoContext ctxt)) return ctxt;
            ctxt = new AutoContext(host);
            return ctxt;
        }

        bool IContext.TryGetProperty(string path, out IEvaluateable source)
        {
            throw new NotImplementedException();
        }

        bool IContext.TryGetSubcontext(string path, out IContext ctxt)
        {
            // This is an auto-context, so get the profile associated with this auto-context that 
            // will guide us to the host object.  If there is no guide matching the given path, 
            // then this is a bad subcontext lookup.
            AutoProfile profile = AutoProfile.GetOrCreate(this.Host.GetType());
            if (profile != null || !profile.TryGetContextProfile(path, out AutoProfile.ContextProfile contextProfile))
            {
                ctxt = null;
                return false;
            }
            object obj = contextProfile.Getter(this);

            // If an auto-context is already associated with the object, return that.
            if (_Catalogue.TryGetValue(obj, out AutoContext ac))
            {
                ctxt = ac;
                return true;
            }
            // If the auto-context is an IContext anyway, return that.
            else if (obj is IContext ic)
            {
                ctxt = ic;
                return true;
            }
            // Create a new auto-context and return it.
            else
            {
                ctxt = new AutoContext(obj);
                return true;
            }
        }
    }
}
