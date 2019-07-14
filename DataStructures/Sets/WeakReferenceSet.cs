using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.DataStructures.Sets
{
    /// <summary>
    /// Uses hashing jiggery pokery to answer a question:  does my set already have a weak reference to that object?
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class WeakReferenceSet<T> : ISet<T> where T : class
    {

        private AbstractNode _Root = new ListNode();


        /// <summary>Iterates through and counts the items in this set.  This is always an O(n) operation.</summary>
        public int Count => this.Count();

        bool ICollection<T>.IsReadOnly => throw new NotImplementedException();

        public bool Add(T item)
        {
            _Root = _Root.Add(item, item.GetHashCode(), out bool changed);
            return changed;
        }
        

        void ICollection<T>.Clear()
        {
            throw new NotImplementedException();
        }

        bool ICollection<T>.Contains(T item)
        {
            throw new NotImplementedException();
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        void ISet<T>.ExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        void ISet<T>.IntersectWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.IsSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.IsSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.Overlaps(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.SetEquals(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        void ISet<T>.UnionWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }



        #region WeakReferenceSet node classes

        private abstract class AbstractNode : IEnumerable<T>
        {
            public const int MAX_BITS = 8;
            public const int MAX_LIST_SIZE = 8;

            public abstract AbstractNode Add(T item, int hashCode, out bool changed);
            public abstract AbstractNode Remove(T item, int hashCode, out bool changed);
            public abstract bool Contains(T item, int hashCode);

            public abstract AbstractNode Compact(out int activeItems);


            protected internal abstract IEnumerator<T> GetEnumerator();

            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }


        private sealed class SingletonNode : AbstractNode
        {
            public readonly WeakReference<T> Singleton;
            public SingletonNode(T item) { Singleton = new WeakReference<T>(item); }

            public override AbstractNode Add(T item, int hashCode, out bool changed)
            {
                if (!Singleton.TryGetTarget(out T existing)) { changed = true; Singleton.SetTarget(item); return this; }
                else if (existing.Equals(item)) { changed = false; return this; }
                else { changed = true; return new ListNode(existing, item); }
            }

            public override AbstractNode Remove(T item, int hashCode, out bool changed)
            {
                if (!Singleton.TryGetTarget(out T existing) || !existing.Equals(item)) { changed = false; return this; }
                else { changed = true; return null; }
            }

            public override bool Contains(T item, int hashCode) => Singleton.TryGetTarget(out T existing) && existing.Equals(item);

            protected internal override IEnumerator<T> GetEnumerator()
            {
                if (Singleton.TryGetTarget(out T existing)) yield return existing;
                yield break;
            }
            public override AbstractNode Compact(out int activeItems)
            {
                if (Singleton.TryGetTarget(out T existing)) { activeItems = 1; return this; }
                else { activeItems = 0; return null; }
            }
        }


        private sealed class ListNode : AbstractNode
        {
            public readonly List<WeakReference<T>> List;
            public ListNode(params T[] items) { }

            public override AbstractNode Add(T item, int hashCode, out bool changed)
            {
                if (!Contains(item, hashCode))
                {
                    changed = false;
                    return this;
                }
                else if (List.Count < MAX_LIST_SIZE)
                {
                    List.Add(new WeakReference<T>(item));
                    changed = true;
                    return this;
                }
                else
                {
                    List.Add(new WeakReference<T>(item));
                    AbstractNode result = new Node(List.Select(wr => wr.TryGetTarget(out T existing) ? existing : null)
                                                        .Where(existing => existing != null));
                    changed = true;
                    return result;
                }
            }

            public override AbstractNode Compact(out int activeItems)
            {
                int active = 0;
                List.RemoveAll(wr => { if (!wr.TryGetTarget(out T existing)) return true; else active++; return false; });
                return active;
            }

            public override bool Contains(T item, int hashCode)
            {
                bool matched = false;
                List.RemoveAll(wr =>
                {
                    if (!wr.TryGetTarget(out T existing)) return true;                  // No match, but prune it.
                    else if (existing.Equals(item)) { matched = true; return false; }   // Matched, so don't prune.
                    return false;                                                       // No match, and don't prune.
                });
                return matched;
            }

            public override AbstractNode Remove(T item, int hashCode, out bool changed)
            {
                bool removed = false;
                List.RemoveAll(wr =>
                {
                    if (!wr.TryGetTarget(out T existing)) return true;
                    else if (existing.Equals(item)) { removed = true; return true; }
                    else return false;
                });
                changed = removed;

                // The removal plus pruning could remove everything.
                if (List.Count == 0)
                    return null;

                // If all that's left is a single item, convert to a new singleton.
                else if (List.Count == 1)
                {
                    if (List[0].TryGetTarget(out T existing)) return new SingletonNode(existing);
                    return null;
                }
                else return this;
            }

            protected internal override IEnumerator<T> GetEnumerator()
            {
                List<T> valid = new List<T>();
                List.RemoveAll(wr =>
                {
                    if (!wr.TryGetTarget(out T existing)) return true;
                    valid.Add(existing);
                    return false;
                });
                return valid.GetEnumerator();
            }
        }


        private class Node : AbstractNode
        {

            public int ModMask { get; private set; }
            private int _Bits = 1;
            public int Bits { get => _Bits; set { _Bits = value; ModMask = GetBitMask_Little(value); } }
            private AbstractNode[] _Children;
            private readonly HashSet<int> _Occupied = new HashSet<int>();

            public Node(IEnumerable<T> items)
            {
                foreach (var item in items) Add(item, item.GetHashCode(), out _);
            }

            private static int GetBitMask_Little(int bits)
            {
                int result = 0;
                for (int i = 0; i < bits; i++) result |= (1 << i);
                return result;
            }


            public override AbstractNode Add(T item, int hashCode, out bool changed)
            {
                int index = hashCode & ModMask;
                if (_Children[index] == null)
                {
                    _Children[index] = new SingletonNode(item);
                    changed = true;
                    _Occupied.Add(index);
                    return this;
                }
                else
                {
                    // A Node never converts itself.
                    _Children[index] = _Children[index].Add(item, hashCode >> Bits, out changed);
                    return this;
                }
            }

            public override AbstractNode Remove(T item, int hashCode, out bool changed)
            {
                int index = hashCode & ModMask;
                if (_Children[index] == null)
                {
                    changed = false;
                    return this;
                }
                else
                {
                    // A Node never converts itself.
                    _Children[index] = _Children[index].Remove(item, hashCode >> Bits, out changed);
                    if (_Children[index] == null) _Occupied.Remove(index);
                    if (_Occupied.Count == 0) return null;
                    if (_Occupied.Count == 1) return _Children[_Occupied.First()];
                    return this;
                }
            }

            public override AbstractNode Compact(out int activeItems)
            {
                int active = 0;
                List<int> unoccupied = new List<int>();
                foreach (int index in _Occupied)
                {
                    AbstractNode child = _Children[index];                    
                    if (child == null) unoccupied.Add(index);
                    else
                    {
                        _Children[index] = child.Compact(out int childActive);
                        active += childActive;
                    }
                    
                }
                activeItems = active;
                foreach (int index in unoccupied)
                {
                    _Occupied.Remove(index);
                }

                if (_Occupied.Count == 0) return null;
                else if (_Occupied.Count == 1) return _Children[_Occupied.First()];
                return this;
            }


            public override bool Contains(T item, int hashCode)
            {
                int index = hashCode & ModMask;
                if (_Children[index] == null) return false;
                return _Children[index].Contains(item, hashCode >> Bits);
            }

            protected internal override IEnumerator<T> GetEnumerator()
            {
                List<int> unoccupied = new List<int>();
                foreach (int index in _Occupied)
                {
                    IEnumerable<T> child = _Children[index];
                    if (child != null) foreach (T item in child) yield return item;
                    else unoccupied.Add(index);
                }
                foreach (int index in unoccupied) _Occupied.Remove(index);
            }

        }

        #endregion

    }
}
