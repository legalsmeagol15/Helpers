using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dependency
{

    public interface IContext
    {
        object Parent { get; }        

        bool Get(string name, out Variable variable);
        bool Get(string name, out object subcontext);        
    }
    
}
