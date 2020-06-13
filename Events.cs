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
        /// <summary>
        /// Separately invokes each listener in the given delegate or event.  If an exception occurs in any invocation, 
        /// the exception is caught and re-thrown in the calling thread.
        /// </summary>
        /// <param name="sender">The sending object for the event to fire from.</param>
        /// <param name="e">The <seealso cref="EventArgs"/> to send to the invocation target.</param>
        /// <param name="evnt">The event or delegate whose listeners will be invoked.</param>
        /// <param name="onEnd">Optional. The callback that will be called after each invocation.  If omitted, all 
        /// exceptions will be ignored but exceptions will not prevent the calling of later invocations from the 
        /// event's invocation list.</param>
        public static void InvokeAsynchronous(object sender, EventArgs e, Delegate evnt, AsyncCallback onEnd = null)
        {
            // Separately fire each invocation to assure that an exception in one invocation will not 
            // preclude the firing of the others.

            var invocations = evnt.GetInvocationList();
            foreach (var i in invocations)
            {
                var methodInvoke = (EventHandler)i;
                methodInvoke.BeginInvoke(sender, e, onEnd ?? _NoThrow, null);
            }

            void _NoThrow(IAsyncResult iar)
            {
                // TODO:  Is this really needed?  What happens on a null callback?

                var ar = (System.Runtime.Remoting.Messaging.AsyncResult)iar;
                var method = (EventHandler)ar.AsyncDelegate;

                try
                {
                    method.EndInvoke(iar);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Method " + evnt.Method.Name + " threw an exception \"" + ex.Message + "\" during delegate invocation.");
                }
            }
        }
    }
}
