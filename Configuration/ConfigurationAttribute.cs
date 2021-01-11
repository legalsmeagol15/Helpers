using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using DataStructures;
using System.Reflection;

namespace Helpers
{
    public enum Flags
    {
        None = 0,
        IsRequired = 1 << 1,
        IsSubsection = 1 << 2
    }

    [AttributeUsage(validOn: AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class ConfigurationAttribute : System.Attribute
    {
        public readonly string Key;
        public readonly string Name;
        public readonly Flags Flags;
        public readonly VersionInvervalSet Versions;
        public readonly dynamic DefaultValue;
        public readonly Type LoaderType;

        public bool Includes(Version version) => Versions.Contains(version);

        public ConfigurationAttribute(string name = null, object defaultValue = null, Flags flags = Flags.None, string versionControls = ">=0.0.0.0", string key = "")
        {
            this.Key = key;
            this.Name = name;
            this.DefaultValue = defaultValue;
            this.Flags = flags;
            if (string.IsNullOrWhiteSpace(versionControls))
                this.Versions = new VersionInvervalSet(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            else
                this.Versions = new VersionInvervalSet(versionControls.Split(','));
        }
    }

    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public sealed class ConfigurationDeclaredAttribute : ConfigurationAttribute
    {
        public readonly string MemberName;

        public ConfigurationDeclaredAttribute(string name, string memberName = null, object defaultValue = null, Flags flags = Flags.None, string versionControls = ">=0.0.0.0", string key = "")
            : base (name, defaultValue, flags, versionControls, key)
        {
            this.MemberName = memberName;
        }
    }

    
}
