using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Sets
{
    public static class Extensions
    {

        public static  bool Contains<T>(this IEnumerable<T> set, Func<T, bool> predicate)
        {
            foreach (T item in set)            
                if (predicate(item)) return true;
            
            return false;
        }
    }
}
