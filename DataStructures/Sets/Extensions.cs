using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Sets
{
    public static class Extensions
    {

        public static  bool Contains<T>(this IEnumerable<T> set, Func<T, bool> predicate) => IndexOf(set, predicate) >= 0;

        public static int IndexOf<T>(this IEnumerable<T> set, Func<T, bool> predicate)
        {
            int idx = 0;
            foreach (T item in set)
            {
                if (predicate(item)) return idx;
                idx++;
            }
            return -1;
        }
    }
}
