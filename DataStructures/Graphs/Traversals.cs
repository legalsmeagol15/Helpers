using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Graphs
{
    public static class Traversals
    {
       
        /// <summary>
        /// Produces a set of edges that describe a depth-first traversal, starting at the given vertex.
        /// </summary>
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

        public static IEnumerable<Edge<T>> BreadthFirstTraversal<T>(Vertex<T> vertex)
        {
            HashSet<Vertex<T>> visited = new HashSet<Vertex<T>>();
            visited.Add(vertex);

            Queue<Edge<T>> edgeQueue = new Queue<Edge<T>>();
            foreach (Edge<T> e in vertex.Edges) edgeQueue.Enqueue(e);

            while (edgeQueue.Count > 0)
            {
                Edge<T> edge = edgeQueue.Dequeue();
                if (visited.Contains(edge.Next)) continue;
                yield return edge;                

                vertex = edge.Next;
                visited.Add(vertex);
                foreach (Edge<T> e in vertex.Edges) edgeQueue.Enqueue(e);
            }
        }

        /// <summary>
        /// Uses breadth-first traversal to find the shortest unweighted path from  the given start to the given goal.
        /// </summary>
        /// <returns>Returns null if no path exists from the given vertex to the goal; otherwise, returns the path.</returns>
        public static IEnumerable<Edge<T>> GetShortestUnweightedPath<T>(Vertex<T> vertex, Vertex<T> goal)
        {
            Dictionary<Vertex<T>, Edge<T>> Priors = new Dictionary<Vertex<T>, Edge<T>>();
            Priors.Add(vertex, null);
            Queue<Edge<T>> edgeQueue = new Queue<Edge<T>>();
            foreach (Edge<T> e in vertex.Edges) edgeQueue.Enqueue(e);

            //Traverse and build a set of prior links.
            while (edgeQueue.Count > 0)
            {
                Edge<T> edge = edgeQueue.Dequeue();                
                if (Priors.ContainsKey(edge.Next)) continue;

                Priors.Add(edge.Next, edge);
                vertex = edge.Next;
                if (vertex.Equals(goal)) break;
                foreach (Edge<T> e in vertex.Edges) edgeQueue.Enqueue(e);
            }

            //Now, traverse the trail in reverse.
            if (!Priors.ContainsKey(goal)) return null;
            LinkedList<Edge<T>> trail = new LinkedList<Edge<T>>();            
            while (true)
            {
                Edge<T> edgeTo = Priors[goal];
                if (edgeTo == null) break;
                trail.AddFirst(edgeTo);
                goal = edgeTo.Prior;
            }
            return trail;

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
