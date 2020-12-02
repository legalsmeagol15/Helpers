using DataStructures;
using Dependency.Functions;
using Dependency.Values;
using Helpers;
using Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    /// <summary>
    /// For variables that can exhibit <seealso cref="List{T}"/>-like behaviors.  Note that any 
    /// <seealso cref="Variable"/> can hold a <seealso cref="Vector"/> as its contents or 
    /// value, but the <see cref="Listing{T}"/> allows for indexed binding and updating.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Listing<T> : IVariable<IList<T>>, IIndexable, ISyncUpdater, IAsyncUpdater,
                                    IUpdatedVariable, IList<T>
    {
        // TODO:  "Listing<T>" is a stupid class name.  Rethink this.  Just don't collide the name with regular List<T>.
        private readonly Dictionary<int, WeakReference<Indexed<Number>>> _Members;
        private readonly IConverter<T> _MemberConverter;

        public Listing(params IEvaluateable[] contents) : this(null, contents) { }
        public Listing(IConverter<T> converter = null, params IEvaluateable[] contents)
        {
            this._MemberConverter = converter ?? Dependency.Values.Converter<T>.Default;
            this._Contents = new Vector(contents);
        }

        private IEvaluateable _Contents;
        public IEvaluateable Contents
        {
            get => _Contents;
            set
            {
                Update.ForVariable(this, value).Execute();
                if (value is Vector v)
                {
                    Update.StructureLock.EnterWriteLock();
                    try
                    {
                        var toRemove = new List<int>();
                        // Clean up the members roster.
                        foreach (var kvp in _Members)
                        {
                            int idx = kvp.Key;
                            if (!kvp.Value.TryGetTarget(out Indexed<Number> member))
                            {
                                toRemove.Add(idx);
                                continue;
                            }
                            member.Parent = null;   // So we don't see the members' value updates.    
                            if (idx >= 0 && idx < v.Count)
                            {
                                member.Contents = v[idx];
                                member.Parent = this;
                            }
                            else
                            {
                                // The member should be forced to update so it can re-index.
                                member.Contents = new NoEqual();
                                toRemove.Add(idx);
                            }
                        }
                        foreach (int tr in toRemove)
                            _Members.Remove(tr);
                    }
                    finally { Update.StructureLock.ExitWriteLock(); }
                }
            }
        }
        public IEvaluateable Value => Contents.Value;


        private List<T> _Native = null;
        public IList<T> Native
        {
            get
            {
                if (_Native != null)
                    return _Native;
                if (!(_Contents is Vector v_contents))
                    return _Native = null;
                _Native = new List<T>(v_contents.Value.Select(iev => _MemberConverter.TryConvertDown(iev, out T nat) ? nat : default));
                return _Native;
            }
            set
            {
                Contents = new Vector(value.Select(nat => _MemberConverter.ConvertUp(nat)));
                _Native = null;
                // This eventually leads to _Native being re-initialized.  It just happens lazy.
            }
        }
        IList<T> IVariable<IList<T>>.Native { get => Native; set => Native = value; }
        private ISyncUpdater _Parent;
        ISyncUpdater ISyncUpdater.Parent { get => _Parent; set { _Parent = value; } }

        IEvaluateable IEvaluateable.Value => throw new NotImplementedException();

        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
        {
            if (!(ordinal is Number n) || !n.IsInteger) { val = default; return false; }
            if (!(_Contents is Vector v)) { val = default; return false; }
            int idx = (int)n;
            if (idx < 0 || idx >= v.Count) { val = default; return false; }
            if (_Members.TryGetValue(idx, out WeakReference<Indexed<Number>> wr)
                && wr.TryGetTarget(out Indexed<Number> idxed))
            {
                val = idxed;
                return true;
            }
            else
            {
                idxed = new Indexed<Number>(this, v[idx], idx);
                wr = new WeakReference<Indexed<Number>>(idxed);
                _Members[idx] = wr;
                val = idxed;
                return true;
            }
        }

        void IIndexable.IndexedContentsChanged(IEvaluateable index, IEvaluateable value)
        {
            if (_Contents is Vector v)
            {
                v[(Number)index] = value;
            }
            else
                Debug.Fail("How could _Contents not be a vector if there are indexed members?");
        }

        ITrueSet<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild, ITrueSet<IEvaluateable> indexDomain)
        {
            // Is this update coming from the contents?  Or is it coming from a member?
            if (!(Contents is Vector v_contents))
            {
                // We don't cache the old value, so nothing to compare against.
                OnListingChanged(Update.UniversalSet);
                return Update.UniversalSet;
            }
            if (indexDomain.IsUniversal || updatedChild.Equals(Contents))
            {
                // The contents are to completely change, no matter what.
                v_contents.ClearValueInternal();
                _Native = null;
                OnListingChanged(Update.UniversalSet);
                return Update.UniversalSet;
            }
            Vector current_v_value = v_contents.GetValueInternal();
            if (current_v_value == null)
            {
                // Since nobody has wanted the Contents' value anyway, there couldn't possibly be 
                // any listeners or parents for this list.
                Debug.Assert(this._Parent == null);
                Debug.Assert(_Listeners.Count == 0);
                OnListingChanged(indexDomain);
                return indexDomain;
            }

            // Otherwise, update at the appropriate indices.  Really, this should be the update 
            // from a single member variable.
            Debug.Assert(indexDomain is DataStructures.Sets.TrueSet<IEvaluateable> set && set.Count == 1);
            foreach (var idx in indexDomain.OfType<Number>())
            {
                if (idx >= 0 && idx < current_v_value.Count)
                    current_v_value[idx] = updatedChild.Value;
            }
            OnListingChanged(indexDomain);
            return indexDomain;
        }
        bool IUpdatedVariable.CommitValue(IEvaluateable newValue)
        {
            // This means the Contents just changed.
            _Native = null;
            return true;
        }
        bool IUpdatedVariable.CommitContents(IEvaluateable newContent)
        {
            if (ReferenceEquals(_Contents, newContent)) return false;
            _Contents = newContent;
            return true;
        }

        private readonly WeakReferenceSet<ISyncUpdater> _Listeners = new WeakReferenceSet<ISyncUpdater>();
        bool IAsyncUpdater.RemoveListener(ISyncUpdater listener) => _Listeners.Remove(listener);
        bool IAsyncUpdater.AddListener(ISyncUpdater listener) => _Listeners.Add(listener);
        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => _Listeners;



        #region ListingChanged members

        public class ListingChangedEventArgs : EventArgs
        {
            public readonly DiscreteIntervalSet<int> Indices;
            public ListingChangedEventArgs(DiscreteIntervalSet<int> indices) { this.Indices = indices; }
        }

        public delegate void ListingChangedEventHandler(object sender, ListingChangedEventArgs e);
        public event ListingChangedEventHandler ListingChanged;

        /// <summary>Called immediately after the cached value is updated.</summary>
        private void OnListingChanged(ITrueSet<IEvaluateable> changedIndices)
        {
            // Cannot be a ValueChangedHandler because there is no guarantee that the last-most 
            // change will be received last by any consumers.
            if (ListingChanged != null)
            {
                var handlers = ListingChanged.GetInvocationList();
                if (handlers.Length > 0)
                {
                    Int32IntervalSet iis = changedIndices.IsUniversal ?
                                           Int32IntervalSet.Infinite() :
                                           new Int32IntervalSet(changedIndices.OfType<Number>().Select(n => (int)n).ToArray());
                    ListingChangedEventArgs e = new ListingChangedEventArgs(iis);
                    Events.InvokeAsynchronous(this, ListingChanged, e);
                }
            }
        }

        #endregion



        #region Listing IList<T> members

        public int Count => Native.Count;

        public bool IsReadOnly => Native.IsReadOnly;

        public T this[int index] { get => Native[index]; set => throw new NotImplementedException(); }

        public int IndexOf(T item) => Native.IndexOf(item);

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }


        public bool Contains(T item) => Native.Contains(item);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => Native.CopyTo(array, arrayIndex);


        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Native.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Native.GetEnumerator();


        #endregion
    }

}
