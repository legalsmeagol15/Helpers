using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Dependency.Helpers;

namespace Dependency
{

    internal enum Role { None = 0, Source = 1, Listener = 2, Variable = Source | Listener, Constant = Source | 4 }


    [AttributeUsage(validOn: AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class PropertyAttribute :  Attribute 
    {
        internal readonly string Contents;
        internal readonly Role Role;
        public readonly string[] Aliases;
        public bool IsSource => (Role & Role.Source) != Role.None;
        public bool IsListener => (Role & Role.Listener) != Role.None;
        internal PropertyAttribute(Role role, string initialContents, params string[] aliases) {
            this.Role = role;
            this.Aliases = aliases;
            this.Contents = initialContents;
        }
        public PropertyAttribute(bool source, bool listener, string initialContents = null, params string[] aliases) 
            : this((source ? Role.Source : 0) | (listener ? Role.Listener : 0), initialContents, aliases) { }
    }


    /// <summary>The subcontext attribute can be applied to any member class which itself has either a subcontext or a 
    /// variable.</summary>
    [AttributeUsage(validOn: AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SubContextAttribute : Attribute
    {
        public readonly string[] Aliases;
        public SubContextAttribute(params string[] aliases) { this.Aliases = aliases; }
    }

    
    public sealed class DependencyContext : IContext
    {
        internal static readonly IDictionary<DependencyContext, IFunctionFactory> Functions 
            = new Dictionary<DependencyContext, IFunctionFactory>();
        private readonly Dictionary<string, ISubcontext> _Subcontexts = new Dictionary<string, ISubcontext>();

        // This is the context that can handle subcontexts not known at compile time.
        public static bool IsManaged(object obj) => ManagedContext.HostContexts.ContainsKey(obj);
        
        public DependencyContext(IFunctionFactory functionFactory = null)
        {
            Functions[this] = functionFactory;
        }
                
        public void Add(object subcontext, string name)
        {    
            if (_Subcontexts.ContainsKey(name))
                throw new ManagedDependencyException("Duplicate name '" + name + "' with existing subcontext.");
            ISubcontext isc = (subcontext is ISubcontext) ? (ISubcontext)subcontext : new ManagedContext(this, subcontext);
            _Subcontexts[name] = isc;
            if (isc.Parent == null) isc.Parent = this;
        }

        bool IContext.TryGetProperty(string token, out IEvaluateable source) { source = null; return false; }

        public bool TryGetSubcontext(string token, out IContext ctxt)
        {
            if (_Subcontexts.TryGetValue(token,  out ISubcontext sub_ctxt)) { ctxt = sub_ctxt; return true; }
            ctxt = null;
            return false;
        }
    }


    internal sealed class ManagedContext : ISubcontext
    {
        internal static readonly Dictionary<object, WeakReference<IContext>> HostContexts = new Dictionary<object, WeakReference<IContext>>();
        internal readonly object Host;
        internal readonly ManagedProfile Profile;        
        internal readonly Dictionary<string, WeakReference<ISubcontext>> ManagedSubcontexts = new Dictionary<string, WeakReference<ISubcontext>>();
        internal readonly Dictionary<string, WeakReference<IEvaluateable>> ManagedProperties = new Dictionary<string, WeakReference<IEvaluateable>>();
        
        public IContext Parent { get; set; }
        internal ManagedContext(IContext parent, object host) {
            this.Parent = parent;
            this.Host = host;
            this.Profile = ManagedProfile.FromType(host.GetType());
        }
        
        public bool TryGetProperty(string token, out IEvaluateable source)
        {
            // If the property currently exists, return that.
            if (ManagedProperties.TryGetValue(token, out WeakReference<IEvaluateable> weakRef)
                && weakRef.TryGetTarget(out source))
                return true;

            // If no profile for creating the property exists, then the token is invalid.
            if (!Profile.PropertyTemplates.TryGetValue(token, out ManagedProfile.PropertyTemplate template))
            {
                source = null;
                return false;
            }

            // A non-source property is not allowed to be retrieved.
            if (!template.Role.IsSource())
            {
                source = null;
                return false;
            }

            // If the property is a templated constant there is no need to keep a reference to it.  Just read the Host property and return.
            object currentVal = template.Info.GetValue(Host);
            if (template.Role.IsConstant())
            {
                source = Obj2Eval(currentVal);
                return true;
            }

            // Otherwise, time to create a new variable from the template.
            source = (currentVal is Variable v) ? v : (v = new Variable(this, token, Obj2Eval(currentVal)));
            if (template.Contents == null)
            {
                object clrValue = template.Info.GetValue(this.Host);
                v.Contents = Helpers.Obj2Eval(clrValue);
            }   
            else
            {
                IContext temp = this;
                IFunctionFactory fac = null;
                while (temp != null)
                {
                    if (temp is DependencyContext dc) { fac = DependencyContext.Functions[dc]; break; }
                    else if (temp is ISubcontext isc) temp = isc.Parent;
                    else break;
                }
                v.Contents = Parse.FromString(template.Contents, fac, this);  // TODO:  What about functions factory?
            }
                

            foreach (string alias in template.Aliases)
            {
                if (ManagedProperties.TryGetValue(alias, out weakRef)) weakRef.SetTarget(source);
                else ManagedProperties[alias] = new WeakReference<IEvaluateable>(source);
            }
            return true;
        }

        public bool TryGetSubcontext(string token, out IContext ctxt)
        {
            if (ManagedSubcontexts.TryGetValue(token, out var weakRef) && weakRef.TryGetTarget(out ISubcontext sub_ctxt))
            {
                ctxt = sub_ctxt;
                return true;
            }

            if (!Profile.ContextTemplates.TryGetValue(token, out ManagedProfile.ContextTemplate template))
            {
                ctxt = null;
                return false;
            }

            object currentObj = template.Info.GetValue(Host);
            if (currentObj == null)
                throw new ManagedDependencyException("Subcontext must refer to an instantiated object.");
            sub_ctxt = (currentObj is ISubcontext) ? (ISubcontext)currentObj : new ManagedContext(this, currentObj);
            HostContexts[Host] = new WeakReference<IContext>(sub_ctxt);
            foreach (string alias in template.Aliases)
            {
                if (ManagedSubcontexts.TryGetValue(alias, out weakRef)) weakRef.SetTarget(sub_ctxt);
                else ManagedSubcontexts[alias] = new WeakReference<ISubcontext>(sub_ctxt);
            }
            ctxt = sub_ctxt;
            return true;
        }
    }


    internal sealed class ManagedProfile
    {
        private static readonly Dictionary<Type, ManagedProfile> _Profiles = new Dictionary<Type, ManagedProfile>();

        public readonly Dictionary<string, PropertyTemplate> PropertyTemplates = new Dictionary<string, PropertyTemplate>();
        public readonly Dictionary<string, ContextTemplate> ContextTemplates = new Dictionary<string, ContextTemplate>();

        public static ManagedProfile FromType(Type type)
        {
            if (_Profiles.TryGetValue(type, out ManagedProfile mp)) return mp;
            mp = new ManagedProfile();
            HashSet<string> aliases = new HashSet<string>();
            foreach (PropertyInfo pInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (ContextTemplate.TryCreate(pInfo, out ContextTemplate ctxt))
                {
                    foreach (string alias in ctxt.Aliases)
                    {                        
                        if (!aliases.Add(alias)) throw new ManagedDependencyException("Duplicate alias '" + alias + ";.");
                        mp.ContextTemplates.Add(alias, ctxt);
                    }
                }
                else if (PropertyTemplate.TryCreate(pInfo, out PropertyTemplate p))
                {
                    foreach (string alias in p.Aliases)
                    {
                        if (!aliases.Add(alias)) throw new ManagedDependencyException("Duplicate alias '" + alias + ";.");
                        mp.PropertyTemplates.Add(alias, p);
                    }
                }
            }
            _Profiles[type] = mp;
            return mp;
        }

        public sealed class ContextTemplate
        {
            public readonly PropertyInfo Info;
            public readonly string[] Aliases;
            private ContextTemplate(PropertyInfo info, string[] aliases) { this.Info = info; this.Aliases = aliases; }
            public static bool TryCreate(PropertyInfo pInfo, out ContextTemplate context)
            {
                
                SubContextAttribute scAttr = pInfo.GetCustomAttribute<SubContextAttribute>();
                if (scAttr == null) { context = null; return false; }

                // Every subcontext object MUST have a parent.
                if (typeof(IContext).IsAssignableFrom(pInfo.PropertyType) && !(typeof(ISubcontext).IsAssignableFrom(pInfo.PropertyType)))
                    throw new ManagedDependencyException("Object managed subcontexts cannot implement " + typeof(IContext).Name + " unless they inherit " + typeof(ISubcontext).Name + ".");

                if (pInfo.CanWrite)
                    throw new ManagedDependencyException("Object managed subcontexts must be read-only.");

                string[] aliases = (scAttr.Aliases != null && scAttr.Aliases.Length > 0) ? scAttr.Aliases : new string[] { pInfo.Name };
                context = new ContextTemplate(pInfo, aliases);
                return true;
            }
        }
        public sealed class PropertyTemplate
        {
            public readonly PropertyInfo Info;
            public readonly Role Role;
            public readonly string[] Aliases;
            public readonly string Contents;

            private PropertyTemplate(PropertyInfo info, Role role, string contents, string[] aliases) { this.Info = info;
                this.Role = role;
                this.Contents = contents;
                this.Aliases = aliases; }

            public static bool TryCreate(PropertyInfo pInfo, out PropertyTemplate property)
            {
                PropertyAttribute vAttr = pInfo.GetCustomAttribute<PropertyAttribute>();
                if (vAttr == null) { property = null; return false; }
                string[] aliases = (vAttr.Aliases != null && vAttr.Aliases.Length > 0) ? vAttr.Aliases : new string[] { pInfo.Name };
                property = new PropertyTemplate(pInfo, vAttr.Role, vAttr.Contents, aliases);
                return true;
            }
        }
    }


    public sealed class ManagedDependencyException : Exception
    {
        public ManagedDependencyException(string msg) : base(msg) { }
    }
}
