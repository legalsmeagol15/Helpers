using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    

    public interface IFunction : IEvaluateable
    {
        IEvaluateable[] Inputs { get; }

        IEvaluateable UpdateValue();

    }

    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public sealed class InputTypeConstraintAttribute : Attribute
    {
        internal TypesAllowed[] AllowedTypes;
        public readonly bool IsParams;
        public readonly int Index;

        internal InputTypeConstraintAttribute(int index, bool isParams, params TypesAllowed[] allowedTypes) {
            this.Index = index;
            this.IsParams = isParams;
            this.AllowedTypes = allowedTypes ?? new TypesAllowed[0];          
        }
        internal InputTypeConstraintAttribute(int index, params TypesAllowed[] allowedTypes) : this(index, false, allowedTypes) { }

        //public InputTypeAllowedAttribute(bool isParams, Type[] allowedTypes)
        //{
        //    this.IsParams = isParams;
        //    this.AllowedTypes = allowedTypes == null ? new TypesAllowed[0] : new TypesAllowed[allowedTypes.Length];
        //    for (int i = 0; i < allowedTypes.Length; i++)
        //    {
        //        Type t = allowedTypes[i];
        //        if (typeof(Number).IsAssignableFrom(t)) this.AllowedTypes[i] = TypesAllowed.Real;
        //        else if (typeof(String).IsAssignableFrom(t)) this.AllowedTypes[i] = TypesAllowed.NonEmptyString;
        //        //else if (typeof(Vector).IsAssignableFrom(t)) this.AllowedTypes[i] = EvaluatedTypeEnum.Vector;
        //        //else if (typeof(Complex).IsAssignableFrom(t)) this.AllowedTypes[i] = EvaluatedTypeEnum.Complex;
        //        else this.AllowedTypes[i] = TypesAllowed.Other;
        //    }
        //}
        //public InputTypeAllowedAttribute(Type[] allowedTypes) : this(false, allowedTypes) { }
    }

    public abstract class Function : IFunction
    {
        private IEvaluateable _Value = null;

        private static readonly IDictionary<Type, InputTypeConstraintAttribute[]> _TypeConstraints 
            = new Dictionary<Type, InputTypeConstraintAttribute[]>();
        
        IEvaluateable IEvaluateable.Value => _Value ?? (_Value = ((IFunction)this).UpdateValue());

        IEvaluateable IFunction.UpdateValue() => UpdateAndCheckValue();

        private IEvaluateable UpdateAndCheckValue()
        {
            // If an input evaluates to an error, we simply pass that further up the chain.  Therefore, all inputs 
            // must be evaluated before we can call Evaluate();
            IEvaluateable[] evaluatedInputs = new IEvaluateable[Inputs.Length];
            for (int i = 0; i < Inputs.Length; i++)
            {
                IEvaluateable ei = Inputs[0].Value;
                if (ei is EvaluationError) return ei;
                evaluatedInputs[i] = ei;
            }

            // Functions will allow only certain numbers and different types of inputs.  The first time a function 
            // type is evaluated, it caches the constraints attribute.
            if (!_TypeConstraints.TryGetValue(this.GetType(), out InputTypeConstraintAttribute[] constraints))
            {
                _TypeConstraints.Add(this.GetType(), 
                                     constraints = this.GetType().GetCustomAttributes<InputTypeConstraintAttribute>().ToArray());
            }
            // If there are no constraints still, we don't care how many or what inputs.  The function will handle it 
            // in its own Evaluate(IEvaluateable[]) method.

            // Step through each constraint to check if the evaluated inputs match.
            bool countMatch = false;
            bool typeMatch = false;

            int unmatchedIdx = -1;
            InputTypeConstraintAttribute constraint = null;
            for (int constraintIdx = 0; !typeMatch && constraintIdx < constraints.Length; constraintIdx++)
            {
                constraint = constraints[constraintIdx];

                // Does the number of evaluated inputs match the constraint's requirements?
                if (constraint.IsParams)
                {
                    if (evaluatedInputs.Length < constraint.AllowedTypes.Length) continue;
                }
                else
                    if (evaluatedInputs.Length != constraint.AllowedTypes.Length) continue;
                countMatch = true;
                typeMatch = true;

                // Do the evaluated inputs match the constraint's allowed types?
                for (int i = 0; typeMatch && i < evaluatedInputs.Length; i++)
                {
                    TypesAllowed r = constraint.AllowedTypes[Math.Max(i, constraint.AllowedTypes.Length - 1)];
                    switch (evaluatedInputs[i])
                    {
                        case Number n: if (_IsNumberAllowed(n, r)) continue; typeMatch = false; break;
                        case String s: if (_IsStringAllowed(s, r)) continue; typeMatch = false; break;
                        default: typeMatch = false; break;
                    }
                    if (!typeMatch)
                        unmatchedIdx = Math.Max(i, unmatchedIdx);
                    break;
                }
                if (typeMatch) break;
            }

            if (!countMatch)
                return new InputCountError(this, evaluatedInputs, constraints.Select(c => c.AllowedTypes));
            else if (!typeMatch)
                return new TypeMismatchError(this, evaluatedInputs, constraint == null ? -1 : constraint.Index, unmatchedIdx, constraints.Select(c => c.AllowedTypes));
            else 
                return Evaluate(constraint.Index, evaluatedInputs);

            bool _IsStringAllowed(String str, TypesAllowed c)
            {
                if (string.IsNullOrWhiteSpace(str.Value)) return (TypesAllowed.ZeroNullEmpty & c) != 0;
                else return (TypesAllowed.String & c) != 0;
            }

            bool _IsNumberAllowed(Number num, TypesAllowed c)
            {
                if (num.IsInteger) { if ((TypesAllowed.IntegerNumber & c) == 0) return false; }
                else { if ((TypesAllowed.NonIntegerNumber & c) == 0) return false; }

                if (num.Value < 0) return (TypesAllowed.NegativeNumber & c) != 0;
                else if (num.Value > 0) return (TypesAllowed.PositiveNumber & c) != 0;
                else return (TypesAllowed.ZeroNullEmpty & c) != 0;
            }
        }

        protected abstract IEvaluateable Evaluate(int constraintIndex, IEvaluateable[] inputs);

        public IEvaluateable[] Inputs { get; protected internal set; } = null;
    }

    public abstract class NamedFunction : Function, IExpression
    {
        public string Name { get; }
    }

    


    /// <summary>Creates <see cref="NamedFunction"/> objects based on the given string name.</summary>
    public interface IFunctionFactory
    {
        /// <summary>
        /// Tries to create a <see cref="NamedFunction"/> based on the given token, from the catalogue available to 
        /// the <see cref="IFunctionFactory"/>.
        /// </summary>
        /// <returns>Returns true in the case of success, false if not.  If true, the given parameter 
        /// <paramref name="nf"/> will contain the <see cref="NamedFunction"/>; otherwise, the parameter will be 
        /// null.</returns>
        bool TryCreate(string token, out NamedFunction nf);
    }

    public sealed class ReflectedFunctionFactory
    {
        private readonly IDictionary<string, Func<NamedFunction>> _Catalogue;

        public ReflectedFunctionFactory()
        {
            _Catalogue = GetReflectedFunctionCreators();
        }

        public static IDictionary<string, Func<NamedFunction>> GetReflectedFunctionCreators()
        {
            Dictionary<string, Func<NamedFunction>> result = new Dictionary<string, Func<NamedFunction>>();
            foreach (Type t in GetDefaultTypes())
            {
                NamedFunction specimen = CreateFunction(t);
                string name = specimen.Name.ToUpper();
                if (result.ContainsKey(name)) throw new Exception("Duplicate function name: " + name);
                result.Add(name, () => CreateFunction(t));
            }
            return result;
        }

        private static NamedFunction CreateFunction(Type type)
            => (NamedFunction)Activator.CreateInstance(type);

        private static IEnumerable<Type> GetDefaultTypes(Assembly assembly = null)
            => (assembly ?? Assembly.GetAssembly(typeof(NamedFunction))).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(NamedFunction)));
    }


}
