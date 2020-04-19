using System;
using System.Collections.Generic;
using System.Linq;
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

    public delegate void LogEventHander(object sender, LogEventArgs e);

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
