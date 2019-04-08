using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Functions
{
    public interface IVariable<T>
    {
        T Value { get; }
    }
}
