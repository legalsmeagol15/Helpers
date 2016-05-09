using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DataStructures
{
    public class QuadTree<T> : ICollection<T>
    {
        /// <summary>
        /// The number of items that may be stored on a particular node before fitting children will be sought.
        /// </summary>
        private const int NODE_CONTENTS_CAPACITY = 20;

        /// <summary>
        /// The smallest allowable size of a node.  This is VERY small.
        /// </summary>
        private const double MIN_NODE_SIZE = double.Epsilon * 4096;

        private Dictionary<T, Node> _Nodes = new Dictionary<T, Node>();
        private Dictionary<T, Rect> _Bounds = new Dictionary<T, Rect>();
        private Node _Root = null;
        private Func<T, Rect> _BoundaryGetter;

        public QuadTree(Func<T, Rect> boundaryGetter)
        {
            this._BoundaryGetter = boundaryGetter;
        }
        

        #region RectTree contents changing

        public int Add(T item, int copies = 1)
        {
            return Add(item, _BoundaryGetter(item), copies);
        }
        /// <summary>
        /// Adds the given item to this quad tree, and returns the count of identical items on the tree when the add is complete.
        /// </summary>
        /// <param name="item">The item to add to this tree.</param>
        /// <param name="copies">Optional.  The number of copies of the given item to add to the tree.  If omitted, a single copy will be added.</param>
        public int Add(T item, Rect itemBounds, int copies = 1)
        {            
            //If this is the first item, the solution is easy.
            if (_Root == null)
            {                
                _Root = new Node(itemBounds);
                _Root.Contents.Add(item);
                _Nodes[item] = _Root;
                _Bounds[item] = itemBounds;
                Count = copies;
                return copies;
            }

            //Check to see if other identical items exist.
            Rect compareBounds;
            if (_Bounds.TryGetValue(item, out compareBounds))
            {
                if (itemBounds != compareBounds)
                    throw new InvalidOperationException("Quadtree coherence error:  the added item's bounds do not match the bounds of other identical items on this quad tree.");
                return _Nodes[item].Contents.Add(item, copies);
            }

            //Check to see if the item will fit in the _Root anyway.  If not, a new Root must be created and everything in the old copied over.
            if (!_Root.Boundary.Contains(itemBounds))
                AdjustExtent(Rect.Union(itemBounds, _Root.Boundary));

            //Since this is a new, unique item, in a non-empty tree that can contain it, get an appropriate node for it.
            Node addedNode = AddUnsafe(_Root, item, itemBounds, 1);
            _Nodes[item] = addedNode;
            _Bounds[item] = itemBounds;
            Count += copies;
            return addedNode.Contents.CountOf(item);
        }
        void ICollection<T>.Add(T item)
        {
            Add(item, _BoundaryGetter(item), 1);
        }
        /// <summary>
        /// Adds the given number of copies of the given item to the indicated node, or to one  of the node's descendant nodes.
        /// </summary>
        private Node AddUnsafe(Node focus, T item, Rect itemBounds, int copies)
        {            
            while (true)
            {
                //If it already exists here, just add another copy.
                if (focus.Contents.Contains(item))
                {
                    focus.Contents.Add(item, copies);
                    Count += copies;
                    return focus;
                }

                //If it doesn't already exist here, but adding would not overrun capacity, or if a child node would be too small, add here.
                if (focus.Contents.Count < NODE_CONTENTS_CAPACITY || focus.Boundary.Width < MIN_NODE_SIZE || focus.Boundary.Height < MIN_NODE_SIZE)
                {
                    focus.Contents.Add(item, copies);
                    _Bounds[item] = itemBounds;
                    Count += copies;
                    return focus;
                }


                //Time to add to children?
                int childIdx = GetQuadrantIndex(focus, itemBounds);
                if (focus.Children == null)
                {
                    //If it won't fit in a child, cram it in focus's contents even though the max carry has been reached.
                    if (childIdx == -1)
                    {
                        focus.Contents.Add(item, copies);
                        _Bounds.Add(item, itemBounds);
                        _Nodes.Add(item, focus);
                        Count += copies;
                        return focus;
                    }

                    //Create the children.
                    focus.Children = new Node[4];
                    Point center = new Point(focus.Boundary.X + (focus.Boundary.Width / 2), focus.Boundary.Y + (focus.Boundary.Height / 2));
                    focus.Children[0] = new Node(new Rect(center, focus.Boundary.TopRight));
                    focus.Children[1] = new Node(new Rect(center, focus.Boundary.TopLeft));
                    focus.Children[2] = new Node(new Rect(center, focus.Boundary.BottomLeft));
                    focus.Children[3] = new Node(new Rect(center, focus.Boundary.BottomRight));
                }

                //Since it fits in a child, update the focus to the child and continue.
                focus = focus.Children[childIdx];
            }
        }


        private void AdjustExtent(Rect newExtent)
        {            
            Node newRoot = new Node(newExtent);
            Dictionary<T, Node> newNodes = new Dictionary<T, Node>();
            foreach (KeyValuePair<T, Node> kvp in _Nodes)
            {
                int existingCount = kvp.Value.Contents.CountOf(kvp.Key);
                Rect existingBounds = _Bounds[kvp.Key];                
                newNodes[kvp.Key] = AddUnsafe(newRoot, kvp.Key, existingBounds, existingCount);
            }
            _Root = newRoot;
            _Nodes = newNodes;
        }


        /// <summary>
        /// Removes all items from this quad tree.
        /// </summary>
        public void Clear()
        {
            _Root = null;
            _Nodes.Clear();
            _Bounds.Clear();
            Count = 0;
        }
        

        public bool Remove(T item)
        {
            Node node;
            if (!_Nodes.TryGetValue(item, out node)) return false;
            Rect itemBoundary = _Bounds[item];  //Use to detect if the item abutted the extent.

            //From here, removal must be successful.  If the node's contents no longer contain the item, that means the item is gone entirely.
            node.Contents.Remove(item);
            Count--;

            //What if the removal pulled out the last instance?  Could change the structure of the tree.
            if (!node.Contents.Contains(item))
            {
                _Bounds.Remove(item);
                _Nodes.Remove(item);

                //Was the removal the last item in this entire tree?
                if (Count == 0)
                {
                    Clear();
                    return true;
                }

                //Did the removal cause the extent to shrink?
                double error = Math.Min(node.Boundary.Height, node.Boundary.Width) / 16;
                Rect extent = _Root.Boundary;
                if (Math.Abs(extent.Left - itemBoundary.Left) < error || Math.Abs(extent.Right - itemBoundary.Right) < error
                    || Math.Abs(extent.Top - itemBoundary.Top) < error || Math.Abs(extent.Bottom - itemBoundary.Bottom) < error)
                {
                    Rect newExtent = GetExtent();
                    if (_Root.Boundary != newExtent)
                    {
                        AdjustExtent(newExtent);
                        return true;
                    }                        
                }
            }

            //Since this removal was not the last one in a node (or it was but the tree structure remains unchanged), just return true.
            return true;           
        }

        #endregion


        #region QuadTree contents queries

        /// <summary>
        /// Returns the bounds that indexed the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Rect BoundsOf(T item)
        {
            Rect b;
            if (!_Bounds.TryGetValue(item, out b)) return Rect.Empty;
            return b;
        }

        

        /// <summary>
        /// The count of items contained in this quadtree.
        /// </summary>
        public int Count { get; protected set; } = 0;
        int ICollection<T>.Count { get { return Count; } }

        public int CountOf(T item)
        {
            return _Nodes[item].Contents.CountOf(item);
        }

        /// <summary>
        /// Returns whether this quadtree contains the given item.
        /// </summary>
        public bool Contains(T item) { return _Nodes.ContainsKey(item); }



        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The overall size of this items contained in this quadtree.
        /// </summary>
        public Rect Extent { get { return (_Root == null) ? Rect.Empty : _Root.Boundary; } }

        /// <summary>
        /// Finds the unioned extent of all the items on this quad tree.  This is an O(n) operation.
        /// </summary>        
        private Rect GetExtent()
        {
            Rect result = Rect.Empty;
            foreach (Rect b in _Bounds.Values)            
                result = Rect.Union(result, b);
            return result;
        }

        /// <summary>
        /// Returns all items whose boundaries intersect with the given Rect.
        /// </summary>
        public IEnumerable<T> GetIntersection(Rect bounds)
        {
            List<T> result = new List<T>();
            if (_Root == null) return result;

            Queue<Node> q = new Queue<Node>();
            q.Enqueue(_Root);
            while (q.Count > 0)
            {
                Node focus = q.Dequeue();
                if (!focus.Boundary.IntersectsWith(bounds)) continue;
                foreach (T item in focus.Contents)                
                    if (_Bounds[item].IntersectsWith(bounds)) result.Add(item);
                if (focus.Children == null) continue;
                q.Enqueue(focus.Children[0]);
                q.Enqueue(focus.Children[1]);
                q.Enqueue(focus.Children[2]);
                q.Enqueue(focus.Children[3]);
            }
            return result;
        }


        bool ICollection<T>.IsReadOnly { get { return false; } }

        /// <summary>
        /// Steps through all the unique items in this quad tree.  In such an iteration, items which have multiple instances in the tree will be returned only once.
        /// </summary>        
        public IEnumerator<T> GetEnumerator()
        {
            return _Nodes.Keys.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }


        #endregion


        #region QuadTree miscellaneous

        /// <summary>
        /// Returns the 0-based index of the quadrant of the given rect relative to the given node.  If the rect would straddle the center point or a boundary, returns -1.
        /// </summary>
        /// <param name="node">The node whose quadrant number is sought.  For example, if the boundary would be entirely contained in the upper-right quadrant, the result would be 0.  The quadrant 
        /// numbers count in counter-clockwise order from there.</param>
        /// <param name="boundary">The Rect that will appear in a given quadrant.</param>        
        private static int GetQuadrantIndex(Node node, Rect boundary)
        {
            QuadrantFlags qf = GetQuadrant(node, boundary);
            switch (qf)
            {
                case QuadrantFlags.TopRight: return 0;
                case QuadrantFlags.TopLeft: return 1;
                case QuadrantFlags.BottomLeft: return 2;
                case QuadrantFlags.BottomRight: return 3;
            }
            return -1;
        }


        /// <summary>
        /// Returns bitwise quadrant flags indicating where the given rect will appear relative to the given node.  
        /// </summary> 
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


        /// <summary>
        /// The lightweight data class storing items in this tree.
        /// </summary>
        protected class Node
        {
            public readonly Rect Boundary;
            public Node[] Children;
            public Node Parent;
            public HashCollection<T> Contents = new HashCollection<T>();
            public Node(Rect boundary, Node parent = null)
            {
                Boundary = boundary;
                Parent = parent;
            }
        }

        #endregion


    }
}
