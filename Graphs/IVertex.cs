using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphs
{
    public interface IVertex<TData>
    {
        TData Data { get; }

        IEnumerable<IEdge<TData>> Edges { get; }
    }
}
