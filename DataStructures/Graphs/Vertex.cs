using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Graphs
{
    public class Vertex<T>
    {
        public readonly T Data;
        public readonly HashSet<Edge<T>> Edges = new HashSet<Edge<T>>();
        public Vertex(T data)
        {
            this.Data = data;
        }
    }
}
