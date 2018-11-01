using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Contexts
{
    class ContextNotFoundException : Exception
    {
        public ContextNotFoundException(string missingName, string parentName) 
            : base("No sub-context with name + " + missingName + " found in parent " + parentName +  ".")
        {
        }
    }
}
