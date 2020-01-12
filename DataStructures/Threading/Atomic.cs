using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Threading
{
    public class Atomic<T>
    {
        private T _Value;
        public T Value
        {
            get {  lock (_Lock) { return _Value; } }
            set {  lock (_Lock) { _Value = value; } }
        }
        private readonly object _Lock = new object();

        public static bool operator ==(Atomic<T> a, Atomic<T> b) => a._Value.Equals(b._Value);
        public static bool operator !=(Atomic<T> a, Atomic<T> b) => a._Value.Equals(b._Value);
        public static implicit operator Atomic<T>(T v) { return new Atomic<T>(v); }
        public static implicit operator T(Atomic<T> a) { return a._Value; }

        public Atomic(T v) { _Value = v; }

        public override bool Equals(object obj) => obj is Atomic<T> other && other._Value.Equals(_Value);
        public override int GetHashCode() => _Value.GetHashCode();
        public override string ToString() => _Value.ToString();
    }
}
