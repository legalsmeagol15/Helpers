using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics
{
    public static class Random
    {
        public static T[] Permute<T>(IEnumerable<T> items, int scramblings = -1, int seed = 0)
        {
            T[] result = items.ToArray();            
            if (scramblings < 0) scramblings = 10 * result.Length;
            System.Random rng = new System.Random(seed);

            for (int i = 0; i < scramblings; i++)
            {
                int a = rng.Next(result.Length);
                int b = rng.Next(result.Length);
                T temp = result[a];
                result[a] = result[b];
                result[b] = temp;                
            }

            return result;
        }
    }
}
