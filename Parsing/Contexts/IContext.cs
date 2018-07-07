using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    public interface IContext : IEnumerable<IContext>
    {        
        bool TryGet(string key, out IContext sub);

        bool TryGet(string key, out IEvaluateable var);
    }
}
