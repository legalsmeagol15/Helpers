using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics
{
    public static class Sorting
    {
        public static void MergeSort<T>(IList<T> list, int startIndex, int endIndex) where T : IComparable<T>
        {
            throw new NotImplementedException();
        }
        public static void MergeSort<T>(IList<T> list) where T : IComparable<T>
        {
            MergeSort(list, 0, list.Count - 1);
        }

        /// <summary>
        /// Insertion-sorts the items in the given list.
        /// </summary>
        /// <remarks>The insertion sort is a very lightweight sorting algorithm, which works in place and requires only minimal additional memory to execute.  
        /// However, its behavior in worst-case scenarios (i.e., the perfectly contra-sorted list) exhibits O(n^2) asymptotic time complexity.  Therefore, the 
        /// insertion sort is best used only to sort small lists.  In this case, a "small" list is usually no more than 50-100 items.</remarks>        
        public static void InsertionSort<T>(IList<T> items) where T : IComparable<T>
        {
            for (int overallIdx = 1; overallIdx < items.Count; overallIdx++)
            {
                int walkIdx = overallIdx;
                while (walkIdx > 0 && items[walkIdx - 1].CompareTo(items[walkIdx]) > 0)
                {
                    T temp = items[walkIdx];
                    items[walkIdx] = items[--walkIdx];
                    items[walkIdx] = temp;
                }
            }
        }
    }
}
