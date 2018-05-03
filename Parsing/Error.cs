using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing
{
    public class Error : IEvaluatable
    {
        public readonly string Message;
        public readonly string Type;
        public readonly object Complainant;
        public readonly int Start;
        public readonly int End;

        public Error(string message, int startIdx = -1, int endIdx = -1,  object complainant = null) { this.Message = message; this.Start = startIdx; this.End = endIdx; this.Complainant = complainant; }

        IEvaluatable IEvaluatable.Evaluate() => this;
    }
}
