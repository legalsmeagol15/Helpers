using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public interface IVertex<TLabel>
    {
        TLabel Label { get; }
        IEnumerable<IEdge<TLabel>> Edges { get; }
    }
}
