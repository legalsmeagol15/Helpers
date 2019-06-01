using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    public class Vector : IEvaluateable
    {
        IEvaluateable IEvaluateable.Value => throw new NotImplementedException();
    }
}
