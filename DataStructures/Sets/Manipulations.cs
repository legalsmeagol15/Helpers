using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Sets
{
    public static class Manipulations
    {

        public static void Move<T>(this List<T> list, int oldIndex, int itemCount, int newIndex)
        {
            if (oldIndex < 0 || newIndex < 0)
                throw new IndexOutOfRangeException("Invalid index.");
            if (newIndex + itemCount - 1 > list.Count)
                throw new IndexOutOfRangeException("Cannot move " + itemCount + " items to index " + newIndex + " in a " + list.Count + "-item list.");

            int disturbedStart = Math.Min(oldIndex, newIndex);
            int disturbedEnd = Math.Max(oldIndex, newIndex) + itemCount - 1;
            int disturbedCount = disturbedEnd - disturbedStart + 1;
            
            T[] moving = new T[itemCount];
            for (int i = 0; i < itemCount; i++) moving[i] = list[i + oldIndex];

            throw new NotImplementedException();
        }
    }
}
