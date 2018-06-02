using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    public class Range : IEvaluateable, IIndexable<IEvaluateable>
    {
        private Number _From, _To;
        public Range(Number a, Number b) { this._From = a; this._To = b; }

        public IEvaluateable this[int index]
        {
            get
            {
                if (!(_From.IsInteger)) return new Error("Non-integer range start cannot be indexed.");
                decimal idx = _From.Value + index;
                if (idx > _To) return new Error("Index exceeds maximum range.");
                return new Number(idx);
            }
        }

        IEvaluateable IEvaluateable.Evaluate() => this;

        public override string ToString() => _From + ":" + _To;

        public override bool Equals(object obj)
        {
            Range other = obj as Range;
            if (other == null) return false;
            return _From.Value == other._From.Value && _To.Value == other._To.Value;
        }

        public override int GetHashCode() => (int)unchecked(_From.Value + _To.Value);
    }
}
