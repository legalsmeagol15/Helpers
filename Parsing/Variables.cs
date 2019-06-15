using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    public delegate void StandardizeTokenDelegate(string token, out string stripped, out Mobility mobility);

    public class Variable : IVariable, ISource, IListener, INamed
    // NOT an ITerms, otherwise this variable's terms will be added to any variable this is a term of.
    {
        public IContext Context { get; private set; }

        public IEvaluateable Contents { get; set; }

        public string Name { get; private set; }

        public IEvaluateable Value => Contents.Value;
        IEvaluateable IEvaluateable.UpdateValue() => Contents.UpdateValue();

        IEnumerable<IListener> ISource.Listeners => throw new NotImplementedException();
        

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

    /// <summary>
    /// An <see cref="AutoContext"/> allows participation of a non-<seealso cref="IContext"/> class or struct 
    /// instance in the dependency system.  For standard <seealso cref="IContext"/> objects, the usual behavior is 
    /// applied, but for non-<seealso cref="IContext"/> objects, the <see cref="AutoContext"/> will use reflection to 
    /// maintain the dependency structure.
    /// <para/>While this allows easy implementation of the dependency system, it is not fast.  If performance is an 
    /// issue consider implementing the <seealso cref="IContext"/> interface for any class whose members must 
    /// participate in dependency system.
    /// </summary>
    public sealed class AutoContext : IContext
    {
        /// <summary>An <see cref="AutoContext"/> never has a parent.</summary>
        IContext IContext.Parent => null;

        private readonly StandardizeTokenDelegate StandardizeToken;

        private readonly IDictionary<string, IContext> _DirectSubcontexts = new Dictionary<string, IContext>();
        private readonly IDictionary<string, ISource> _DirectSources = new Dictionary<string, ISource>();
        // No point in having direct listeners.

        
        private readonly IDictionary<object, WeakReference< WeakContext>> _InternalContexts = new Dictionary<object, WeakReference<WeakContext>>();

        public AutoContext(StandardizeTokenDelegate tokenStandardizer)
        {
            this.StandardizeToken = tokenStandardizer;            
        }

        /// <summary>
        /// Adds a hard reference to the context.  This will make the given context accessible through the given name, 
        /// but the context's <seealso cref="IContext.Parent"/> will not point to this <see cref="AutoContext"/>.
        /// </summary>
        /// <param name="ctxt">The sub-context to add to this <see cref="AutoContext"/>.</param>
        /// <param name="name">The name of the sub-context through which it will be accessible in this 
        /// <see cref="AutoContext"/>.</param>
        /// <returns>Returns true if the sub-context is added, false if it already existed on this 
        /// <see cref="AutoContext"/>.</returns>
        public bool AddContext(IContext ctxt, string name)
        {   
            if (_DirectSubcontexts.ContainsKey(name)) return false;
            _DirectSubcontexts[name] = ctxt;
            return true;
        }
        /// <summary>Adds a weak reference to the context.</summary>
        public bool AddContext(object obj, string name)
        {
            if (obj is IContext ic) return AddContext(ic, name);
            if (!_InternalContexts.TryGetValue(name, out var weakRef))
                _InternalContexts[name] = (weakRef = new WeakReference<WeakContext>(null));
            if (weakRef.TryGetTarget(out var _)) return false;

            AutoProfile prof = AutoProfile.FromType(obj.GetType(), StandardizeToken);
            WeakContext wc = new WeakContext(this, prof);
            weakRef.SetTarget(wc);
            return true;
        }
        

        bool IContext.TryGetSource(string token, out ISource source, out Mobility mobility)
        {
            StandardizeToken(token, out token, out mobility);
            return _DirectSources.TryGetValue(token, out source);
        }
        private bool TryGetExistingSource(object context, string token, out ISource src)
        {
            StandardizeToken(token, out token, out Mobility _);
            if (context == this && _DirectSources.TryGetValue(token, out src))
                return true;
            else if (_InternalContexts.TryGetValue(context, out var weakRef)
                        && weakRef.TryGetTarget(out WeakContext weakCtxt)
                        && weakCtxt.TryGetSource(token, out src, out _))
                return true;
            src = null;
            return false;
        }

        bool IContext.TryGetSubcontext(string token, out IContext ctxt)
        {
            if (_DirectSubcontexts.TryGetValue(token, out ctxt)) return true;
            if (_InternalContexts.TryGetValue(token, out var weakRef) && weakRef.TryGetTarget(out WeakContext w_ctxt)) { ctxt = w_ctxt; return true; }
            ctxt = null;
            return false;
        }

        
        /// <summary>
        /// Forces the indicated source to read its value via reflection from the CLR and then propogate its updated 
        /// value through the dependency system.  This method relies on reflection to update the value.
        /// </summary>
        public bool UpdateSource(object context, string sourceName)
        {
            if (!TryGetExistingSource(context, sourceName, out ISource src)) return false;
            if (src is WeakSource weakSrc)
            {                
                weakSrc.UpdateValue();
                return true;
            } else
            {
                
                src.NotifyListeners();
            }
            return false;
        }
        /// <summary>
        /// Updates the contents of the indicated source to the new value, and then propogates the updated value of 
        /// the source through the dependency system.
        /// </summary>
        public bool UpdateSource(object context, string sourceName, object newValue) 
            => UpdateSource(context, sourceName, Helpers.Obj2Eval(newValue));
        /// <summary>
        /// Updates the contents of the indicated source to the new value, and then propogates the updated value of 
        /// the source through the dependency system.
        /// </summary>
        public bool UpdateSource(object context, string sourceName, IEvaluateable newValue)
        {
            if (!TryGetExistingSource(context, sourceName, out ISource src))
                return false;
            src.Contents = newValue;
            src.UpdateValue();
            return true;
        }


        

        [AttributeUsage(validOn: AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
        public sealed class VariableAttribute : Attribute
        {
            public readonly bool Source;
            public readonly bool Listener;
            public VariableAttribute(bool source, bool listener) { this.Source = source; this.Listener = listener; }
        }

        /// <summary>
        /// The subcontext attribute can be applied to any member class 
        /// </summary>
        [AttributeUsage(validOn: AttributeTargets.Property , AllowMultiple = false, Inherited = false)]
        public sealed class SubContextAttribute : Attribute { }

        private sealed class WeakContext : IContext
        {
            
            public IContext Parent { get; private set; }
            public readonly object Host;
            private readonly AutoProfile Profile;
            private readonly StandardizeTokenDelegate StandardizeToken;

            public readonly Dictionary<string, WeakReference<IContext>> Subcontexts = new Dictionary<string, WeakReference<IContext>>();
            public readonly Dictionary<string, WeakReference<ISource>> Sources = new Dictionary<string, WeakReference<ISource>>();
            public readonly Dictionary<string, WeakReference<IListener>> Listeners = new Dictionary<string, WeakReference<IListener>>();

            public WeakContext(IContext parent, AutoProfile profile)
            {
                this.Parent = parent;
                this.Profile = profile;
                this.StandardizeToken = profile.StandardizeToken;
            }
            internal bool TryGetSource(string token, out ISource source, out Mobility mobility)
            {
                StandardizeToken(token, out token, out mobility);
                source = null;
                if (!Sources.TryGetValue(token, out var weakRef))
                    Sources[token] = (weakRef = new WeakReference<ISource>(null));
                if (!weakRef.TryGetTarget(out source))
                {
                    if (!Profile.SourceProperties.TryGetValue(token, out PropertyInfo pInfo)) return false;
                    WeakSource ws = new WeakSource(this, () => Helpers.Obj2Eval(pInfo.GetValue(Host)));
                }
                return true;
            }
            
            bool IContext.TryGetSource(string token, out ISource source, out Mobility mobility) => this.TryGetSource(token, out source, out mobility);

            bool IContext.TryGetSubcontext(string token, out IContext ctxt) { ctxt = null; return Subcontexts.TryGetValue(token, out var weakRef) && weakRef.TryGetTarget(out ctxt); }

        }
        private sealed class WeakSource : ISource
        {
            private readonly Func<IEvaluateable> _ReadFromCLR;
            public readonly IContext Context;
            public WeakSource(IContext context, Func<IEvaluateable> clr_reader)
            {
                this.Context = context;
                this._ReadFromCLR = clr_reader;
                this.Value = _ReadFromCLR();
            }

            public IEnumerable<IListener> Listeners { get; } = new HashSet<IListener>();

            public IEvaluateable Value { get; private set; }

            IEnumerable<IListener> ISource.Listeners => throw new NotImplementedException();

            IEvaluateable IEvaluateable.Value => throw new NotImplementedException();

            public IEvaluateable Contents { get => Value; set { Value = Contents; this.NotifyListeners(); } }

            public IEvaluateable UpdateValue() { Value = _ReadFromCLR(); this.NotifyListeners(); return Value; }
            
        }

        private sealed class WeakListener : IListener
        {
            public readonly Action<object> Write;
            public readonly IContext Context;
            public WeakListener(IContext context, Action<object> clr_writer) { this.Context = context; this.Write = clr_writer; }

            IEvaluateable IListener.Contents => throw new NotImplementedException();
        }
        private sealed class WeakVariable : IVariable
        {
            public IContext Context { get; private set; }

            IContext IVariable.Context => throw new NotImplementedException();

            IEnumerable<IListener> ISource.Listeners => throw new NotImplementedException();
            
            IEvaluateable IEvaluateable.Value => throw new NotImplementedException();

            string INamed.Name => throw new NotImplementedException();

            IEvaluateable IListener.Contents => throw new NotImplementedException();

            IEvaluateable ISource.Contents { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public WeakVariable(IContext context) { this.Context = context; }

            IEvaluateable IEvaluateable.UpdateValue()
            {
                throw new NotImplementedException();
            }
        }


        private sealed class AutoProfile 
        {
            internal static readonly Dictionary<Type, AutoProfile> Profiles = new Dictionary<Type, AutoProfile>();
            internal readonly Type Type;
            internal readonly StandardizeTokenDelegate StandardizeToken;
            internal Dictionary<string, PropertyInfo> SourceProperties = new Dictionary<string, PropertyInfo>();
            internal Dictionary<string, PropertyInfo> ListenerProperties = new Dictionary<string, PropertyInfo>();
            internal Dictionary<string, AutoProfile> SubcontextProfiles = new Dictionary<string, AutoProfile>();
            
            public static AutoProfile FromType(Type type, StandardizeTokenDelegate standardizeToken)
            {
                if (Profiles.TryGetValue(type, out AutoProfile prof))
                {
                    if (prof != null && prof.StandardizeToken != standardizeToken)
                        throw new Exception("A token of this type already exists with inconsistent token standardization.");
                    return prof;
                }
                AutoProfile result = new AutoProfile(type, standardizeToken);
                foreach (PropertyInfo pInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    string name = pInfo.Name;
                    standardizeToken(name, out name, out Mobility _);
                    var vAttr = pInfo.GetCustomAttribute<VariableAttribute>();
                    if (vAttr != null)
                    {
                        if (vAttr.Source) result.SourceProperties[name] = pInfo;
                        if (vAttr.Listener) result.ListenerProperties[name] = pInfo;
                    }
                    
                    if (pInfo.GetCustomAttribute<SubContextAttribute>() != null)
                    {
                        AutoProfile subProfile = FromType(pInfo.PropertyType, standardizeToken);
                        if (subProfile == null) continue;
                        result.SubcontextProfiles[pInfo.Name] = subProfile;
                    }
                }
                if (result.SourceProperties.Count == 0 || result.ListenerProperties.Count == 0 || result.SubcontextProfiles.Count == 0)
                    return null;
                Profiles[type] = result;
                return result;
            }

            private AutoProfile(Type type, StandardizeTokenDelegate standardizeToken) { this.Type = type; this.StandardizeToken = standardizeToken; }
        }
    }


}
