using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Cartesian
{
    /// <summary>
    /// Represents a two-dimensional point using decimals rather than ints or doubles.
    /// </summary>
    public struct PointM
    {
        public readonly decimal X;
        public readonly decimal Y;
        public PointM(decimal x, decimal y)
        {
            this.X = x;
            this.Y = y;
        }


        #region PointM arithmetic operators

        public static PointM operator +(PointM p, VectorM v)
        {
            return new PointM(p.X + v.X, p.Y + v.Y);
        }
        public static PointM operator -(PointM p, VectorM v)
        {
            return new PointM(p.X - v.X, p.Y - v.Y);
        }


        #endregion



        #region PointM comparison operators

        public override bool Equals(object obj)
        {
            if (!(obj is PointM)) return false;
            PointM other = (PointM)obj;
            return X == other.X && Y == other.Y;
        }
        public override int GetHashCode()
        {
            return Math.Abs((int)X + (int)Y);
        }
        public static bool operator ==(PointM a, PointM b)
        {
            return a.X == b.X && a.Y == b.Y;
        }
        public static bool operator !=(PointM a, PointM b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        #endregion



        #region PointM conversion operators

        public override string ToString()
        {
            return X + ", " + Y;
        }

        public static explicit operator System.Windows.Point(PointM p)
        {
            return new System.Windows.Point((double)p.X, (double)p.Y);
        }
        public static implicit operator PointM(System.Windows.Point p)
        {
            return new PointM((decimal)p.X, (decimal)p.Y);
        }
        public static explicit operator PointM(VectorM v)
        {
            return new PointM(v.X, v.Y);        
        }

        
        


        #endregion
    }
}
