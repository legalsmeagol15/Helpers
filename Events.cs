using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    /// <summary>To be invoked whenever a value changes.</summary>
    public delegate void ValueChangedHandler<T>(object sender, ValueChangedArgs<T> e);

    /// <summary>A simple, generic class representing a change from one value to another.</summary>
    public class ValueChangedArgs<T> : EventArgs
    {
        /// <summary>The value before the change.</summary>
        public readonly T Before;
        /// <summary>The value after the change.</summary>
        public readonly T After;
        /// <summary>Creates a new <see cref="ValueChangedArgs{T}"/>.</summary>
        public ValueChangedArgs(T before, T after) { this.Before = before; this.After = after; }
    }

    public delegate void PreviewHandler(object sender, PreviewEventArgs e);

    public class PreviewEventArgs : EventArgs
    {
        public bool Cancel;
        public readonly object Message;
        public PreviewEventArgs(object message, bool cancel = false) { this.Message = message; this.Cancel = cancel; }
    }

    /// <summary>
    /// Helper methods related to events.
    /// </summary>
    public static class Events
    {
        /// <param name="sender">The sending object for the event to fire from.</param>
        /// <param name="e">The <seealso cref="EventArgs"/> to send to the invocation target.</param>
        /// <param name="event">The event or delegate whose listeners will be invoked.</param>
        /// <param name="exceptionMessage">The exception message to display, if the subscriber borked up.</param>
        public static void InvokeAsynchronous(object sender, Delegate @event, EventArgs e, string exceptionMessage)
        {
            // Separately fire each invocation to assure that an exception in one invocation will not 
            // preclude the firing of the others.
            var invocations = @event.GetInvocationList();
            foreach (EventHandler eh in invocations)
            {
                eh.BeginInvoke(sender, e, _NoThrow, eh);
            }

            void _NoThrow(IAsyncResult iar)
            {
                // This will run on a separate thread from the original caller, and a separate 
                // from the subscribing method.
                EventHandler eh = (EventHandler)iar.AsyncState;
                try
                {
                    eh.EndInvoke(iar);
                }
                catch
                {
                    // What to do with exception but output?  There's not a call stack to chew up 
                    // with a new exception, and raising an exception here will cause a thread 
                    // leak because the thread running _NoThrow will never be reaped by the thread 
                    // pool.
                    Console.WriteLine(exceptionMessage);
                }
            }
        }
        /// <summary>
        /// Separately invokes each listener in the given delegate or event.  If an exception occurs in any invocation, 
        /// the exception is caught and re-thrown in the calling thread.
        /// </summary>
        /// <param name="sender">The sending object for the event to fire from.</param>
        /// <param name="e">The <seealso cref="EventArgs"/> to send to the invocation target.</param>
        /// <param name="event">The event or delegate whose listeners will be invoked.</param>
        /// <param name="onEnd">Optional. The callback that will be called after each invocation.  If omitted, all 
        /// exceptions will be ignored but exceptions will not prevent the calling of later invocations from the 
        /// event's invocation list.</param>
        public static void InvokeAsynchronous(object sender, Delegate @event, EventArgs e, AsyncCallback onEnd = null)
        {
            // Separately fire each invocation to assure that an exception in one invocation will not 
            // preclude the firing of the others.
            var invocations = @event.GetInvocationList();
            foreach (EventHandler eh in invocations)
            {
                eh.BeginInvoke(sender, e, onEnd ?? _NoThrow, eh);
            }
            
            void _NoThrow(IAsyncResult iar)
            {
                // This will run on a separate thread from the original caller, and a separate 
                // from the subscribing method.
                EventHandler eh = (EventHandler)iar.AsyncState;
                try
                {
                    eh.EndInvoke(iar);
                }
                catch (Exception ex)
                {
                    // What to do with exception but output?  There's not a call stack to chew up 
                    // with a new exception, and raising an exception here will cause a thread 
                    // leak because the thread running _NoThrow will never be reaped by the thread 
                    // pool.
                    Console.WriteLine("Method " + @event.Method.Name + " threw an exception \"" + ex.Message + "\" during delegate invocation.");
                }
            }
        }
    }
}
