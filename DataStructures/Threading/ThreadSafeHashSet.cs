using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Threading
{
    public class ThreadSafeHashSet<T>
    {
        private readonly HashSet<T> _InternalSet = new HashSet<T>();

        public ThreadSafeHashSet()
        {
            throw new NotImplementedException();
        }
    }
}
