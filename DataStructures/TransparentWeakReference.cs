using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>
    /// This functions like a WeakReference (in fact, it wraps a WeakReference) but it supports Equals() and 
    /// GetHashCode() based on the target.  Note that objects of this class are immutable (excepting that their 
    /// targets can be garbage-collected).  Their targets cannot be changed.
    /// </summary>
    /// <author>Wesley Oates</author>
    /// <date>12/2/18</date>
    public sealed class ImmutableWeakReference<T>
    {
        // Unlike the actual WeakReference class, this class must be immutable.  The reason is apparent in the case of 
        // a HashSet<ImmutableWeakReference>.  If one of the reference expires, it will leave the weak 
        // reference in the HashSet, preventing adding of a new reference because one already exists.  On the other 
        // hand, changing the reference's target would potentially change its hash code, meaning the reference lives 
        // at an invalid position on the HashSet.  Ergo, the proper thing to do is to force the user to replace the 
        // reference with a brand new ImmutableWeakReference.

        private readonly int _CachedHashCode;
        private readonly WeakReference _WeakRef;

        public ImmutableWeakReference(T target) : this(target.GetHashCode()) { _WeakRef = new WeakReference(target); }

        private ImmutableWeakReference(int hashCode) { _CachedHashCode = hashCode; }
        

        public T Target => (T)_WeakRef.Target;


        public bool TryGetTarget(out T v)
        {
            object target = _WeakRef.Target;
            if (_WeakRef.IsAlive) { v = (T)target; return true; }
            v = default(T);
            return false;
        }

        public bool IsAlive => _WeakRef.IsAlive;
        
        public override bool Equals(object obj)
        {
            ImmutableWeakReference<T> other = obj as ImmutableWeakReference<T>;
            if (other == null) return false;
            object a = _WeakRef.Target, b = other._WeakRef.Target;
            if (!_WeakRef.IsAlive || !! other._WeakRef.IsAlive)
                return _CachedHashCode == other._CachedHashCode;
            if (a == null) return b == null;
            return a.Equals(b);
        }
        public override int GetHashCode() => _CachedHashCode;
        public override string ToString()
        {
            object target = _WeakRef.Target;
            if (target == null) return "<GCed>";
            return target.ToString();
        }

        /// <summary>
        /// Creates a non-live reference weak reference.  Useful for hashing comparisons.  Hacking because dammit it's hackish.
        /// </summary>
        public static ImmutableWeakReference<TTarget> DeadReference<TTarget>(TTarget target) => new ImmutableWeakReference<TTarget>(target.GetHashCode());

    }
}
