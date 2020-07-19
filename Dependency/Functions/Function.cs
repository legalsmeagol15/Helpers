using DataStructures;
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
    public abstract class Function : IFunction, ISyncUpdater
    {
        internal ISyncUpdater Parent { get; set; }
        private IList<IEvaluateable> _Inputs;
        protected internal IList<IEvaluateable> Inputs
        {
            get => _Inputs;
            internal set
            {
                _Inputs = value;
                foreach (var iev in value)
                    if (iev is ISyncUpdater ide)
                        ide.Parent = this;
            }
        }
        

        IList<IEvaluateable> IFunction.Inputs => Inputs;

        public IEvaluateable Value { get; private set; } = Dependency.Null.Instance;
        ISyncUpdater ISyncUpdater.Parent { get => Parent; set => Parent = value; }

        ITrueSet<IEvaluateable> ISyncUpdater.Update(Variables.Update update, 
                                                       ISyncUpdater updatedChild)
        {
            // TODO:  since I know which child was updated, it makes sense to cache the evaluations and updated only the changed one.
            if (updatedChild != null && updatedChild.Value is Error err)
            {
                if (Value.Equals(err)) return null;
                Value = err;
                return Dependency.Variables.Update.UniversalSet;
            }
            else return Update(EvaluateInputs());
        }
        public ITrueSet<IEvaluateable> Update(ICollection<IEvaluateable> updatedDomain) 
            => Update(EvaluateInputs(), updatedDomain);

        [DebuggerStepThrough]
        protected virtual IEvaluateable[] EvaluateInputs() => _Inputs.Select(s => s.Value).ToArray();

        /// <summary>
        /// Attempts to update by calling the inheriting 
        /// <seealso cref="Function.Evaluate(IEvaluateable[], int, ICollection{IEvaluateable}, out ITrueSet{IEvaluateable})"/>
        /// method.  
        /// The <seealso cref="Function.Value"/> will be set to this result
        /// <para/>1. If the given <paramref name="evalInputs"/> matches one of the 
        /// <seealso cref="Functions"/>'s associated <seealso cref="TypeControl.Constraint"/>s, 
        /// the evaluation will be returned accordingly by returning the result of the 
        /// <seealso cref="Function.Evaluate(IEvaluateable[], int, ICollection{IEvaluateable}, out ITrueSet{IEvaluateable})"/>
        /// method.
        /// <para/>2. If one of the given <paramref name="evalInputs"/> is an <seealso cref="Error"/>, that 
        /// <seealso cref="Error"/> will be returned.
        /// <para/>3. If no <seealso cref="TypeControl.Constraint"/> matches the count of 
        /// <paramref name="evalInputs"/>, an <seealso cref="InputCountError"/> will be returned.
        /// <para/>4. If one or more <paramref name="evalInputs"/> types do not match the closest 
        /// <seealso cref="TypeControl.Constraint"/>, a <seealso cref="TypeMismatchError"/> will be returned.
        /// </summary>
        /// <param name="evalInputs">The evaluate inputs for this <seealso cref="Function"/> to perform its operation 
        /// on.</param>
        /// <param name="updatedDomain">The indices updated below.</param>
        /// <returns>Returns the collection of updated indices, if a change was made; otherwise, returns null.
        /// </returns>
        protected ITrueSet<IEvaluateable> Update(IEvaluateable[] evalInputs, ICollection<IEvaluateable> updatedDomain)
        {
            TypeControl tc;
            if (this is ICacheValidator icv) tc = icv.TypeControl ?? (icv.TypeControl = TypeControl.GetConstraints(this.GetType()));
            else tc = TypeControl.GetConstraints(this.GetType());

            IEvaluateable newValue;
            ITrueSet<IEvaluateable> updatedIndices = null;
            if (tc.TryMatchTypes(evalInputs, out int bestConstraint, out int unmatchedArg, out Error firstError))
                newValue = Evaluate(evalInputs, bestConstraint, updatedDomain,  out updatedIndices);
            else if (firstError != null)
                newValue = firstError;
            else if (bestConstraint < 0)
                newValue = new InputCountError(this, evalInputs, tc);
            else
                newValue = new TypeMismatchError(this, evalInputs, bestConstraint, unmatchedArg, tc);

            if (newValue.Equals(Value)) return null;
            Value = newValue;
            return updatedIndices;
        }

        /// <summary>
        /// A function will call this method to evaluate the given evaluated inputs.
        /// </summary>
        /// <param name="evaluatedInputs"></param>
        /// <param name="constraintIndex"></param>
        /// <param name="updatedDomain">The indices that were updated below.</param>
        /// <param name="indices">Out.  The indices for which the value was changed.</param>
        /// <returns></returns>
        protected virtual IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, 
                                                 int constraintIndex, 
                                                 ICollection<IEvaluateable> updatedDomain, 
                                                 out ITrueSet<IEvaluateable> indices)
        {
            IEvaluateable result = Evaluate(evaluatedInputs, constraintIndex);
            indices = (result == null) ? default : Dependency.Variables.Update.UniversalSet;
            return result;
        }

        /// <summary>
        /// A function will call this method to evaluate the given evaluated inputs.
        /// </summary>
        /// <param name="evaluatedInputs"></param>
        /// <param name="constraintIndex"></param>
        /// <returns></returns>
        protected virtual IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
            => throw new InvalidOperationException("At least one method of " + nameof(Evaluate) + " must be overridden.");

        
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
