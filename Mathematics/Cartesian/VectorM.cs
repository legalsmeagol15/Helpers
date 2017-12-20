using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Cartesian
{
    /// <summary>
    /// Represents a two-dimensional vector using decimals rather than ints or doubles.
    /// </summary>
    public struct VectorM
    {
        public readonly decimal X;
        public readonly decimal Y;
        public VectorM(decimal x, decimal y)
        {
            this.X = x;
            this.Y = y;
            
        }

        public decimal Length
        {
            get
            {
                return Mathematics.Operations.Sqrt((X * X) + (Y * Y));
            }
        }
        public decimal LengthSquared
        {
            get
            {
                decimal l = Length;
                return l * l;
            }
        }
        public VectorM GetNormalized()
        {
            decimal l = Length;
            return new VectorM(X / l, Y / l);            
        }
        


        #region VectorM arithmetic operators

        public static VectorM operator +(VectorM a, VectorM b)
        {
            return new VectorM(a.X + b.X, a.Y + b.Y);
        }
        public static VectorM operator -(VectorM a, VectorM b)
        {
            return new VectorM(a.X - b.X, a.Y - b.Y);
        }
        public static VectorM operator *(VectorM v, decimal d)
        {
            return new VectorM(v.X * d, v.Y * d);
        }
        public static VectorM operator *(decimal d, VectorM v)
        {
            return v * d;
        }
        public static VectorM operator /(VectorM v, decimal d)
        {
            return new VectorM(v.X / d, v.Y / d);
        }
        public static VectorM operator -(VectorM v)
        {
            return new VectorM(-v.X, -v.Y);
        }

        #endregion



        #region VectorM comparison operators

        public override bool Equals(object obj)
        {
            if (!(obj is VectorM)) return false;
            VectorM other = (VectorM)obj;
            return X == other.X && Y == other.Y;
        }
        public override int GetHashCode()
        {
            return Math.Abs((int)X + (int)Y);
        }
        public static bool operator ==(VectorM a, VectorM b)
        {
            return a.X == b.X && a.Y == b.Y;
        }
        public static bool operator !=(VectorM a, VectorM b)
        {
            return a.X != b.X || a.Y != b.Y;
        }

        #endregion



        #region VectorM conversion operators

        public override string ToString()
        {
            return X + ", " + Y;
        }

        public static explicit operator System.Windows.Vector(VectorM v)
        {
            return new System.Windows.Vector((double)v.X, (double)v.Y);
        }
        public static implicit operator VectorM(System.Windows.Vector v)
        {
            return new VectorM((decimal)v.X, (decimal)v.Y);
        }
        public static explicit operator VectorM(PointM p)
        {
            return new VectorM(p.X, p.Y);
        }

        #endregion
    }
}
