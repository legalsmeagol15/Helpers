using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public class Configuration<T>
    {
        private const BindingFlags BINDING_FILTER = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public readonly string Key;
        private readonly List<Profile<PropertyInfo>> Properties = new List<Profile<PropertyInfo>>();
        private readonly List<Profile<FieldInfo>> Fields = new List<Profile<FieldInfo>>();

        private Configuration(string key = "")
        {
            this.Key = key;
            HashSet<string> members = new HashSet<string>();

            // Get all the properties declared in the hierarchy.
            if (typeof(T).IsClass || typeof(T).IsValueType)
            {
                var declaredConfigs = typeof(T).GetCustomAttributes(typeof(ConfigurationDeclaredAttribute), true);
                
                foreach (ConfigurationDeclaredAttribute dc in declaredConfigs.OfType<ConfigurationDeclaredAttribute>())
                {
                    if (!key.Equals(dc.Key))
                        continue;
                    else if (!members.Add(dc.MemberName))
                        throw new InvalidOperationException("Duplicate " + nameof(Configuration<T>) + " handling under this key for member " + dc.MemberName);

                    else if (typeof(T).GetProperty(dc.MemberName, BINDING_FILTER) is PropertyInfo pinfo
                            && Profile<PropertyInfo>.Create(pinfo, key, members) is Profile<PropertyInfo> p)
                        Properties.Add(p);


                    else if (typeof(T).GetField(dc.MemberName, BINDING_FILTER) is FieldInfo finfo
                            && Profile<FieldInfo>.Create(finfo, key, members) is Profile<FieldInfo> f)
                        Fields.Add(f);

                    else
                        throw new InvalidOperationException("No matching property or field named " + dc.MemberName + " on type " + typeof(T).Name);
                }
            }

            // Get all the properties and fields that are directly marked configurable.
            foreach (PropertyInfo pinfo in typeof(T).GetProperties(BINDING_FILTER))
                if (Profile<PropertyInfo>.Create(pinfo, key, members) is Profile<PropertyInfo> p)
                    Properties.Add(p);
            foreach (FieldInfo finfo in typeof(T).GetFields(BINDING_FILTER))
                if (Profile<FieldInfo>.Create(finfo, key, members) is Profile<FieldInfo> f)
                    Fields.Add(f);
        }

        private class Profile<U> where U:MemberInfo
        {
            public readonly string MemberName;
            public readonly U MemberInfo;
            public readonly ConfigurationAttribute ConfigAttribute;

            public static Profile<U> Create(U minfo, string key, ISet<string> existingNames)
            {
                if (minfo is PropertyInfo pinfo)
                {
                    if (!(pinfo.CanWrite && pinfo.CanRead))
                        throw new InvalidOperationException("PropertInfo profiles must be readable and writeable.");
                }

                if (minfo is FieldInfo finfo)
                {
                    if (finfo.IsInitOnly)
                        throw new InvalidOperationException("FieldInfo profiles must not be readonly.");
                }
                else
                    throw new InvalidOperationException("Invalid profile type: " + minfo.GetType().Name);

                var configs = minfo.GetCustomAttributes<ConfigurationAttribute>(true).Where(ca => key.Equals(ca.Key));
                if (!configs.Any())
                    return null;
                if (configs.Count() > 1)
                    throw new InvalidOperationException("Multiple configurations for member '" + minfo.Name + "' under key '" + key + "'.");
                var config = configs.First();

                string name = (string.IsNullOrWhiteSpace(config.Name)) ? minfo.Name : config.Name;
                if (!existingNames.Add(name))
                    throw new InvalidOperationException("Duplicate configuration profile name '" + name + "' under key '" + key + "'.");

                return new Profile<U>(name, minfo, config);
            }
            private Profile(string memberName, U minfo, ConfigurationAttribute configAttrib)
            {
                this.MemberName = memberName;
                this.MemberInfo = minfo;
                this.ConfigAttribute = configAttrib;
            }
        }


        public void Save(T obj, string filename = null)
        {
            throw new NotImplementedException();
        }

        public void Load(T target, string filename = null)
        {
            throw new NotImplementedException();
        }

    }
}
