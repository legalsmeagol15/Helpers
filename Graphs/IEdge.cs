using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphs
{
    public interface IEdge<TData>
    {
        IVertex<TData> Origin { get; }
        IVertex<TData> Destination { get; }
        double Cost { get; }
    }
}
