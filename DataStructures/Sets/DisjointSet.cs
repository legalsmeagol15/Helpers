using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{

    /// <summary>
    /// A disjoint set maintains a collection of items in a state such that they are within distinct sets from each other.
    /// TODO:  validate DisjointSet
    /// </summary>
    public sealed class DisjointSet<T> : IEnumerable<T>
    {
        private class TIndexPair
        {
            public readonly T Item;
            public  int Index { get; set; }
            public override bool Equals(object obj) { return Item.Equals(((TIndexPair)obj).Item); }
            public override int GetHashCode() { return Item.GetHashCode(); }
            public TIndexPair(T item, int index) { Item = item;  Index = index; }
        }

        private Dictionary<T, int> _Indices = new Dictionary<T, int>(); 
        private List<TIndexPair> _Refs = new List<TIndexPair>();

        public bool Add(T item)
        {
            if (_Indices.ContainsKey(item)) return false;
            _Indices[item] = _Refs.Count;
            _Refs.Add(new TIndexPair(item, _Refs.Count));
            return true;
        }
        public void Clear() { _Indices.Clear(); _Refs.Clear(); }
        public void Join(T a, T b)
        {
            int idxA = _Indices[a], idxB = _Indices[b];            
            _Refs[idxB].Index = (_Refs[idxA].Index = _Refs[_Refs[idxA].Index].Index);            
        }
        public bool Remove(T item) { throw new NotImplementedException(); }

        /// <summary>Returns whether the two items share a group within this disjoint set.</summary>
        public bool ShareGroup(T a, T b)
        {            
            int idxA = _Indices[a], idxB = _Indices[b];
            while (_Refs[idxA].Index != _Refs[_Refs[idxA].Index].Index)
                _Refs[idxA].Index = _Refs[_Refs[idxA].Index].Index;
            while (_Refs[idxB].Index != _Refs[_Refs[idxB].Index].Index)
                _Refs[idxB].Index = _Refs[_Refs[idxB].Index].Index;
            return _Refs[idxA].Index == _Refs[idxB].Index;
        }


        IEnumerator IEnumerable.GetEnumerator() { return _Indices.Keys.GetEnumerator(); }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return _Indices.Keys.GetEnumerator(); }
    }


}
