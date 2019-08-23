using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>
    /// A simple data tree structure.  Storage and retrieval happen in O(d) time, where 'd' is the depth of the data 
    /// item to be retrieved.
    /// </summary>
    public sealed class DataTree<T> : IEnumerable<T>
    {
        private readonly Dictionary<string, DataTree<T>> _Children = new Dictionary<string, DataTree<T>>();

        /// <summary>The count of children at this node.</summary>
        public int Count { get; private set; } = 0;

        /// <summary>Data stored at this node of the <see cref="DataTree{T}"/>.</summary>
        public  T Data { get; private set; }

        /// <summary>
        /// The characters that represent a delimiter between path segments for this <see cref="DataTree{T}"/>.
        /// </summary>
        public readonly string Delimiter;



        public DataTree(string delimiter = ".") { this.Delimiter = delimiter; }

        /// <summary>Retrieves the child <see cref="DataTree{T}"/> node of this node.</summary>
        /// <exception cref="PathException">Thrown when the indicated path does not exist.</exception>"
        public DataTree<T> this[string path]
        {
            get
            {
                try
                {
                    if (!_Children.TryGetValue(path, out DataTree<T> child))
                        throw new PathException(Delimiter);
                    return child;
                } catch (PathException pEx)
                {
                    pEx.paths.AddFirst(path);
                    throw;
                }
            }
        }

        public IEnumerable<DataTree<T>> GetChildren() => _Children.Values;

        /// <summary>Sets the value at the indicated deliminated path.</summary>
        /// <param name="path">The path to follow to the appropriate node to hold the new data.</param>
        /// <param name="data">The data to be stored.</param>
        /// <param name="createNodes">If the path does not exist, setting this to true will create the path as it 
        /// goes; otherwise, an exception will be thrown.</param>
        /// <returns>Returns the <see cref="DataTree{T}"/> node where the data now resides.</returns>
        /// <exception cref="PathException">Thrown when <paramref name="createNodes"/> is set to false and an 
        /// indicated path does not exist.</exception>
        public DataTree<T> Set(string path, T data, bool createNodes = true)
        {
            DataTree<T> focus = this;
            string[] split = path.Split(new string[] { Delimiter }, StringSplitOptions.None);
            for (int i = 0; i <split.Length; i++)
            {
                string pathSegment = split[i];
                if (!focus._Children.TryGetValue(pathSegment, out DataTree<T> child))
                {
                    if (!createNodes)
                    {
                        PathException ex = new PathException(Delimiter);
                        for (int j = 0; j <= i; j++) ex.paths.AddLast(split[j]);
                        throw ex;
                    }
                    child = new DataTree<T>(Delimiter);
                    focus._Children[pathSegment] = child;
                    focus.Count++;
                }
                focus = child;
            }
            focus.Data = data;
            return focus;
        }
        public DataTree<T> Get(string path)
        {
            DataTree<T> focus = this;
            foreach (string pathSegment in path.Split(new string[] { Delimiter }, StringSplitOptions.None))
                focus = focus[pathSegment];
            return focus;
        }

        public bool TryGet(string path, out DataTree<T> node)
        {
            node = this;
            string[] split = path.Split(new string[] { Delimiter }, StringSplitOptions.None);
            for (int i = 0; i < split.Length; i++)
            {
                string pathSegment = split[i];
                if (!node._Children.TryGetValue(pathSegment, out DataTree<T> child)) return false;
                node = child;
            }
            return true;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Traverse();

        IEnumerator IEnumerable.GetEnumerator() => Traverse();

        private IEnumerator<T> Traverse()
        {
            Stack<DataTree<T>> stack = new Stack<DataTree<T>>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                var focus = stack.Pop();
                yield return focus.Data;
                foreach (var child in focus._Children.Values)
                    stack.Push(child);
            }
        }
    }

    /// <summary>Thrown when a path does not exist on a <seealso cref="DataTree{T}"/>.</summary>
    public sealed class PathException : Exception
    {
        private string _Delimiter;
        internal readonly Deque<string> paths;
        internal PathException(string delimiter, IEnumerable<string> segments = null)
        {
            this._Delimiter = delimiter;
            this.paths = (segments == null) ? new Deque<string>() : new Deque<string>(segments);
        }
        public override string Message => "Path " + string.Join(_Delimiter, paths) + " is invalid.";
    }
}
