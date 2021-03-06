﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataStructures.Sets;
using Dependency.Functions;
using Dependency.Values;
using Dependency.Variables;
using Mathematics;

namespace Dependency
{

    /// <summary>
    /// Represents an indexable set of <seealso cref="IEvaluateable"/> values.      /// 
    /// </summary>
    public sealed class Vector : ITypeGuarantee, IContext, ISyncUpdater, IIndexable, IList<IEvaluateable>
    // Though a Vector has inputs, it CANNOT be a Function.
    // Should a Vector be mutable, or should it not?  I've gone back and forth.  At this point, I'm 
    // saying YES.
    {
        private readonly List<Indexed<Number>> _MemberContents = new List<Indexed<Number>>();
        internal IEnumerable<IEvaluateable> GetContents() => _MemberContents.Select(idxed => idxed.Contents);
        public IEnumerable<IEvaluateable> Inputs
        {
            get => _MemberContents.Select(m => m.Contents);
            internal set
            {
                _MemberContents.Clear();
                if (_Value != null) _Value.Clear();
                AddRange(value);
            }
        }
        private Vector _Value = null;
        IEvaluateable IEvaluateable.Value => this.Value;
        public Vector Value => _Value ?? (_Value = new Vector(_MemberContents.Select(i => i.Value)) { Parent = this });
        public IEvaluateable this[int idx]
        {
            get
            {
                if (TryIndexWrapper(new Number(idx), out var wrapper))
                    return wrapper.Contents;
                return new IndexingError(null, "Index " + idx + " out of range.");
            }
            set
            {
                var wrapper = _MemberContents[idx];
                if (wrapper.Contents.Equals(value)) return;
                wrapper.Contents = value;
                if (_Value != null)
                    _Value[idx] = value.Value;
            }
        }

        internal Vector(IEnumerable<IEvaluateable> contents) { Inputs = contents; }
        internal Vector(IList<IEvaluateable> contents) : this((IEnumerable<IEvaluateable>)contents) { }
        public Vector(params IEvaluateable[] contents) : this((IEnumerable<IEvaluateable>)contents) { }
        public Vector(params decimal[] contents)
            : this(contents.Select(m => new Number(m)).OfType<IEvaluateable>().ToArray())
        { }
        public Vector() { }

        public int Count => _MemberContents.Count;


        bool IContext.TryGetSubcontext(string path, out IContext ctxt) { ctxt = null; return false; }

        internal bool TryGetProperty(string path, out IEvaluateable source)
        {
            switch (path)
            {
                case "size":
                case "count":
                case "length": source = new Number(Value.Count); return true;
                case "min": source = Number.Zero; return true;
                case "max": source = new Number(Value.Count - 1); return true;
                default: source = null; return false;
            }
        }
        bool IContext.TryGetProperty(string path, out IEvaluateable source) => this.TryGetProperty(path, out source);

        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Vector;
        internal ISyncUpdater Parent { get; set; }
        ISyncUpdater ISyncUpdater.Parent { get => Parent; set { Parent = Value; } }
        private readonly object _UpdateLock = new object();
        internal ITrueSet<IEvaluateable> Update(ITrueSet<IEvaluateable> updatedIndices)
        {
            if (_Value == null) return updatedIndices;
            TrueSet<IEvaluateable> changed = new TrueSet<IEvaluateable>();
            foreach (Number idx in updatedIndices.OfType<Number>())
            {
                IEvaluateable newValue = _MemberContents[idx].Value;
                if (_Value[idx].Equals(newValue)) continue;
                _Value[idx] = _MemberContents[idx].Value;
                changed.Add(idx);
            }
            return changed.IsEmpty ? null : changed;
        }
        ITrueSet<IEvaluateable> ISyncUpdater.Update(Update caller,
                                                       ISyncUpdater updatedChild,
                                                       ITrueSet<IEvaluateable> updatedDomain)
        {   
            lock (_UpdateLock)
            {
                
                // The Vector's updates must be serialized because there is a race condition with 
                // the indices coming from the update below.  
                Indexed<Number> wrapper = (Indexed<Number>)updatedChild;
                Debug.Assert(updatedDomain != null
                             && updatedDomain.Contains(wrapper.Index));
                return Update(updatedDomain);
            }
            
        }

        bool ICollection<IEvaluateable>.IsReadOnly => false;

        private bool TryIndexWrapper(IEvaluateable ordinal, out Indexed<Number> wrapper)
        {
            if (ordinal is Number n && n.IsInteger)
            {
                int idx = (int)n;
                if (idx >= 0 && idx < Count)
                {
                    wrapper = _MemberContents[idx];
                    return true;
                }
            }
            wrapper = default;
            return false;
        }
        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
        {
            if (this.TryIndexWrapper(ordinal, out var wrapper))
            {
                val = wrapper.Contents;
                return true;
            }
            val = default;
            return false;
        }

        /// <summary>Returns true iff all members are alls Equals().</summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Vector other)) return false;
            if (Count != other.Count) return false;
            for (int i = 0; i < Count; i++)
                if (!Equals(_MemberContents[i].Value, other._MemberContents[i].Value))
                    return false;
            return true;
        }

