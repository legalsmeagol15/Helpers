using Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Sets
{
    /// <summary>
    /// A set that can be universal./>
    /// </summary>
    /// <remarks>
    /// While in the <seealso cref="IsUniversal"/>==true state, this set tracks the set of 
    /// exclusions and presumes everything else is included (which is the inverse of the usual 
    /// state, where it tracks only inclusions and presumes everything else is exclused).
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class TrueSet<T> : ISet<T>, ITrueSet<T>
    {
        private readonly ISet<T> _Internal;
        private TrueSet(HashSet<T> newInternal, bool isUniversal = false)
        {
            this._Internal = newInternal;
            _IsUniversal = isUniversal;
        }
        public TrueSet(IEnumerable<T> items = null) : this(items == null ? new HashSet<T>() : new HashSet<T>(items)) { }
        public TrueSet(params T[] items) : this(items == null ? new HashSet<T>() : new HashSet<T>(items)) { }
        public TrueSet<T> CreateUniversal() => new TrueSet<T>(new HashSet<T>(), true);

        public bool IsEmpty => !_IsUniversal && _Internal.Count == 0;

        private bool _IsUniversal = false;  // When this is true, it's an exclusion set, not an inclusion set.
        public bool IsUniversal => _IsUniversal;


        public bool Add(T item) => _IsUniversal ? _Internal.Remove(item) : _Internal.Add(item);
        public void Clear() { _Internal.Clear(); _IsUniversal = false; }
        public bool Contains(T item) => _IsUniversal ^ _Internal.Contains(item);
        public int Count => _IsUniversal ? throw new InvalidOperationException() : _Internal.Count;
        public bool Remove(T item) => _IsUniversal ? _Internal.Add(item) : _Internal.Remove(item);

        public void IncludeUniversal() { _Internal.Clear(); _IsUniversal = true; }


        ITrueSet<T> ITrueSet<T>.And(ITrueSet<T> other)
        {
            throw new NotImplementedException();
        }

        ITrueSet<T> ITrueSet<T>.Or(ITrueSet<T> other)
        {
            throw new NotImplementedException();
        }

        ITrueSet<T> ITrueSet<T>.Not() => new TrueSet<T>(new HashSet<T>(_Internal), !_IsUniversal);



        #region TrueSet<T> ISet<T> pass-through members

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotImplementedException();

        void ISet<T>.ExceptWith(IEnumerable<T> other) => throw new NotImplementedException();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();

        void ISet<T>.IntersectWith(IEnumerable<T> other) => throw new NotImplementedException();

        bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other) => throw new NotImplementedException();

        bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other) => throw new NotImplementedException();

        bool ISet<T>.IsSubsetOf(IEnumerable<T> other) => throw new NotImplementedException();

        bool ISet<T>.IsSupersetOf(IEnumerable<T> other) => throw new NotImplementedException();

        bool ISet<T>.Overlaps(IEnumerable<T> other) => throw new NotImplementedException();

        bool ISet<T>.SetEquals(IEnumerable<T> other) => throw new NotImplementedException();

        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other) => throw new NotImplementedException();

        void ISet<T>.UnionWith(IEnumerable<T> other) => throw new NotImplementedException();

        void ICollection<T>.Add(T item) => Add(item);
        bool ICollection<T>.IsReadOnly => _Internal.IsReadOnly;

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();


        #endregion
    }
}
