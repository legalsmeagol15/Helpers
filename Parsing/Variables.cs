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

        private readonly IDictionary<Type, AutoProfile> _Profiles = new Dictionary<Type, AutoProfile>();
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
                src.Contents = Helpers.Obj2Eval(weakSrc.Read());
                src.UpdateValue();
                return true;
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



        /// <summary>
        /// A source attribute can be applied to any property that is not write-only.  Note that if the property's CLR 
        /// type cannot be readily converted into an <seealso cref="IEvaluateable"/> object, the value of the source 
        /// will be the property's ToString() value.  Also, note that updating the value will require a call to the 
        /// host <see cref="AutoContext"/>'s <seealso cref="AutoContext.UpdateSource(object, string)"/> method.
        /// </summary>
        [AttributeUsage(validOn: AttributeTargets.Property , AllowMultiple = false, Inherited = false)]
        public sealed class SourceAttribute : Attribute {
        }

        /// <summary>
        /// A listener attribute can be applied to any property that is not read-only.  Note that if the dependency 
        /// system attempts to set the value of the property to something that cannot be accommodated by its CLR type, 
        /// an exception will be thrown.
        /// </summary>
        [AttributeUsage(validOn: AttributeTargets.Property , AllowMultiple = false, Inherited = false)]
        public sealed class ListenerAttribute : Attribute { }

        /// <summary>
        /// The subcontext attribute can be applied to any member class 
        /// </summary>
        [AttributeUsage(validOn: AttributeTargets.Property , AllowMultiple = false, Inherited = false)]
        public sealed class SubContextAttribute : Attribute { }

        private sealed class WeakContext : IContext
        {
            
            public IContext Parent { get; private set; }
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
                return Sources.TryGetValue(token, out var weakRef) && weakRef.TryGetTarget(out source);
            }
            bool IContext.TryGetSource(string token, out ISource source, out Mobility mobility) => this.TryGetSource(token, out source, out mobility);

            bool IContext.TryGetSubcontext(string token, out IContext ctxt)
            {
                ctxt = null;
                return Subcontexts.TryGetValue(token, out var weakRef) && weakRef.TryGetTarget(out ctxt);
            }
            
        }
        private sealed class WeakSource : ISource
        {
            public readonly Func<object> Read;
            public readonly IContext Context;
            public WeakSource(IContext context, Func<object> clr_reader) { this.Context = context; this.Read = clr_reader; }
        }

        private sealed class WeakListener : IListener
        {
            public readonly Action<object> Write;
            public readonly IContext Context;
            public WeakListener(IContext context, Action<object> clr_writer) { this.Context = context; this.Write = clr_writer; }
        }
        private sealed class WeakVariable : IVariable
        {
            public IContext Context { get; private set; }
            public WeakVariable(IContext context) { this.Context = context; }
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
                    if (pInfo.GetCustomAttribute<SourceAttribute>() != null)
                    {                        
                        result.SourceProperties[name] = pInfo;
                    }
                    if (pInfo.GetCustomAttribute<ListenerAttribute>() != null)
                    {                        
                        result.ListenerProperties[name] = pInfo;
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
