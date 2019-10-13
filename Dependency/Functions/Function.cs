using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Dependency.TypeControl;

namespace Dependency.Functions
{
    public abstract class Function : IFunction, IDynamicItem
    {
        internal IDynamicItem Parent { get; set; }
        private IList<IEvaluateable> _Inputs;
        protected internal IList<IEvaluateable> Inputs
        {
            get => _Inputs;
            internal set
            {
                _Inputs = value;
                foreach (var iev in value)
                    if (iev is IDynamicItem ide)
                        ide.Parent = this;
            }
        }
        

        IList<IEvaluateable> IFunction.Inputs => Inputs;

        public IEvaluateable Value { get; private set; }
        IDynamicItem IDynamicItem.Parent { get => Parent; set => Parent = value; }

        public bool Update(IEvaluateable forcedValue = null)
        {
            if (forcedValue == null) return Update(EvaluateInputs());
            else if (forcedValue.Equals(Value)) return false;
            Value = forcedValue;
            return true;
        }
        public bool Update() => Update(EvaluateInputs());

        [DebuggerStepThrough]
        protected virtual IEvaluateable[] EvaluateInputs() => _Inputs.Select(s => s.Value).ToArray();

        protected bool Update(IEvaluateable[] evalInputs)
        {
            TypeControl tc;
            if (this is ICacheValidator icv) tc = icv.TypeControl ?? (icv.TypeControl = TypeControl.GetConstraints(this.GetType()));
            else tc = TypeControl.GetConstraints(this.GetType());

            IEvaluateable newValue;
            if (tc.TryMatchTypes(evalInputs, out int bestConstraint, out int unmatchedArg))
                newValue = Evaluate(evalInputs, bestConstraint);
            else if (bestConstraint < 0)
                newValue = new InputCountError(this, evalInputs, tc);
            else
                newValue = new TypeMismatchError(this, evalInputs, bestConstraint, unmatchedArg, tc);

            if (newValue.Equals(Value)) return false;
            Value = newValue;
            return true;
        }
        
        /// <summary>
        /// A function will call this method to evaluate the given evaluated inputs.
        /// </summary>
        /// <param name="evaluatedInputs"></param>
        /// <param name="constraintIndex"></param>
        /// <returns></returns>
        protected abstract IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex);

        public override string ToString() 
            => this.GetType().Name + "(" + string.Join(",", this._Inputs.Select(i => i.ToString())) + ")";

    }



    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public sealed class VariadicAttribute : Attribute
    {
        public readonly int Index;
        public TypeFlags[] TypeFlags;
        public VariadicAttribute(int index, params TypeFlags[] typeFlags) { this.Index = index; this.TypeFlags = typeFlags; }
        public VariadicAttribute(params TypeFlags[] typeFlags) : this(0, typeFlags) { }
    }

    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public sealed class NonVariadicAttribute : Attribute
    {
        public readonly int Index;
        public TypeFlags[] TypeFlags;
        public NonVariadicAttribute(int index, params TypeFlags[] typeFlags) { this.Index = index; this.TypeFlags = typeFlags; }
        public NonVariadicAttribute(params TypeFlags[] typeFlags) : this(0, typeFlags) { }
    }
}
