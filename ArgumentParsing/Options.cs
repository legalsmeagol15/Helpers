using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Arguments
{

    public static class Options
    {
        /// <summary>
        /// Returns an object, of type T, containing the parsed arguments provided.
        /// </summary>
        /// <exception cref="ProfileException">Thrown when an error occurs due to the setup of the class type T.</exception>
        /// <exception cref="ParsingException">Thrown when a supplied argument cannot be parsed.</exception>
        public static T Parse<T>(params string[] args)
        {
            if (typeof(T).GenericTypeArguments.Any())
                throw new ProfileException("Result objects cannot be generic.");
            T optionsObject = Activator.CreateInstance<T>();
            Profile prof = new Profile(typeof(T));
            prof.Parse(args, optionsObject);
            return optionsObject;
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

    /// <summary>An exception thrown when profiling the argument object.</summary>
    public class ProfileException : Exception
    {
        /// <summary>Creates a new <see cref="ProfileException"/>.</summary>
        protected internal ProfileException(string message) : base(message) { }
    }

    /// <summary>An exception thrown when profiling the member names of the argument object.</summary>
    public sealed class NamingException : ProfileException
    {
        /// <summary>Creates a new <see cref="ProfileException"/>.</summary>
        internal NamingException(string message) : base(message) { }
    }

    /// <summary>An exception thrown when graphing the argument groups of the argument object.</summary>
    public sealed class GroupException : ProfileException
    {
        /// <summary>Creates a new <see cref="ProfileException"/>.</summary>
        internal GroupException(string message) : base(message) { }
    }

    /// <summary>An exception thrown when analyzing the argument structure of the argument object.</summary>
    public sealed class StructureException : ProfileException
    {
        internal StructureException(string message) : base(message) { }
    }



}
