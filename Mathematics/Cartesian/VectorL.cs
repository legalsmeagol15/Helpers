using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Cartesian
{
    public struct VectorL
        //TODO:  Fully implement VectorL
    {
        public readonly long X;
        public readonly long Y;
        public VectorL(long x, long y)
        {
            X = x;
            Y = y;
            throw new NotImplementedException();
        }
        public long DotProduct(VectorL other)
        {
            return this * other;
        }
       

        public static VectorL operator +(VectorL a, VectorL b)
        {
            return new VectorL(a.X + b.X, a.Y + b.Y);
        }
        public static VectorL operator -(VectorL a, VectorL b)
        {
            return new VectorL(a.X - b.X, a.Y - b.Y);
        }
        public static long operator *(VectorL a, VectorL b)
        {
            //Gets the dot product.
            return (a.X * b.X) + (a.Y * b.Y);
        }
        public static VectorL operator *(VectorL a, long scalar)
        {
            return new VectorL(a.X * scalar, a.Y * scalar);
        }
        public static VectorL operator *(VectorL a, double scalar)
        {
            return new VectorL(a.X * (long)scalar, a.Y * (long)scalar);
        }
        public static VectorL operator *(VectorL a, float scalar)
        {
            return new VectorL(a.X * (long)scalar, a.Y * (long)scalar);
        }
        public static VectorL operator /(VectorL a, long divisor)
        {
            return new VectorL(a.X / divisor, a.Y / divisor);
        }
        public static VectorL operator /(VectorL a, double divisor)
        {
            return new VectorL(a.X / (long)divisor, a.X / (long)divisor);
        }
        public static VectorL operator /(VectorL a, float divisor)
        {
            return new VectorL(a.X / (long)divisor, a.X / (long)divisor);
        }

    }

    public static class Smoo
    {
        public static void Blah()
        {
            System.Windows.Vector v = new System.Windows.Vector(0, 5);
            

        }
    }
}
