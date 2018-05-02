using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Cartesian
{
    /// <summary>
    /// Embodies a two-dimensional point.
    /// </summary>
    [Serializable()]
    public struct Point : ITransformable<Point, Matrix2>
    {
        /// <summary>The x-coordinate.</summary>
        public double X { get; private set; }
        /// <summary>The y-coordinate.</summary>
        public double Y { get; private set; }

        /// <summary>Creates a point at the given x,y.</summary>        
        public Point(double x, double y) { X = x;  Y = y; }
        public Point3 ToPoint3() => new Point3(X, Y, 0);
        public Point GetTransformed(Matrix2 matrix)
        {
            throw new NotImplementedException();
        }

#pragma warning disable 1591        
        public static Point operator +(Point p, Vector v) { return new Point(p.X + v.X, p.Y + v.Y); }
        public static Point operator -(Point p, Vector v) { return new Point(p.X - v.X, p.Y - v.Y); }
        public static Vector operator -(Point destination, Point origin) { return new Vector(destination.X - origin.X, destination.Y - origin.Y); }

        
        
#pragma warning restore 1591
    }

    /// <summary>
    /// A data structure which embodies a point in three-dimensional space.
    /// </summary>
    public struct Point3
    {
        /// <summary>The x-coordinate.</summary>
        public double X { get; private set; }
        /// <summary>The y-coordinate.</summary>
        public double Y { get; private set; }
        /// <summary>The Z-coordinate.</summary>
        public double Z { get; private set; }

        /// <summary>Creates a point at the given x,y,z.</summary>
        public Point3(double x, double y, double z) { X = x; Y = y; Z = z; }


#pragma warning disable 1591
        public static Point3 operator +(Point3 p, Vector3 v) { return new Point3(p.X + v.X, p.Y + v.Y, p.Z + v.Z); }
        public static Point3 operator -(Point3 p, Vector3 v) { return new Point3(p.X - v.X, p.Y - v.Y, p.Z - v.Z); }
        public static Vector3 operator -(Point3 destination, Point3 origin) { return new Vector3(destination.X - origin.X, destination.Y - origin.Y, destination.Z - origin.Z); }
#pragma warning restore 1591

    }
}
