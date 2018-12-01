using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parsing.Dependency;

namespace Parsing
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
        public readonly Variable Tested, Dependee;
        IEvaluateable Contents;
        public CircularDependencyException(IEvaluateable contents, Variable tested, Variable dependee) : base("A circular dependency exists.")
        {
            this.Contents = contents; this.Tested = tested; this.Dependee = dependee;
        }
    }

    public class ContextInvalidException : Exception
    {
        public readonly Context Context;
        public ContextInvalidException(Context context, string message = null) : base(message ?? "Context traversal exception.") { this.Context = context; }
    }



    public sealed class DependencyAttributeException : Exception
    {
        public DependencyAttributeException(string message) : base(message) { }

        public DependencyAttributeException(string message, Exception innerException) : base(message, innerException) { }
    }

}
