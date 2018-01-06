using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphs
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public interface IVertex<TData>
    {
        /// <summary>
        /// 
        /// </summary>
        TData Data { get; }

        /// <summary>
        /// 
        /// </summary>
        IEnumerable<IEdge<TData>> Edges { get; }
    }
}
