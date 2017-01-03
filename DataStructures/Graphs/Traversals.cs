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
        public static IEnumerable<IEdge<T>> DepthFirstTraversal<T>(IVertex<T> vertex)
        {
            //Mark the given vertex as visited.
            HashSet<IVertex<T>> visited = new HashSet<IVertex<T>>();
            visited.Add(vertex);

            //Add the given vertex's edges to the stack.
            Stack<IEdge<T>> edgeStack = new Stack<IEdge<T>>();
            foreach (IEdge<T> e in vertex.Edges) edgeStack.Push(e);

            //Work thru the stack.
            while (edgeStack.Count > 0)
            {
                IEdge<T> edge = edgeStack.Pop();
                if (visited.Contains(edge.Next)) continue; //If an edge leads to a visited vertex, go on to the next edge.
                yield return edge;
                
                vertex = edge.Next;
                visited.Add(vertex);
                foreach (Edge<T> e in vertex.Edges) edgeStack.Push(e);
            }
        }

        public static IEnumerable<IEdge<T>> BreadthFirstTraversal<T>(IVertex<T> vertex)
        {
            HashSet<IVertex<T>> visited = new HashSet<IVertex<T>>();
            visited.Add(vertex);

            Queue<IEdge<T>> edgeQueue = new Queue<IEdge<T>>();
            foreach (Edge<T> e in vertex.Edges) edgeQueue.Enqueue(e);

            while (edgeQueue.Count > 0)
            {
                IEdge<T> edge = edgeQueue.Dequeue();
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
        public static IEnumerable<IEdge<T>> GetShortestUnweightedPath<T>(IVertex<T> vertex, IVertex<T> goal)
        {
            Dictionary<IVertex<T>, IEdge<T>> Priors = new Dictionary<IVertex<T>, IEdge<T>>();
            Priors.Add(vertex, null);
            Queue<IEdge<T>> edgeQueue = new Queue<IEdge<T>>();
            foreach (Edge<T> e in vertex.Edges) edgeQueue.Enqueue(e);

            //Traverse and build a set of prior links.
            while (edgeQueue.Count > 0)
            {
                IEdge<T> edge = edgeQueue.Dequeue();                
                if (Priors.ContainsKey(edge.Next)) continue;

                Priors.Add(edge.Next, edge);
                vertex = edge.Next;
                if (vertex.Equals(goal)) break;
                foreach (Edge<T> e in vertex.Edges) edgeQueue.Enqueue(e);
            }

            //Now, traverse the trail in reverse.
            if (!Priors.ContainsKey(goal)) return null;
            LinkedList<IEdge<T>> trail = new LinkedList<IEdge<T>>();            
            while (true)
            {
                IEdge<T> edgeTo = Priors[goal];
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

        public static IEnumerable<Tuple<T,T>> ShortestPath<T>(T vertex, Func<T, IEnumerable<T>> neighborGetter, Func<T,T,double> weightGetter)
        {
            HashSet<T> visited = new HashSet<T>();
            visited.Add(vertex);
            Stack<Edge<T>> stack = new Stack<Edge<T>>();
            throw new NotImplementedException();
            
        }
    }
}
