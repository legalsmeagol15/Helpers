using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Graphs
{
    public class Edge<T>
    {
        public readonly Vertex<T> Prior;
        public readonly Vertex<T> Next;

        public readonly double Weight = 1.0;

        public Edge(Vertex<T> prior, Vertex<T> next, double weight = 1.0)
        {
            this.Prior = prior;
            this.Next = next;
            this.Weight = weight;
        }

        public bool EqualsWeighted(Edge<T> other)
        {
            if (!Equals(other)) return false;
            return Weight == other.Weight;
        }
        public override bool Equals(object obj)
        {
            Edge<T> other = obj as Edge<T>;
            if (other == null) return false;
            return Prior.Equals(other.Prior) && Next.Equals(other.Next);
        }
        public override int GetHashCode()
        {
            return Math.Abs(Prior.Data.GetHashCode() + Next.Data.GetHashCode());
        }
    }
}
