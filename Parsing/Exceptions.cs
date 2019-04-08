using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Parsing.Dependency;

namespace Dependency
{

    public class ReferenceTooShortException : Exception
    {
        public readonly Reference Reference;
        public ReferenceTooShortException(Reference reference)
            : base("A reference must point to a " + typeof(Variable).Name + " or " + typeof(Function).Name
                  + ".  The reference \"" + reference.ToString() + "\" does not.")
        { this.Reference = reference; }
    }

    public class ReferenceTooLongException : Exception
    {
        public readonly Reference Reference;
        public ReferenceTooLongException(Reference reference)
            : base("A reference must conclude with a " + typeof(Variable).Name + " or " + typeof(Function).Name
                  + ".  The reference \"" + reference.ToString() + "\" does not end with a valid reference.")
        { this.Reference = reference; }
    }

    public class ReferenceUnmatchedException : Exception
    {
        public readonly Reference Reference;
        public ReferenceUnmatchedException(Reference reference)
            : base("No valid referenced matches \"" + reference.ToString() + "\".")
        { this.Reference = reference; }
    }

    public class SyntaxException : Exception
    {
        public readonly string Entry;
        public readonly int Position;
        public readonly object Context;
        internal SyntaxException(string message, string entry, int position, object context) : this(message, entry, position, context,null) { }
        internal SyntaxException(string message, string entry, int position, object context, Exception inner) : base(message, inner)
        {
            this.Entry = entry;
            this.Position = position;
            this.Context = context;            
        }
    }


    public class CircularDependencyException : Exception
    {
        /// <summary>
        /// The listener or dependent that currently exists in the dependency system.
        /// </summary>
        public readonly Variable ExistingListener;

        /// <summary>
        /// The source or dependee that currently exists in the dependency system.
        /// </summary>
        public readonly Variable ExistingSource;
        IEvaluateable Contents;
        public CircularDependencyException(IEvaluateable contents, Variable listener, Variable source)
            : base("A circular dependency exists:  \"" + listener.Aliases.FirstOrDefault() + "\" already listens to source \"" + source.Aliases.FirstOrDefault() + "\".")
        {
            this.Contents = contents; this.ExistingListener = listener; this.ExistingSource = source;
        }
    }
    


    public sealed class DependencyAttributeException : Exception
    {
        public DependencyAttributeException(string message) : base(message) { }

        public DependencyAttributeException(string message, Exception innerException) : base(message, innerException) { }
    }

}
