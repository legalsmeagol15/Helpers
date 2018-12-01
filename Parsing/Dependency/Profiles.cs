using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Dependency
{

    internal class ClassProfile
    {
        public Type Type { get; private set; }
        public DependencyClassAttribute Attribute { get; private set; }
        public Dictionary<string, PropertyProfile> PropertyProfiles;
        public Dictionary<string, ContextProfile> ContextProfiles;
        public Dictionary<string, FunctionProfile> FunctionProfiles;

        public Function.Factory Functions { get; private set; }


        private ClassProfile() { }

        public static ClassProfile FromType(object obj) => ClassProfile.FromType(obj.GetType());
        public static ClassProfile FromType(Type type)
        {

            ClassProfile result = new ClassProfile
            {
                Type = type,
                Attribute = type.GetCustomAttribute<DependencyClassAttribute>(),
                PropertyProfiles = new Dictionary<string, PropertyProfile>(),
                ContextProfiles = new Dictionary<string, ContextProfile>(),
                FunctionProfiles = new Dictionary<string, FunctionProfile>()
            };

            // Get all decorated properties.
            foreach (PropertyInfo pInfo in type.GetProperties())
            {
                DependencyPropertyAttribute depPropAttr = pInfo.GetCustomAttribute<DependencyPropertyAttribute>();
                if (depPropAttr != null)
                    __AddPropertyProfile(depPropAttr, pInfo);
                DependencyContextAttribute depCtxtAttr = pInfo.GetCustomAttribute<DependencyContextAttribute>();
                if (depCtxtAttr != null)
                    __AddContextProfile(depCtxtAttr, pInfo);
            }

            // Get all decorated fields.
            foreach (FieldInfo fInfo in type.GetFields())
            {
                DependencyPropertyAttribute depPropAttr = fInfo.GetCustomAttribute<DependencyPropertyAttribute>();
                if (depPropAttr != null)
                    __AddPropertyProfile(depPropAttr, fInfo);
                DependencyContextAttribute depCtxtAttr = fInfo.GetCustomAttribute<DependencyContextAttribute>();
                if (depCtxtAttr != null)
                    __AddContextProfile(depCtxtAttr, fInfo);
            }

            foreach (MethodInfo mInfo in type.GetMethods())
            {
                DependencyFunctionAttribute depFuncAttr = mInfo.GetCustomAttribute<DependencyFunctionAttribute>();
                if (depFuncAttr != null)
                {
                    if (__IsDuplicate(mInfo.Name))
                        throw new DependencyAttributeException("Duplicate use of alias \"" + mInfo.Name + "\"");
                    FunctionProfile fp = new FunctionProfile(result, depFuncAttr, mInfo);
                    result.FunctionProfiles[mInfo.Name] = fp;
                }
            }

            return result;

            void __AddPropertyProfile(DependencyPropertyAttribute depPropAttr, MemberInfo info)
            {
                PropertyProfile pp;
                switch (info)
                {
                    case PropertyInfo pInfo: pp = new PropertyProfile(result, depPropAttr, pInfo); break;
                    case FieldInfo fInfo: pp = new PropertyProfile(result, depPropAttr, fInfo); break;
                    default: throw new DependencyAttributeException("Use of " + depPropAttr.GetType().Name + " decoration on invalid member.");
                }
                if (!depPropAttr.AliasesOnly) pp.Aliases.Add(info.Name);
                pp.Aliases.AddRange(depPropAttr.Aliases);
                foreach (string alias in pp.Aliases)
                {
                    if (__IsDuplicate(alias))
                        throw new DependencyAttributeException("Duplicate use of alias \"" + alias + "\"");
                    result.PropertyProfiles[alias] = pp;
                }
            }

            void __AddContextProfile(DependencyContextAttribute depCtxtAttr, MemberInfo info)
            {
                ContextProfile cp;
                switch (info)
                {
                    case PropertyInfo pInfo: cp = new ContextProfile(result, depCtxtAttr, pInfo); break;
                    case FieldInfo fInfo: cp = new ContextProfile(result, depCtxtAttr, fInfo); break;
                    default: throw new DependencyAttributeException("Use of " + depCtxtAttr.GetType().Name + " decoration on invalid member.");
                }
                if (!depCtxtAttr.AliasesOnly) cp.Aliases.Add(info.Name);
                cp.Aliases.AddRange(depCtxtAttr.Aliases);
                foreach (string alias in cp.Aliases)
                {
                    if (__IsDuplicate(alias))
                        throw new DependencyAttributeException("Duplicate use of alias \"" + alias + "\"");
                    result.ContextProfiles[alias] = cp;
                }
            }

            bool __IsDuplicate(string alias)
            {
                return result.PropertyProfiles.ContainsKey(alias)
                    || result.ContextProfiles.ContainsKey(alias)
                    || result.FunctionProfiles.ContainsKey(alias);
            }


        }
    }


    internal class PropertyProfile
    {
        private static readonly Dictionary<Tuple<Type, Type>, Type> _ActionTypes = new Dictionary<Tuple<Type, Type>, Type>();
        private static readonly Dictionary<Tuple<Type, Type>, Type> _FunctionTypes = new Dictionary<Tuple<Type, Type>, Type>();

        public readonly ClassProfile ClassProfile;
        public readonly Type ClrType;
        public readonly Parsing.Dependency.TypeGuarantee TypeGuarantees;
        internal readonly List<string> Aliases = new List<string>();
        
        private readonly Delegate _Updater = null;

        public readonly bool IsWeak;
        
        internal void UpdateCLR(object obj, IEvaluateable iev)
        {
            // 'Update' takes the value from the Parsing.Dependency.Variable, and sticks it in the CLR property/field.
            if (_Updater == null) return;            
            switch (iev)
            {                
                case Number n: _Updater.DynamicInvoke(obj, n.ToDouble()); break;
                case Parsing.String s: _Updater.DynamicInvoke(obj, s.Value); break;
                default: throw new NotImplementedException();
            }            
        }


        public PropertyProfile(ClassProfile classProfile, DependencyPropertyAttribute attribute, FieldInfo fInfo)
        {
            throw new NotImplementedException();
        }

        public PropertyProfile(ClassProfile classProfile, DependencyPropertyAttribute attribute, PropertyInfo pInfo)
        {
            this.ClassProfile = classProfile;
            this.ClrType = pInfo.PropertyType;
            this.TypeGuarantees = attribute.Types;
            this.IsWeak = attribute.IsWeak;

            // Using a cached delegate makes for faster execution than simply calling the reflection object, which 
            // apparently has to do a lot of security and context checking before it executes.  Since security and 
            // context are guaranteed here, just use the delegate.

            // Create a setter delegate.                
            MethodInfo propSetter = pInfo.GetSetMethod();
            if (propSetter != null)
            {
                Type actionT = typeof(Action<>).MakeGenericType(classProfile.Type, pInfo.PropertyType);
                _Updater = Delegate.CreateDelegate(actionT, null, propSetter);
            }
            
        }
        
        public Variable Create(Context parentCtxt)
        {
            Variable v = new Variable(parentCtxt);
            v.ValueChanged += Update;
            return v;
        }

        private void Update(object sender, DataStructures.ChangedEventArgs<Variable, IEvaluateable> e)
        {
            Variable v = e.Object;
            if (v.Parent == null) return;
            UpdateCLR(v.Parent.Object, e.After);            
        }
    }


    internal class FunctionProfile
    {
        private Function _ReuseDetector = null;
        private Func<object, Function> CreateFunction;
        public readonly ClassProfile ClassProfile;
        public readonly MethodInfo Info;
        public readonly DependencyFunctionAttribute Attribute;
        public FunctionProfile(ClassProfile classProfile, DependencyFunctionAttribute attribute, MethodInfo info)
        {
            throw new NotImplementedException();

        }
    }


    internal class ContextProfile
    {
        public readonly ClassProfile ClassProfile;
        public readonly Type Type;
        public readonly bool IsWeak;
        public readonly List<string> Aliases = new List<string>();

        private readonly Delegate _ObjectGetter = null;

        public Context Create(Context parentCtxt)
        {
            object obj = _ObjectGetter.DynamicInvoke(parentCtxt.Object);
            if (obj == null) throw new ContextInvalidException(parentCtxt, "A context cannot be created from a null object.");
            Context result = new Context(parentCtxt, ClassProfile, obj);
            return result;
        }
        
        public ContextProfile(ClassProfile classProfile, DependencyContextAttribute attribute, PropertyInfo info)
        {
            this.ClassProfile = classProfile;
            this.Type = info.PropertyType;
            this.IsWeak = attribute.IsWeak;

            // Create object getter delegate.                
            MethodInfo objGetter = info.GetGetMethod();
            if (objGetter != null)
            {
                Type funcT = typeof(Func<>).MakeGenericType(info.PropertyType, typeof(object));
                _ObjectGetter = Delegate.CreateDelegate(funcT, null, objGetter);                
            }
        }

        public ContextProfile(ClassProfile classProfile, DependencyContextAttribute attribute, FieldInfo info)
        {
            this.ClassProfile = classProfile;
            this.Type = info.FieldType;
            this.IsWeak = attribute.IsWeak;

            // Create object getter delegate.             
            Func<object, object> f = (obj) => info.GetValue(obj);
            _ObjectGetter = f;            
        }
    }
    
}
