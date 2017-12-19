using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Mathematics.Cartesian;

namespace DataStructures
{
    /// <summary>
    /// A data structure that supports fast operations for getting and retrieving items based on a two-dimensional location.
    /// </summary>
    public sealed class QuadTreeZ<T> : ICollection<T>
        //TODO:  Validate all members of QuadTree
    {
        /// <summary>
        /// The standard capacity per node.  Note that this is not a ceiling, but a floor - items added to a node will not be sent down to the node's children until the node's capacity is reached.
        /// </summary>
        private const int NODE_CAPACITY = 20;

        /// <summary>
        /// The root node of this quad tree.  An empty tree will have a null root.
        /// </summary>
        private Node _Root { get; set; } = null;

        /// <summary>
        /// The extent of items contained in this quad tree, which may be different from the Root's boundary if the item that originally defined the boundary is removed from the Root.
        /// </summary>
        public Rect Extent { get; private set; } = Rect.Empty;

        private Dictionary<T, Node> _Nodes = new Dictionary<T, Node>();
        private Dictionary<T, Rect> _Bounds = new Dictionary<T, Rect>();
        private Func<T, Rect> _BoundaryGetter;

        public QuadTreeZ(Func<T, Rect> boundaryGetter)
        {
            this._BoundaryGetter = boundaryGetter;
        }


        #region QuadTree contents manipulation members
        

        /// <summary>
        /// Adds the given item to this QuadTree.
        /// </summary> 
        public void Add(T item)
        {
            Rect itemBoundary = _BoundaryGetter(item);

            //If the root is null, then the new item defines the new root.
            if (_Root == null)
            {

                _Root = new Node(itemBoundary);
                Extent = itemBoundary;
                _Root.Contents.Add(item, itemBoundary);
                Count = 1;
                return;
            }

            //No matter what happens from here, the new extent will equal the old extent unioned with the item's boundary.
            Extent = Rect.Union(itemBoundary, Extent);

            //If the current root couldn't contain the new boundary, time to create a new root, and copy everything over from the old.
            if (!_Root.Boundary.Contains(itemBoundary))
            {

                Stack<Node> oldStack = new Stack<Node>();
                oldStack.Push(_Root);
                _Root = new Node(Extent);

                while (oldStack.Count > 0)
                {
                    //Add the focus node's contents to the new root.
                    Node focus = oldStack.Pop();
                    foreach (T oldItem in focus.Contents.Keys)
                        AddUnsafe(oldItem, focus.Contents[oldItem]);

                    //Now put the child nodes (if any) on the stack.
                    if (focus.Children == null) continue;
                    oldStack.Push(focus.Children[0]);
                    oldStack.Push(focus.Children[1]);
                    oldStack.Push(focus.Children[2]);
                    oldStack.Push(focus.Children[3]);
                }

            }

            //Finally, since the root already exists and the item will fit the root, just add to the tree existing.
            AddUnsafe(item, itemBoundary);
            Count++;
            return;
        }

        /// <summary>
        /// Builds an appropriate node to hold the given item, and returns this node.  This method does no extent checking, or determining if the root is null.
        /// </summary>        
        /// <param name="item">The item to add.</param>
        /// <param name="itemBoundary">The boundary of the item to add.</param>
        /// <returns>Returns the node to which the item was added.</returns>
        private Node AddUnsafe(T item, Rect itemBoundary)
        {
            Node node = _Root;

            //Which node to add it to?  Might even need to build a new node.
            while (true)
            {
                //If the item would just fit in this node's contents, add to this node.
                if (node.Contents.Count < NODE_CAPACITY)
                {
                    node.Contents.Add(item, itemBoundary);
                    return node;
                }

                //Find out what relative quadrant the item would fit into.                
                QuadrantFlags qf = GetQuadrant(node, itemBoundary);
                int idx = GetIndex(node, itemBoundary);

                //If the item would span multiple children then just add to this node.
                if (idx < 0)
                {
                    node.Contents.Add(item, itemBoundary);
                    return node;
                }

                //If the node has no child nodes, now is the time to build the child nodes.
                if (node.Children == null)
                {
                    Point center = new Point((node.Boundary.Left + node.Boundary.Right) / 2, (node.Boundary.Top + node.Boundary.Bottom) / 2);
                    node.Children = new Node[4];
                    node.Children[0] = new Node(new Rect(center, node.Boundary.TopRight));
                    node.Children[1] = new Node(new Rect(center, node.Boundary.TopLeft));
                    node.Children[2] = new Node(new Rect(center, node.Boundary.BottomLeft));
                    node.Children[3] = new Node(new Rect(center, node.Boundary.BottomRight));
                }

                //Set the focus node to the indicated child, and repeat.
                node = node.Children[idx];

            }
        }

        /// <summary>
        /// Removes all items from this tree.
        /// </summary>
        public void Clear()
        {
            _Root = null;
            Extent = Rect.Empty;
            Count = 0;
        }


        /// <summary>
        /// Removes the specified item from the tree.  If the item did not exist on the tree, return false; otherwise returns true.
        /// </summary>
        public bool Remove(T item)
        {
            //Step #1 - find the containing node.              
            Rect itemBoundary;
            Node container = FindContainingNode(item, out itemBoundary);
            if (container == null) return false;

            //Step #2 - remove from the containing node.           
            container.Contents.Remove(item);
            Count--;

            //Step #3 - Was that the last item in the tree?
            if (Count == 0)
            {
                _Root = null;
                Extent = Rect.Empty;
                return true;
            }

            //Step #4 - If the item removed was at the boundary, then the extent must simply be updated.          
            if (itemBoundary.Top == _Root.Boundary.Top || itemBoundary.Bottom == _Root.Boundary.Bottom || itemBoundary.Left == _Root.Boundary.Left || itemBoundary.Right == _Root.Boundary.Right)
                UpdateExtent();

            //Finally, just return indicating that a change was made.
            return true;
        }


        /// <summary>
        /// Updates the cached Extent rect.
        /// </summary>
        private void UpdateExtent()
        {
            Extent = Rect.Empty;
            if (_Root == null) return;
            Stack<Node> stack = new Stack<Node>();
            stack.Push(_Root);
            while (stack.Count > 0)
            {
                Node focus = stack.Pop();
                foreach (T item in focus.Contents.Keys)
                    Extent = Rect.Union(Extent, focus.Contents[item]);

                if (focus.Children == null) continue;
                stack.Push(focus.Children[0]);
                stack.Push(focus.Children[1]);
                stack.Push(focus.Children[2]);
                stack.Push(focus.Children[3]);
            }
        }

        #endregion



        #region QuadTree contents getting members


        /// <summary>
        /// Returns whether the given item is contained in this tree.
        /// </summary>
        public bool Contains(T item) { return FindContainingNode(item) != null; }


        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (_Root == null) return;
            foreach (T item in GetDescendantNodes(_Root).SelectMany((node) => node.Contents.Keys))
            {
                if (arrayIndex >= array.Length) return;
                array[arrayIndex++] = item;
            }
        }


