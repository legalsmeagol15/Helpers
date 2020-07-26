using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics
{
    public static class Set
    {
        public static bool IterateEquals<T>(this IEnumerable<T> a, IEnumerable<T> b)
        {
            var iterA = a.GetEnumerator();
            var iterB = b.GetEnumerator();
            while (iterA.MoveNext())
            {
                if (!iterB.MoveNext()) return false;
                if (!iterA.Current.Equals(iterB.Current)) return false;
            }
            if (iterB.MoveNext()) return false;
            return true;
        }
    }
    //TODO:  move this into the Set static class
    public interface ITrueSet<T>
    {
        bool IsEmpty { get; }
        bool IsUniversal { get; }
        bool Contains(T item);
        ITrueSet<T> And(ITrueSet<T> other);
        ITrueSet<T> Or(ITrueSet<T> other);
        ITrueSet<T> Not();
    }
}
