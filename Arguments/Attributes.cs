using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arguments
{
    /// <summary>An attribute indicating that a property, field, or method can be referenced or invoked by an alias 
    /// or flag.</summary>
    [AttributeUsage(validOn: AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class AliasAttribute : Attribute
    {
        internal const bool DEFAULT_CASE_SENSITIVE = false;
        internal readonly string Alias;
        internal readonly char Flag = '\0';
        internal readonly bool IsCaseSensitive;

        /// <summary>
        /// Creates an alias attribute with the indicated name and case sensitivity rule.  Optionally, a flag may be 
        /// provided.
        /// </summary>
        public AliasAttribute(string alias, bool isCaseSensitive = DEFAULT_CASE_SENSITIVE, char flag = '\0')
        {
            this.IsCaseSensitive = isCaseSensitive;
            this.Alias = alias;
            this.Flag = flag;
        }

    }

    /// <summary>Indicates a conjunctive set of argument requirements.</summary>
    [AttributeUsage(validOn: AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class GroupAttribute : Attribute
    {
        internal const bool DEFAULT_GROUP_REQUIRED = false;
        internal readonly string Name;
        internal readonly IEnumerable<string> Exclusions;
        internal readonly bool? Required = null;
        /// <summary>Creates a new group attribute.</summary>
        public GroupAttribute(string name, params string[] exclusions)
        {
            this.Name = name.ToLower();
            this.Exclusions = exclusions;            
        }
        /// <summary>Creates a new group attribute.</summary>
        public GroupAttribute(string name, bool required, params string[] exclusions) : this (name, exclusions)
        {
            Required = required;
        }
    }
    


    /// <summary>Apply this attribute to determine the help message when parsing fails for this property or field.
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class HelpAttribute : Attribute
    {
        internal readonly string _Message;
        /// <summary>Creates a new help message attribute.</summary>
        public HelpAttribute(string message) { this._Message = message; }
    }


    /// <summary>
    /// Marks the properties and fields that will be specially parsed from an argument array.
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class InvocationAttribute : Attribute
    {
        /// <summary>The property names parsed by this method.</summary>
        public readonly string Invocation;

        /// <summary>Creates a new pattern attribute specifying the pattern type and the applicable parser.</summary>
        public InvocationAttribute(string invocation) { this.Invocation = invocation; }
    }

    /// <summary>Use this attribute to mark a member as unavailable for automatic parsing.</summary>
    [AttributeUsage(validOn: AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public sealed class NoParseAttribute : Attribute { }

}
