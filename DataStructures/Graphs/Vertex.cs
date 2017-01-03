using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Graphs
{
    public class Vertex<T> : IVertex<T>
    {
        /// <summary>
        /// The data associated with this vertex.
        /// </summary>
        public T Label { get; }

        
        /// <summary>
        /// The edges associated with this vertex.
        /// </summary>
        public readonly HashSet<Edge<T>> Edges = new HashSet<Edge<T>>();
        IEnumerable<IEdge<T>> IVertex<T>.Edges { get { return Edges; } }

        /// <summary>
        /// Creates a new vertex with the given data.
        /// </summary>
        /// <param name="data"></param>
        public Vertex(T data)
        {
            if (data == null) throw new ArgumentException("Vertex.Data cannot be null.");
            Label = data;
        }
        /// <summary>
        /// Returns true if the label of this vertex Equals() the label of the given vertex.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Vertex<T>) return ((Vertex<T>)obj).Label.Equals(Label);
            return false;
        }
        /// <summary>
        /// Returns the hash code of the label.
        /// </summary>        
        public override int GetHashCode()
        {
            return Label.GetHashCode();
        }
        /// <summary>
        /// Returns the string representation of the label.
        /// </summary>        
        public override string ToString()
        {
            return Label.ToString();
        }
    }
}
