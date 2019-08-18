using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mathematics;

namespace Helpers.DataStructures.Trees
{
    public interface IRect
    {
        double Left { get; }
        double Right { get; }
        double Top { get; }
    }

    public class QuadTree<T> : IEnumerable<T> where T:IRect
    {
        private sealed class Node
        {

        }
    }
}
