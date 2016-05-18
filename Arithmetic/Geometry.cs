using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Arithmetic
{
    public static class Geometry
    {
        /// <summary>
        /// Returns the distance between two two-dimensional points.
        /// </summary>
        public static double GetDistance(Point a, Point b)
        {
            double xDiff = a.X - b.X;
            double yDiff = a.Y - b.Y;
            return Math.Sqrt((xDiff * xDiff) + (yDiff * yDiff));
        }

        /// <summary>
        /// Returns the closest point among the given points.
        /// </summary> 
        public static Point GetClosestPoint(Point toPoint, IEnumerable<Point> amongPoints)
        {
            double d;
            return GetClosestPoint(toPoint, amongPoints, out d);
        }
        /// <summary>
        /// Returns the closest point among the given points, specifying the distance in the given 'out' double.
        /// </summary> 
        public static Point GetClosestPoint(Point toPoint, IEnumerable<Point> amongPoints, out double distance)
        {
            distance = double.PositiveInfinity;
            Point result = new Point(double.NaN, double.NaN);
            foreach (Point pt in amongPoints)
            {
                double newDist = GetDistance(toPoint, pt);
                if (newDist< distance)
                {
                    result = pt;
                    distance = newDist;
                }
            }            
            return result;
        }
        /// <summary>
        /// Returns the closest point among the given points, specifying the distance in the given 'out' double and the index of the closest point in the given 
        /// 'out' int.
        /// </summary> 
        public static Point GetClosestPoint(Point toPoint,  IList<Point> amongPoints, out int index, out double distance)
        {
            distance = double.PositiveInfinity;
            Point result = new Point(double.NaN, double.NaN);
            index = -1;
            for (int i = 0; i < amongPoints.Count; i++)
            {
                Point pt = amongPoints[i];
                double newDist = GetDistance(toPoint, pt);
                if (newDist< distance)
                {
                    result = pt;
                    index = i;
                    distance = newDist;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the closest point on a line defined by the given line points, specifying the distance in the given 'out' double.
        /// </summary>
        /// <param name="toPoint">The point nearest which a point in the given line is sought.</param>
        /// <param name="lineA">The first point defining the line whose nearest point is sought.</param>
        /// <param name="lineB">The second point defining the line whose nearest point is sought.</param>
        /// <param name="distance">The distance between the given point and the nearest point in the line will be given in this 'out' variable.</param>        
        public static Point GetClosestPoint(Point toPoint, Point lineA, Point lineB, out double distance)
        {
            //If the lines are horizontal or vertical, the solution is easy.
            if (lineA.X == lineB.X)
            {
                distance = Math.Abs(lineA.X - toPoint.X);
                return new Point(lineA.X, toPoint.Y);
            }
            if (lineA.Y == lineB.Y)
            {
                distance = Math.Abs(lineA.Y - toPoint.Y);
                return new Point(toPoint.X, lineA.Y);
            }

            //Otherwise, the solution algebraically is:
            throw new NotImplementedException();

        }


    }
}