        /// <summary>
        /// Returns the number of items held in this quad tree.
        /// </summary>
        public int Count { get; private set; }

        int ICollection<T>.Count { get { return this.Count; } }
        bool ICollection<T>.IsReadOnly { get { return false; } }



        /// <summary>
        /// Finds the deepest node, beginning at the root, that entirely contains the given boundary.
        /// </summary>
        private Node FindContainingNode(T item, out Rect boundary)
        {
            boundary = new Rect();
            return FindContainingNode(item);
        }

        /// <summary>
        /// Finds the deepest node, beginning at the root, that entirely contains the given boundary.
        /// </summary>
        private Node FindContainingNode(T item)
        {
            Rect boundary = _BoundaryGetter(item);
            if (_Root == null || !_Root.Boundary.Contains(boundary)) return null;
            Node focus = _Root;
            while (true)
            {
                //If this node has the child, just return this node.
                if (focus.Contents.ContainsKey(item)) return focus;

                //If focus has no kids, then there isn't a contianing node.
                if (focus.Children == null) return null;

                //If none of focus's kids can completely contain the boundary, then there isn't a containing node.                
                int idx = GetIndex(focus, focus.Contents[item]);
                if (idx < 0) return null;

                //Set the focus to the next child and repeat.
                focus = focus.Children[idx];
            }
        }


