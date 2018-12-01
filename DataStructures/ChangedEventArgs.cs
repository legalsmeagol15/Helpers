using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public class ChangedEventArgs<TObject, TValue> : ChangedEventArgs<TValue>
    {
        public readonly TObject Object;
        
        public ChangedEventArgs(TObject obj, TValue before, TValue after) : base(before, after) { this.Object = obj; }
    }

    public class ChangedEventArgs<TValue>
    {
        public readonly TValue Before, After;
        public ChangedEventArgs(TValue before, TValue after)
        {
            if (before.Equals(after)) throw new Exception("No change.");
            this.Before = before; this.After = after;
        }
    }
}
