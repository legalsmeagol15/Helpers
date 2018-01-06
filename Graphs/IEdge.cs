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
    public interface IEdge<TData>
    {
        /// <summary>
        /// 
        /// </summary>
        IVertex<TData> Origin { get; }
        /// <summary>
        /// 
        /// </summary>
        IVertex<TData> Destination { get; }
        /// <summary>
        /// 
        /// </summary>
        double Cost { get; }
    }
}