        public ISet<T> GetContainedBy(Rect other)
        {
            HashSet<T> result = new HashSet<T>();
            if (_Root == null) return result;

            Stack<Node> stack = new Stack<Node>();
            stack.Push(_Root);
            while (stack.Count > 0)
            {
                Node focus = stack.Pop();
                if (!other.Contains(focus.Boundary)) continue;
                foreach (T item in focus.Contents.Keys)
                    if (other.Contains(focus.Contents[item])) result.Add(item);
                if (focus.Children == null) continue;
                stack.Push(focus.Children[0]);
                stack.Push(focus.Children[1]);
                stack.Push(focus.Children[2]);
                stack.Push(focus.Children[3]);
            }
            return result;
        }

        public ISet<T> GetContainers(Rect other)
        {
            HashSet<T> result = new HashSet<T>();
            if (_Root == null) return result;

            Stack<Node> stack = new Stack<Node>();
            stack.Push(_Root);
            while (stack.Count > 0)
            {
                Node focus = stack.Pop();
                if (!focus.Boundary.Contains(other)) continue;
                foreach (T item in focus.Contents.Keys)
                    if (focus.Contents[item].Contains(other)) result.Add(item);
                if (focus.Children == null) continue;
                stack.Push(focus.Children[0]);
                stack.Push(focus.Children[1]);
                stack.Push(focus.Children[2]);
                stack.Push(focus.Children[3]);
            }
            return result;
        }



        /// <summary>
        /// Returns all nodes of this tree in depth-first search order.
        /// </summary>
        /// <param name="parent">The parent whose descendant nodes are sought.</param>        
        /// <remarks>This method does NOT use the 'yield-return' pattern.</remarks>
        private List<Node> GetDescendantNodes(Node parent)
        {
            List<Node> result = new List<Node>();

            Stack<Node> stack = new Stack<Node>();
            stack.Push(parent);

            while (stack.Count > 0)
            {
                Node focus = stack.Pop();
                result.Add(focus);
                if (focus.Children != null)
                {
                    if (focus.Children[0] != null) stack.Push(focus.Children[0]);
                    if (focus.Children[1] != null) stack.Push(focus.Children[1]);
                    if (focus.Children[2] != null) stack.Push(focus.Children[2]);
                    if (focus.Children[3] != null) stack.Push(focus.Children[3]);
                }
            }
            return result;
        }


        /// <summary>
        /// Returns an iterating enumerator for transversing all the items in this tree.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            //If the root is null, there are no members to return.
            if (_Root == null) yield break;

            List<Node> allNodes = GetDescendantNodes(_Root);
            for (int i = 0; i < allNodes.Count; i++)
            {
                Node focus = allNodes[i];
                foreach (T item in focus.Contents.Keys) yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }

        /// <summary>
        /// Returns the unordered list of all items contained on this tree that intersect the given rect.
        /// </summary>        
        public IEnumerable<T> GetIntersection(Rect rectangle)
        {
            List<T> result = new List<T>();
            //HashSet<T> result = new HashSet<T>();
            if (_Root == null) return result;

            Stack<Node> stack = new Stack<Node>();
            stack.Push(_Root);
            while (stack.Count > 0)
            {
                //If it's a real intersector, add to the result.
                Node focus = stack.Pop();
                if (!focus.Boundary.IntersectsWith(rectangle))
                    continue;
                foreach (T item in focus.Contents.Keys)
                    if (focus.Contents[item].IntersectsWith(rectangle)) result.Add(item);

                //Add any children nodes to process.
                if (focus.Children == null) continue;
                stack.Push(focus.Children[0]);
                stack.Push(focus.Children[1]);
                stack.Push(focus.Children[2]);
                stack.Push(focus.Children[3]);
            }
            return result;
        }

