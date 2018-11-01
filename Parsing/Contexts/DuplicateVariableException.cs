using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Contexts
{
    public class DuplicateVariableException : Exception
    {
        public readonly string ContextName, VariableName;
        public DuplicateVariableException(string varName, string contextName)
            : base("A variable with name " + varName + " already exists in context " + contextName + ".")
        {
            this.ContextName = contextName;
            this.VariableName = varName;
        }
    }
}
