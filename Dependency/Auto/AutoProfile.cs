using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Auto
{
    internal sealed class AutoProfile
    {
        private static readonly Dictionary<Type, AutoProfile> _Profiles = new Dictionary<Type, AutoProfile>();
        private readonly Dictionary<string, PropertyProfile> _PropertyProfiles = new Dictionary<string, PropertyProfile>();
        private readonly Dictionary<string, ContextProfile> _ContextProfiles = new Dictionary<string, ContextProfile>();

        private AutoProfile(Type type)
        {
            foreach (PropertyInfo pInfo in type.GetProperties()){
                List<string> aliases = new List<string>();
                Pattern pattern = Pattern.None;
                bool isContext = false;
                foreach (Attribute attr in pInfo.GetCustomAttributes())
                {
                    if (attr is DependencyAliasAttribute attr_as_alias)
                        aliases.Add(attr_as_alias.Alias);
                    if (!isContext && attr is DependencyContextAttribute attr_as_context)
                        isContext = true;
                    if (pattern == Pattern.None && attr is DependencyVariableAttribute attr_as_variable)
                        pattern = attr_as_variable.Pattern;
                }
                if (pattern != Pattern.None)
                    foreach (string alias in aliases)
                        _PropertyProfiles.Add(alias, new PropertyProfile(pattern, pInfo.GetValue, pInfo.SetValue));
                if (isContext)
                    foreach (string alias in aliases)
                        _ContextProfiles.Add(alias, new ContextProfile(pInfo.GetValue, pInfo.SetValue));
            }

            foreach (FieldInfo fInfo in type.GetFields())
            {
                List<string> aliases = new List<string>();
                Pattern pattern = Pattern.None;
                bool isContext = false;
                foreach (Attribute attr in fInfo.GetCustomAttributes())
                {
                    if (attr is DependencyAliasAttribute attr_as_alias)
                        aliases.Add(attr_as_alias.Alias);
                    if (!isContext && attr is DependencyContextAttribute attr_as_context)
                        isContext = true;
                    if (pattern == Pattern.None && attr is DependencyVariableAttribute attr_as_variable)
                        pattern = attr_as_variable.Pattern;
                }
                if (pattern != Pattern.None)
                    foreach (string alias in aliases)
                        _PropertyProfiles.Add(alias, new PropertyProfile(pattern, fInfo.GetValue, fInfo.SetValue));
                if (isContext)
                    foreach (string alias in aliases)
                        _ContextProfiles.Add(alias, new ContextProfile(fInfo.GetValue, fInfo.SetValue));
            }
        }

        internal bool TryGetPropertyProfile(string path, out PropertyProfile propProfile) 
            => _PropertyProfiles.TryGetValue(path, out propProfile);
        internal bool TryGetContextProfile(string path, out ContextProfile contextProfile)
            => _ContextProfiles.TryGetValue(path, out contextProfile);

        internal sealed class PropertyProfile
        {
            public readonly Pattern Pattern;
            public readonly Func<object, object> Getter;
            public readonly Action<object, object> Setter;
            public PropertyProfile(Pattern pattern, Func<object, object> getter, Action<object, object> setter)
            {
                this.Pattern = pattern;
                this.Getter = getter;
                this.Setter = setter;
            }
        }

        internal sealed class ContextProfile
        {
            public readonly Func<object, object> Getter;
            public readonly Action<object, object> Setter;
            public ContextProfile(Func<object, object> getter, Action<object, object> setter)
            {
                this.Getter = getter;
                this.Setter = setter;
            }
        }

        public static AutoProfile GetOrCreate(Type type)
        {
            if (_Profiles.TryGetValue(type, out AutoProfile ap)) return ap;
            return new AutoProfile(type);
        }
    }
}
