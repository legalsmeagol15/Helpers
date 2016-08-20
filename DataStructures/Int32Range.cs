using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public class Int32Range : Range4<int>
    {

        static Int32Range()
        {
            Bound.GetValueDistance = (a, b) => { return a - b; };
        }

        public Int32Range(Int32 min, Int32 max) : base((Int32)min, (Int32)max) { }
    }
}
