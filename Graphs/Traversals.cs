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
}
