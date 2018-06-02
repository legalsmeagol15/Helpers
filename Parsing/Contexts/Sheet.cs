using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    /// <summary>
    /// Concurrent access is expected.
    /// </summary>
    public sealed class Sheet : Context
    {
        public Sheet(string name) : base(name) { }

        
    }
}
