using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections;

namespace Graphs
{
    /// <summary>
    /// A <see cref="TopologicalSort{T}"/> can be iterated through to return items in topologically-sorted order, or it can be iterated 
    /// through level-by-level by indexing through the levels.
    /// </summary>    
    public sealed class TopologicalSort<T> : IEnumerable<T>
    {
        //TODO:  validate it all.
        private readonly IList<IList<T>> _Tranches;
        
        /// <summary>Returns the level at the index indicated.  All levels are guaranteed to be completely sorted with respect to their 
        /// contents.</summary>
        public IList<T> Tranch(int index) => _Tranches[index];

        /// <summary>Returns the number of items in the entire sort.</summary>
        public int Count { get => _Tranches.Sum(lvl => lvl.Count); }

        private TopologicalSort() { _Tranches = new List<IList<T>>(); }

        /// <summary>Creates a topological sort beginning at the provided origin.</summary>
        public static TopologicalSort<T> FromOrigin(T origin, Func<T, IEnumerable<T>> edges) => FromOrigin(new T[] { origin }, edges);
        /// <summary>Creates a topological sort beginning at the provided origins.</summary>
        public static TopologicalSort<T> FromOrigin(IEnumerable<T> origins, Func<T, IEnumerable<T>> edges)
        {
            TopologicalSort<T> topoSort = new TopologicalSort<T>();

            // Step #1:  determine how many inbound edges each item has.
            Dictionary<T, Node> nodes = GetTopologicalGraph(origins, edges);
            Queue<Node> queue = new Queue<Node>();

            // Step #2:  now, go level-by-level to produce the sort.
            foreach (Node originNode in origins.Select(o => nodes[o])) queue.Enqueue(originNode);
            Queue<Node> nextQueue = new Queue<Node>();
            List<T> thisLevel = new List<T>();
            while (queue.Count > 0)
            {
                Node focusNode = queue.Dequeue();
                thisLevel.Add(focusNode.Item);
                Debug.Assert(!nodes.ContainsKey(focusNode.Item), "Oops.  In Step #2, the focusNode.Item shouldn't exist in Nodes.");
                foreach (Node childNode in focusNode.Children)
                {
                    if (--childNode.Inbound == 0) { nextQueue.Enqueue(childNode); nodes.Remove(childNode.Item); }
                    Debug.Assert(childNode.Inbound >= 0, "Oops.  Removed more inbound than actually existed.");
                }
                if (queue.Count == 0)
                {
                    queue = nextQueue;
                    nextQueue = new Queue<Node>();
                    topoSort._Tranches.Add(thisLevel);
                    thisLevel = new List<T>();
                }                
            }

            // Step #3 - sanity check
            Debug.Assert(!nodes.Any(), "Oops.  There were some nodes part of the graph which were descendants of the origin were not removed.");

            // Step #4 - return the completed sort.
            return topoSort;
        }

        private IEnumerator<T> GetEnumerator() { foreach (IList<T> level in _Tranches) foreach (T item in level) yield return item; }
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns a topological graph, which is a set of nodes whose <seealso cref="Node.Inbound"/> property counts the 
        /// number of inbound edges.
        /// </summary>   
        public static Dictionary<T, Node> GetTopologicalGraph(T origin, Func<T, IEnumerable<T>> edges) 
            => GetTopologicalGraph(new T[] { origin }, edges);

        /// <summary>
        /// Returns a topological graph, which is a set of nodes whose <seealso cref="Node.Inbound"/> property counts the 
        /// number of inbound edges.
        /// </summary>        
        public static Dictionary<T, Node> GetTopologicalGraph(IEnumerable<T> origins, Func<T, IEnumerable<T>> edges)
        {
            Dictionary<T, Node> dict = new Dictionary<T, Node>();
            Queue<Node> queue = new Queue<Node>();
            IEnumerable<Node> originNodes = origins.Select(orig => new Node(orig, 0));
            foreach (Node originNode in originNodes)
                queue.Enqueue(originNode);
            HashSet<T> visited = new HashSet<T>();
            while (queue.Count > 0)
            {
                Node focusNode = queue.Dequeue();
                if (!visited.Add(focusNode.Item)) throw new CycleException(focusNode.Item, edges);
                foreach (T child in edges(focusNode.Item))
                {
                    if (dict.TryGetValue(child, out Node childNode)) childNode.Inbound++;
                    else dict[child] = (childNode = new Node(child, 1));
                    focusNode.Children.Add(childNode);
                    queue.Enqueue(childNode);
                }                
            }
            return dict;
        }

        /// <summary>
        /// The levels.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IList<T>> Tranches() => _Tranches;

        /// <summary>A graph node suitable for performing a topological sort.</summary>
        public class Node
        {
            /// <summary>
            /// 
            /// </summary>
            public readonly T Item;            

            /// <summary>
            /// 
            /// </summary>
            public int Inbound;

            /// <summary>
            /// 
            /// </summary>
            public readonly List<Node> Children = new List<Node>();  // Makes Step #2 go a little faster.

            /// <summary>
            /// 
            /// </summary>
            /// <param name="item"></param>
            /// <param name="inbound"></param>
            public Node(T item, int inbound) { this.Item = item; this.Inbound = inbound; }
        }



        /// <summary>
        /// An exception thrown when a topological sort is attempted on a graph containing a cycle.  Only directed acyclic graphs can 
        /// produce a valid topological sort.
        /// </summary>
        public class CycleException : Exception
        {
            /// <summary>An item on the indicated cycle.</summary>
            public readonly T Item;
            /// <summary>The items that constitute the discovered cycle.</summary>
            public readonly IList<T> Cycle;
            /// <summary>Creates a new CycleException.</summary>
            public CycleException(T item, Func<T, IEnumerable<T>> edges) 
                : base("A cycle exists in the given graph.  A valid topological sort cannot be produced.")
            {
                // TODO:  find the cycle using "edges".  Not needed now.
                Cycle = null;
                this.Item = item;
            }
        }
    }

    
}
