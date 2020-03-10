using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public static class Algorithm_Lab
    {
        /// <summary>
        /// Returns the minimum edit distance necessary to convert string <paramref name="a"/> 
        /// into string <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The origin string.</param>
        /// <param name="b">The target string.</param>
        /// <param name="limit_distance">Optional.  The maximum distance that may be returned.  
        /// This parameter acts as a limit on calculation that may be used in time-sensitive 
        /// situations.</param>
        public static int EditDistance(string a, string b, int limit_distance = int.MaxValue)
        {
            // see https://www.geeksforgeeks.org/edit-distance-dp-5/

            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;


            // Dynamic programming makes this O(a.length * b.length), instead of O(3^a.length)
            int[,] known = new int[a.Length, b.Length];

            return _EditDistance(a.Length, b.Length, limit_distance);


            int _EditDistance(int a_idx, int b_idx, int limit)
            {
                if (limit < 0)
                    return limit_distance;
                if (a_idx == 0)
                    return b_idx;
                if (b_idx == 0)
                    return a_idx;
                int tmp = known[a_idx, b_idx];
                if (tmp > 0)
                    return tmp;
                // If the substrings end the same, then return the next-smallest substrings.
                tmp = _EditDistance(a_idx - 1, b_idx - 1, limit-1);
                if (a[a_idx] != b[b_idx])
                {
                    tmp = Math.Min(tmp, _EditDistance(a_idx, b_idx - 1, limit-1)); // insert
                    tmp = Math.Min(tmp, _EditDistance(a_idx - 1, a_idx, limit-1)); // remove
                    // We already calculated a replace value.
                    tmp++;
                }
                return (known[a_idx, b_idx] = tmp);
            }
        }
    }
}
