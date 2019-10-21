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

        public IEvaluateable Value { get; private set; } = Dependency.Null.Instance;
        IDynamicItem IDynamicItem.Parent { get => Parent; set => Parent = value; }

        bool IDynamicItem.Update(IDynamicItem updatedChild)
        {
            // TODO:  since I know which child was updated, it makes sense to cache the evaluations and updated only the changed one.
            if (updatedChild.Value is Error err) { if (Value.Equals(err)) return false; Value = err; return true; }
            else return Update(EvaluateInputs());
        }
        public bool Update() => Update(EvaluateInputs());

        [DebuggerStepThrough]
        protected virtual IEvaluateable[] EvaluateInputs() => _Inputs.Select(s => s.Value).ToArray();

        /// <summary>
        /// Attempts to update by calling the inheriting <seealso cref="Function.Update(IEvaluateable[])"/> method.  
        /// The <seealso cref="Function.Value"/> will be set to this result
        /// <para/>1. If the given <paramref name="evalInputs"/> match one of the <seealso cref="Functions"/>'s 
        /// associated <seealso cref="TypeControl.Constraint"/>s, the evaluation will be returned accordingly by 
        /// returning the result of the <seealso cref="Function.Update(IEvaluateable[])"/> method.  
        /// <para/>2. If one of the given <paramref name="evalInputs"/> is an <seealso cref="Error"/>, that 
        /// <seealso cref="Error"/> will be returned.
        /// <para/>3. If no <seealso cref="TypeControl.Constraint"/> matches the count of 
        /// <paramref name="evalInputs"/>, an <seealso cref="InputCountError"/> will be returned.
        /// <para/>4. If one or more <paramref name="evalInputs"/> types do not match the closest 
        /// <seealso cref="TypeControl.Constraint"/>, a <seealso cref="TypeMismatchError"/> will be returned.
        /// </summary>
        /// <param name="evalInputs">The evaluate inputs for this <seealso cref="Function"/> to perform its operation 
        /// on.</param>
        /// <returns>Returns true if <seealso cref="Value"/> is changed by this method; otherwise, returns false.
        /// </returns>
        protected bool Update(IEvaluateable[] evalInputs)
        {
            TypeControl tc;
            if (this is ICacheValidator icv) tc = icv.TypeControl ?? (icv.TypeControl = TypeControl.GetConstraints(this.GetType()));
            else tc = TypeControl.GetConstraints(this.GetType());

            IEvaluateable newValue;
            if (tc.TryMatchTypes(evalInputs, out int bestConstraint, out int unmatchedArg, out Error firstError))
                newValue = Evaluate(evalInputs, bestConstraint);
            else if (firstError != null)
                newValue = firstError;
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


    /// <summary>
    /// When applied to a <seealso cref="Function"/>-deriving class, indicates that the last input type is allowed to 
    /// repeat some arbitrary number of repetitions in the <seealso cref="IEvaluateable"/>[] parameter in the call to 
    /// the <seealso cref="Function.Evaluate(IEvaluateable[], int)"/> method.  For example, 
    /// <seealso cref="Operators.Addition"/> allows any count of <seealso cref="Number"/> inputs (because "3+7+4" is a 
    /// valid addition).
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public sealed class VariadicAttribute : Attribute
    {
        public readonly int Index;
        public TypeFlags[] TypeFlags;
        public VariadicAttribute(int index, params TypeFlags[] typeFlags) { this.Index = index; this.TypeFlags = typeFlags; }
        public VariadicAttribute(params TypeFlags[] typeFlags) : this(0, typeFlags) { }
    }

    /// <summary>
    /// When applied to a <seealso cref="Function"/>-deriving class, indicates that only a defined number of inputs 
    /// are allowed for the <seealso cref="IEvaluateable"/>[] parameter in the call to the 
    /// <seealso cref="Function.Evaluate(IEvaluateable[], int)"/> method.  For example, <seealso cref="Cos"/> allows 
    /// only a single <seealso cref="Number"/> as its input type.
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
    public sealed class NonVariadicAttribute : Attribute
    {
        public readonly int Index;
        public TypeFlags[] TypeFlags;
        public NonVariadicAttribute(int index, params TypeFlags[] typeFlags) { this.Index = index; this.TypeFlags = typeFlags; }
        public NonVariadicAttribute(params TypeFlags[] typeFlags) : this(0, typeFlags) { }
    }
}
