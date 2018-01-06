using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Graphs
{
    //public class Edge<T> : IEdge<T>
    //{
    //    /// <summary>
    //    /// The vertex this edge is coming from.
    //    /// </summary>
    //    public IVertex<T> Prior { get; }
    //    /// <summary>
    //    /// The vertex this edge is going to.
    //    /// </summary>
    //    public IVertex<T> Next { get; }
    //    /// <summary>
    //    /// The weight of this edge.
    //    /// </summary>
    //    public double Weight { get; } = 1.0;

    //    public Edge(IVertex<T> prior, IVertex<T> next, double weight = 1.0)
    //    {
    //        this.Prior = prior;
    //        this.Next = next;
    //        this.Weight = weight;
    //    }

    //    /// <summary>
    //    /// Returns true if the Prior and Next vertices are Equals().
    //    /// </summary>
    //    public bool IsParallel(IEdge<T> other)
    //    {
    //        if (other == null) return false;
    //        return Prior.Equals(other.Prior) && Next.Equals(other.Next);
    //    }
    //    /// <summary>
    //    /// Returns true if the Prior and Next vertices are Equals(), and the weight of the given edge equals this weight.
    //    /// </summary>
    //    public override bool Equals(object obj)
    //    {
    //        Edge<T> other = obj as Edge<T>;
    //        if (other == null) return false;
    //        return Prior.Equals(other.Prior) && Next.Equals(other.Next) && Weight == other.Weight;
    //    }
    //    public override int GetHashCode()
    //    {
    //        return Math.Abs(Prior.Label.GetHashCode() + Next.Label.GetHashCode());
    //    }

    //    /// <summary>
    //    /// Returns an identically-weighted edge whose Prior and Next are reversed.
    //    /// </summary>
    //    public static Edge<T> operator -(Edge<T> original)
    //    {
    //        return new Edge<T>(original.Next, original.Prior, original.Weight);
    //    }
    //}
}
