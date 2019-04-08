using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace DataStructures.Threading
{
    /// <summary>
    /// The RedoWorker is a BackgroundWorker with special added functionality for managing multiple requests for work 
    /// that come while the worker is busy.  The worker is generic to support strong type safety.  Once a subsequent work request 
    /// is received, the worker attempts to cancel any further work on the current work item and re-start work on the new request.  
    /// If the worker should not cancel work in progress, consider a PipelineWorker with a short pipe as an alternative.
    /// </summary>
    /// <author>Wesley Oates</author>
    /// <date>March 16, 2016.</date>
    [DesignerCategory("Code")]
    public class RedoWorker<T> : BackgroundWorker
    {

        /// <summary>
        /// Creates a new RedoWorker, with an optional argument for whether the worker should report progress.
        /// </summary>
        /// <param name="reportsProgress">Whether or not this worker should raise progress reporting events.  The 
        /// default is false.</param>      
        public RedoWorker(bool reportsProgress = false)
        {
            this.WorkerReportsProgress = reportsProgress;
            this.WorkerSupportsCancellation = true;                        
        }


        /// <summary>
        /// The item on which the worker is currently working.
        /// </summary>
        public T Current { get; private set; } = default(T);

        /// <summary>
        /// The item on which the worker should begin work when the current work item is cancelled.
        /// </summary>
        public T Redo { get; private set; } = default(T);

        /// <summary>
        /// Returns whether the worker will begin work on the 'Redo' item once the current work item is cancelled.
        /// </summary>
        public bool IsRedoPending { get; private set; } = false;


        /// <summary>
        /// Begins work on the given item, or if work is currently underway, lodges the new work item as the 'Redo' item to begin 
        /// once the prior work is cancelled.
        /// </summary>
        public void Work(T workItem = default(T))
        {
            if (!IsRedoPending && !IsBusy)
            {
                //If the worker is idle, just get started with the givne item as the current item.
                Current = workItem;
                RunWorkerAsync(Current);
            }
            else
            {                
                IsRedoPending = true;
                Redo = workItem;
                CancelAsync();
                //Work on the redo will be started at the work-complete method.
            }

        }



        /// <summary>
        /// When a work item is completed, raises the completion event.  Then, if there is a Redo item pending, the worker 
        /// asynchronously begins work on the next item.
        /// </summary>
        /// <param name="e">The event argument for the recently-completed work item.</param>
        /// <remarks>This method always executes on the main thread.</remarks>
        protected override void OnRunWorkerCompleted(RunWorkerCompletedEventArgs e)
        {
            //If there is work to redo, get the worker started on the next one.
            if (IsRedoPending)
            {
                Current = Redo;
                Redo = default(T);
                IsRedoPending = false;
                RunWorkerAsync(Current);
            }
            else
            {
                //Raise the completion event for the just-done work item.            
                Current = default(T);
                base.OnRunWorkerCompleted(e);
            }
        }

    }
}
