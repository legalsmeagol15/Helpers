using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{

    /// <summary>
    /// Maintains a dependency structure among the contained objects.  If a circular dependency is added, throws an exception.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DependencyGraph<T> : IEnumerable<T>
    {
        private readonly Dictionary<T, HashSet<T>> _Listeners = new Dictionary<T, HashSet<T>>();
        private readonly Dictionary<T, HashSet<T>> _Sources = new Dictionary<T, HashSet<T>>();
        private readonly HashSet<T> _Heads = new HashSet<T>();



        #region DependencyGraph contents manipulation

        public bool AddSolo(T soloItem)
        {
            if (_Listeners.ContainsKey(soloItem)) return false;
            _Listeners[soloItem] = new HashSet<T>();
            _Sources[soloItem] = new HashSet<T>();
            _Heads.Add(soloItem);
            return true;
        }
        /// <summary>
        /// Adds a dependence relationship between the two items.  If the items do not exist on this dependency graph, adds them.
        /// </summary>
        /// <returns>Returns true if the graph is changed, either by adding the items or simply by adding the dependence relationship.  
        /// Otherwise, returns false.</returns>
        public bool AddDependence(T source, T listener)
        {
            if (ContainsDependency(listener, source) >= 0)
                throw new CircularDependencyException("Circular dependency.");

            HashSet<T> listenersOfSource, sourcesOfListener;
            if (!_Listeners.TryGetValue(source, out listenersOfSource))
            {
                listenersOfSource = new HashSet<T>();
                _Listeners[source] = listenersOfSource;
            }
            if (!_Sources.TryGetValue(listener, out sourcesOfListener))
            {
                sourcesOfListener = new HashSet<T>();
                _Sources[source] = sourcesOfListener;
            }

            if (_Sources[source].Count == 0) _Heads.Add(source);

            return sourcesOfListener.Add(source) && listenersOfSource.Add(listener);
        }


        /// <summary>Clears all items from this graph.</summary>
        public void Clear()
        {
            _Listeners.Clear();
            _Sources.Clear();
            _Heads.Clear();
        }


        /// <summary>Removes the given item entirely from this graph, severing it from all dependence relationships.</summary>
        /// <returns>Returns true if the item existed on the graph and was removed; if the item did not exist on the graph to begin with, 
        /// returns false.</returns>
        public bool Remove(T item)
        {
            HashSet<T> listeners, sources;
            if (!_Listeners.TryGetValue(item, out listeners)) return false;
            sources = _Sources[item];

            foreach (T remainingSource in sources)
            {
                HashSet<T> listenersToRemainingSource = _Listeners[remainingSource];
                listenersToRemainingSource.Remove(item);
            }
            foreach (T remainingListener in listeners)
            {
                HashSet<T> sourcesOfRemainingListener = _Sources[remainingListener];
                sourcesOfRemainingListener.Remove(item);
            }

            _Listeners.Remove(item);
            _Sources.Remove(item);
            _Heads.Remove(item);

            return true;
        }
        /// <summary>Removes the dependence relationship between the two items, but does NOT remove them from the graph.</summary>
        /// <returns>Returns true if the two items existing on this graph AND a dependence relationship existed between them to be 
        /// removed; otherwise, returns false.</returns>
        public bool RemoveDependence(T source, T listener)
        {
            HashSet<T> listenersOfSource, sourcesOfListener;
            if (!_Listeners.TryGetValue(source, out listenersOfSource)
                || !_Sources.TryGetValue(listener, out sourcesOfListener)) return false;

            bool removed = sourcesOfListener.Remove(source) && listenersOfSource.Remove(listener);
            if (sourcesOfListener.Count == 0) _Heads.Add(listener);
            return removed;
        }
        public int RemoveSources(T listener)
        {
            if (!_Sources.TryGetValue(listener, out HashSet<T> sources)) return 0;
            int count = 0;
            foreach (T src in sources)
                if (_Listeners.TryGetValue(src, out HashSet<T> listenersOfSource))
                {
                    listenersOfSource.Remove(listener);
                    count++;
                }
            sources.Clear();
            return count;
        }
        public int RemoveListeners(T source)
        {
            if (!_Listeners.TryGetValue(source, out HashSet<T> listeners)) return 0;
            int count = 0;
            foreach (T l in listeners)
                if (_Sources.TryGetValue(l, out HashSet<T> sourcesOfListener))
                {
                    sourcesOfListener.Remove(source);
                    count++;
                }
            listeners.Clear();
            return count;
        }

        #endregion



        #region DependencyGraph contents queries

        /// <summary>Returns whether the given item is contained in this graph.</summary>
        public bool Contains(T item) { return _Listeners.ContainsKey(item); }


        /// <summary>
        /// Determines whether the given source contains the given listener somewhere lower in the dependency graph.  The value returned 
        /// is the depth at which the dependency exists.  If there is no dependency, returns -1.
        /// </summary>
        public int ContainsDependency(T source, T listener)
        {
            if (source.Equals(listener)) return 0;
            if (_Listeners.ContainsKey(source))
            {
                foreach (T l in _Listeners[source])
                {
                    int recursive = ContainsDependency(l, listener);
                    if (recursive >= 0) return 1 + recursive;
                }
            }            
            return -1;
        }


        /// <summary>Returns the count of items in this graph.</summary>
        public int Count { get { return _Listeners.Count; } }


        /// <summary>Returns an enumerator specifying the contents of this graph in breadth-first order.</summary>
        public IEnumerator<T> BreadthFirst()
        {
            Queue<T> queue = new Queue<T>(_Heads);
            while (queue.Count > 0)
            {
                T focus = queue.Dequeue();
                yield return focus;
                foreach (T child in _Listeners[focus]) queue.Enqueue(child);
            }
        }
        /// <summary>Returns an enumerator specifying the contents of this graph in depth-first order.</summary>
        public IEnumerator<T> DepthFirst()
        {
            Stack<T> stack = new Stack<T>(_Heads);
            while (stack.Count > 0)
            {
                T focus = stack.Pop();
                yield return focus;
                foreach (T child in _Listeners[focus]) stack.Push(child);
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return DepthFirst(); }

        IEnumerator IEnumerable.GetEnumerator() { return DepthFirst(); }

        /// <summary>Returns an enumerator for the sources of the given item.</summary>
        public IEnumerator<T> Sources(T item) { return _Sources[item].GetEnumerator(); }
        /// <summary>Returns an enumerator for the listeners of the given item.</summary>
        public IEnumerator<T> Listeners(T item) { return _Listeners[item].GetEnumerator(); }
        /// <summary>Returns an enumerable of the sources of the given item.  Returns null if there are no sources for the item.</summary>
        public IEnumerable<T> GetSources(T item)
        {
            return _Sources.TryGetValue(item, out HashSet<T> sources) ? sources : null;
        }
        /// <summary>Returns an enumerable of the listeners of the given item.  Returns null if there are no listeners for the item.</summary>
        public IEnumerable<T> GetListeners(T item)
        {
            return (_Listeners.TryGetValue(item, out HashSet<T> listeners)) ? listeners : null;
        }
        /// <summary>Returns the set of heads in this dependency graph.</summary>
        public IEnumerable<T> GetHeads() { return _Heads; }

        public bool TryGetSources(T item, out HashSet<T> sources) { return _Sources.TryGetValue(item, out sources); }
        public bool TryGetListeners(T item, out HashSet<T> sources) { return _Listeners.TryGetValue(item, out sources); }

        #endregion



        
    }


    /// <summary>An exception thrown when an invalid circular dependency is added to a DependencyGraph.</summary>
    public class CircularDependencyException : InvalidOperationException
    {
        /// <summary>Creates a new CircularDependencyException.</summary>
        public CircularDependencyException(string message) : base(message) { }
    }
}
