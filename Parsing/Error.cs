using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing
{
    public class Error : IExpression
    {
        public readonly string Message;
        public readonly string Type;
        public readonly object Complainant;
        IExpression IExpression.Evaluate() => this;

        public Error(string message, object complainant = null) { this.Message = message; this.Complainant = complainant; }
    }
}
