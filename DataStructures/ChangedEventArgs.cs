using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public class ChangedEventArgs<TObject, TValue>
    {
        public readonly TObject Object;
        public readonly TValue Before, After;
        public ChangedEventArgs(TObject obj, TValue before, TValue after) { this.Object = obj; this.Before = before; this.After = after; }
    }
}
