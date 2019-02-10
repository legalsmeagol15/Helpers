using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dependency.Dependency
{
    /*
     * Imagine a struct that is defined like this:
     * struct Color {
     * }
     */

    public abstract class ClassProfile
    {
        public readonly Dictionary<string, PropertyProfile> PropertyProfiles = new Dictionary<string, PropertyProfile>();
        public readonly Dictionary<string, FunctionProfile> FunctionProfiles = new Dictionary<string, FunctionProfile>();
        internal static Dictionary<Type, ClassProfile> _ClassProfiles = new Dictionary<Type, ClassProfile>();

        internal readonly Regex VariableNames = null;
    }
    
    public abstract class PropertyProfile
    {

    }
    internal sealed class PropertyProfile<T> : PropertyProfile
    {

    }

    public abstract class FunctionProfile
    {

    }
    
}