        /// <summary>
        /// Returns the set of all items contained in this tree that intersect or contain the given point.
        /// </summary>        
        public ISet<T> GetIntersection(Point point)
        {
            HashSet<T> result = new HashSet<T>();
            if (_Root == null) return result;

            Stack<Node> stack = new Stack<Node>();
            stack.Push(_Root);
            while (stack.Count > 0)
            {
                Node focus = stack.Pop();
                if (!focus.Boundary.Contains(point)) continue;
                foreach (T item in focus.Contents.Keys)
                    if (focus.Contents[item].Contains(point)) result.Add(item);
                if (focus.Children == null) continue;
                stack.Push(focus.Children[0]);
                stack.Push(focus.Children[1]);
                stack.Push(focus.Children[2]);
                stack.Push(focus.Children[3]);
            }
            return result;
        }


        /// <summary>
        /// Returns the 0-based index of the quadrant of the given rect relative to the given node.  If the rect would straddle the center point or a boundary, returns -1.
        /// </summary>
        /// <param name="node">The node whose quadrant number is sought.  For example, if the boundary would be entirely contained in the upper-right quadrant, the result would be 0.  The quadrant 
        /// numbers count in counter-clockwise order from there.</param>
        /// <param name="boundary">The Rect that will appear in a given quadrant.</param>        
        private static int GetIndex(Node node, Rect boundary)
        {
            QuadrantFlags qf = GetQuadrant(node, boundary);
            return (qf == QuadrantFlags.TopRight) ? 0 : (qf == QuadrantFlags.TopLeft) ? 1 : (qf == QuadrantFlags.BottomLeft) ? 2 : (qf == QuadrantFlags.BottomRight) ? 3 : -1;
        }


        /// <summary>
        /// Returns bitwise quadrant flags indicating where the given rect will appear relative to the given node.  
        /// </summary>
        /// <param name="node"></param>
        /// <param name="boundary"></param>
        /// <returns></returns>
        private static QuadrantFlags GetQuadrant(Node node, Rect boundary)
        {
            QuadrantFlags result = 0;
            Point center = new Point((boundary.Left + boundary.Right) / 2, (boundary.Top + boundary.Bottom) / 2);

            if (node.Boundary.TopRight.X >= center.X && node.Boundary.TopRight.Y >= center.Y) result |= QuadrantFlags.TopRight;
            if (node.Boundary.TopLeft.X <= center.X && node.Boundary.TopLeft.Y >= center.Y) result |= QuadrantFlags.TopLeft;
            if (node.Boundary.BottomLeft.X <= center.X && node.Boundary.BottomLeft.Y <= center.Y) result |= QuadrantFlags.BottomLeft;
            if (node.Boundary.BottomRight.X <= center.X && node.Boundary.BottomRight.Y <= center.Y) result |= QuadrantFlags.BottomRight;

            return result;
        }

        /// <summary>
        /// Contains bitwise flags that indicate positions relative to a central point, or quadrants in a Rect.
        /// </summary>
        [Flags]
        private enum QuadrantFlags { TopRight = 0x01, Top = 3, TopLeft = 0x02, Left = 6, BottomLeft = 0x04, Bottom = 12, BottomRight = 0x08, Right = 9, All = 15 }

        #endregion



        /// <summary>
        /// A lightweight data structure used to store the tree structure of this QuadTree.  Items added or removed for a node are not checked for fit.
        /// </summary>
        private class Node
        {
            /// <summary>
            /// The child nodes of this node.  If no children have ever been set, this returns null.
            /// </summary>
            public Node[] Children { get; internal set; } = null;

            /// <summary>
            /// The item contents of the node.  This is a dictionary to support O(1) contents checking and removals.
            /// </summary>            
            public readonly Dictionary<T, Rect> Contents = new Dictionary<T, Rect>();

            /// <summary>
            /// The Rect boundary of this node.
            /// </summary>
            public readonly Rect Boundary;

            /// <summary>
            /// Creates a new Node with the given Rect boundary.
            /// </summary>            
            public Node(Rect boundary)
            {
                this.Boundary = boundary;
            }
        }



    }
}
