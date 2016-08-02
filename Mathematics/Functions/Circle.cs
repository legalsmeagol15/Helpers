using Mathematics;
using Mathematics.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mathematics.Calculus
{
    public sealed class Circle : IFunction<Point>
    {
        public Point Center { get; }

        public double Radius { get; } = 0.0;

        public double ArcStart { get; } = 0.0;
        public double ArcEnd { get; } = Mathematics.CommonValues.PiTimes2;


        public Circle(Point center, double radius)
        {
            this.Center = center;
            this.Radius = radius;
        }
        public Circle(Point center, double radius, double arcStart, double arcEnd) : this(center, radius)
        {
            this.ArcStart = arcStart;
            this.ArcEnd = arcEnd;
        }

       
        public double GetY(double atX)
        {
            double t = atX - Center.X;
            return (Radius * Math.Sin(t)) + Center.Y;
        }
        Point IFunction<Point>.Evaluate(double atX)
        {
            return new Point(atX, GetY(atX));     
        }
    }
}
