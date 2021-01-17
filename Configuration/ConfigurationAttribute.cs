using System;
using DataStructures;
using System.Reflection;

namespace Helpers
{
    
    /// <summary>
    /// Use this attribute to mark the properties and fields of a class that should be included in 
    /// configuration.
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class ConfigurationAttribute : System.Attribute
    {
        public readonly string Key;
        public bool IsRequired;
        public readonly VersionInvervalSet Versions;
        public readonly dynamic DefaultValue;
        public readonly Type TypeConverter;
        public readonly string[] ConversionXPaths;
        public readonly bool ApplyToSubsections;

        public bool Includes(Version version) => Versions.Contains(version);

        public ConfigurationAttribute(object defaultValue = null,bool isRequired = true,
                                      string versionControls = ">=0.0.0.0", string key = "", 
                                      bool apply_to_subsections = true,
                                      Type typeConverter = null, params string[] conversionXPaths)
        {
            this.Key = key;
            this.DefaultValue = defaultValue;
            this.IsRequired = isRequired;
            this.ApplyToSubsections = apply_to_subsections;
            if (string.IsNullOrWhiteSpace(versionControls))
                this.Versions = new VersionInvervalSet(Assembly.GetExecutingAssembly().GetName().Version.ToString());
            else
                this.Versions = new VersionInvervalSet(versionControls.Split(','));
            if (typeConverter != null && !(typeof(System.ComponentModel.TypeConverter)).IsAssignableFrom(typeConverter))
                throw new ArgumentException(nameof(typeConverter) + " must inherit from " + typeof(System.ComponentModel.TypeConverter).FullName + ".", nameof(typeConverter));
            this.TypeConverter = typeConverter;
            if (conversionXPaths != null && conversionXPaths.Length > 0)
            {
                if (!typeof(ConfigurationConverter).IsAssignableFrom(this.TypeConverter))
                    throw new ArgumentException("Only " + nameof(ConfigurationConverter) + " types may designate " + nameof(conversionXPaths));
                this.ConversionXPaths = conversionXPaths;
            }
            else
                this.ConversionXPaths = null;
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
                                              bool isRequired = true, 
                                              string versionControls = ">=0.0.0.0", 
                                              string key = "", bool applyToSubsections=true,
                                              Type typeConverter = null, 
                                              params string[] conversionXPaths)
            : base (defaultValue, isRequired, versionControls, string.IsNullOrWhiteSpace(key) ? memberName : key, 
                    applyToSubsections, typeConverter, conversionXPaths)
        {
            this.MemberName = memberName;            
        }
    }

    
}
