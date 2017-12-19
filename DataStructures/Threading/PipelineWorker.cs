using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace DataStructures.Threading
{


    /// <summary>
    /// The PipelineWorker is a BackgroundWorker with special added functionality for managing multiple requests for work 
    /// that come to the worker at the same time.  This class avoids the problem of what to do if a BackgroundWorker is 
    /// busy, and simply enqueues the work given to it.   The PipelineWorker is generic for strong type safety of work 
    /// items.  The PipelineWorker can set a limit to its pipe size, and the strategy for handling pipeline overflow may be 
    /// set as well.
    /// </summary>
    /// <author>Wesley Oates</author>
    /// <date>Mar 1, 2013, amended Oct 31, 2015, amended March 16, 2016.</date>
    //TODO:  someday, a heap-based pipeline worker that defaults to a time-added comparator may be preferable to a LinkedList.    
    [DesignerCategory("Code")]
    public class PipelineWorker<T> : BackgroundWorker
    {

        /// <summary>
        /// Creates a new PipelineWorker, with optional arguments for whether the worker should report progress, be 
        /// cancellable, and the maximum allowed size of the pipe.
        /// </summary>
        /// <param name="reportsProgress">Whether or not this worker should raise progress reporting events.  The 
        /// default is false.</param>
        /// <param name="cancellable">Whether or not this worker should allow users to cancel work through the 
        /// pipeline.  Note that cancel will flush the pipeline.  The default value is false.</param>
        /// <param name="pipeSize">The maximum pipeline size.  If the queue is at maximum size and another work item is 
        /// added, an exception will be thrown.  The default value is int.MaxValue.</param>  
        /// <param name="strategy">The strategy when the pipeline worker is overflowed</param>
        public PipelineWorker(bool reportsProgress = false, bool cancellable = false, int pipeSize = int.MaxValue, 
            OverflowStrategies strategy = OverflowStrategies.ThrowException)
        {
            this.WorkerReportsProgress = reportsProgress;
            this.WorkerSupportsCancellation = cancellable;        
            this.PipeSize = pipeSize;
        }


        #region PipelineWorker properties

        /// <summary>
        /// The maximum size of the pipeline before an exception is thrown.
        /// </summary>
        public int PipeSize { get; private set; }

       

        /// <summary>
        /// The strategy this worker uses when duplicate work items are added.
        /// </summary>
        public OverflowStrategies OverflowStrategy { get; set; }

        
        private LinkedList<T> _Queue = new LinkedList<T>();

        /// <summary>
        /// A set of strategies that determine how a pipeline worker should handle the case of an item being added to the pipe 
        /// when it is already full.
        /// </summary>
        public enum OverflowStrategies
        {
            /// <summary>
            /// Throws an exception when the pipe is overflowed.
            /// </summary>
            ThrowException,

            /// <summary>
            /// Dequeues an item at the head of the pipe to make room for an overflow item.
            /// </summary>
            DrainPipe,

            /// <summary>
            /// Ignores and refuses to enqueue an item that would cause a pipe overflow.
            /// </summary>
            IgnoreOverflow
        }

        
        #endregion



        #region PipelineWorker queueing functions

        /// <summary>
        /// The item on which the worker is currently working.
        /// </summary>
        public T Current { get; private set; } = default(T);


        /// <summary>
        /// Returns the last pipeline work item which will be worked on, or in other words, the most recently enqueued 
        /// item.
        /// </summary>
        public T Last { get { return _Queue.Last(); } }


        /// <summary>
        /// Returns the next pipeline work item which will be worked on.
        /// </summary>        
        public T Next { get { return _Queue.First(); } }


        /// <summary>
        /// Returns the position of the given work item in the pipeline.  For comparison, this method uses the native or 
        /// overridden object.Equals() method.  <para/>This method is an O(N) operation with respect to the items currently in the 
        /// pipe.  The pipeline search begins at the head (i.e., the next item that will be processed).
        /// </summary>
        /// <param name="item">The item to search the pipeline for.</param>
        /// <returns>Returns the current position in the pipeline where the work item appears.  If the given item 
        /// does not appear in the pipeline, returns -1.</returns>        
        public int Position(T item)
        {
            int i = 0;
            foreach (T compare in _Queue)
            {
                if (compare.Equals(item)) return i;
                i++;
            }
            return -1;
        }

        /// <summary>
        /// Returns whether this pipeline worker contains the given work item.  The result will exclude the currently-worked item.
        /// <para/>This method is an O(N) operation in the worst case.
        /// </summary>
        public bool IsQueued(T item)
        {
            return Position(item) > -1;
        }


        /// <summary>
        /// Adds the given work item to the pipeline, and begins asynchronous work if it has not been done already.
        /// </summary>
        /// <param name="workItem">The item to be worked on.</param>
        /// <remarks>This method will always execute on the main thread.\n\nIf the duplicate-handling strategy is 
        /// anything but the standard (allowing all duplicates), this method will be an O(N) operation, where N is the 
        /// size of the queue presently existing.  If the strategy allows all duplicates, then the method will be an 
        /// O(1) operation.</remarks>        
        public void Enqueue(T workItem)
        {
            //Step #1 - Check for pipeline overflow.
            if (_Queue.Count >= PipeSize)
            {
                switch (OverflowStrategy)
                {
                    case OverflowStrategies.DrainPipe:
                        while (_Queue.Count > PipeSize - 1) _Queue.RemoveFirst();
                        _Queue.AddLast(workItem);
                        break;
                    case OverflowStrategies.IgnoreOverflow:
                        //Do nothing.
                        break;
                    case OverflowStrategies.ThrowException:
                        throw new PipelineOverflowException("Too many items added to " + this.GetType().Name
                                                       + " with pipeline of size " + PipeSize + ".");                
                    default:
                        throw new NotImplementedException("Have not implemented overflow strategy " + OverflowStrategy.ToString() + ".");

                }                
            }

            //Step #2 - enqueue the item.
            _Queue.AddLast(workItem);


            //Step #3 - start the async thread, but only if the queue's count is 1 and the worker is not busy.  No need 
            //to handle other situations.  (If the queue's count is >1, the worker will be re-started on completion in 
            //another method.  If the worker is busy, then then this item will be started when the currently-worked item 
            //is completed.)
            if (_Queue.Count == 1 && !IsBusy)
            {
                Current = _Queue.First();
                _Queue.RemoveFirst();
                RunWorkerAsync(Current);
            }


        }


        #endregion


        #region PipelineWorker threading members

        /// <summary>
        /// Clears all work items out of the pipe.
        /// </summary>
        /// <param name="requestCancel">Specifies whether to attempt to cancel work on the current item.</param>
        public void Flush(bool requestCancel = true)
        {
            _Queue.Clear();
            if (requestCancel)
                CancelAsync();
            //Setting the Current to null will happen in the override of OnRunWorkerCompleted, which is called even if 
            //a cancel was requested and occured.            
        }


        /// <summary>
        /// When a work item is completed, raises the completion event, and dequeues the worked item.  Then, if there is 
        /// more in the queue, the worker asynchronously begins work on the next item.
        /// </summary>
        /// <param name="e">The event argument for the recently-completed work item.</param>
        /// <remarks>This method always executes on the main thread.  Changes that must be synchronized with the internal 
        /// queue happen here because this method executes on the main thread.</remarks>
        protected override void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
        {
            //Raise the completion event for the just-done work item.
            base.OnRunWorkerCompleted(e);
            Current = default(T);

            //If there are more on the queue, get the worker started on the next one.
            if (_Queue.Count > 0 && !IsBusy)
            {
                Current = _Queue.First();
                _Queue.RemoveFirst();
                RunWorkerAsync(Current);
            }
        }

        #endregion


        #region PipelineWorker exceptions

        /// <summary>
        /// The exception that is thrown when attempting to enqueue too many items.
        /// </summary>
        public class PipelineOverflowException : Exception
        {
            /// <summary>
            /// Creates a new PipelineOverflowException with the given message.
            /// </summary>
            /// <param name="message"></param>
            public PipelineOverflowException(string message) : base(message)
            {
            }
        }
        #endregion

    }
}
