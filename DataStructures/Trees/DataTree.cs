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

        /// <summary>Retrieves the data at the indicated delimited path.</summary>
        /// <exception cref="PathException">Thrown when the indicated path does not exist.</exception>"
        public T Get(string path)
        {
            DataTree<T> focus = this;
            foreach (string pathSegment in path.Split(new string[] { Delimiter }, StringSplitOptions.None))
                focus = focus[pathSegment];
            return focus.Data;
        }

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

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>Thrown when a path does not exist on a <seealso cref="DataTree{T}"/>.</summary>
    public sealed class PathException : Exception
    {
        private string _Delimiter;
        internal readonly Deque<string> paths = new Deque<string>();
        internal PathException(string delimiter) { this._Delimiter = delimiter; }
        public override string Message => "Path " +  string.Join(_Delimiter, paths) + " is invalid.";
    }
}
