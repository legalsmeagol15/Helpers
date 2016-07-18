using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Cartesian
{

    public struct PointL
        
    {
        public readonly long X;
        public readonly long Y;

        public PointL(long x, long y)
        {
            X = x;
            Y = y;
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return X + ", " + Y;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PointL)) return false;
            PointL other = (PointL)obj;
            return X == other.X && Y == other.Y;
        }
        public override int GetHashCode()
        {
            return Math.Abs((int)X + (int)Y);
        }

    }
}
