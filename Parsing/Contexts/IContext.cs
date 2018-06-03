using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    public interface IContext : IEnumerable<IContext>
    {        
        IContext GetSubContext(string key);
    }
}
