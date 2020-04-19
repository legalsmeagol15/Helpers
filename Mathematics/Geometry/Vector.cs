using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Geometry
{
    public interface IPoint <T> { T X { get; } T Y { get; } }
    /// <summary>
    /// A data structure embodying a vector in two-dimensional space.
    /// </summary>
    [Serializable]
    public struct Vector : IPoint<double>
    {
        /// <summary>The x-coordinate.</summary>
        public double X { get; private set; }
        /// <summary>The y-coordinate.</summary>
        public double Y { get; private set; }
        /// <summary>Creates a new two-dimensional vector.</summary>
        public Vector(double x, double y) { X = x; Y = y; }

        /// <summary>
        /// Returns the square of the length of this vector.  <para/>Many mathematical operations rely on the square of the length rather than the 
        /// length itself.  This is an optimization.
        /// </summary>
        public double GetLengthSquared() { return (X * X) + (Y * Y); }
        /// <summary> Returns the length of the vector. </summary>
        public double GetLength() { return Math.Sqrt(GetLengthSquared()); }
        /// <summary>Edits this vector to be a normalized vector.</summary>
        public void Normalize() { double len = GetLength(); X /= len; Y /= len; }

#pragma warning disable 1591
        public static Vector operator +(Vector a, Vector b) { return new Vector(a.X + b.X, a.Y + b.Y); }
        public static Vector operator -(Vector a, Vector b) { return new Vector(a.X - b.X, a.Y - b.Y); }
        public static Vector operator -(Vector a) { return new Vector(-a.X, -a.Y); }
        public static Vector operator *(Vector v, double d) { return new Vector(v.X * d, v.Y * d); }
        public static Vector operator *(double d, Vector v) { return v * d; }
        public static Vector operator /(Vector v, double d) { return new Vector(v.X / d, v.Y / d); }
        public static bool operator  ==(Vector a, Vector b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Vector a, Vector b) => a.X != b.X || a.Y != b.Y;
        public override int GetHashCode()
        {
            unchecked
            {
                return Math.Abs(X.GetHashCode() + Y.GetHashCode());
            }
        }
        public override bool Equals(object obj) => obj is Vector other && this == other;
#pragma warning restore 1591

        public override string ToString() => X + "," + Y;

    }

    /// <summary>
    /// A data structure embodying a vector in three-dimensional space.
    /// </summary>
    public struct Vector3
    {

        /// <summary>The x-coordinate.</summary>
        public double X { get; private set; }
        /// <summary>The y-coordinate.</summary>
        public double Y { get; private set; }
        /// <summary>The z-coordinate.</summary>
        public double Z { get; private set; }
        /// <summary>Creates a new three-dimensional vector.</summary>
        public Vector3(double x, double y, double z) { X = x; Y = y; Z = z; }

        /// <summary>
        /// Returns the square of the length of this vector.  <para/>Many mathematical operations rely on the square of the length rather than the 
        /// length itself.  This is an optimization.
        /// </summary>
        public double GetLengthSquared() { return (X * X) + (Y * Y) + (Z * Z); }
        /// <summary> Returns the length of the vector. </summary>
        public double GetLength() { return Math.Sqrt(GetLengthSquared()); }
        /// <summary>Edits this vector to be a normalized vector.</summary>
        public void Normalize() { double len = GetLength(); X /= len; Y /= len; Z /= len; }

#pragma warning disable 1591
        public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
        public static Vector3 operator -(Vector3 a) { return new Vector3(-a.X, -a.Y, -a.Z); }
        public static Vector3 operator *(Vector3 v, double d) { return new Vector3(v.X * d, v.Y * d, v.Z * d); }
        public static Vector3 operator *(double d, Vector3 v) { return v * d; }
        public static Vector3 operator /(Vector3 v, double d) { return new Vector3(v.X / d, v.Y / d, v.Z / d); }
#pragma warning restore 1591

    }
}
