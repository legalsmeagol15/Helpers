using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    [Serializable]
    public class Range : IEvaluateable, IIndexable
    {
        private Number _From, _To;

        int IIndexable.MaxIndex => throw new NotImplementedException();

        int IIndexable.MinIndex => throw new NotImplementedException();

        IEvaluateable IIndexable.this[IEvaluateable index] => throw new NotImplementedException();

        public Range(Number a, Number b) { this._From = a; this._To = b; }

        public IEvaluateable this[IEvaluateable index]
        {
            get
            {
                if (index is Number n)
                {
                    // If the Range starts with an int, should it be indexed by an int?
                    if (!(_From.IsInteger)) return new EvaluationError("Non-integer range start cannot be indexed.", -1, -1, this);
                    decimal idx = _From.Value + n;
                    if (idx > _To) return new EvaluationError("Index exceeds maximum range.");
                    return new Number(idx);
                }
                else if (index is DataContext.Variable v)
                {
                    throw new NotImplementedException("TODO:  implement indexing into spreadsheet variables.");
                }
                else return new EvaluationError("Indexing into a Range requires an integer value.", -1, -1, this);
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
