using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Graphs
{
    public static class Traversals
    {
       

        public static IEnumerable<Edge<T>> DepthFirstTraversal<T>(Vertex<T> vertex)
        {
            //Mark the given vertex as visited.
            HashSet<Vertex<T>> visited = new HashSet<Vertex<T>>();
            visited.Add(vertex);

            //Add the given vertex's edges to the stack.
            Stack<Edge<T>> edgeStack = new Stack<Edge<T>>();
            foreach (Edge<T> e in vertex.Edges) edgeStack.Push(e);

            //Work thru the stack.
            while (edgeStack.Count > 0)
            {
                Edge<T> edge = edgeStack.Pop();
                if (visited.Contains(edge.Next)) continue; //If an edge leads to a visited vertex, go on to the next edge.
                yield return edge;

                vertex = edge.Next;
                visited.Add(vertex);
                foreach (Edge<T> e in vertex.Edges) edgeStack.Push(e);
            }
        }

        public static IEnumerable<Tuple<T,T>> DepthFirstTraversal<T>(T vertex,  Func<T, IEnumerable<T>> neighborGetter)
        {
            //Mark the given vertex as visited.
            HashSet<T> visited = new HashSet<T>();
            visited.Add(vertex);

            //Add the given vertex's edges to the stack.
            Stack<T> stack = new Stack<T>();
            foreach (T neighbor in neighborGetter(vertex)) stack.Push(neighbor);

            //Work thru the stack.
            while (stack.Count > 0)
            {
                T nextVertex = stack.Pop();
                if (visited.Contains(nextVertex)) continue;
                yield return new Tuple<T,T>(vertex, nextVertex);

                vertex = nextVertex;
                visited.Add(vertex);
                foreach (T neighbor in neighborGetter(vertex)) stack.Push(neighbor);
            }           
        }
    }
}
