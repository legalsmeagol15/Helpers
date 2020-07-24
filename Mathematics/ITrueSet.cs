using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics
{
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
