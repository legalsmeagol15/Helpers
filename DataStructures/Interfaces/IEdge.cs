using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public interface IEdge<TLabel>
    {
        IVertex<TLabel> Prior { get; }
        IVertex<TLabel> Next { get; }
        double Weight { get; }
    }
}
