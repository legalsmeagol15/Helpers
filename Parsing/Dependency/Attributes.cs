using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dependency
{
    
    
    public enum ReturnTypes
    {
        None = 0,
        Number = 1,
        String = 2,
        Matrix = 4,
        Variable = 8,
        Formula  = 16,
        All = (Number | String | Matrix | Variable | Formula)
    }
    

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class DependencyFunctionAttribute : Attribute
    {
       
    }
    



    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class DependencyPropertyAttribute : Attribute
    {        
        /// <summary>
        /// A bitwise flag indicating the allowed return types of a property.
        /// </summary>
        public ReturnTypes ReturnTypes { get; } = ReturnTypes.All;
                
        /// <summary>The starting contents of the dependency property associated with this property.</summary>
        public string Contents { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public TypeGuarantee Types { get; } = TypeGuarantee.ANY;

        /// <summary>
        /// The set of names that may refer to this property within its context.  For example, in the context of a 
        /// color, "red" may refer to the red value, but so may "r" or "RED".  Omit all aliases if only the name of 
        /// the property should be used (and make sure that <seealso cref="AliasesOnly"/> is set to false).
        /// </summary>
        public IEnumerable<string> Aliases { get; set; }


        /// <summary>
        /// Whether a dependency property is intended to be weakly referenced.
        /// </summary>
        public bool IsWeak { get; set; } = false;

        /// <summary>
        /// If this property is true, the property's name will not be include among the reference paths to this 
        /// property.
        /// </summary>
        public bool AliasesOnly { get; set; } = false;
    }


    

}
