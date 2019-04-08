using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mathematics.Cartesian
{
    /// <summary>
    /// Embodies shapes in invariant class types.
    /// </summary>
    public interface IAbstractShape
    {
        Rect GetBounds();
    }
}
