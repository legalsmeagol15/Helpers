using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Cartesian
{
    public class Globe
    {
        public Point3 Center { get; private set; }
        public double xAxis { get; private set; }
        public double yAxis { get; private set; }
        public double zAxis { get; private set; }
        public Globe(double xCenter, double yCenter, double zCenter, double xAxis, double yAxis, double zAxis)
        {

        }

        public Globe(Point3 center, double xAxis, double yAxis, double zAxis)
        {
            this.Center = center;
            this.xAxis = xAxis;
        }
    }
}
