using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public struct Prioritized<T> : IComparable<Prioritized<T>>
    {
        public readonly T Value;
        public readonly int Priority;
        public Prioritized(T value, int priority) { this.Value = value;this.Priority = priority; }

        int IComparable<Prioritized<T>>.CompareTo(Prioritized<T> other)
            => Priority.CompareTo(other.Priority);

        public override string ToString() => Value.ToString();

        public override bool Equals(object obj) => obj is Prioritized<T> other && Value.Equals(other.Value);
        public override int GetHashCode() => Value.GetHashCode();
    }
}
