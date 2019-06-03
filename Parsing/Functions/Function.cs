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

    }
    
    

    public abstract class Function : IFunction
    {
        private IEvaluateable _Value = null;
        IEvaluateable IEvaluateable.Value => _Value ?? (_Value = ((IFunction)this).UpdateValue());
        IEvaluateable IEvaluateable.UpdateValue() => (_Value = Evaluate(Inputs.Select(i => i.UpdateValue()).ToArray()));
        protected abstract IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs);

        protected internal IEvaluateable[] Inputs { get; internal set; } = null;
        IEvaluateable[] IFunction.Inputs => Inputs;
        

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
