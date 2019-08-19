using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mathematics.Geometry;

namespace Helpers.DataStructures.Trees
{

    /// <summary>
    /// A data structure that stores items...
    /// TODO:  this might not be quite correct, it may be that an item that CAN fit in a quarter node MUST be put in that node.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QuadTreeD<T> : IEnumerable<T> where T : IRect<double>
    {
        private Node _Root = null;

        /// <summary>The count of content items on this <see cref="QuadTreeD{T}"/>.</summary>
        public int Count { get; private set; }

        /// <summary>Adds the given item to this <see cref="QuadTreeD{T}"/>.</summary>
        public void Add (T item)
        {
            if (_Root == null)
            {
                _Root = new Node(null, item.Left, item.Right, item.Bottom, item.Top);
                _Root.Items.Add(item);
            }
            else if (_Root.CanContain(item))
            {
                _Root.Add(item);
            }
            else if (!_Root.HasChildren)
            {
                _Root.Rect = new RectD(Math.Min(_Root.Rect.Left, item.Left),
                                       Math.Max(_Root.Rect.Right, item.Right),
                                       Math.Min(_Root.Rect.Bottom, item.Bottom),
                                       Math.Max(_Root.Rect.Top, item.Top));
                _Root.Add(item);
            }
            else
            {
                Node oldRoot = _Root;
                _Root = new Node(null, Math.Min(_Root.Rect.Left, item.Left),
                                       Math.Max(_Root.Rect.Right, item.Right),
                                       Math.Min(_Root.Rect.Bottom, item.Bottom),
                                       Math.Max(_Root.Rect.Top, item.Top));
                _Root.Add(item);
                foreach (T oldItem in oldRoot.GetContents())
                    _Root.Add(oldItem);
            }
            Count++;
        }

        /// <summary>Removes all items from this <see cref="QuadTreeD{T}"/>.</summary>
        public void Clear() { _Root = null; Count = 0; }

        /// <summary>
        /// Calculates the furthers extent of the bounds that would contain all the content items of this 
        /// <see cref="QuadTreeD{T}"/>.  This is an O(n) operation, where n is the number of content items existing.
        /// </summary>
        public RectD GetBounds()
        {
            RectD result = RectD.Empty;
            if (_Root != null)
            {
                foreach (T item in _Root.GetContents())
                    result = result.GetUnion(item);
            }
            return result;
        }

        /// <summary>
        /// Removes the given item from this <see cref="QuadTreeD{T}"/>, if it exists.  Returns true on success.  If 
        /// the item did not exist on the tree to begin with, returns false.
        /// </summary>
        public bool Remove(T item)
        {
            if (_Root == null)
                return false;
            if (!_Root.Remove(item))
                return false;
            if (!_Root.Items.Any() && !_Root.HasChildren)
                _Root = null;
            Count--;
            return true;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            if (_Root == null) yield break;
            foreach (T item in _Root.GetContents()) yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (_Root == null) yield break;
            foreach (T item in _Root.GetContents()) yield return item;
        }

        private sealed class Node
        {
            public const int NODE_CAPACITY = 16;
            public readonly Node Parent;
            public double MidX => (Rect.Right - Rect.Left) / 2;
            public double MidY => (Rect.Top - Rect.Bottom) / 2;
            public RectD Rect = new RectD();
            public readonly List<T> Items = new List<T>(NODE_CAPACITY);
            /// <summary>
            /// Arranged like:
            /// _____________
            /// |     |     |
            /// |  1  |  0  |
            /// |_____|_____|
            /// |     |     |
            /// |  2  |  3  |
            /// |_____|_____|
            /// 
            /// </summary>
            public Node[] Children;
            public Node (Node parent, double left, double right, double bottom, double top)
            {
                this.Parent = parent;
                this.Rect = new RectD(left, right, bottom, top);
            }
            public bool HasChildren => Children[0] != null || Children[1] != null || Children[2] != null || Children[3] != null;
           
            /// <summary>
            /// Adds the given item to this node.  If it won't fit in this node, but could fit  on a child, adds it to 
            /// the child.
            /// </summary>
            public void Add(T item)
            {
                int quadrant;
                // If the Items is at capacity, or no child will fit the item anyway, just add it here.
                if (Items.Count < NODE_CAPACITY || (quadrant = GetFittingQuadrant(item)) < 0)
                {
                    Items.Add(item);
                    return;
                }
                // Otherwise a child can fit it, so add to the child.
                else
                {
                    Node child = Children[quadrant];
                    if (child == null) Children[quadrant] = (child = CreateChild(quadrant));
                    child.Add(item);
                }
            }

            /// <summary>Returns whether this node could contain the given item.</summary>
            public bool CanContain(T item) => Rect.Contains(item);

            /// <summary>Returns whether the given item exists at this node.</summary>
            public bool Contains(T item)
            {
                Node node = this;
                while (node != null)
                {
                    if (node.Items.Contains(item)) return true;
                    int quadrant = GetFittingQuadrant(item);
                    if (quadrant < 0) return false;
                    node = Children[quadrant];
                }
                return false;
            }

            public IEnumerable<T> GetContents()
            {
                Stack<Node> stack = new Stack<Node>();
                stack.Push(this);
                while (stack.Count > 0)
                {
                    Node n = stack.Pop();
                    foreach (T item in n.Items) yield return item;
                    for (int i = 0; i < n.Children.Length; i++)
                        if (n.Children[i] != null)
                            stack.Push(n.Children[i]);
                }
            }
            /// <summary>Returns the content items which would intersect the given region.</summary>
            public IEnumerable<T> GetIntersection(IRect<double> region)
            {
                Node node = this;
                while (node != null)
                {
                    foreach (T item in node.Items)
                        if (Intersects(item, region))
                            yield return item;
                    int quadrant = GetFittingQuadrant(region);
                    if (quadrant < 0) yield break;
                    node = node.Children[quadrant];
                }
            }            

            /// <summary>
            /// Removes the item from this node, if it exists.  If it exists and is removed, returns true.  Otherwise, 
            /// returns false.
            /// </summary>
            public bool Remove(T item)
            {
                Node node = this;
                while (node != null)
                {
                    if (node.Items.Remove(item))
                    {
                        // Removal was successful, so prune empty parts of the tree.                        
                        while (true)
                        {

                            if (node.Items.Any() || node.HasChildren) break;
                            Node parent = node.Parent;
                            if (parent == null) break;
                            for (int i = 0; i < parent.Children.Length; i++)
                                if (ReferenceEquals(parent.Children[i], node))
                                    parent.Children[i] = null;
                            node = parent;
                        }
                        return true;
                    }
                    else
                    {
                        // Removal was not successful, so see if a child might be holding the item.
                        int quadrant = GetFittingQuadrant(item);
                        if (quadrant < 0) return false;
                        node = Children[quadrant];
                    }             
                }

                // No node left to search, so the item doesn't exist here.
                return false;
            }


            private int GetFittingQuadrant(IRect<double> item)
            {
                double midX = MidX, midY = MidY;
                if (item.Left >= midX && item.Bottom >= midY) return 0;
                if (item.Right <= midX && item.Bottom >= midY) return 1;
                if (item.Right <= midX && item.Top <= midY) return 2;
                if (item.Left >= midX && item.Top <= midY) return 3;
                return -1;
            }
            private Node CreateChild(int quadrant)
            {
                switch (quadrant)
                {
                    case 0: return new Node(this, MidX, Rect.Right, MidY, Rect.Top);
                    case 1: return new Node(this, Rect.Left, MidX, MidY, Rect.Top);
                    case 2: return new Node(this, Rect.Left, MidX, Rect.Bottom, MidY);
                    case 3: return new Node(this, MidX, Rect.Right, Rect.Bottom, MidY);
                }
                throw new NotImplementedException();
            }
            private static bool Intersects(IRect<double> a, IRect<double> b)
            {
                if (a.Right < b.Left || b.Left > a.Right) return false;
                if (a.Top < b.Bottom || b.Bottom > a.Top) return false;
                return true;
            }
        }
    }
}
