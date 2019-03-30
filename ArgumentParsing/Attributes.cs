using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arguments
{
    /// <summary>The parsing layout patterns for an argument.</summary>
    public enum ArgumentPattern
    {
        /// <summary>An option which does not allow an assigned value.</summary>
        KeyOnly,
        /// <summary>An option which may allow an assigned value, but which does not require it.</summary>
        ValueOptional,
        /// <summary>An option which may must have a value associated with it.</summary>
        ValueRequired
    }

    [AttributeUsage(validOn: AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class AliasAttribute : Attribute
    {
        internal static readonly bool DEFAULT_CASE_SENSITIVE = false;
        internal readonly string Alias;
        internal readonly bool IsCaseSensitive;
        public AliasAttribute(bool isCaseSensitive, string alias) { this.IsCaseSensitive = isCaseSensitive; this.Alias = alias; }
        public AliasAttribute(string alias) : this(DEFAULT_CASE_SENSITIVE, alias) { }
    }

    /// <summary>Indicates a conjunctive set of argument requirements.</summary>
    [AttributeUsage(validOn: AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class GroupAttribute : Attribute
    {
        internal static readonly bool DEFAULT_GROUP_REQUIRED = false;
        internal readonly string Name;
        internal readonly IEnumerable<string> Exclusions;
        internal readonly bool? Required = null;
        /// <summary>Creates a new group attribute.</summary>
        public GroupAttribute(string name, params string[] exclusions)
        {
            this.Name = name;
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
    /// Marks the properties and fields that will be parsed from an argument array.
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class PatternAttribute : Attribute
    {
        /// <summary>The type of this option.</summary>

        /// <summary>Whether this <see cref="PatternAttribute"/> will allow an associated value or not.</summary>
        public ArgumentPattern Pattern = ArgumentPattern.KeyOnly;

        /// <summary>
        /// If a value is associated with this option, this method will parse the given string into a value.  If the 
        /// given value is not parsed (as in the case of an optional value), this method should return null.
        /// </summary>
        public Func<string, object> ValueParser = null;

        /// <summary>Creates a new pattern attribute specifying the pattern type and the applicable parser.</summary>
        public PatternAttribute(ArgumentPattern pattern, Func<string, object> parser)
        {
            if (pattern == ArgumentPattern.KeyOnly && parser != null)
                throw new Exception("Key-only argument patterns may not have a supplied parser.");
            Pattern = pattern;
            ValueParser = parser;
        }

        /// <summary>Creates a new pattern attribute specifying a key-only option pattern.</summary>
        public PatternAttribute(ArgumentPattern pattern = ArgumentPattern.KeyOnly) : this(pattern, null) { }
    }

}
