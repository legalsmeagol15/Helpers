using System;
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

    /// <summary>
    /// Use this attribute to mark the properties and fields of a class that should be included in 
    /// configuration.
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class ConfigurationAttribute : System.Attribute
    {
        public readonly string Key;
        public readonly Flags Flags;
        public readonly VersionInvervalSet Versions;
        public readonly dynamic DefaultValue;
        public readonly Type TypeConverter;

        public bool Includes(Version version) => Versions.Contains(version);

        public ConfigurationAttribute(object defaultValue = null, Flags flags = Flags.None, 
                                      string versionControls = ">=0.0.0.0", string key = "", 
                                      Type typeConverter = null)
        {
            this.Key = key;
            this.DefaultValue = defaultValue;
            this.Flags = flags;
            if (string.IsNullOrWhiteSpace(versionControls))
                this.Versions = new VersionInvervalSet(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            else
                this.Versions = new VersionInvervalSet(versionControls.Split(','));
            if (typeConverter != null && !(typeof(System.ComponentModel.TypeConverter)).IsAssignableFrom(typeConverter))
                throw new ArgumentException(nameof(typeConverter) + " must inherit from " + typeof(System.ComponentModel.TypeConverter).FullName + ".", nameof(typeConverter));
            this.TypeConverter = typeConverter;
        }
    }

    /// <summary>
    /// Use this attribute to mark the properties declared in parent classes or partial classes 
    /// that should be included in configuration.
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public sealed class ConfigurationDeclaredAttribute : ConfigurationAttribute
    {
        public readonly string MemberName;

        public ConfigurationDeclaredAttribute(string memberName, object defaultValue = null,
                                              Flags flags = Flags.None, string versionControls = ">=0.0.0.0", 
                                              string key = "", Type typeConverter = null)
            : base (defaultValue, flags, versionControls, string.IsNullOrWhiteSpace(key) ? memberName : key, 
                    typeConverter)
        {
            this.MemberName = memberName;            
        }
    }

    
}
