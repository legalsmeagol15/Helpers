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
            if (data == null) throw new ArgumentException("Vertex.Data cannot be null.");
            Data = data;
        }
        public override bool Equals(object obj)
        {
            if (obj is Vertex<T>) return ((Vertex<T>)obj).Data.Equals(Data);
            return false;
        }
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }
        public override string ToString()
        {
            return "Vertex(" + Data.ToString() + ")";
        }
    }
}
