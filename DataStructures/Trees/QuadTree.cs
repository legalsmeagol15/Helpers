using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mathematics.Geometry;
using Dependency;
using System.Diagnostics;
using System.Reflection;

namespace DataStructures
{
    public sealed class QuadTreeD<TItem> : AbstractQuadTree<TItem, double> where TItem : IBounded<double>
    {
        protected override bool Exclude(double size, IRect<double> rect) => (rect.Right - rect.Left) < size || (rect.Top - rect.Bottom) < size;

        protected override double GetMid(double a, double b) => (a + b) / 2;

        
    }

    public sealed class QuadTreeN<TItem> : AbstractQuadTree<TItem, Number> where TItem : IBounded<Number>
    {
        protected override bool Exclude(Number size, IRect<Number> rect) => (rect.Right - rect.Left) < size || (rect.Top - rect.Bottom) < size;

        protected override Number GetMid(Number a, Number b) => (a + b) / 2;
    }

    [DebuggerDisplay("Count = {Count}")]
    [DefaultMember("Item")]
    public abstract class AbstractQuadTree<TItem, TNumber> where TItem : IBounded<TNumber>
                                                               where TNumber : struct, IComparable<TNumber>
    {
        private Node _Root = null;

        /// <summary>The count of content items on this <see cref="AbstractQuadTree{TItem, TNumber}"/>.</summary>
        public int Count { get; private set; }

        private struct BoundedPoint : IBounded<TNumber>
        {
            internal readonly IPoint<TNumber> Point;
            private readonly IRect<TNumber> _Bounds;
            IRect<TNumber> IBounded<TNumber>.Bounds => _Bounds;
            public BoundedPoint(IPoint<TNumber> pt) { this.Point = pt; this._Bounds = new Rect<TNumber>(pt.X, pt.X, pt.Y, pt.Y); }
            public override bool Equals(object obj) => obj is BoundedPoint other && Point.Equals(other.Point);
            public override int GetHashCode() => Point.GetHashCode();
        }
        /// <summary>Adds the given point to this <see cref="AbstractQuadTree{TItem, TNumber}"/>.</summary>
        public void Add(IPoint<TNumber> item) { Add(new BoundedPoint(item)); }
        /// <summary>Adds the given item to this <see cref="AbstractQuadTree{TItem, TNumber}"/>.</summary>
        public void Add(TItem item) { Add((IBounded<TNumber>)item); }
        private void Add(IBounded<TNumber> item)
        {
            IRect<TNumber> bounds = item.Bounds;
            if (_Root == null)
            {
                _Root = new Node(this, null, bounds.Left, bounds.Right, bounds.Bottom, bounds.Top);
                _Root.Items.Add(item);
            }
            else if (_Root.Bounds.Contains(item.Bounds))
            {
                _Root.Add(item);
            }
            else if (!_Root.HasChildren)
            {
                _Root.Bounds = bounds;
                _Root.Add(item);
            }
            else
            {
                Node oldRoot = _Root;
                IRect<TNumber> newBounds = _Root.Bounds.GetUnion(item.Bounds);
                _Root = new Node(this, null, newBounds.Left, newBounds.Right, newBounds.Bottom, newBounds.Top);
                _Root.Add(item);
                foreach (TItem oldItem in oldRoot.GetContents())
                    _Root.Add(oldItem);
            }
            Count++;
        }

        /// <summary>Removes all items from this <see cref="AbstractQuadTree{TItem, TNumber}"/>.</summary>
        public void Clear() { _Root = null; Count = 0; }

        /// <summary>
        /// Calculates the furthest extent of the bounds that would contain all the content items 
        /// of this <see cref="AbstractQuadTree{TItem, TNumber}"/>.  This is an O(n) operation, 
        /// where n is the number of content items existing.
        /// </summary>
        public Rect<TNumber> GetBounds()
        {
            Rect<TNumber> result = Rect<TNumber>.Empty;
            if (_Root != null)
            {
                foreach (TItem item in _Root.GetContents())
                    result = (Rect<TNumber>)result.GetUnion(item.Bounds);
            }
            return result;
        }

        /// <summary>
        /// Removes the given item from this <see cref="AbstractQuadTree{TItem, TNumber}"/>, if it exists.  Returns true on success.  If 
        /// the item did not exist on the tree to begin with, returns false.
        /// </summary>
        public bool Remove(TItem item)
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


        protected abstract TNumber GetMid(TNumber a, TNumber b);
        protected abstract bool Exclude(TNumber size, IRect<TNumber> rect);

        public IEnumerable<IPoint<TNumber>> GetPoints(IRect<TNumber> bounds)
        {
            if (_Root == null || !bounds.Overlaps(_Root.Bounds)) yield break;
            Stack<Node> stack = new Stack<Node>();
            stack.Push(_Root);
            while (stack.Count > 0)
            {
                Node focus = stack.Pop();
                foreach (var child in focus.Children)
                    if (child != null && bounds.Overlaps(child.Bounds)) stack.Push(child);
                foreach (var item in focus.Items)
                {
                    if (!bounds.Overlaps(item.Bounds)) continue;
                    if (item is BoundedPoint bp) yield return bp.Point;
                }

            }
        }
        public IEnumerable<TItem> GetIntersectors(IRect<TNumber> bounds)
        {
            if (_Root == null || !bounds.Overlaps(_Root.Bounds)) yield break;
            Stack<Node> stack = new Stack<Node>();
            stack.Push(_Root);
            while (stack.Count > 0)
            {
                Node focus = stack.Pop();
                foreach (var child in focus.Children)
                    if (child != null && bounds.Overlaps(child.Bounds)) stack.Push(child);
                foreach (var item in focus.Items)
                {
                    if (!bounds.Overlaps(item.Bounds)) continue;
                    if (item is TItem ti) yield return ti;
                }
                    
            }
        }
        public IEnumerable<TItem> GetIntersectors(IPoint<TNumber> point)
        {
            if (_Root == null || !_Root.Bounds.Contains(point)) yield break;
            Stack<Node> stack = new Stack<Node>();
            stack.Push(_Root);
            while (stack.Count > 0)
            {
                Node focus = stack.Pop();
                foreach (var child in focus.Children)
                    if (child != null && child.Bounds.Contains(point)) stack.Push(child);
                foreach (var item in focus.Items)
                {
                    if (!item.Bounds.Contains(point)) continue;
                    if (item is TItem ti) yield return ti;
                }                    
            }
        }

        private sealed class Node : IBounded<TNumber>
        {
            public const int NODE_CAPACITY = 16;
            public readonly Node Parent;
            private readonly AbstractQuadTree<TItem, TNumber> _Tree;
            public TNumber MidX => _Tree.GetMid(Bounds.Right, Bounds.Left);
            public TNumber MidY => _Tree.GetMid(Bounds.Top, Bounds.Bottom);
            public IRect<TNumber> Bounds { get; internal set; } = Rect<TNumber>.Empty;
            public readonly List<IBounded<TNumber>> Items = new List<IBounded<TNumber>>(NODE_CAPACITY);
            // _____________
            // |     |     |
            // |  1  |  0  |
            // |_____|_____|
            // |     |     |
            // |  2  |  3  |
            // |_____|_____|            
            public readonly Node[] Children = new Node[4];
            public Node(AbstractQuadTree<TItem, TNumber> quadTree, Node parent, TNumber left, TNumber right, TNumber bottom, TNumber top)
            {
                this._Tree = quadTree;
                this.Parent = parent;
                this.Bounds = new Rect<TNumber>(left, right, bottom, top);
            }
            public bool HasChildren => Children[0] != null || Children[1] != null || Children[2] != null || Children[3] != null;

            /// <summary>
            /// Adds the given item to this node.  If it won't fit in this node, but could fit  on a child, adds it to 
            /// the child.
            /// </summary>
            public void Add(IBounded<TNumber> item)
            {
                int quadrant;
                // If the Items is at capacity, or no child will fit the item anyway, just add it here.
                if (Items.Count < NODE_CAPACITY || (quadrant = GetFittingQuadrant(item.Bounds)) < 0)
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

            /// <summary>Returns whether the given item exists at this node.</summary>
            public bool Contains(TItem item)
            {
                Node node = this;
                while (node != null)
                {
                    if (node.Items.Contains(item)) return true;
                    int quadrant = GetFittingQuadrant(item.Bounds);
                    if (quadrant < 0) return false;
                    node = Children[quadrant];
                }
                return false;
            }

            public IEnumerable<TItem> GetContents(TNumber minSize = default(TNumber)) => GetContents(Bounds, minSize);
            /// <summary>Returns the content items which would intersect the given region.</summary>
            public IEnumerable<TItem> GetContents(IRect<TNumber> region, TNumber minSize = default(TNumber))
            {
                Stack<Node> stack = new Stack<Node>();
                stack.Push(this);
                while (stack.Count > 0)
                {
                    Node node = stack.Pop();
                    if (_Tree.Exclude(minSize, node.Bounds)) continue;
                    if (!node.Bounds.Overlaps(region)) continue;
                    foreach (TItem item in node.Items)
                        if (!_Tree.Exclude(minSize, item.Bounds))
                            yield return item;
                    for (int i = 0; i < 4; i++)
                    {
                        Node n = node.Children[i];
                        if (n != null) stack.Push(n);
                    }
                }
            }

            /// <summary>
            /// Removes the item from this node, if it exists.  If it exists and is removed, returns true.  Otherwise, 
            /// returns false.
            /// </summary>
            public bool Remove(IBounded<TNumber> item)
            {
                Node node = this;
                while (node != null)
                {
                    if (node.Items.Remove(item) || (item is BoundedPoint bp && node.Items.Remove(bp)))
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
                        int quadrant = GetFittingQuadrant(item.Bounds);
                        if (quadrant < 0) return false;
                        node = Children[quadrant];
                    }
                }

                // No node left to search, so the item doesn't exist here.
                return false;
            }


            private int GetFittingQuadrant(IRect<TNumber> itemBounds)
            {
                TNumber midX = MidX, midY = MidY;
                if (itemBounds.Left.CompareTo(midX) >= 0 && itemBounds.Bottom.CompareTo(midY) >= 0) return 0;
                if (itemBounds.Right.CompareTo(midX) <= 0 && itemBounds.Bottom.CompareTo(midY) >= 0) return 1;
                if (itemBounds.Right.CompareTo(midX) <= 0 && itemBounds.Top.CompareTo(midY) <= 0) return 2;
                if (itemBounds.Left.CompareTo(midX) >= 0 && itemBounds.Top.CompareTo(midY) <= 0) return 3;
                return -1;
            }
            private Node CreateChild(int quadrant)
            {
                switch (quadrant)
                {
                    case 0: return new Node(_Tree, this, MidX, Bounds.Right, MidY, Bounds.Top);
                    case 1: return new Node(_Tree, this, Bounds.Left, MidX, MidY, Bounds.Top);
                    case 2: return new Node(_Tree, this, Bounds.Left, MidX, Bounds.Bottom, MidY);
                    case 3: return new Node(_Tree, this, MidX, Bounds.Right, Bounds.Bottom, MidY);
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
