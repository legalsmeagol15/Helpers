using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;

namespace Dependency
{
    public sealed class Vector : IFunction, IEvaluateable, ILiteral<object[]>, ITypeGuarantee, IContext, ISyncUpdater, IIndexable
    // Though a Vector has inputs, it CANNOT be a Function.
    {
        public IList<IEvaluateable> Inputs { get; internal set; }  // TODO:  indexes shouldn't be publicly settable.
        private Vector _Value = null;
        public Vector Value => _Value ?? (_Value = new Vector(Inputs.Select(i => i.Value)));
        public IEvaluateable this[int idx]
        {
            get
            {
                if (idx >= 0 && idx <= Inputs.Count) return Inputs[idx].Value;
                return new IndexingError(this, Inputs, "Index " + idx + " out of range.");
            }
        }

        private Vector(Vector contentVector)
        {

        }
        internal Vector(IEnumerable<IEvaluateable> contents) => Inputs = contents.ToArray();
        internal Vector(IList<IEvaluateable> contents) => Inputs = contents.ToArray();
        public Vector(params IEvaluateable[] contents) => Inputs = contents;
        public Vector(params decimal[] contents) 
            : this (contents.Select(m => new Number(m)).OfType<IEvaluateable>().ToArray())
        {
        }
        public Vector() { }

        IEvaluateable IEvaluateable.Value => Value;

        public int Size => Inputs.Count;

        public IEvaluateable MaxIndex => new Number(Inputs.Count - 1);

        public IEvaluateable MinIndex => Number.Zero;

        public override bool Equals(object obj)
        {
            if (!(obj is Vector other)) return false;
            if (Size != other.Size) return false;
            for (int i = 0; i < Size; i++) if (!Equals(Inputs[i].Value, other.Inputs[i].Value)) return false;
            return true;
        }
        //public override int GetHashCode() { unchecked { return (int)Inputs.Sum(i => i.GetHashCode()); } }
        public override int GetHashCode() => base.GetHashCode();

        bool IContext.TryGetSubcontext(string path, out IContext ctxt) { ctxt = null; return false; }

        internal bool TryGetProperty(string  path, out IEvaluateable source)
        {
            switch (path)
            {
                case "size":
                case "count":
                case "length": source = new Number(Value.Size); return true;
                case "min": source = Number.Zero; return true;
                case "max": source = new Number(Value.Size - 1); return true;
                default: source = null; return false;
            }
        }
        bool IContext.TryGetProperty(string  path, out IEvaluateable source) => this.TryGetProperty(path, out source);

        object[] ILiteral<object[]>.CLRValue => Inputs.ToArray();
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Vector;

        ISyncUpdater ISyncUpdater.Parent { get; set; }

        public override string ToString() => "{" + string.Join(",", Inputs.Select(i => i.ToString())) + "}";

        // If the value of an indexed member changed, then of course the value of the vector changed.
        bool ISyncUpdater.Update(Variables.Update u, ISyncUpdater uc, IEnumerable<IEvaluateable> ui) => true;

        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
        {
            if (!(ordinal is Number n) || !n.IsInteger) { val = null; return false; }
            val= this[(int)n];
            return !(val is Error);
        }
    }
}
