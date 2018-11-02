using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parsing.Contexts;

namespace Parsing
{

    public class SyntaxException : Exception
    {
        public readonly string Entry;
        public readonly int Position;
        public readonly string Context;
        internal SyntaxException(string message, string entry, int position, string context) : this(message, entry, position, context,null) { }
        internal SyntaxException(string message, string entry, int position, string context, Exception inner) : base(message, inner)
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

    public class ContextTraversalException : Exception
    {
        public readonly IContext Revisited;
        public ContextTraversalException(IContext revisited) : base("Context traversal exception.") { this.Revisited = revisited; }
    }


    /// <summary>
    /// Thrown when a Variable, in the course of pruning itself from its sources and listeners, manages to end up referenced by one 
    /// of its listeners even after attempting to remove itself.  Note that this exception may indicate that this Variable's lists of 
    /// sources and listeners may be partially prune, i.e., corrupted.  Accordingly, it is basically used for validation.
    /// </summary>
    public class PrunedVariableException : Exception
    {
        public readonly Variable Pruned, Variable;
        public PrunedVariableException(Variable pruned, Variable variable)
            : this(pruned, variable, "A pruned variable " + pruned.Name + " has been referenced in Variable " + variable.Name + ".")
        { }
        public PrunedVariableException(Variable pruned, Variable variable, string message)
            : base(message)
        {
            this.Pruned = pruned;
            this.Variable = variable;
        }

    }
}
