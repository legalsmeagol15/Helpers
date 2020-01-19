using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Auto
{
    public enum Pattern
    {
        None, Source, Blended
    }

    [AttributeUsage(validOn:AttributeTargets.Property | AttributeTargets.Field, AllowMultiple =false, Inherited =true)]
    public sealed class DependencyVariableAttribute : Attribute
    {
        public readonly Pattern Pattern;
        public DependencyVariableAttribute(Pattern pattern = Pattern.Blended) { this.Pattern = pattern; }
    }

    [AttributeUsage(validOn: AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class DependencyContextAttribute : Attribute
    {

    }

    [AttributeUsage(validOn: AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class DependencyAliasAttribute: Attribute
    {
        public readonly string Alias;
        
        public DependencyAliasAttribute(string alias)
        {
            this.Alias = alias;
        }

    }
}
