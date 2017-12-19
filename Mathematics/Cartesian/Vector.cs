using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Cartesian
{
    public struct Vector
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public Vector(double x, double y) { X = x; Y = y; }

        /// <summary>
        /// Returns the square of the length of this vector.  <para/>Many mathematical operations rely on the square of the length rather than the 
        /// length itself.  This is an optimization.
        /// </summary>
        /// <returns></returns>
        public double GetLengthSquared() { return (X * X) + (Y * Y); }
        public double GetLength() { return Math.Sqrt(GetLengthSquared()); }
        public void Normalize() { double len = GetLength(); X /= len; Y /= len; }

        public static Vector operator +(Vector a, Vector b) { return new Vector(a.X + b.X, a.Y + b.Y); }
        public static Vector operator -(Vector a, Vector b) { return new Vector(a.X - b.X, a.Y - b.Y); }
        public static Vector operator -(Vector a) { return new Vector(-a.X, -a.Y); }
        public static Vector operator *(Vector v, double d) { return new Vector(v.X * d, v.Y * d); }
        public static Vector operator *(double d, Vector v) { return v * d; }
        public static Vector operator /(Vector v, double d) { return new Vector(v.X / d, v.Y / d); }

    }
}
