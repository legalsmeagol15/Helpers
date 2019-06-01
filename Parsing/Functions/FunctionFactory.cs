using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    public interface IFunctionFactory
    {
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
