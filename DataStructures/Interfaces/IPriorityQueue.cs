using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public interface IPriorityQueue<T>
    {
        /// <summary>
        /// Adds the given item to the priority queue.  The item will be moved to its correct priority position.
        /// </summary>        
        void Enqueue(T item);

        /// <summary>
        /// Removes and returns the item at the head of the priority queue.
        /// </summary>        
        T Dequeue();

        /// <summary>
        /// Returns the item at the head of the priority queue without making any changes.
        /// </summary>
        T Peek();

        /// <summary>
        /// Returns the number of items in the priority queue.
        /// </summary>
        int Count { get; }
    }
}
