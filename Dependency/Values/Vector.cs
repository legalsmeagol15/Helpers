using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;

namespace Dependency
{
    public sealed class Vector : IFunction, IEvaluateable, ILiteral<object[]>, ITypeGuarantee, IContext, IDynamicItem
    // Though a Vector has inputs, it CANNOT be a Function.
    {
        public IList<IEvaluateable> Inputs { get; internal set; }
        private IEvaluateable[] _Values;
        public IEvaluateable this[int idx]
        {
            get
            {
                if (idx >= 0 && idx <= Inputs.Count) return Inputs[idx].Value;
                return new IndexingError(this, this, new Number(idx), "Index out of range.");
            }
        }

        public Vector(params IEvaluateable[] contents)
        {
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

        public int Size => Inputs.Count;

        public IEvaluateable MaxIndex => new Number(Inputs.Count - 1);

        public IEvaluateable MinIndex => Number.Zero;

        public override bool Equals(object obj) => (obj is Vector other) && this == other;
        //public override int GetHashCode() { unchecked { return (int)Inputs.Sum(i => i.GetHashCode()); } }
        public override int GetHashCode() => base.GetHashCode();

        bool IContext.TryGetSubcontext(object path, out IContext ctxt) { ctxt = null; return false; }

        internal bool TryGetProperty(object path, out IEvaluateable source)
        {
            switch (path)
            {
                case Number n: source = this[(int)n]; return true;
                case int idx: source = this[idx]; return true;
                case "size":
                case "count":
                case "length": source = new Number(_Values.Length); return true;
                case "min": source = Number.Zero; return true;
                case "max": source = new Number(_Values.Length - 1); return true;
                default: source = null; return false;
            }
        }
        bool IContext.TryGetProperty(object path, out IEvaluateable source) => this.TryGetProperty(path, out source);

        object[] ILiteral<object[]>.CLRValue => Inputs.ToArray();
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Vector;

        IDynamicItem IDynamicItem.Parent { get; set; }

        public override string ToString() => "{" + string.Join(",", Inputs.Select(i => i.ToString())) + "}";

        bool IDynamicItem.Update(IDynamicItem updatedChild, IEvaluateable forcedValue) => true;
    }
}
