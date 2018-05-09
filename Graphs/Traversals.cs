using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphs
{
    /// <summary>
    /// A collection of graph-traversal methods.
    /// </summary>
    public static class Traversals
    {

        /// <summary>
        /// Performs a depth first traversal of the graph defined by the given item, with the neighbors of a vertex defined by the 
        /// neighbor getter function.  The items returned will be all those not excluded by the given limiter function.
        /// </summary>        
        /// <param name="root">An item contained in the graph to be traversed.</param>
        /// <param name="neighborGetter">The function used to find the neighbors of a given vertex.  For example, providing "(a) => a.getChildren()" will cause the traversal to examine the 
        /// getChildren() method defined in 'T' to determine where to examine next.</param>
        /// <param name="limiter">The function that specifies the limits of where to traverse.  So long as the limiter returns 
        /// 'true' regarding a particular node, that node will be included in the traversal.  If omitted, all nodes reachable with the 
        /// given <paramref name="neighborGetter"/> will be traversed.</param>
        /// <param name="rejected">The collection to which non-traversed nodes (leafs) are are added.  A rejected child is one 
        /// that will not permit traversal under the limiter function.  For example, imagine that the class Node has a List&lt;Node&gt; called Children 
        /// defined, to represent a tree structure.  The variable 'root' is the Node object to be traversed. Then calling: <para/>
        /// DepthFirstTraversal(root, o&gt;o.Children) <para/>
        /// will return all the nodes in the tree structure, whether branch or leaf.  On the other hand, <para/>
        /// DepthFirstTraversal(root, o=&gt;o.Children, candidate=&gt;candidate.Children.Count&gt;0, new List&lt;Node&gt; leaves) will return
        /// all non-leaf Nodes in the tree, but return the leaves in the new list.        /// 
        /// </param>
        /// <remarks>This method uses the yield-return pattern for traversed nodes.</remarks>
        public static IEnumerable<T> DepthFirstTraversal<T>(T root, Func<T, IEnumerable<T>> neighborGetter, Func<T, bool> limiter, ICollection<T> rejected)
        {   
            limiter = limiter ?? (node => true);
            HashSet<T> visited = new HashSet<T>();
            Stack<T> stack = new Stack<T>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                T focus = stack.Pop();
                visited.Add(focus);
                yield return focus;
                IEnumerable<T> children = neighborGetter(focus);
                foreach (T child in children)
                {
                    if (visited.Contains(child)) continue;
                    if (limiter(child)) stack.Push(child);
                    else if (rejected != null) rejected.Add(child);                    
                }
                    
            }
        }

        /// <summary>
        /// Performs a depth first traversal of the graph defined by the given item, with the neighbors of a vertex defined by the 
        /// neighbor getter function.
        /// </summary>        
        /// <param name="root">An item contained in the graph to be traversed.</param>
        /// <param name="neighborGetter">The function used to find the neighbors of a given vertex.  For example, providing 
        /// "(a) => a.getChildren()" will cause the traversal to examine the getChildren() method defined in 'T' to determine where to 
        /// examine next.</param>        
        /// <remarks>This method uses the yield-return pattern for traversed nodes.</remarks>
        public static IEnumerable<T> DepthFirstTraversal<T>(T root, Func<T, IEnumerable<T>> neighborGetter)
        {
            HashSet<T> visited = new HashSet<T>();
            Stack<T> stack = new Stack<T>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                T focus = stack.Pop();
                visited.Add(focus);
                yield return focus;
                IEnumerable<T> children = neighborGetter(focus);
                foreach (T child in children)
                    if (!visited.Contains(child)) stack.Push(child);

            }
        }

        


        /// <summary>
        /// A lightweight data object for tracking pre- and post-times in a depth-first search.
        /// </summary>
        private class DFSNode
        {
            public readonly object Data;
            public uint PreTime;
            public uint PostTime;
            public readonly DFSNode Previous;

            public DFSNode(object Data, DFSNode previous = null)
            {
                this.Data = Data;
                PreTime = 0;
                PostTime = 0;
                this.Previous = previous;
            }
        }


        /// <summary>
        /// Performs a breadh-first traversal of the graph defined by the given item, beginning at that item, with the neighbors of a vertex defined by the neighbor getter function.
        /// </summary>        
        /// <param name="item">An item contained in the graph to be traversed.  This item will be  the starting item of the bread-first search.</param>
        /// <param name="neighborGetter">The function used to find the neighbors of a given vertex.  For example, providing "(a) => a.getChildren()" will cause the traversal to examine the 
        /// getChildren() method defined in 'T' to determine where to examine next.</param>
        /// <remarks>This method uses the yield-return pattern.</remarks>
        public static IEnumerable<T> BreadthFirstTraversal<T>(T item, Func<T, IEnumerable<T>> neighborGetter)
        {
            HashSet<T> visited = new HashSet<T>();
            Queue<T> q = new Queue<T>();
            q.Enqueue(item);
            while (q.Count > 0)
            {
                T focus = q.Dequeue();
                visited.Add(focus);
                yield return focus;
                IEnumerable<T> children = neighborGetter(focus);
                foreach (T child in children)
                    if (!visited.Contains(child)) q.Enqueue(child);                
            }
        }
    }


    /// <summary>
    /// An object created using the given traversal (child-getting) method to produce a tree.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Graph<T>
    {
        private Dictionary<T, Graph<T>> _Nodes = null;
        /// <summary>The parent nodes of this node.</summary>
        public List<Graph<T>> Parents { get; private set; } = new List<Graph<T>>();
        /// <summary>Returns the list of parents of the given child.</summary>
        public List<Graph<T>> ParentsOf(T child) => _Nodes[child].Parents;
        /// <summary>The contents at this node of the tree.</summary>
        public T Contents { get; private set; }
        /// <summary>The child nodes of this node.</summary>
        public List<Graph<T>> Children { get; private set; }
        /// <summary>Returns the list of children of the given parent.</summary>
        public List<Graph<T>> ChildrenOf(T parent) => _Nodes[parent].Children;
        
        private Graph(Graph<T> parent, T Contents) { this.Parents.Add(parent); this.Contents = Contents; }

        private Graph(T starter, Func<T, IEnumerable<object>> children, Func<T, bool> validator = null, bool rejectCycles = false)
        {
            if (validator == null) validator = (child) => true;
            _Nodes = new Dictionary<T, Graph<T>>(); //Exists only for the starter node.

            this.Contents = starter;
            Stack<Graph<T>> stack = new Stack<Graph<T>>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                Graph<T> node = stack.Pop();

                // If there are no children, best to never even create a list of them.
                IEnumerable<object> kids = children(node.Contents);
                if (kids==null || !kids.Any()) continue;

                // Create the children's nodes.
                node.Children = new List<Graph<T>>();
                foreach (object obj in kids)
                {
                    if (!(obj is T)) throw new ArrayTypeMismatchException("Object of type + " + obj.GetType().Name + " cannot be stored in graph of type " + starter.GetType().Name);
                    T kid = (T)obj;

                    if (_Nodes.TryGetValue(kid, out Graph<T> childNode))
                    {
                        if (rejectCycles) throw new InvalidOperationException("A cycle exists: " + kid.ToString());
                        childNode.Parents.Add(node);
                        node.Children.Add(childNode);
                        continue;
                    }
                    else if
                        (!validator(kid)) continue;
                    else
                    {
                        childNode = new Graph<T>(node, kid);
                        node.Children.Add(node);
                        stack.Push(childNode);
                    }
                }
            }
        }

        /// <summary>Returns an acyclic graph which traverses edges via the given Func lambda.</summary>
        public static Graph<T> Acyclic(T starter, Func<T, IEnumerable<object>> children)
        {
            return new Graph<T>(starter, children, null, true);
        }


    }
}
