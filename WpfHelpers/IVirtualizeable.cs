using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DataStructures
{
    public interface IVirtualizeable
    {
        Rect Boundary { get; }

        long ZIndex { get; }

        event EventHandler ShapeChanged;
    }
}
