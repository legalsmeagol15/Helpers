using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.CLArgs
{
    public static class CLArgs
    {
        public static object Parse<T>(T result, params string[] args)
        {
            if (result == null) throw new ArgumentNullException("result");

            int idx = 0;
            while (idx < args.Length)
            {

            }

        }


    }

    /// <summary>
    /// An exception thrown by the argument parser.  The exception's message will contain the help text.
    /// </summary>
    public sealed class ParsingException : Exception
    {
        /// <summary>Create a new <see cref="ParsingException"/>.</summary>
        /// <param name="message">The help message accompanying the parsing failure.</param>
        public ParsingException(string message) : base(message) { }
    }

    
    [AttributeUsage( validOn: AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class OptionAttribute : Attribute
    {
        /// <summary>The type of this option.</summary>
        public enum OptionPattern
        {
            /// <summary>An option which does not allow an assigned value.</summary>
            KeyOnly,
            /// <summary>An option which may allow an assigned value, but which does not require it.</summary>
            ValueOptional,
            /// <summary>An option which may must have a value associated with it.</summary>
            ValueRequired
        }
        /// <summary>The help text to be displayed in the event of an invalid option.</summary>
        public string Help = null;

        /// <summary>
        /// Whether this option is required for all calls at the command line.  If <see cref="IsRequired"/> is 
        /// <code>true</code>, then it will function as required member of every group of options as well.
        /// </summary>
        public bool IsRequired = false;

        /// <summary>The character that signifies this option.</summary>
        public char Flag;

        /// <summary>
        /// The keywords that will function as aliases to the <see cref="Flag"/> character.
        /// </summary>
        public string[] Aliases = new string[0];

        /// <summary>Whether this <see cref="OptionAttribute"/> will allow an associated value or not.</summary>
        public OptionPattern Pattern = OptionPattern.KeyOnly;

        /// <summary>
        /// If a value is associated with this option, this method will parse the given string into a value.
        /// </summary>
        public Func<string, object> ValueParser = null;

        /// <summary>
        /// The group associated with this option.  If omitted, this option will be part of the default "general" 
        /// group.
        /// </summary>
        public string Group = "general";

    }

    [AttributeUsage(validOn: AttributeTargets.Class |  AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public sealed class HelpAttribute : Attribute
    {
       
    }
    

    internal class Profile
    {
        internal readonly Dictionary<string, Group> Groups = new Dictionary<string, Group>();
        public Profile(Type type)
        {
            OptionAttribute[] options = (OptionAttribute[])type.GetCustomAttributes(typeof(OptionAttribute), true);
            foreach (OptionAttribute oa in options)
            {
                if (oa.Group == null)
                {

                }
            }
        }

        public class Group
        {
            public readonly string Name;
            public readonly List<OptionAttribute> Options;
            public Group(string name) { this.Name = name; }
        }
    }
}