        internal Vector GetValueInternal() => _Value;
        internal void ClearValueInternal() => _Value = null;

        public override int GetHashCode() => base.GetHashCode(); // The mem location determines hash.
        public override string ToString() => "{" + string.Join(",", _MemberContents.Take(3).Select(i => i.Contents.ToString())) + (_MemberContents.Count > 3 ? "..." : "") + "}";

        #region Vector IList implementations


        public int IndexOf(IEvaluateable item)
        {
            for (int i = 0; i < _MemberContents.Count; i++)
            {
                if (_MemberContents[i].Contents.Equals(item))
                    return i;
            }
            return -1;
        }

        public void Insert(int index, IEvaluateable item)
        {
            _MemberContents.Insert(index, new Indexed<Number>(this, item, index));
            for (int i = index + 1; i < _MemberContents.Count; i++)
                _MemberContents[i].Index++;
            if (_Value != null) _Value.Insert(index, item.Value);
        }

        public void RemoveAt(int index)
        {
            _MemberContents.RemoveAt(index);
            for (int i = index; i < _MemberContents.Count; i++)
            {
                var wrapper = _MemberContents[i];
                wrapper.Index--;
                wrapper.Parent = null;
            }
            if (_Value != null) _Value.RemoveAt(index);
        }

        public void Add(IEvaluateable item)
        {
            _MemberContents.Add(new Indexed<Number>(this, item, _MemberContents.Count));
            if (_Value != null)
                _Value.Add(item.Value);
        }

        public void AddRange(IEnumerable<IEvaluateable> items)
        {
            int minIdx = _MemberContents.Count;
            if (_Value != null)
            {
                _Value.Clear();
                foreach (IEvaluateable item in items)
                {
                    _MemberContents.Add(new Indexed<Number>(this, item, _MemberContents.Count));
                    _Value.Add(item.Value);
                }
            }
            else
            {
                foreach (IEvaluateable item in items)
                    _MemberContents.Add(new Indexed<Number>(this, item, _MemberContents.Count));
            }
        }

        public void Clear()
        {
            int lastIdx = _MemberContents.Count - 1;
            foreach (var wrapper in _MemberContents) wrapper.Parent = null;
            _MemberContents.Clear();
            if (_Value != null)
                _Value.Clear();
        }

        public bool Contains(IEvaluateable item) => IndexOf(item) >= 0;

        void ICollection<IEvaluateable>.CopyTo(IEvaluateable[] array, int arrayIndex)
        {
            int i = 0;
            while (arrayIndex < array.Length)
                array[arrayIndex++] = _MemberContents[i++].Contents;
        }

        public bool Remove(IEvaluateable item)
        {
            int idx = IndexOf(item);
            if (idx < 0) return false;
            _MemberContents.RemoveAt(idx);
            if (_Value != null)
                _Value.RemoveAt(idx);
            return true;
        }

        IEnumerator<IEvaluateable> IEnumerable<IEvaluateable>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private IEnumerator<IEvaluateable> GetEnumerator() => _MemberContents.GetEnumerator();



        #endregion

        void IIndexable.IndexedContentsChanged(IEvaluateable index, IEvaluateable value)
        {
            if (_Value != null) _Value[(Number)index] = value;
        }

    }
}
