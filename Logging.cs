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

    public class Logging
    {
        private static Logging _Logger;
        public static Logging Log
        {
            get => _Logger ?? (_Logger = new Logging());
        }        

        private Outputs _Outputs;
        public enum Outputs { NONE = 0, EVENT = 1 << 1, FILE = 1 << 2 }
        public void SetOutputs(Outputs outputs) { this._Outputs = outputs; }
        public bool OutputsToEvent => (_Outputs & Outputs.EVENT) != Outputs.NONE;
        public bool OutputsToFile => (_Outputs & Outputs.FILE) != Outputs.NONE;

        public Logging(Outputs outputs = Outputs.EVENT | Outputs.FILE) { this._Outputs = outputs; }

        public void Debug(string message, string facility = "fac1", [CallerMemberName] string prefix = null)
            => Do_Log(facility, LoggingSeverity.DEBUG, message, prefix);
        public void Info(string message, string facility = "fac1", [CallerMemberName] string prefix = null)
            => Do_Log(facility, LoggingSeverity.INFO, message, prefix);
        public void Warning(string message, string facility = "fac1", [CallerMemberName] string prefix = null)
            => Do_Log(facility, LoggingSeverity.WARNING, message, prefix);
        public void Error(string message, string facility = "fac1", [CallerMemberName] string prefix = null)
            => Do_Log(facility, LoggingSeverity.ERROR, message, prefix);
        public void Critical(string message, string facility = "fac1", [CallerMemberName] string prefix = null)
            => Do_Log(facility, LoggingSeverity.CRITICAL, message, prefix);

        private void Do_Log(string facility, LoggingSeverity severity, string message, string prefix, string separator = " - ")
        {
            if (prefix != null)
                message = prefix + separator + message;
            if (OutputsToEvent)
                OnLog?.Invoke(this, new LogEventArgs(facility, severity, message));
            if (OutputsToFile)
                throw new NotImplementedException();
        }

        public event LogEventHandler OnLog;
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
