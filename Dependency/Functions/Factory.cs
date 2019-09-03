using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Functions
{

    internal interface IFunctionEmitter
    {
        NamedFunction Create();
    }

    public sealed class ReflectedFunctionFactory : IFunctionFactory
    {
        private readonly IDictionary<string, Func<NamedFunction>> _Catalogue;

        public ReflectedFunctionFactory()
        {
            _Catalogue = GetReflectedFunctionCreators();
        }

        private static IDictionary<string, Func<NamedFunction>> GetReflectedFunctionCreators()
        {
            Dictionary<string, Func<NamedFunction>> result = new Dictionary<string, Func<NamedFunction>>();
            foreach (Type t in GetDefaultTypes())
            {
                NamedFunction specimen = CreateFunction(t);
                string name = specimen.Name.ToUpper();                
                if (result.ContainsKey(name)) throw new Exception("Duplicate function name: " + name);
                if (specimen is IFunctionEmitter ife) 
                    result[name] = () => ife.Create();
                else
                    result[name] = () => CreateFunction(t);
                
            }
            return result;
        }

        private static NamedFunction CreateFunction(Type type)
            => (NamedFunction)Activator.CreateInstance(type);

        private static IEnumerable<Type> GetDefaultTypes(Assembly assembly = null)
            => (assembly ?? Assembly
                                .GetAssembly(typeof(IFunctionEmitter)))
                                .GetTypes()
                                .Where(t => t.IsClass 
                                         && !t.IsAbstract
                                         && typeof(NamedFunction).IsAssignableFrom(t) 
                                         && t.GetConstructor(Type.EmptyTypes) != null);

        public bool TryCreate(string token, out NamedFunction nf)
        {
            if (_Catalogue.TryGetValue(token.ToUpper(), out Func<NamedFunction> emitter)) { nf = emitter(); return true; }
            nf = null;
            return false;
        }
    }


}
