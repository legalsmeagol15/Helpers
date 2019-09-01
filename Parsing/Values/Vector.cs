using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;

namespace Dependency
{
    public sealed class Vector : IFunction, IEvaluateable, IIndexable, ILiteral<object[]>, ITypeGuarantee, IContext, IDynamicItem
        // Though a Vector has inputs, it CANNOT be a Function.
    {
        public IList<IEvaluateable> Inputs { get; internal set; }
        private IEvaluateable[] _Values;
        
        public Vector(params IEvaluateable[] contents) {
            Inputs = contents;
            _Values = new IEvaluateable[contents.Length];
            for (int i = 0; i < contents.Length; i++)
            {
                var c = contents[i];
                if (c is IDynamicItem idi) idi.Parent = this;
                _Values[i] = c.Value;
            }
        }
        public Vector() { }

        IEvaluateable IEvaluateable.Value => this;

        IEvaluateable IIndexable.this[IEvaluateable ordinal] => (ordinal is Number n) ? _Values[(int)n] : new IndexingError(this, this, ordinal);
        public IEvaluateable this [int index] => _Values[index];

        public int Size => Inputs.Count;

        public IEvaluateable MaxIndex => new Number(Inputs.Count - 1);

        public IEvaluateable MinIndex => Number.Zero;
        
        public override bool Equals(object obj) => (obj is Vector other) && this == other;
        public override int GetHashCode() { unchecked { return Inputs.Sum(i => i.GetHashCode()); } }

        bool IContext.TryGetSubcontext(object path, out IContext ctxt) { ctxt = null; return false; }

        internal bool TryGetProperty(object path, out IEvaluateable source)
        {
            source = null;
            int idx = -1;
            if (path is string str)
            {
                switch (str.ToLower())
                {
                    case "size":
                    case "length": source = new Number(_Values.Length);break;
                    case "min": source = Number.Zero; break;
                    case "max": source = new Number(_Values.Length - 1);break;
                }
            }
            else if (path is int i)
                idx = i;
            if (idx >= 0)
            {
                source = idx < Inputs.Count ? Inputs[idx].Value : Dependency.Null.Instance;
            }
            return source != null;
        }
        bool IContext.TryGetProperty(object path, out IEvaluateable source) => this.TryGetProperty(path, out source);

        object[] ILiteral<object[]>.CLRValue => Inputs.ToArray();
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.VectorReal;

        IDynamicItem IDynamicItem.Parent { get; set; }

        public override string ToString() => "{" + string.Join(",", Inputs.Select(i => i.ToString())) + "}";

        bool IDynamicItem.Update()
        {
            throw new NotImplementedException();
        }
    }
}
