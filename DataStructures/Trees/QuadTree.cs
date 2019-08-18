using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mathematics.Geometry;

namespace Helpers.DataStructures.Trees
{
    public class QuadTreeD<T> : IEnumerable<T> where T : IRect<double>
    {
        private Node _Root = null;
        
        public void Add (T item)
        {
            if (_Root == null)
            {
                _Root = new Node(null, item.Left, item.Right, item.Bottom, item.Top);
                _Root.Items.Add(item);
                return;
            }
            else if (_Root.Contains(item))
            {
                _Root.Add(item);
                return;
            }
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

            public bool Contains(T item) => Rect.Contains(item);

            public IEnumerable<T> GetInclusions(IRect<double> region)
            {

            }

            public bool Includes(T item)
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

            public bool Remove(T item)
            {
                Node node = this;
                while (node != null)
                {
                    if (node.Items.Remove(item)) return true;
                    int quadrant = GetFittingQuadrant(item);
                    if (quadrant < 0) return false;
                    node = Children[quadrant];                    
                }
                return false;
            }


            private int GetFittingQuadrant(T item)
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
        }
    }
}
