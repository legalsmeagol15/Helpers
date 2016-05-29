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
        /// <param name="amongPoints">The points to search for the closest point.  If omitted, this method will return a distance of double.PositiveInfinity and 
        /// an index of -1.</param>
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
        /// <param name="line0">The first point defining the line whose nearest point is sought.</param>
        /// <param name="line1">The second point defining the line whose nearest point is sought.</param>
        /// <param name="distance">The distance between the given point and the nearest point in the line will be given in this 'out' variable.</param>        
        /// <param name="t">The traversal distance from the first point to the second point.</param>
        /// <remarks>From code posted by 0BZen at http://www.gamedev.net/topic/444154-closest-point-on-a-line/. </remarks>
        public static Point GetClosestPoint(Point toPoint, Point line0, Point line1, out double t, out double distance)
        {
            Vector line0toPoint = toPoint - line0;
            Vector segment = line1 - line0;
            double segSquared = segment.X * segment.X + segment.Y * segment.Y;
            double dotProd = Vector.Multiply(line0toPoint, segment);
            //double dotProd = line0toPoint.X * segment.X + line0toPoint.Y * segment.Y;
            t = dotProd / segSquared;            
            Point result = line0 + (segment * t);
            distance = GetDistance(toPoint, result);
            return result;
        }

        /// <summary>
        /// The default precision in the traversal degree.
        /// </summary>
        private const double DEFAULT_PRECISION = 0.000001;

        /// <summary>
        /// Uses recursion to approximate the closest point on the given Bezier curve to the given point.
        /// </summary>
        /// <param name="toPoint">The point whose closest point on the curve is sought.</param>
        /// <param name="curve">The Bezier curve to examine.</param>
        /// <param name="t">The result traversal between the start point and end points of the given Bezier curve, from 0 to 1.</param>
        /// <param name="distance">The distance between the given point and the closest point of the curve.</param>
        /// <param name="precision">Optional.  The precision to which the nearest point is sought.  This functions as a limit on the depth of recursion.</param>
        /// <returns>Returns the (approximate) point that is closest.</returns>
        /// <remarks>Validated visually on 5/29/16.
        /// <para/>This method recursively divides the bezier into three segments, and then determines which of those segments is nearest, and subdivides that 
        /// segment from there.  The resulting 't' value will always be within the distance of the given precision value of the true closest result.  The 
        /// method is O(tbd) with respect to the level of precision required.</remarks>
        public static Point GetClosestPoint(Point toPoint, Calculus.BezierCubic curve, out double t, out double distance, double precision = DEFAULT_PRECISION)
        {
            //This method must track t, point, and distance at four points in the curve, at all times.  Use arrays to avoid spilling registers to who-knows-where, 
            //and instead keep the items in cache (hopefully).
            double gap = 1d / 3d;
            double[] tt = new double[4] { 0, gap, 2*gap, 1 };
            Point[] pt = new Point[4] { curve.Evaluate(tt[0]), curve.Evaluate(tt[1]), curve.Evaluate(tt[2]), curve.Evaluate(tt[3]) };
            double[] dist = new double[4] { GetDistance(toPoint, pt[0]), GetDistance(toPoint, pt[1]), GetDistance(toPoint, pt[2]), GetDistance(toPoint, pt[3]) };
            char seg = '-';

            while (gap * 2 > precision)
            {
                //Which pair are the closest?
                double sumDistA = dist[0] + dist[1];
                double sumDistB = dist[1] + dist[2];
                double sumDistC = dist[2] + dist[3];
                char newSeg = sumDistA < sumDistB ? (sumDistA < sumDistC ? 'a' : 'c') : (sumDistB < sumDistC ? 'b' : 'c');

                //If the result is being smooshed to either tt[0] or tt[3], this is an indication that the wrong route has been chosen.  Adjust the segments and 
                //try again.   
                if (newSeg == seg)
                {
                    if (seg == 'a' && tt[0] > 0.0)
                    {
                        //Shift everything downward, finding the values for [0] anew.
                        tt[3] = tt[2];
                        tt[2] = tt[1];
                        tt[1] = tt[0];
                        tt[0] = Math.Max(0.0, tt[1] - gap);
                        pt[3] = pt[2];
                        pt[2] = pt[1];
                        pt[1] = pt[0];
                        pt[0] = curve.Evaluate(tt[0]);
                        dist[3] = dist[2];
                        dist[2] = dist[1];
                        dist[1] = dist[0];
                        dist[0] = GetDistance(toPoint, pt[0]);
                        continue;
                    }
                    if (seg == 'c' && tt[3] < 1.0)
                    {
                        //Shift everything upward, finding the values for [3] over again.
                        tt[0] = tt[1];
                        tt[1] = tt[2];
                        tt[2] = tt[3];
                        tt[3] = Math.Min(1.0, tt[2] + gap);
                        pt[0] = pt[1];
                        pt[1] = pt[2];
                        pt[2] = pt[3];
                        pt[3] = curve.Evaluate(tt[3]);
                        dist[0] = dist[1];
                        dist[1] = dist[2];
                        dist[2] = dist[3];
                        dist[3] = GetDistance(toPoint, pt[3]);
                        continue;
                    }
                }

                //Since recursion will continue from here, adjust the bracketing points to the appropriate intermediate points.
                seg = newSeg;
                switch (newSeg)
                {
                    case 'a':
                        tt[3] = tt[1];
                        pt[3] = pt[1];
                        dist[3] = dist[1];                        
                        break;
                    case 'b':
                        tt[0] = tt[1];
                        tt[3] = tt[2];
                        pt[0] = pt[1];
                        pt[3] = pt[2];
                        dist[0] = dist[1];
                        dist[3] = dist[2];
                        break;
                    case 'c':
                        tt[0] = tt[2];
                        pt[0] = pt[2];
                        dist[0] = dist[2];
                        break;
                }

                //Find the new intermediate 't's to evaluate.
                gap = (tt[3] - tt[0]) / 3;
                tt[1] = tt[0] + gap;
                tt[2] = tt[1] + gap;
                pt[1] = curve.Evaluate(tt[1]);
                pt[2] = curve.Evaluate(tt[2]);
                dist[1] = GetDistance(toPoint, pt[1]);
                dist[2] = GetDistance(toPoint, pt[2]);
            }

            //Now, there are four potential points that have been narrowed down significantly.  Choose the nearest.
            distance = dist[0];
            int idx = 0;
            for (int  i = 1; i < dist.Length; i++)
            {
                if (dist[i]< distance)
                {
                    distance = dist[i];
                    idx = i;
                }
            }
            t = tt[idx];
            return pt[idx];
        }
       

        public static Point GetClosestPoint(Point toPoint, Calculus.BezierQuadratic curve, out double t, out double distance, double precision = DEFAULT_PRECISION)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the shared border, if any, of the two Rect objects.
        /// </summary>
        public static Quadrant SharedBorder(this Rect a, Rect b)
        {
            Quadrant result = 0;
            if (a.Right == b.Right) result = Quadrant.Right;
            if (a.Top == b.Top) result |= Quadrant.Top;
            if (a.Left == b.Left) result |= Quadrant.Left;
            if (a.Bottom == b.Bottom) result |= Quadrant.Bottom;
            return result;
        }

        /// <summary>
        /// Describes a cardinal direction orientation.
        /// </summary>
        [Flags]
        public enum Quadrant
        {
            None = 0,
            Right = 1,
            Top = 2,
            Left = 4,
            Bottom = 8,
            TopRight = 3,
            TopLeft = 6,
            BottomLeft = 12,
            BottomRight = 9
        }

    }
}
