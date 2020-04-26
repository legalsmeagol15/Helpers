using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public enum LoggingSeverity
    {
        DEBUG = 10,
        INFO = 20,
        WARNING = 30,
        ERROR = 40,
        CRITICAL = 50
    }

    /// <summary>Designed to be static so it can be reached easily from any class that can 
    /// reference <seealso cref="Helpers"/>.  This static class is state-based.</summary>
    public static class Log
    {
        // TODO:  all for async logging.

        public const string DEFAULT_FACILITY = "fac1";
        public enum LoggingChannels { NONE = 0, FILE = 1 << 1, EVENT = 1 << 2 }

        /// <summary>The current state of the channels output.</summary>
        public static LoggingChannels Channels { get => _Channels; set => _Channels = value; }
        private static LoggingChannels _Channels = LoggingChannels.FILE | LoggingChannels.EVENT;
        public static bool OutputsToFile => (_Channels & LoggingChannels.FILE) != LoggingChannels.NONE;
        public static bool OutputsToEvent => (_Channels & LoggingChannels.EVENT) != LoggingChannels.NONE;

        public static string Facility { get; set; } = DEFAULT_FACILITY;
        public static bool ShowPrefix { get; set; } = true;
        public static bool ShowSender { get; set; } = true;

        /// <summary>Logs a message at severity <seealso cref="LoggingSeverity.DEBUG"/>.</summary>
        /// <param name="sender">The object sending the message.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="facility">Optional.  The facility to which the message should be sent.  
        /// If omitted, sent to <seealso cref="DEFAULT_FACILITY"/>.</param>
        /// <param name="prefix">Optional.  A caller-specifed prefix.  This will be the 
        /// <seealso cref="CallerMemberNameAttribute"/>.</param>
        public static void Debug(object sender, string message, string facility = null, [CallerMemberName] string prefix = null)
            => Do_Log(sender, facility, LoggingSeverity.DEBUG, message, prefix);

        /// <summary>Logs a message at severity <seealso cref="LoggingSeverity.INFO"/>.</summary>
        /// <param name="sender">The object sending the message.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="facility">Optional.  The facility to which the message should be sent.  
        /// If omitted, sent to <seealso cref="DEFAULT_FACILITY"/>.</param>
        /// <param name="prefix">Optional.  A caller-specifed prefix.  This will be the 
        /// <seealso cref="CallerMemberNameAttribute"/>.</param>
        public static void Info(object sender, string message, string facility = null, [CallerMemberName] string prefix = null)
            => Do_Log(sender, facility, LoggingSeverity.INFO, message, prefix);

        /// <summary>Logs a message at severity <seealso cref="LoggingSeverity.WARNING"/>.</summary>
        /// <param name="sender">The object sending the message.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="facility">Optional.  The facility to which the message should be sent.  
        /// If omitted, sent to <seealso cref="DEFAULT_FACILITY"/>.</param>
        /// <param name="prefix">Optional.  A caller-specifed prefix.  This will be the 
        /// <seealso cref="CallerMemberNameAttribute"/>.</param>
        public static void Warning(object sender, string message, string facility = null, [CallerMemberName] string prefix = null)
            => Do_Log(sender, facility, LoggingSeverity.WARNING, message, prefix);

        /// <summary>Logs a message at severity <seealso cref="LoggingSeverity.ERROR"/>.</summary>
        /// <param name="sender">The object sending the message.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="facility">Optional.  The facility to which the message should be sent.  
        /// If omitted, sent to <seealso cref="DEFAULT_FACILITY"/>.</param>
        /// <param name="prefix">Optional.  A caller-specifed prefix.  This will be the 
        /// <seealso cref="CallerMemberNameAttribute"/>.</param>
        public static void Error(object sender, string message, string facility = null, [CallerMemberName] string prefix = null)
            => Do_Log(sender, facility, LoggingSeverity.ERROR, message, prefix);

        /// <summary>Logs a message at severity <seealso cref="LoggingSeverity.CRITICAL"/>.</summary>
        /// <param name="sender">The object sending the message.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="facility">Optional.  The facility to which the message should be sent.  
        /// If omitted, sent to <seealso cref="DEFAULT_FACILITY"/>.</param>
        /// <param name="prefix">Optional.  A caller-specifed prefix.  This will be the 
        /// <seealso cref="CallerMemberNameAttribute"/>.</param>
        public static void Critical(object sender, string message, string facility = null, [CallerMemberName] string prefix = null)
            => Do_Log(sender, facility, LoggingSeverity.CRITICAL, message, prefix);

        private static void Do_Log(object sender, string facility, LoggingSeverity severity, string message, string prefix, string separator = " - ")
        {
            if (ShowPrefix && prefix != null)
                message = prefix + separator + message;
            if (ShowSender && sender != null)
                message = sender.GetType().Name + "." + message;
            if (facility == null)
                facility = Facility ?? DEFAULT_FACILITY;
            if (OutputsToFile)
                throw new NotImplementedException();
            if (OutputsToEvent)
                Logged?.Invoke(sender, new LogEventArgs(Facility, severity, message));            
        }

        /// <summary>Invoked upon receiving a logging message.  Fires after the file channel is 
        /// output, if required.</summary>
        public static event LogEventHandler Logged;        
    }


    public delegate void LogEventHandler(object sender, LogEventArgs e);

    public class LogEventArgs : EventArgs
    {
        public readonly string Facility;
        public readonly  LoggingSeverity Severity;
        public readonly string Message;

        public LogEventArgs(string facility, LoggingSeverity severity, string message)
        {
            this.Facility = facility;
            this.Severity = severity;
            this.Message = message;
        }
    }
}
